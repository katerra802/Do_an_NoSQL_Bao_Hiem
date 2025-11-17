document.addEventListener("DOMContentLoaded", function () {

    // ================================
    // QUẢN LÝ CHECKBOX QUA NHIỀU TRANG
    // ================================
    let selectedAll = false;
    let selectedIds = new Set(JSON.parse(localStorage.getItem('selectedPolicyIds') || '[]'));
    let excludedIds = new Set(JSON.parse(localStorage.getItem('excludedPolicyIds') || '[]'));

    const checkAll = document.getElementById("checkAll");
    const rowCheckboxes = document.querySelectorAll(".row-check");

    // Khôi phục trạng thái checkbox khi load trang
    function restoreCheckboxState() {
        selectedAll = localStorage.getItem('selectAllPolicy') === 'true';

        if (selectedAll) {
            checkAll.checked = true;
            rowCheckboxes.forEach(cb => {
                cb.checked = !excludedIds.has(cb.value);
            });
        } else {
            rowCheckboxes.forEach(cb => {
                if (selectedIds.has(cb.value)) {
                    cb.checked = true;
                }
            });

            const allChecked = Array.from(rowCheckboxes).every(cb => cb.checked);
            if (checkAll) checkAll.checked = allChecked;
        }
    }

    // Lưu trạng thái checkbox
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

    // Sự kiện "Chọn tất cả"
    if (checkAll) {
        checkAll.addEventListener("change", function () {
            if (this.checked) {
                selectedAll = true;
                excludedIds.clear();
                selectedIds.clear();
                localStorage.setItem('selectAllPolicy', 'true');
                localStorage.removeItem('excludedPolicyIds');
                rowCheckboxes.forEach(cb => cb.checked = true);
            } else {
                selectedAll = false;
                excludedIds.clear();
                selectedIds.clear();
                localStorage.removeItem('selectAllPolicy');
                localStorage.removeItem('excludedPolicyIds');
                localStorage.removeItem('selectedPolicyIds');
                rowCheckboxes.forEach(cb => cb.checked = false);
            }
        });
    }

    // Sự kiện checkbox từng dòng
    rowCheckboxes.forEach(cb => {
        cb.addEventListener("change", function () {
            if (selectedAll) {
                if (!this.checked) {
                    excludedIds.add(this.value);
                } else {
                    excludedIds.delete(this.value);
                }

                const allCheckedNow = Array.from(rowCheckboxes).every(c => c.checked);
                if (checkAll) checkAll.checked = allCheckedNow && excludedIds.size === 0;
            } else {
                if (this.checked) {
                    selectedIds.add(this.value);
                } else {
                    selectedIds.delete(this.value);
                }

                const allChecked = Array.from(rowCheckboxes).every(c => c.checked);
                if (checkAll) checkAll.checked = allChecked;
            }

            saveCheckboxState();
        });
    });

    restoreCheckboxState();

    // ================================
    // LẤY TỔNG SỐ HỒNG SƠ (CHO CHỌN TẤT CẢ)
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

        const params = new URLSearchParams();
        for (const key in filters) {
            if (filters[key]) params.append(key, filters[key]);
        }

        try {
            const res = await fetch(`/PolicyApplications/GetTotalCount?${params.toString()}`);
            const data = await res.json();
            return data.count;
        } catch (err) {
            console.error("Lỗi lấy tổng số:", err);
            return 0;
        }
    };

    // ================================
    // LẤY DANH SÁCH ID ĐÃ CHỌN (THỰC TẾ)
    // ================================
    window.getSelectedIds = function () {
        // Lấy tất cả checkbox đang được tick trên trang hiện tại
        const currentPageChecked = Array.from(document.querySelectorAll(".row-check:checked"))
            .map(chk => chk.value);

        const selectAllMode = localStorage.getItem('selectAllPolicy') === 'true';

        if (selectAllMode) {
            // Nếu đang ở chế độ "chọn tất cả"
            // Trả về null để server hiểu là "chọn tất cả trừ excludedIds"
            return null;
        } else {
            // Chế độ chọn từng cái
            // Trả về các ID thực sự được chọn
            return currentPageChecked.length > 0 ? currentPageChecked : Array.from(selectedIds);
        }
    };

    // ================================
    // XÓA TRẠNG THÁI SAU KHI THAO TÁC
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
    // BỘ LỌC TỰ ĐỘNG SUBMIT
    // ================================
    const filterForm = document.getElementById("filterForm");
    if (filterForm) {
        filterForm.querySelectorAll("select, input[type=date]").forEach(el => {
            el.addEventListener("change", () => {
                window.clearSelection();
                filterForm.submit();
            });
        });

        filterForm.querySelectorAll("input[name='price_from'], input[name='price_to']").forEach(el => {
            el.addEventListener("change", () => {
                window.clearSelection();
                filterForm.submit();
            });
        });
    }

    // ================================
    // XUẤT EXCEL - FIXED VERSION
    // ================================
    const exportBtn = document.getElementById("exportExcelBtn");
    if (exportBtn) {
        exportBtn.addEventListener("click", function () {
            const selectAllMode = localStorage.getItem('selectAllPolicy') === 'true';
            const excludedIdsArray = Array.from(new Set(JSON.parse(localStorage.getItem('excludedPolicyIds') || '[]')));

            let queryString = '';

            if (selectAllMode) {
                // Chế độ "chọn tất cả"
                // Gửi excludeIds để server loại trừ
                if (excludedIdsArray.length > 0) {
                    queryString = `excludeIds=${excludedIdsArray.join(',')}`;
                }

                // Thêm các filters
                const filters = {
                    status: document.querySelector("select[name='status']")?.value || '',
                    price_from: document.querySelector("input[name='price_from']")?.value || '',
                    price_to: document.querySelector("input[name='price_to']")?.value || '',
                    search: document.querySelector("input[name='search']")?.value || '',
                    from_date: document.querySelector("input[name='from_date']")?.value || '',
                    to_date: document.querySelector("input[name='to_date']")?.value || ''
                };

                const params = [];
                for (const key in filters) {
                    if (filters[key]) {
                        params.push(`${key}=${encodeURIComponent(filters[key])}`);
                    }
                }

                if (params.length > 0) {
                    queryString += (queryString ? '&' : '') + params.join('&');
                }

                // Đánh dấu là "chọn tất cả"
                queryString += (queryString ? '&' : '') + 'exportAll=true';

            } else {
                // Chế độ chọn từng cái
                const selectedIds = window.getSelectedIds();

                if (!selectedIds || selectedIds.length === 0) {
                    window.showToast("warning", "Vui lòng chọn ít nhất một hồ sơ để xuất!");
                    return;
                }

                queryString = `ids=${selectedIds.join(',')}`;
            }

            // Redirect đến URL xuất Excel
            window.location.href = '/PolicyApplications/ExportExcel' + (queryString ? '?' + queryString : '');
        });
    }

}); // KẾT THÚC DOMContentLoaded

