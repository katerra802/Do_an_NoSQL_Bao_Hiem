document.addEventListener("DOMContentLoaded", function () {

    // ================================
    // QUẢN LÝ CHECKBOX QUA NHIỀU TRANG
    // ================================
    let selectedAll = false;
    let selectedIds = new Set(JSON.parse(localStorage.getItem('selectedPolicyIds') || '[]'));
    let excludedIds = new Set(JSON.parse(localStorage.getItem('excludedPolicyIds') || '[]'));

    const checkAll = document.getElementById("checkAll");
    const rowCheckboxes = document.querySelectorAll(".row-check");

    // Khôi phục checkbox khi load trang
    function restoreCheckboxState() {
        selectedAll = localStorage.getItem('selectAllPolicy') === 'true';

        if (selectedAll) {
            checkAll.checked = true;
            rowCheckboxes.forEach(cb => {
                cb.checked = !excludedIds.has(cb.value);
            });
        } else {
            rowCheckboxes.forEach(cb => {
                if (selectedIds.has(cb.value)) cb.checked = true;
            });

            const allChecked = Array.from(rowCheckboxes).every(cb => cb.checked);
            if (checkAll) checkAll.checked = allChecked;
        }
    }

    function saveCheckboxState() {
        if (selectedAll) {
            localStorage.setItem('selectAllPolicy', 'true');
            localStorage.setItem('excludedPolicyIds', JSON.stringify([...excludedIds]));
        } else {
            localStorage.removeItem('selectAllPolicy');
            localStorage.removeItem('excludedPolicyIds');
            localStorage.setItem('selectedPolicyIds', JSON.stringify([...selectedIds]));
        }
    }

    // "Chọn tất cả"
    if (checkAll) {
        checkAll.addEventListener("change", function () {
            if (this.checked) {
                selectedAll = true;
                selectedIds.clear();
                excludedIds.clear();
                localStorage.setItem('selectAllPolicy', 'true');
                rowCheckboxes.forEach(cb => cb.checked = true);
            } else {
                selectedAll = false;
                selectedIds.clear();
                excludedIds.clear();
                localStorage.removeItem('selectAllPolicy');
                localStorage.removeItem('selectedPolicyIds');
                localStorage.removeItem('excludedPolicyIds');
                rowCheckboxes.forEach(cb => cb.checked = false);
            }
        });
    }

    // Checkbox từng dòng
    rowCheckboxes.forEach(cb => {
        cb.addEventListener("change", function () {
            if (selectedAll) {
                if (!this.checked) excludedIds.add(this.value);
                else excludedIds.delete(this.value);

                const allChecked = Array.from(rowCheckboxes).every(c => c.checked);
                if (checkAll) checkAll.checked = allChecked && excludedIds.size === 0;
            } else {
                if (this.checked) selectedIds.add(this.value);
                else selectedIds.delete(this.value);

                const allChecked = Array.from(rowCheckboxes).every(c => c.checked);
                if (checkAll) checkAll.checked = allChecked;
            }

            saveCheckboxState();
        });
    });

    restoreCheckboxState();

    // ================================
    // LẤY TỔNG SỐ HỢP ĐỒNG (CHO CHỌN TẤT CẢ)
    // ================================
    window.getTotalCount = async function () {
        const filters = {
            status: document.querySelector("select[name='status']")?.value || '',
            price_from: document.querySelector("input[name='price_from']")?.value || '',
            price_to: document.querySelector("input[name='price_to']")?.value || '',
            search: document.querySelector("input[name='search']")?.value || '',
            from_date: document.querySelector("input[name='from_date']")?.value || '',
            to_date: document.querySelector("input[name='to_date']")?.value || ''
        };

        const params = new URLSearchParams(filters);

        try {
            const res = await fetch(`/Policies/GetTotalCount?${params}`);
            const data = await res.json();
            return data.count;
        } catch (err) {
            console.error("Lỗi lấy tổng số hợp đồng:", err);
            return 0;
        }
    };

    // ================================
    // LẤY DANH SÁCH ID ĐÃ CHỌN
    // ================================
    window.getSelectedIds = function () {
        const currentPageChecked = Array.from(document.querySelectorAll(".row-check:checked"))
            .map(chk => chk.value);

        const selectAllMode = localStorage.getItem('selectAllPolicy') === 'true';

        if (selectAllMode) return null;
        return currentPageChecked.length > 0 ? currentPageChecked : Array.from(selectedIds);
    };

    // ================================
    // CLEAR SELECTION
    // ================================
    window.clearSelection = function () {
        selectedAll = false;
        selectedIds.clear();
        excludedIds.clear();
        localStorage.removeItem('selectAllPolicy');
        localStorage.removeItem('selectedPolicyIds');
        localStorage.removeItem('excludedPolicyIds');
    };

    // ================================
    // AUTO SUBMIT FILTER
    // ================================
    const filterForm = document.getElementById("filterForm");
    if (filterForm) {
        filterForm.querySelectorAll("select, input[type=date]").forEach(el => {
            el.addEventListener("change", () => {
                window.clearSelection();
                filterForm.submit();
            });
        });
    }

    // ================================
    // EXPORT EXCEL
    // ================================
    const exportBtn = document.getElementById("exportExcelBtn");
    if (exportBtn) {
        exportBtn.addEventListener("click", function () {

            const selectAllMode = localStorage.getItem('selectAllPolicy') === 'true';
            const excluded = Array.from(excludedIds);

            let query = "";

            if (selectAllMode) {
                const filters = {
                    status: document.querySelector("select[name='status']")?.value || '',
                    price_from: document.querySelector("input[name='price_from']")?.value || '',
                    price_to: document.querySelector("input[name='price_to']")?.value || '',
                    search: document.querySelector("input[name='search']")?.value || '',
                    from_date: document.querySelector("input[name='from_date']")?.value || '',
                    to_date: document.querySelector("input[name='to_date']")?.value || ''
                };

                const params = new URLSearchParams(filters).toString();

                query = params;
                if (excluded.length > 0) {
                    query += `&excludeIds=${excluded.join(",")}`;
                }

                query += "&exportAll=true";
            } else {
                const selected = window.getSelectedIds();
                if (!selected || selected.length === 0) {
                    window.showToast("warning", "Vui lòng chọn ít nhất một hợp đồng để xuất!");
                    return;
                }
                query = `ids=${selected.join(",")}`;
            }

            window.location.href = '/Policies/ExportExcel?' + query;
        });
    }

}); // END DOMContentLoaded