// ================================
// XÓA HÀNG LOẠT
// ================================
window.bulkDelete = async function () {
    const selectAllMode = localStorage.getItem('selectAllPolicy') === 'true';
    const selectedIds = window.getSelectedIds();
    const excludedIds = new Set(JSON.parse(localStorage.getItem('excludedPolicyIds') || '[]'));

    if (!selectAllMode && (!selectedIds || selectedIds.length === 0)) {
        window.showToast("warning", "Vui lòng chọn ít nhất 1 hồ sơ để xóa!");
        return;
    }

    let count = 0;
    if (selectAllMode) {
        count = await window.getTotalCount();
        count -= excludedIds.size;
    } else {
        count = selectedIds.length;
    }

    const message = `Bạn có chắc chắn muốn xóa <strong>${count}</strong> hồ sơ đã chọn không?`;

    window.confirmAction({
        title: "Xác nhận hành động",
        message: message,
        dangerText: "Hành động này không thể hoàn tác.",
        confirmButtonText: "Xác nhận",
        cancelButtonText: "Hủy bỏ",
        icon: "warning",
        onConfirm: async () => {
            Swal.fire({
                title: "Đang xóa...",
                didOpen: () => Swal.showLoading(),
                allowOutsideClick: false
            });

            try {
                let url = "/PolicyApplications/BulkDelete";
                let body;

                if (selectAllMode) {
                    url += "?deleteAll=true";
                    body = JSON.stringify({
                        excludeIds: [...excludedIds],
                        status: document.querySelector("select[name='status']")?.value || '',
                        price_from: document.querySelector("input[name='price_from']")?.value || '',
                        price_to: document.querySelector("input[name='price_to']")?.value || '',
                        search: document.querySelector("input[name='search']")?.value || '',
                        from_date: document.querySelector("input[name='from_date']")?.value || '',
                        to_date: document.querySelector("input[name='to_date']")?.value || ''
                    });
                } else {
                    body = JSON.stringify(selectedIds);
                }

                const res = await fetch(url, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: body
                });

                if (res.ok) {
                    const data = await res.json();
                    window.clearSelection();
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message || "Xóa thành công!"
                    }));
                    location.reload();
                } else {
                    window.showToast("error", "Không thể xóa hồ sơ. Vui lòng thử lại.");
                }
            } catch (err) {
                window.showToast("error", "Kết nối đến server thất bại.");
            }
        }
    });
};

// ================================
// CẬP NHẬT TRẠNG THÁI HÀNG LOẠT
// ================================
window.bulkUpdateStatus = async function () {
    const selectAllMode = localStorage.getItem('selectAllPolicy') === 'true';
    const selectedIds = window.getSelectedIds();
    const excludedIds = new Set(JSON.parse(localStorage.getItem('excludedPolicyIds') || '[]'));

    if (!selectAllMode && (!selectedIds || selectedIds.length === 0)) {
        window.showToast("warning", "Vui lòng chọn ít nhất 1 hồ sơ để cập nhật!");
        return;
    }

    let count = 0;
    if (selectAllMode) {
        count = await window.getTotalCount();
        count -= excludedIds.size;
    } else {
        count = selectedIds.length;
    }

    const statuses = {
        submitted: "Đã nộp",
        under_review: "Đang xét duyệt",
        approved: "Đã duyệt",
        rejected: "Từ chối",
    };

    const options = Object.entries(statuses)
        .map(([code, name]) => `<option value="${code}">${name}</option>`)
        .join("");

    window.confirmAction({
        title: "Xác nhận hành động",
        html: `
            <div style="margin-top:10px; color:#444; font-size:15px;">
                <label class="fw-semibold mb-2">
                    Chọn trạng thái mới cho <b>${count}</b> hồ sơ:
                </label>
                <select id="bulkStatusSelect" class="select-input">
                    ${options}
                </select>
            </div>
        `,
        confirmButtonText: "Cập nhật",
        cancelButtonText: "Hủy bỏ",
        icon: "warning",
        iconColor: "#dc2626",
        preConfirm: () => {
            const select = document.getElementById("bulkStatusSelect");
            return select ? select.value : null;
        },
        onConfirm: async (newStatus) => {
            if (!newStatus) {
                window.showToast("warning", "Vui lòng chọn trạng thái mới!");
                return;
            }

            Swal.fire({
                title: "Đang cập nhật...",
                didOpen: () => Swal.showLoading(),
                allowOutsideClick: false
            });

            try {
                let url = "/PolicyApplications/BulkUpdateStatus";
                let body;

                if (selectAllMode) {
                    url += "?updateAll=true";
                    body = JSON.stringify({
                        newStatus: newStatus,
                        excludeIds: [...excludedIds],
                        status: document.querySelector("select[name='status']")?.value || '',
                        price_from: document.querySelector("input[name='price_from']")?.value || '',
                        price_to: document.querySelector("input[name='price_to']")?.value || '',
                        search: document.querySelector("input[name='search']")?.value || '',
                        from_date: document.querySelector("input[name='from_date']")?.value || '',
                        to_date: document.querySelector("input[name='to_date']")?.value || ''
                    });
                } else {
                    body = JSON.stringify({
                        ids: selectedIds,
                        newStatus: newStatus
                    });
                }

                const res = await fetch(url, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: body
                });

                if (res.ok) {
                    const data = await res.json();
                    Swal.close();
                    window.clearSelection();
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message || "Cập nhật thành công!"
                    }));
                    location.reload();
                } else {
                    Swal.close();
                    window.showToast("error", "Không thể cập nhật. Vui lòng thử lại.");
                }
            } catch (err) {
                Swal.close();
                window.showToast("error", "Kết nối đến server thất bại.");
            }
        },
    });
};

// ================================
// XÓA SINGLE RECORD
// ================================
window.confirmDelete = function (appId) {
    window.confirmAction({
        title: 'Xác nhận xóa',
        message: 'Bạn có chắc chắn muốn xóa hồ sơ này?',
        dangerText: "Hành động này không thể hoàn tác.",
        confirmButtonText: 'Xác nhận',
        cancelButtonText: 'Hủy',
        icon: 'warning',
        onConfirm: async function () {
            try {
                const res = await fetch(`/PolicyApplications/Delete/${appId}`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" }
                });

                if (res.ok) {
                    const data = await res.json();
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message || "Hồ sơ đã được xóa."
                    }));
                    location.reload();
                } else {
                    window.showToast("error", "Không thể xóa hồ sơ. Vui lòng thử lại.");
                }
            } catch (err) {
                window.showToast("error", "Đã xảy ra lỗi khi xóa hồ sơ.");
            }
        }
    });
}