// ================================
// XÓA HÀNG LOẠT
// ================================
window.bulkDelete = async function () {

    const selectAllMode = localStorage.getItem('selectAllPolicy') === 'true';
    const selected = window.getSelectedIds();
    const excluded = Array.from(new Set(JSON.parse(localStorage.getItem('excludedPolicyIds') || '[]')));

    if (!selectAllMode && (!selected || selected.length === 0)) {
        window.showToast("warning", "Vui lòng chọn ít nhất 1 hợp đồng để xóa!");
        return;
    }

    let count = 0;

    if (selectAllMode) {
        count = await window.getTotalCount();
        count -= excluded.length;
    } else {
        count = selected.length;
    }

    window.confirmAction({
        title: "Xác nhận xóa",
        message: `Bạn có chắc chắn muốn xóa <b>${count}</b> hợp đồng đã chọn?`,
        dangerText: "Hành động này không thể hoàn tác.",
        confirmButtonText: "Xóa",
        cancelButtonText: "Hủy",
        icon: "warning",
        onConfirm: async () => {

            Swal.fire({
                title: "Đang xóa...",
                didOpen: () => Swal.showLoading(),
                allowOutsideClick: false
            });

            try {
                let url = "/Policies/BulkDelete";
                let body;

                if (selectAllMode) {
                    url += "?deleteAll=true";

                    body = JSON.stringify({
                        excludeIds: excluded,
                        status: document.querySelector("select[name='status']")?.value || '',
                        price_from: document.querySelector("input[name='price_from']")?.value || '',
                        price_to: document.querySelector("input[name='price_to']")?.value || '',
                        search: document.querySelector("input[name='search']")?.value || '',
                        from_date: document.querySelector("input[name='from_date']")?.value || '',
                        to_date: document.querySelector("input[name='to_date']")?.value || ''
                    });

                } else {
                    body = JSON.stringify(selected);
                }

                const res = await fetch(url, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: body
                });

                if (res.ok) {
                    window.clearSelection();
                    location.reload();
                } else {
                    window.showToast("error", "Không thể xóa hợp đồng. Vui lòng thử lại.");
                }

            } catch (err) {
                window.showToast("error", "Lỗi kết nối đến server.");
            }
        }
    });

};