// ================================
// CẬP NHẬT TRẠNG THÁI SINGLE
// ================================
window.updateStatus = function (appId) {
    const statuses = {
        submitted: "Đã nộp",
        under_review: "Đang xét duyệt",
        approved: "Đã duyệt",
        rejected: "Từ chối",
    };

    const options = Object.entries(statuses)
        .map(([code, name]) => `<option value="${code}">${name}</option>`)
        .join("");

    window.confirmAction({
        title: "Xác nhận cập nhật trạng thái",
        html: `
            <div style="margin-top:10px; color:#444; font-size:15px;">
                <label class="fw-semibold mb-2">Chọn trạng thái mới:</label>
                <select id="statusSelect" class="select-input">
                    ${options}
                </select>
            </div>
        `,
        confirmButtonText: "Cập nhật",
        cancelButtonText: "Hủy bỏ",
        icon: "warning",
        iconColor: "#dc2626",
        preConfirm: () => {
            const select = document.getElementById("statusSelect");
            return select ? select.value : null;
        },
        onConfirm: async (newStatus) => {
            if (!newStatus) {
                window.showToast("warning", "Vui lòng chọn trạng thái mới!");
                return;
            }

            Swal.fire({
                title: "Đang cập nhật...",
                didOpen: () => Swal.showLoading(),
                allowOutsideClick: false
            });

            try {
                const res = await fetch(`/PolicyApplications/UpdateStatus`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ id: appId, newStatus: newStatus })
                });

                if (res.ok) {
                    const data = await res.json();
                    Swal.close();
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message || "Trạng thái đã được cập nhật."
                    }));
                    location.reload();
                } else {
                    Swal.close();
                    window.showToast("error", "Không thể cập nhật trạng thái. Vui lòng thử lại.");
                }
            } catch (err) {
                Swal.close();
                window.showToast("error", "Kết nối đến server thất bại.");
            }
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


function viewPolicyDetails(appId) {
    const modal = $('#policyDetailsModal');
    const content = $('#policyDetailsContent');
    const appNoSpan = $('#modalAppNo');

    // Hiển thị loading
    content.html(`
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Đang tải...</span>
            </div>
            <p class="mt-3 text-muted">Đang tải thông tin...</p>
        </div>
    `);
    modal.modal('show');

    $.ajax({
        url: '/PolicyApplications/GetPolicyApplicationDetails',
        method: 'GET',
        data: { id: appId },
        success: function (data) {
            if (!data) {
                content.html(`<p class="text-center text-danger fw-bold">Không tìm thấy hồ sơ!</p>`);
                return;
            }

            appNoSpan.text(data.app_no);

            const formatCurrency = (v) => v > 0 ? v.toLocaleString('vi-VN') + ' ₫' : '—';
            const formatDate = (d) => d ? new Date(d).toLocaleDateString('vi-VN') : '—';

            // 🔹 Dữ liệu thẩm định (nếu có)
            const uw = data.underwriting || null;

            // Lấy danh sách người thụ hưởng
            const beneficiaries = data.beneficiaries || [];

            const beneficiariesHtml = beneficiaries.length > 0 ? `
                <div class="col-12">
                    <h5 class="fw-bold text-danger mb-3">Thông tin người thụ hưởng</h5>
                    <div class="table-responsive">
                        <table class="table table-bordered">
                            <thead>
                                <tr>
                                    <th>Tên người thụ hưởng</th>
                                    <th>Quan hệ</th>
                                    <th>Phần trăm chia sẻ</th>
                                    <th>Ngày sinh</th>
                                    <th>Số CMND</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${beneficiaries.map(beneficiary => `
                                    <tr>
                                        <td>${beneficiary.full_name || '—'}</td>
                                        <td>${beneficiary.relation || '—'}</td>
                                        <td>${beneficiary.share_percent || '—'}</td>
                                        <td>${formatDate(beneficiary.dob) || '—'}</td>
                                        <td>${beneficiary.national_id || '—'}</td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>
                </div>
            ` : '';

            const html = `
            <div class="fade-in-up">
                <div class="row g-3">
                    <h5 class="fw-bold text-primary text-danger">Thông tin hồ sơ yêu cầu</h5>
                    <!-- Khách hàng -->
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Khách hàng</label>
                            <input type="text" class="form-control bg-white input-input" value="${data.customer?.full_name || '—'}" readonly />
                        </div>
                    </div>

                    <!-- Tư vấn viên -->
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Tư vấn viên</label>
                            <input type="text" class="form-control bg-white input-input" value="${data.advisor?.full_name || '—'}" readonly />
                        </div>
                    </div>

                    <!-- Sản phẩm bảo hiểm -->
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Sản phẩm bảo hiểm</label>
                            <input type="text" class="form-control bg-white input-input" value="${data.product?.name || '—'}" readonly />
                        </div>
                    </div>

                    <!-- Số tiền bảo hiểm -->
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Số tiền bảo hiểm</label>
                            <input type="text" class="form-control bg-white input-input" value="${formatCurrency(data.sum_assured)}" readonly />
                        </div>
                    </div>

                    <!-- Chế độ đóng phí -->
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Chế độ đóng phí</label>
                            <input type="text" class="form-control bg-white input-input" value="${data.premium_mode || '—'}" readonly />
                        </div>
                    </div>

                    <!-- Trạng thái -->
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Trạng thái</label>
                            <input type="text" class="form-control bg-white input-input" value="${data.status || '—'}" readonly />
                        </div>
                    </div>

                    <!-- Ngày nộp hồ sơ -->
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Ngày nộp hồ sơ</label>
                            <input type="text" class="form-control bg-white input-input" value="${formatDate(data.submitted_at)}" readonly />
                        </div>
                    </div>

                    <!-- Ghi chú -->
                    ${data.notes ? `
                    <div class="col-12">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Ghi chú</label>
                            <textarea class="form-control bg-white input-input" rows="3" readonly>${data.notes}</textarea>
                        </div>
                    </div>` : ''}

                    <!-- Tài liệu -->
                    ${data.documents && data.documents.length > 0 ? `
                    <div class="col-12">
                        <div class="mb-3">
                            <label class="form-label fw-semibold">Tài liệu đính kèm</label>
                            <div class="d-flex flex-wrap gap-3">
                                ${data.documents.map(doc => `
                                    <div class="file-item">
                                        <div class="file-info">
                                            <div class="file-icon">${getFileIcon(doc)}</div>
                                            <div class="file-meta">
                                                <a href="/uploads/${doc}" target="_blank" class="file-name text-decoration-none">${doc}</a>
                                            </div>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    </div>` : ''}

                </div>
                ${beneficiariesHtml}

                ${uw ? `
                <hr class="my-4">
                <h5 class="fw-bold text-primary mb-3 text-danger">Thông tin thẩm định</h5>
                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Mức rủi ro</label>
                        <input type="text" class="form-control bg-white input-input" value="${translateRisk(uw.risk_level)}" readonly />
                    </div>

                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Phí gốc</label>
                        <input type="text" class="form-control bg-white input-input" value="${formatCurrency(uw.base_premium || data.approved_premium)}" readonly />
                    </div>

                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Phí phụ trội</label>
                        <input type="text" class="form-control bg-white input-input" value="${formatCurrency(uw.extra_premium)}" readonly />
                    </div>

                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Phí được duyệt cuối cùng</label>
                        <input type="text" class="form-control bg-white input-input" value="${formatCurrency(uw.approved_premium)}" readonly />
                    </div>

                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Quyết định</label>
                        <input type="text" class="form-control bg-white input-input" value="${translateDecision(uw.decision)}" readonly />
                    </div>

                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Ngày thẩm định</label>
                        <input type="text" class="form-control bg-white input-input" value="${formatDate(uw.decided_at)}" readonly />
                    </div>

                    ${uw.notes ? `
                    <div class="col-12">
                        <label class="form-label fw-semibold">Ghi chú thẩm định</label>
                        <textarea class="form-control bg-white input-input" rows="3" readonly>${uw.notes}</textarea>
                    </div>` : ''}
                </div>` : ''}
            </div>`;

            content.html(html);
        },
        error: function (jqXHR, textStatus, errorThrown) {
            content.html(`<p class="text-center text-danger">Lỗi kết nối: ${textStatus} - ${errorThrown}</p>`);
            console.error('Server responded with error:', jqXHR.responseText);
        }
    });
}

// ✅ Hàm phụ trợ
function translateRisk(level) {
    switch (level) {
        case "standard": return "Chuẩn (Standard)";
        case "substandard": return "Cận chuẩn (Substandard)";
        case "preferred": return "Ưu tiên (Preferred)";
        case "declined": return "Từ chối (Declined)";
        default: return "Không xác định";
    }
}

function translateDecision(dec) {
    switch (dec) {
        case "approved": return "Phê duyệt";
        case "approved_with_loading": return "Phê duyệt kèm phụ phí";
        case "rejected": return "Từ chối";
        default: return "Không xác định";
    }
}

function getFileIcon(fileName) {
    const ext = fileName.split('.').pop().toLowerCase();

    switch (ext) {
        case 'pdf':
            return `<i class="fa-solid fa-file-pdf pdf"></i>`;
        case 'doc':
        case 'docx':
            return `<i class="fa-solid fa-file-word word"></i>`;
        case 'xls':
        case 'xlsx':
            return `<i class="fa-solid fa-file-excel excel"></i>`;
        case 'jpg':
        case 'jpeg':
        case 'png':
        case 'gif':
        case 'bmp':
            return `<i class="fa-solid fa-file-image"></i>`;
        default:
            return `<i class="fa-solid fa-file"></i>`;  // Default icon for other files
    }
}

function openUnderwritingModal(appId, basePremium) {
    Swal.fire({
        title: "Thẩm định hồ sơ bảo hiểm",
        html: `
            <div class="text-start">
                <label class="fw-semibold mt-2">Mức rủi ro</label>
                <select id="riskLevel" class="select-input mt-1">
                    <option value="standard">Chuẩn (Standard)</option>
                    <option value="substandard">Cận chuẩn (Substandard)</option>
                    <option value="preferred">Ưu tiên (Preferred)</option>
                    <option value="declined" class="text-danger">Từ chối (Declined)</option>
                </select>

                <div id="premiumSection">
                    <label class="fw-semibold mt-3">Phí gốc</label>
                    <input type="text" id="basePremium" class="input-input bg-light" value="${basePremium.toLocaleString('vi-VN')} ₫" readonly />

                    <label class="fw-semibold mt-3">Phí phụ trội (nếu có)</label>
                    <input id="extraPremium" type="number" class="input-input" placeholder="Nhập số tiền (₫)" />

                    <label class="fw-semibold mt-3">Phí được duyệt cuối cùng</label>
                    <input id="approvedPremium" type="text" class="input-input bg-light" readonly />
                </div>

                <label class="fw-semibold mt-3">Quyết định</label>
                <select id="decision" class="select-input mt-1">
                    <option value="approved">Phê duyệt</option>
                    <option value="approved_with_loading">Phê duyệt kèm phụ phí</option>
                    <option value="rejected">Từ chối</option>
                </select>

                <label class="fw-semibold mt-3">Ghi chú</label>
                <textarea id="notes" class="input-input" rows="3" placeholder="Nhập ghi chú..."></textarea>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: "Xác nhận duyệt",
        cancelButtonText: "Hủy bỏ",
        reverseButtons: true,
        customClass: {
            popup: "custom-swal-popup",
            confirmButton: "custom-swal-confirm-btn",
            cancelButton: "custom-swal-cancel-btn"
        },
        didOpen: () => {
            const riskSelect = document.getElementById("riskLevel");
            const extraInput = document.getElementById("extraPremium");
            const approvedInput = document.getElementById("approvedPremium");
            const premiumSection = document.getElementById("premiumSection");
            const decisionSelect = document.getElementById("decision");

            // Áp dụng logic khi thay đổi mức rủi ro
            const applyRiskLogic = () => {
                const riskLevel = riskSelect.value;

                // Reset trước
                premiumSection.querySelectorAll("input, select").forEach(el => {
                    el.disabled = false;
                });
                premiumSection.style.opacity = "1";

                extraInput.value = "";
                approvedInput.value = basePremium.toLocaleString('vi-VN') + " ₫";

                // Quyết định theo mức rủi ro
                if (riskLevel === "standard") {
                    extraInput.disabled = true;
                    decisionSelect.value = "approved";
                } else if (riskLevel === "substandard") {
                    extraInput.disabled = false;
                } else if (riskLevel === "preferred") {
                    extraInput.disabled = false;
                } else if (riskLevel === "declined") {
                    premiumSection.querySelectorAll("input, select").forEach(el => el.disabled = true);
                    premiumSection.style.opacity = "0.5";
                    decisionSelect.value = "rejected";
                    decisionSelect.disabled = true;
                }
            };

            // Áp dụng logic ngay khi mở modal
            applyRiskLogic();

            // Thay đổi risk level
            riskSelect.addEventListener("change", applyRiskLogic);

            // Tính phí và quyết định
            extraInput.addEventListener("input", () => {
                const extra = parseFloat(extraInput.value || 0);
                const total = basePremium + extra;

                approvedInput.value = total.toLocaleString('vi-VN') + " ₫";
                decisionSelect.value = extra !== 0 ? "approved_with_loading" : "approved";
            });
        },

        preConfirm: () => {
            const riskLevel = document.getElementById("riskLevel").value;
            const extraPremium = parseFloat(document.getElementById("extraPremium").value || 0);
            const totalPremium = basePremium + extraPremium;
            const decision = document.getElementById("decision").value;
            const notes = document.getElementById("notes").value;

            return {
                applicationId: appId,
                basePremium: basePremium,
                riskLevel: riskLevel,
                extraPremium: extraPremium,
                approvedPremium: totalPremium,
                decision: decision,
                notes: notes
            };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/Underwriting/Approve",
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(result.value),

                success: (res) => {
                    if (res.success) {
                        // Lưu toast vào localStorage
                        localStorage.setItem("toastAfterReload", JSON.stringify({
                            type: "success",
                            message: res.message
                        }));

                        setTimeout(() => location.reload(), 800);
                    } else {
                        window.showToast("error", res.message);
                    }
                },

                error: () => {
                    window.showToast("error", "Lỗi hệ thống!");
                }
            });
        }
    });
}


function previewContract(appId) {
    const modal = $('#createPolicyModal');
    const content = $('#createPolicyContent');
    const appNoText = $('#modalPolicyAppNo');

    // Hiển thị loading
    content.html(`
        <div class="text-center py-5">
            <div class="spinner-border text-primary"></div>
            <p class="mt-3 text-muted">Đang tải thông tin...</p>
        </div>
    `);

    modal.modal('show');

    $.get(`/PolicyApplications/GetPolicyApplicationDetails?id=${appId}`, function (data) {

        if (!data) {
            content.html(`<p class="text-center text-danger">Không tìm thấy hồ sơ.</p>`);
            return;
        }

        appNoText.text(data.app_no);

        // Kiểm tra beneficiaries và tạo các option
        const beneficiaries = Array.isArray(data.beneficiaries) ? data.beneficiaries : [];

        // Tạo HTML cho bảng người thụ hưởng
        const beneficiariesHtml = beneficiaries.length > 0 ? `
            <h5 class="fw-bold text-danger mb-3">Thông tin người thụ hưởng</h5>
            <div class="table-responsive">
                <table class="table table-bordered">
                    <thead>
                        <tr>
                            <th>Tên người thụ hưởng</th>
                            <th>Quan hệ</th>
                            <th>Phần trăm chia sẻ</th>
                            <th>Ngày sinh</th>
                            <th>Số CMND</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${beneficiaries.map(beneficiary => `
                            <tr>
                                <td>${beneficiary.full_name || '—'}</td>
                                <td>${beneficiary.relation || '—'}</td>
                                <td>${beneficiary.share_percent || '—'}</td>
                                <td>${formatDate(beneficiary.dob) || '—'}</td>
                                <td>${beneficiary.national_id || '—'}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        ` : `<p class="text-muted">Không có thông tin người thụ hưởng.</p>`;

        // Tạo thông tin hợp đồng
        const policyNo = "PL-" + new Date().getFullYear() + "-" + Math.random().toString(36).substring(2, 8).toUpperCase();
        const issueDate = new Date().toISOString().slice(0, 10);

        const termYears = data.product?.term_years ?? 0;
        const maturity = new Date();
        maturity.setFullYear(maturity.getFullYear() + termYears);
        const maturityDate = maturity.toISOString().slice(0, 10);

        const uw = data.underwriting;

        const html = `
            <div class="fade-in-up">
                <h5 class="fw-bold text-danger mb-3">Thông tin hợp đồng</h5>
                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Mã hợp đồng</label>
                        <input id="inputPolicyNo" class="form-control bg-white input-input" value="${policyNo}" readonly />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Ngày phát hành</label>
                        <input id="inputIssueDate" type="date" class="form-control input-input" value="${issueDate}" />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Ngày hiệu lực</label>
                        <input class="form-control bg-white input-input" value="${issueDate}" readonly />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Ngày đáo hạn</label>
                        <input class="form-control bg-white input-input" value="${maturityDate}" readonly />
                    </div>
                </div>

                <hr class="my-4">

                <h5 class="fw-bold text-danger mb-3">Thông tin hồ sơ yêu cầu</h5>
                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Khách hàng</label>
                        <input class="form-control bg-white input-input" value="${data.customer.full_name}" readonly />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Sản phẩm</label>
                        <input class="form-control bg-white input-input" value="${data.product.name}" readonly />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Số tiền bảo hiểm</label>
                        <input class="form-control bg-white input-input" value="${data.sum_assured.toLocaleString('vi-VN')} ₫" readonly />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Hình thức đóng phí</label>
                        <input class="form-control bg-white input-input" value="${data.premium_mode}" readonly />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label fw-semibold">Phí được duyệt cuối cùng</label>
                        <input class="form-control bg-white input-input"
                            value="${(uw?.approved_premium ?? 0).toLocaleString('vi-VN')} ₫"
                            readonly />
                    </div>
                </div>

                <hr class="my-4">

                ${beneficiariesHtml}

                <hr class="my-4">

                <h5 class="fw-bold text-danger mb-3">Ghi chú hợp đồng</h5>
                <textarea id="inputPolicyNote" class="form-control input-input" rows="3"></textarea>

            </div>
        `;

        content.html(html);

        // Gán listener nút xác nhận
        $('#confirmCreatePolicyBtn').off('click').on('click', function () {
            createPolicy(appId);
        });
    });
}

function formatDate(date) {
    if (!date) return '—';
    const d = new Date(date);
    return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`;
}

function createPolicy(appId) {
    const modal = $('#createPolicyModal');
    const content = $('#createPolicyContent');

    // Lấy thông tin từ các input trong modal
    const policyNo = $('#inputPolicyNo').val();
    const issueDate = $('#inputIssueDate').val();
    const note = $('#inputPolicyNote').val();
    const beneficiaryId = $('#beneficiarySelect').val();  // Nếu chọn người thụ hưởng

    // Kiểm tra nếu người dùng không nhập đầy đủ thông tin
    if (!policyNo || !issueDate) {
        alert("Vui lòng điền đầy đủ thông tin hợp đồng.");
        return;
    }

    // Dữ liệu gửi đi từ frontend đến backend
    const payload = {
        appId: appId,
        policyNo: policyNo,
        issueDate: issueDate,
        notes: note,
        beneficiaryId: beneficiaryId  // Nếu bạn chọn người thụ hưởng
    };

    // Gửi yêu cầu POST đến server
    $.ajax({
        url: '/Policies/CreateFromApplication', // Địa chỉ API tạo hợp đồng
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (response) {
            if (response.success) {
                // Hiển thị thông báo thành công và reload trang
                localStorage.setItem("toastAfterReload", JSON.stringify({
                    type: "success",
                    message: response.message
                }));

                // Đóng modal và reload lại trang sau 1 giây
                setTimeout(() => location.reload(), 1000);
            } else {
                // Hiển thị thông báo lỗi nếu không thành công
                alert(response.message);
            }
        },
        error: function () {
            alert("Có lỗi xảy ra trong quá trình tạo hợp đồng.");
        }
    });
}
function confirmCreatePolicy() {
    // Lấy AppId từ modal
    const appId = $('#modalPolicyAppNo').text().split(":")[1].trim();
    createPolicy(appId);
}

function confirmPremium(appId) {
    window.confirmAction({
        title: "Khách hàng xác nhận phí?",
        message: "Bạn xác nhận rằng khách hàng đã đồng ý mức phí.",
        icon: "question",

        onConfirm: async () => {
            try {
                const res = await fetch("/PolicyApplications/ConfirmPremium", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ id: appId })
                });

                const text = await res.text();

                let json;
                try {
                    json = JSON.parse(text);
                } catch {
                    console.error("Server trả về không phải JSON:", text);
                    return;
                }

                if (json.success) {
                    // ============================
                    // LƯU LOG
                    // ============================
                    const logKey = "premiumConfirmLog";
                    const oldLog = JSON.parse(localStorage.getItem(logKey) || "[]");

                    oldLog.push({
                        appId: appId,
                        message: json.message,
                        time: new Date().toISOString()
                    });

                    localStorage.setItem(logKey, JSON.stringify(oldLog));

                    // ============================
                    // LƯU TOAST ĐỂ HIỂN THỊ SAU RELOAD
                    // ============================
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: json.message
                    }));

                    // Reload trang
                    setTimeout(() => location.reload(), 700);
                } else {
                    console.error("Xác nhận phí thất bại:", json.message);
                }

            } catch (err) {
                console.error("Lỗi kết nối:", err);
            }
        }
    });
}

function collectFirstPremium(appId) {
    window.confirmAction({
        title: "Thu phí bảo hiểm đầu tiên?",
        message: "Xác nhận đã thu phí lần đầu từ khách hàng?",
        icon: "question",
        confirmButtonText: "Xác nhận",

        onConfirm: async () => {
            try {
                const res = await fetch("/PolicyApplications/CollectFirstPremium", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({ Id: appId })
                });

                let text = await res.text();

                let data;
                try {
                    data = JSON.parse(text);
                } catch {
                    console.error("Server trả về không phải JSON:", text);
                    window.showToast("error", text);
                    return;
                }

                if (data.success) {
                    // Lưu toast vào localStorage
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message
                    }));

                    setTimeout(() => location.reload(), 800);
                } else {
                    window.showToast("error", data.message);
                }

            } catch (err) {
                console.error(err);
                window.showToast("error", "Lỗi hệ thống!");
            }
        }
    });
}