// ================================
// CẬP NHẬT TRẠNG THÁI HÀNG LOẠT
// ================================
window.bulkUpdateStatus = async function () {
    const selectAllMode = localStorage.getItem('selectAllPolicy') === 'true';
    const selected = window.getSelectedIds();
    const excluded = Array.from(new Set(JSON.parse(localStorage.getItem('excludedPolicyIds') || '[]')));

    if (!selectAllMode && (!selected || selected.length === 0)) {
        window.showToast("warning", "Vui lòng chọn ít nhất 1 hợp đồng để cập nhật!");
        return;
    }

    const statuses = {
        inforce: "Đang hiệu lực",
        grace: "Gia hạn phí",
        expired: "Hết hiệu lực",
        terminated: "Đã chấm dứt"
    };

    let count = 0;

    if (selectAllMode) {
        count = await window.getTotalCount();
        count -= excluded.length;
    } else {
        count = selected.length;
    }

    const options = Object.entries(statuses)
        .map(([code, name]) => `<option value="${code}">${name}</option>`)
        .join("");

    Swal.fire({
        title: "Cập nhật trạng thái hàng loạt",
        html: `
            <div class="text-start">
                <label class="fw-semibold">Trạng thái mới</label>
                <select id="bulkStatusSelect" class="select-input w-100 mt-2">
                    ${options}
                </select>

                <label class="fw-semibold mt-3">Ghi chú</label>
                <textarea id="bulkNotes" class="input-input w-100 mt-1" rows="3" placeholder="Nhập ghi chú (nếu có)..."></textarea>

                <div id="bulkLockFields" class="mt-3" style="display:none;">
                    <label class="fw-semibold">Lý do chấm dứt</label>
                    <input id="bulkLockReason" type="text" class="input-input w-100" placeholder="Ví dụ: khách hàng hủy hợp đồng">
                </div>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: "Cập nhật",
        cancelButtonText: "Hủy bỏ",
        reverseButtons: true,
        customClass: {
            popup: "custom-swal-popup",
            confirmButton: "custom-swal-confirm-btn",
            cancelButton: "custom-swal-cancel-btn",
        },
        preConfirm: () => {
            const newStatus = document.getElementById("bulkStatusSelect").value;
            const notes = document.getElementById("bulkNotes").value;
            const lockReason = document.getElementById("bulkLockReason").value;
            const isLocked = newStatus === "expired" || newStatus === "terminated";
            return { newStatus, notes, isLocked, lockReason };
        }
    }).then(async (result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: "Đang cập nhật...",
                allowOutsideClick: false,
                didOpen: () => Swal.showLoading(),
            });

            try {
                let url = "/Policies/BulkUpdateStatus";
                let body;

                if (selectAllMode) {
                    url += "?updateAll=true";
                    body = JSON.stringify({
                        newStatus: result.value.newStatus,
                        notes: result.value.notes,
                        isLocked: result.value.isLocked,
                        lockReason: result.value.lockReason,
                        excludeIds: excluded,
                        status: document.querySelector("select[name='status']")?.value || '',
                        price_from: document.querySelector("input[name='price_from']")?.value || '',
                        price_to: document.querySelector("input[name='price_to']")?.value || '',
                        search: document.querySelector("input[name='search']")?.value || '',
                        from_date: document.querySelector("input[name='from_date']")?.value || '',
                        to_date: document.querySelector("input[name='to_date']")?.value || ''
                    });
                } else {
                    body = JSON.stringify({
                        ids: selected,
                        newStatus: result.value.newStatus,
                        notes: result.value.notes,
                        isLocked: result.value.isLocked,
                        lockReason: result.value.lockReason
                    });
                }

                const res = await fetch(url, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: body
                });

                if (res.ok) {
                    // ✅ Lưu toast để hiển thị sau reload
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: `Đã cập nhật trạng thái cho ${count} hợp đồng thành công!`
                    }));

                    window.clearSelection();
                    location.reload();
                } else {
                    window.showToast("error", "Không thể cập nhật trạng thái.");
                }

            } catch (err) {
                window.showToast("error", "Lỗi server.");
            }
        }
    });

    // Khi chọn trạng thái “expired” hoặc “terminated” → hiện ô nhập lý do
    $(document).on("change", "#bulkStatusSelect", function () {
        const val = $(this).val();
        if (val === "expired" || val === "terminated") {
            $("#bulkLockFields").slideDown();
        } else {
            $("#bulkLockFields").slideUp();
        }
    });
};
// ================================
// CẬP NHẬT TRẠNG THÁI SINGLE
// ================================
window.updateStatus = function (policyId) {

    const statuses = {
        inforce: "Đang hiệu lực",
        grace: "Gia hạn phí",
        expired: "Mất hiệu lực"
    };

    const options = Object.entries(statuses)
        .map(([code, name]) => `<option value="${code}">${name}</option>`)
        .join("");

    window.confirmAction({
        title: "Cập nhật trạng thái",
        html: `
            <label>Chọn trạng thái mới:</label>
            <select id="statusSelect" class="select-input mt-2">
                ${options}
            </select>
        `,
        confirmButtonText: "Cập nhật",
        cancelButtonText: "Hủy",
        icon: "warning",
        preConfirm: () => {
            const s = document.getElementById("statusSelect");
            return s ? s.value : null;
        },
        onConfirm: async (newStatus) => {

            Swal.fire({
                title: "Đang cập nhật...",
                didOpen: () => Swal.showLoading(),
                allowOutsideClick: false
            });

            try {
                const res = await fetch(`/Policies/UpdateStatus`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ id: policyId, newStatus: newStatus })
                });

                if (res.ok) {
                    location.reload();
                } else {
                    window.showToast("error", "Lỗi cập nhật trạng thái.");
                }
            } catch (err) {
                window.showToast("error", "Lỗi server.");
            }
        }
    });
};


// ================================
// MODAL XEM CHI TIẾT HỢP ĐỒNG
// ================================
function viewPolicyDetails(policyId) {
    const modal = $('#policyDetailsModal');
    const content = $('#policyDetailsContent');
    const policyNoSpan = $('#modalPolicyNo');

    // Loading UI
    content.html(`
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status"></div>
            <p class="mt-3 text-muted">Đang tải thông tin hợp đồng...</p>
        </div>
    `);
    modal.modal('show');

    $.ajax({
        url: '/Policies/GetPolicyDetails',
        method: 'GET',
        data: { id: policyId },
        success: function (data) {
            if (!data) {
                content.html(`<p class="text-center text-danger fw-bold">Không tìm thấy hợp đồng!</p>`);
                return;
            }

            policyNoSpan.text(data.policy_no || '—');

            const formatCurrency = v => v > 0 ? v.toLocaleString('vi-VN') + ' ₫' : '—';
            const formatDate = d => d ? new Date(d).toLocaleDateString('vi-VN') : '—';

            // Dịch trạng thái sang tiếng Việt
            const getStatusVN = (status) => {
                switch (status) {
                    case "inforce": return "Có hiệu lực";
                    case "grace": return "Gia hạn phí";
                    case "expired": return "Hết hiệu lực";
                    case "terminated": return "Đã chấm dứt";
                    default: return "Không xác định";
                }
            };

            const html = `
                <div class="fade-in-up">
                            <h5 class="fw-bold text-danger">Thông tin chung</h5>
                    <div class="row g-3">

                        <!-- Khách hàng -->
                        <div class="col-md-6">
                            <label class="fw-semibold">Khách hàng</label>
                            <input type="text" class="input-input bg-white w-100" value="${data.customer?.full_name || '—'}" readonly>
                        </div>

                        <!-- Tư vấn viên -->
                        <div class="col-md-6">
                            <label class="fw-semibold">Tư vấn viên</label>
                            <input type="text" class="input-input bg-white w-100" value="${data.advisor?.full_name || '—'}" readonly>
                        </div>

                        <!-- Sản phẩm -->
                        <div class="col-md-6">
                            <label class="fw-semibold">Sản phẩm bảo hiểm</label>
                            <input type="text" class="input-input bg-white w-100" value="${data.product?.name || '—'}" readonly>
                        </div>

                        <!-- Số tiền -->
                        <div class="col-md-6">
                            <label class="fw-semibold">Số tiền bảo hiểm</label>
                            <input type="text" class="input-input bg-white w-100" value="${formatCurrency(data.sum_assured)}" readonly>
                        </div>

                        <!-- Phí hàng năm -->
                        <div class="col-md-6">
                            <label class="fw-semibold">Phí bảo hiểm hàng năm</label>
                            <input type="text" class="input-input bg-white w-100" value="${formatCurrency(data.annual_premium)}" readonly>
                        </div>

                        <div class="col-md-6">
                            <label class="fw-semibold">Trạng thái</label>
                            <input type="text" class="input-input bg-white w-100" value="${getStatusVN(data.status)}" readonly>
                        </div>

                        <!-- Ngày cấp, hiệu lực, đáo hạn -->
                        <div class="col-md-4">
                            <label class="fw-semibold">Ngày cấp</label>
                            <input type="text" class="input-input bg-white w-100" value="${formatDate(data.issue_date)}" readonly>
                        </div>
                        <div class="col-md-4">
                            <label class="fw-semibold">Hiệu lực từ</label>
                            <input type="text" class="input-input bg-white w-100" value="${formatDate(data.effective_date)}" readonly>
                        </div>
                        <div class="col-md-4">
                            <label class="fw-semibold">Ngày đáo hạn</label>
                            <input type="text" class="input-input bg-white w-100" value="${formatDate(data.maturity_date)}" readonly>
                        </div>

                        <!-- Ghi chú -->
                        ${data.notes ? ` 
                        <div class="col-12">
                            <label class="fw-semibold">Ghi chú</label>
                            <textarea class="input-input bg-white w-100" rows="3" readonly>${data.notes}</textarea>
                        </div>` : ''}

                        <!-- Người thụ hưởng -->
                        <div class="col-12">
                            <h5 class="fw-bold text-danger mb-3">Người thụ hưởng</h5>
            <div class="table-responsive">
                <table class="table table-bordered">
                                <thead>
                                    <tr>
                                        <th>Tên</th>
                                        <th>Quan hệ</th>
                                        <th>Phần trăm chia sẻ</th>
                                        <th>Ngày sinh</th>
                                        <th>Số CMND</th>
                                    </tr>
                                </thead>
                                <tbody>
                                   ${data.beneficiaries && data.beneficiaries.length > 0 ? data.beneficiaries.map(beneficiary => `
                            <tr>
                                <td>${beneficiary.full_name || '—'}</td>
                                <td>${beneficiary.relation || '—'}</td>
                                <td>${beneficiary.share_percent || '—'}</td>
                                <td>${formatDate(beneficiary.dob)}</td>
                                <td>${beneficiary.national_id || '—'}</td>
                            </tr>
                        `).join('') : `<tr><td colspan="5" class="text-center">Không có thông tin người thụ hưởng.</td></tr>`}
                    </tbody>
                            </table>
                        </div>
                        </div>

                    </div>
                </div>
            `;

            content.html(html);
        },
        error: function () {
            content.html(`<p class="text-center text-danger fw-bold">Lỗi kết nối, vui lòng thử lại.</p>`);
        }
    });
}


const inputSearch = document.querySelector("input[name='search']");
if (inputSearch) {
    inputSearch.addEventListener("keyup", function (e) {
        if (e.key === "Enter") {
            window.clearSelection();
            document.getElementById("filterForm").submit();
        }
    });
}

$(document).ready(function () {

    // SELECT2 for product dropdown
    $('#productSelect').select2({
        placeholder: "Chọn sản phẩm",
        allowClear: true,
        width: '100%'
    });

    // Auto submit when select product
    $('#productSelect').on('change', function () {
        window.clearSelection();
        $('#filterForm').submit();
    });

    // SELECT2 for advisor dropdown
    $('#advisorSelect').select2({
        placeholder: "Chọn tư vấn viên",
        allowClear: true,
        width: '100%'
    });

    // Auto submit when select advisor
    $('#advisorSelect').on('change', function () {
        window.clearSelection();
        $('#filterForm').submit();
    });

    // Enter to search
    const inputSearch = document.querySelector("input[name='search']");
    if (inputSearch) {
        inputSearch.addEventListener("keyup", function (e) {
            if (e.key === "Enter") {
                window.clearSelection();
                document.getElementById("filterForm").submit();
            }
        });
    }
});




// ===============================
// MỞ MODAL SỬA HỢP ĐỒNG
// ===============================
function editPolicy(id) {
    const modal = $("#editPolicyModal");
    const content = $("#editPolicyContent");
    modal.modal("show");

    content.html(`<div class="text-center py-5"><div class="spinner-border text-danger"></div><p class="mt-2">Đang tải...</p></div>`);

    $.get(`/Policies/GetPolicyDetails?id=${id}`, function (response) {
        // ✅ CHECK response format
        console.log("Policy data:", response);

        if (!response || !response.success) {
            content.html(`<p class="text-danger text-center fw-bold">Không tìm thấy dữ liệu!</p>`);
            return;
        }

        // ✅ LẤY ID TỪ PARAMETER, KHÔNG PHẢI TỪ RESPONSE
        // Vì response trả về snake_case nhưng MongoDB Id không có trong response
        content.html(`
            <form id="editPolicyForm">
                <input type="hidden" id="policyId" value="${id}" />

                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="fw-semibold">Khách hàng</label>
                        <input class="input-input opacity-50" value="${response.customer?.full_name || ''}" readonly>
                    </div>

                    <div class="col-md-6">
                        <label class="fw-semibold">Sản phẩm</label>
                        <input class="input-input opacity-50" value="${response.product?.name || ''}" readonly>
                    </div>

                    <div class="col-md-6">
                        <label class="fw-semibold">Tư vấn viên</label>
                        <input class="input-input" id="advisorName" value="${response.advisor?.full_name || ''}">
                    </div>

                    <div class="col-md-6">
                        <label class="fw-semibold">Số tiền bảo hiểm (₫)</label>
                        <input class="input-input" type="number" id="sumAssured" value="${response.sum_assured || 0}">
                    </div>

                    <div class="col-md-6">
                        <label class="fw-semibold">Ngày cấp</label>
                        <input type="date" class="input-input" id="issueDate" value="${response.issue_date?.split('T')[0] || ''}">
                    </div>

                    <div class="col-md-6">
                        <label class="fw-semibold">Ghi chú</label>
                        <input class="input-input" id="notes" value="${response.notes || ''}">
                    </div>
                </div>
            </form>
        `);
    }).fail(function (xhr) {
        console.error("Get policy details error:", xhr);
        content.html(`<p class="text-danger text-center fw-bold">Lỗi tải dữ liệu!</p>`);
    });
}

// ===============================
// LƯU CẬP NHẬT HỢP ĐỒNG
// ===============================
$("#saveEditBtn").click(function () {
    // ✅ LẤY DATA TỪNG FIELD VÀ TẠO OBJECT ĐÚNG FORMAT
    const data = {
        Id: $("#policyId").val(),
        AdvisorName: $("#advisorName").val(),
        SumAssured: parseFloat($("#sumAssured").val()) || 0,
        IssueDate: $("#issueDate").val(),
        Notes: $("#notes").val() || ""
    };

    // Debug
    console.log("Sending data:", data);

    if (!data.Id) {
        Swal.fire("Lỗi", "Thiếu ID hợp đồng!", "error");
        return;
    }

    Swal.fire({
        title: "Xác nhận cập nhật?",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Cập nhật",
        cancelButtonText: "Hủy"
    }).then(result => {
        if (result.isConfirmed) {
            Swal.fire({
                title: "Đang cập nhật...",
                didOpen: () => Swal.showLoading(),
                allowOutsideClick: false
            });

            $.ajax({
                url: "/Policies/UpdatePolicyInfo",
                method: "POST",
                data: JSON.stringify(data),
                contentType: "application/json",
                success: function (response) {
                    Swal.close();

                    if (response.reload) {
                        // Reload để hiển thị toast từ TempData
                        location.reload();
                    } else {
                        Swal.fire("Thành công", "Cập nhật thông tin hợp đồng thành công!", "success")
                            .then(() => location.reload());
                    }
                },
                error: function (xhr) {
                    Swal.close();
                    console.error("Update error:", xhr);
                    const errorMsg = xhr.responseJSON?.message || "Không thể cập nhật hợp đồng!";
                    Swal.fire("Lỗi", errorMsg, "error");
                }
            });
        }
    });
});

function loadPolicyDetails(policyId) {
    console.log("policyId received:", policyId);

    if (!policyId) {
        // Lưu toast thông báo lỗi vào localStorage
        const toastMessage = {
            type: "error",
            message: "Mã hợp đồng không hợp lệ!"
        };
        localStorage.setItem("toastAfterReload", JSON.stringify(toastMessage));
        return;
    }

    // Hiển thị modal với loading
    $('#createClaimModal').modal('show');
    $('#loadingDiv').show();
    $('#claimDetailsDiv').hide();

    const url = `/Policies/GetPolicyDetails/${policyId}`;
    console.log("Request URL:", url);

    $.get(url, function (data) {
        console.log("Response data:", data);

        if (data.success) {
            // Ẩn loading, hiện nội dung
            $('#loadingDiv').hide();
            $('#claimDetailsDiv').show();

            // Điền thông tin hợp đồng
            $('#modalClaimPolicyNo').text(`Hợp đồng: ${data.policy_no}`);

            // Điền ngày tháng
            $('#policyNo').val(data.policy_no);
            $('#issueDate').val(data.issue_date ? new Date(data.issue_date).toISOString().split('T')[0] : 'N/A');
            $('#effectiveDate').val(data.effective_date ? new Date(data.effective_date).toLocaleDateString('vi-VN') : 'N/A');
            $('#maturityDate').val(data.maturity_date ? new Date(data.maturity_date).toLocaleDateString('vi-VN') : 'N/A');

            // Điền thông tin người thụ hưởng
            if (data.beneficiaries && data.beneficiaries.length > 0) {
                const beneficiaryHTML = data.beneficiaries.map(b => {
                    const fullName = b.full_name || 'Không có tên';
                    const relation = b.relation || 'Không có quan hệ';
                    const national_id = b.national_id || '0';
                    const sharePercent = b.share_percent || '0';
                    return `
                        <tr>
                            <td>${fullName}</td>
                            <td>${relation}</td>
                            <td>${sharePercent}%</td>
                            <td>${national_id}</td>
                        </tr>
                    `;
                }).join('');
                $('#beneficiaryInputList').html(beneficiaryHTML);
            } else {
                $('#beneficiaryInputList').html('<p class="text-muted">Không có thông tin người thụ hưởng</p>');
            }

        } else {
            // Lưu toast thông báo lỗi vào localStorage
            const toastMessage = {
                type: "error",
                message: data.message || 'Không tìm thấy thông tin hợp đồng'
            };
            localStorage.setItem("toastAfterReload", JSON.stringify(toastMessage));
            $('#createClaimModal').modal('hide');
        }
    })
        .fail(function (xhr, status, error) {
            console.error("AJAX Error:", xhr.responseText);
            console.error("Status:", status);
            console.error("Error:", error);

            // Lưu toast thông báo lỗi vào localStorage
            const toastMessage = {
                type: "error",
                message: 'Lỗi khi tải thông tin hợp đồng: ' + (xhr.responseJSON?.message || error)
            };
            localStorage.setItem("toastAfterReload", JSON.stringify(toastMessage));
            $('#createClaimModal').modal('hide');
        });
}

function confirmCreateClaim() {
    console.log('Button clicked!');
    const policyNo = $('#policyNo').val();
    const claimType = $('#claimType').val();
    const eventDate = $('#eventDate').val();
    const eventPlace = $('#eventPlace').val();
    const description = $('#description').val();
    const requestedAmount = parseFloat($('#requestedAmount').val() || 0);
    const cause = $('#cause').val();
    const beneficiaryName = $('#beneficiaryInputList tr:first td:first').text() || '';

    // Validate
    if (!eventDate || !eventPlace || !description || !cause || requestedAmount <= 0) {
        window.showToast("error", "Vui lòng điền đầy đủ thông tin!");
        return;
    }

    const payload = {
        policyNo,
        claimType,
        eventDate,
        eventPlace,
        description,
        cause,
        requestedAmount,
        beneficiaryName
    };

    console.log("Creating claim with payload:", payload);

    $.ajax({
        url: '/Claims/CreateClaimFromPolicy',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (response) {
            if (response.success) {
                // 🔥 Lưu thông báo vào localStorage trước khi reload
                localStorage.setItem(
                    "toastAfterReload",
                    JSON.stringify({
                        type: "success",
                        message: "Hồ sơ yêu cầu đã được tạo thành công!"
                    })
                );

                $('#createClaimModal').modal('hide');
                location.reload(); // reload rồi mới hiển thị toast
            } else {
                window.showToast("error", 'Đã có lỗi khi tạo hồ sơ yêu cầu: ' + (response.message || ''));
                if (response.errors) {
                    console.log("Errors from server:", response.errors);
                    window.showToast("error", "Chi tiết lỗi: " + response.errors.join(", "));
                }
            }
        },
        error: function (xhr) {
            const errorMessage = xhr.responseJSON?.message || 'Không xác định';
            window.showToast("error", 'Lỗi server: ' + errorMessage);
            console.error("AJAX Error:", xhr.responseText);
        }
    });
}

