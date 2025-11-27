document.addEventListener("DOMContentLoaded", function () {

    // ================================
    // QUẢN LÝ CHECKBOX QUA NHIỀU TRANG
    // ================================
    let selectedAll = false;
    let selectedIds = new Set(JSON.parse(localStorage.getItem('selectedClaimIds') || '[]'));
    let excludedIds = new Set(JSON.parse(localStorage.getItem('excludedClaimIds') || '[]'));

    const checkAll = document.getElementById("checkAll");
    const rowCheckboxes = document.querySelectorAll(".row-check");

    function restoreCheckboxState() {
        selectedAll = localStorage.getItem('selectAllClaims') === 'true';

        if (selectedAll) {
            checkAll.checked = true;
            rowCheckboxes.forEach(cb => cb.checked = !excludedIds.has(cb.value));
        } else {
            rowCheckboxes.forEach(cb => cb.checked = selectedIds.has(cb.value));
            checkAll.checked = Array.from(rowCheckboxes).every(cb => cb.checked);
        }
    }

    function saveCheckboxState() {
        if (selectedAll) {
            localStorage.setItem('selectAllClaims', 'true');
            localStorage.setItem('excludedClaimIds', JSON.stringify([...excludedIds]));
        } else {
            localStorage.removeItem('selectAllClaims');
            localStorage.removeItem('excludedClaimIds');
            localStorage.setItem('selectedClaimIds', JSON.stringify([...selectedIds]));
        }
    }

    if (checkAll) {
        checkAll.addEventListener("change", function () {
            if (this.checked) {
                selectedAll = true;
                excludedIds.clear();
                selectedIds.clear();
                localStorage.setItem('selectAllClaims', 'true');

                rowCheckboxes.forEach(cb => cb.checked = true);
            } else {
                selectedAll = false;
                excludedIds.clear();
                selectedIds.clear();
                localStorage.removeItem('selectAllClaims');
                localStorage.removeItem('selectedClaimIds');

                rowCheckboxes.forEach(cb => cb.checked = false);
            }
        });
    }

    rowCheckboxes.forEach(cb => {
        cb.addEventListener("change", function () {
            if (selectedAll) {
                if (!this.checked) excludedIds.add(this.value);
                else excludedIds.delete(this.value);
            } else {
                if (this.checked) selectedIds.add(this.value);
                else selectedIds.delete(this.value);
            }
            saveCheckboxState();
        });
    });

    restoreCheckboxState();

    // ================================
    // CÁC HÀM HỖ TRỢ
    // ================================
    window.getSelectedClaimIds = function () {
        const checked = Array.from(document.querySelectorAll(".row-check:checked")).map(c => c.value);

        return localStorage.getItem('selectAllClaims') === 'true'
            ? null // null = select all
            : (checked.length ? checked : Array.from(selectedIds));
    };

    window.clearClaimSelection = function () {
        selectedAll = false;
        selectedIds.clear();
        excludedIds.clear();
        localStorage.removeItem('selectAllClaims');
        localStorage.removeItem('selectedClaimIds');
        localStorage.removeItem('excludedClaimIds');
    };

    // ================================
    // BỘ LỌC TỰ ĐỘNG SUBMIT
    // ================================
    const filterForm = document.getElementById("filterForm");
    if (filterForm) {
        filterForm.querySelectorAll("select, input[type=date], input[name='search']")
            .forEach(el => {
                el.addEventListener("change", () => {
                    window.clearClaimSelection();
                    filterForm.submit();
                });
            });
    }

    // ================================
    // XÓA CLAIM ĐƠN LẺ
    // ================================
    window.deleteClaim = function (claimId) {
        window.confirmAction({
            title: 'Xác nhận xóa',
            message: 'Bạn có chắc chắn muốn xóa yêu cầu bồi thường này không?',
            dangerText: "Hành động này không thể hoàn tác.",
            confirmButtonText: 'Xóa',
            cancelButtonText: 'Hủy',
            icon: 'warning',
            onConfirm: async function () {

                Swal.fire({
                    title: "Đang xóa...",
                    didOpen: () => Swal.showLoading(),
                    allowOutsideClick: false
                });

                try {
                    const res = await fetch(`/Claims/Delete/${claimId}`, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" }
                    });

                    if (res.ok) {
                        const data = await res.json();

                        localStorage.setItem("toastAfterReload", JSON.stringify({
                            type: data.success ? "success" : "error",
                            message: data.message
                        }));

                        window.clearClaimSelection();
                        location.reload();
                    } else {
                        Swal.close();
                        window.showToast("error", "Không thể xóa yêu cầu. Vui lòng thử lại.");
                    }
                } catch {
                    Swal.close();
                    window.showToast("error", "Lỗi kết nối đến máy chủ.");
                }
            }
        });
    };

    // ================================
    // XÓA HÀNG LOẠT CLAIMS
    // ================================
    window.bulkDeleteClaims = async function () {
        const selectAllMode = localStorage.getItem('selectAllClaims') === 'true';
        const selectedIds = window.getSelectedClaimIds();

        // Đếm số checkbox đang được chọn trên trang hiện tại
        let checkedCount = document.querySelectorAll(".row-check:checked").length;

        // Nếu đang ở chế độ "chọn tất cả" nhưng chưa có checkbox nào được check (do phân trang)
        if (checkedCount === 0 && selectAllMode) {
            checkedCount = document.querySelectorAll(".row-check").length;
        }

        // Nếu không có gì được chọn => cảnh báo
        if (!selectAllMode && checkedCount === 0) {
            window.showToast("warning", "Vui lòng chọn ít nhất 1 yêu cầu để xóa!");
            return;
        }

        const countText = `${checkedCount} yêu cầu`;

        window.confirmAction({
            title: "Xác nhận xóa hàng loạt",
            message: `Bạn có chắc chắn muốn xóa <strong>${countText}</strong> không?`,
            dangerText: "Hành động này không thể hoàn tác.",
            confirmButtonText: "Xác nhận xóa",
            cancelButtonText: "Hủy",
            icon: "warning",
            onConfirm: async () => {
                Swal.fire({
                    title: "Đang xóa...",
                    didOpen: () => Swal.showLoading(),
                    allowOutsideClick: false
                });

                try {
                    let url = "/Claims/BulkDelete";
                    let body;

                    if (selectAllMode) {
                        url += "?deleteAll=true";
                        body = JSON.stringify({}); // Không cần exclude nữa
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

                        window.clearClaimSelection();
                        localStorage.setItem("toastAfterReload", JSON.stringify({
                            type: data.success ? "success" : "error",
                            message: data.message || "Xóa thành công!"
                        }));

                        location.reload();
                    } else {
                        Swal.close();
                        window.showToast("error", "Không thể xóa. Vui lòng thử lại.");
                    }
                } catch {
                    Swal.close();
                    window.showToast("error", "Kết nối đến máy chủ thất bại.");
                }
            }
        });
    };

});



window.openClaimApprovalModal = async function (claimId, maxAmount = 0) {
    try {
        const res = await fetch(`/Claims/GetClaimDetails?id=${claimId}`);
        const data = await res.json();

        if (!data.success || !data.claim) {
            window.showToast("error", data.message || "Không thể tải thông tin yêu cầu!");
            return;
        }

        const requestedAmount = data.claim.payout?.requested_amount ? Number(data.claim.payout.requested_amount) : 0;

        Swal.fire({
            title: "Phê duyệt yêu cầu bồi thường",
            html: `
                <div class="text-start">
                    <!-- ... existing HTML ... -->
                </div>
            `,
            showCancelButton: true,
            confirmButtonText: "Xác nhận",
            cancelButtonText: "Hủy bỏ",
            reverseButtons: true,
            preConfirm: () => {
                const decision = document.getElementById("decision").value;
                const approvedAmount = parseFloat(document.getElementById("approvedAmount").value || 0);
                const payMethod = document.getElementById("payMethod").value;
                const notes = document.getElementById("notes").value.trim();

                if (decision === "approved" && approvedAmount <= 0) {
                    Swal.showValidationMessage("Vui lòng nhập số tiền bồi thường hợp lệ!");
                    return false;
                }

                return {
                    claimId: claimId,
                    decision: decision,
                    approvedAmount: approvedAmount,
                    payMethod: payMethod,
                    notes: notes
                };
            }
        }).then(async (result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: "Đang xử lý...",
                    didOpen: () => Swal.showLoading(),
                    allowOutsideClick: false
                });

                try {
                    const response = await fetch("/Claims/ApproveClaim", {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(result.value)
                    });

                    const res = await response.json();
                    await Swal.close();

                    // ✅ NẾU BACKEND TRẢ VỀ reload = true → RELOAD VÀ HIỆN TOAST TỪ TEMPDATA
                    if (res.reload) {
                        location.reload();
                    } else if (res.success) {
                        window.showToast("success", res.message);
                        setTimeout(() => location.reload(), 800);
                    } else {
                        window.showToast("error", res.message);
                    }
                } catch (err) {
                    await Swal.close();
                    console.error("Approve error:", err);
                    window.showToast("error", "Lỗi kết nối máy chủ!");
                }
            }
        });
    } catch (err) {
        console.error(err);
        window.showToast("error", "Không thể tải dữ liệu yêu cầu!");
    }
};

function openPayoutModal(claimId, claimNo, approvedAmount, policyNo) {
    $('#payoutClaimId').val(claimId);
    $('#payoutClaimNo').val(claimNo);
    $('#payoutApprovedAmount').val(
        approvedAmount ? Number(approvedAmount).toLocaleString('vi-VN') + ' ₫' : '0 ₫'
    );
    $('#payoutAmount').val(approvedAmount);
    $('#payoutClaimPolicy').text(`Hợp đồng: ${policyNo || 'N/A'}`);
    $('#payoutDate').val(new Date().toISOString().split('T')[0]);
    $('#payoutModal').modal('show');
}

async function confirmPayout() {
    const data = {
        ClaimId: $('#payoutClaimId').val(),
        PaidAmount: parseFloat($('#payoutAmount').val()),
        PayMethod: $('#payoutMethod').val(),
        PaidAt: $('#payoutDate').val(),
        Reference: $('#payoutReference').val()
    };

    // Kiểm tra đầu vào
    if (!data.PaidAmount || !data.PayMethod || !data.PaidAt) {
        window.showToast('warning', 'Vui lòng nhập đủ thông tin chi trả!');
        return;
    }

    // Kiểm tra không vượt quá số tiền được duyệt
    const approvedText = $('#payoutApprovedAmount').val().replace(/[^\d]/g, '');
    const approvedAmount = parseFloat(approvedText);
    if (data.PaidAmount > approvedAmount) {
        window.showToast('warning', 'Số tiền chi trả không được vượt quá số tiền đã duyệt!');
        return;
    }

    // ✅ Sử dụng confirmAction thay vì Swal.fire trực tiếp
    window.confirmAction({
        title: "Xác nhận chi trả",
        message: "Bạn có chắc chắn muốn thực hiện chi trả quyền lợi bảo hiểm cho yêu cầu này?",
        icon: "question",
        confirmButtonText: "Xác nhận chi trả",
        cancelButtonText: "Hủy",
        dangerText: "Hành động này sẽ không thể hoàn tác.",
        onConfirm: async function () {
            Swal.fire({ title: "Đang xử lý...", didOpen: () => Swal.showLoading(), allowOutsideClick: false });

            try {
                const res = await fetch("/Claims/CreatePayout", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(data)
                });

                const result = await res.json();
                Swal.close();

                if (result.success) {
                    window.showToast("success", result.message);
                    $("#payoutModal").modal("hide");
                    setTimeout(() => location.reload(), 1000);
                } else {
                    window.showToast("error", result.message);
                }
            } catch (err) {
                Swal.close();
                window.showToast("error", "Lỗi khi kết nối máy chủ.");
            }
        }
    });
}

window.viewPaymentDetail = async function (id) {
    try {
        const res = await fetch(`/Payments/GetPaymentDetail/${id}`);
        if (!res.ok) return window.showToast("error", "Không thể tải chi tiết giao dịch!");
        const data = await res.json();

        document.getElementById("detailPolicy").value = data.policy_no;
        document.getElementById("detailDueDate").value = new Date(data.due_date).toLocaleDateString('vi-VN');
        document.getElementById("detailPaidDate").value = data.paid_date ? new Date(data.paid_date).toLocaleDateString('vi-VN') : "-";
        document.getElementById("detailAmount").value = data.amount.toLocaleString('vi-VN');
        document.getElementById("detailChannel").value = data.channel || "-";
        document.getElementById("detailRef").value = data.reference || "-";
        document.getElementById("detailStatus").value = data.status;

        new bootstrap.Modal(document.getElementById("paymentDetailModal")).show();
    } catch (e) {
        window.showToast("error", "Lỗi kết nối máy chủ!");
    }
};

window.viewClaimDetail = async function (claimId) {
    Swal.fire({
        title: "Đang tải chi tiết...",
        didOpen: () => Swal.showLoading(),
        allowOutsideClick: false
    });

    try {
        const res = await fetch(`/Claims/GetClaimDetails?id=${claimId}`);
        const data = await res.json();
        Swal.close();

        if (!data.success) {
            window.showToast("error", data.message || "Không thể tải dữ liệu!");
            return;
        }

        const c = data.claim;

        // Điền thông tin cơ bản
        document.getElementById("claimDetailHeader").innerText = `Mã yêu cầu: ${c.claim_no}`;
        document.getElementById("detailClaimNo").value = c.claim_no || "-";
        document.getElementById("detailPolicyNo").value = c.policy_no || "-";
        document.getElementById("detailClaimType").value = c.claim_type || "-";
        document.getElementById("detailSubmittedAt").value = c.submitted_at
            ? new Date(c.submitted_at).toLocaleDateString("vi-VN")
            : "-";
        document.getElementById("detailStatus").value = c.status || "-";
        document.getElementById("detailRequestedAmount").value = (c.payout?.requested_amount || 0).toLocaleString("vi-VN");
        document.getElementById("detailApprovedAmount").value = (c.payout?.approved_amount || 0).toLocaleString("vi-VN");
        document.getElementById("detailNotes").value = c.notes || "Không có ghi chú.";

        // 🔹 Hiển thị người thụ hưởng
        const tbody = document.getElementById("beneficiaryTableBody");
        if (c.beneficiaries && c.beneficiaries.length > 0) {
            const rows = c.beneficiaries.map(b => `
                <tr>
                    <td>${b.full_name || '-'}</td>
                    <td>${b.relation || '-'}</td>
                    <td>${b.share_percent ? b.share_percent + '%' : '-'}</td>
                    <td>${b.national_id || '-'}</td>
                </tr>
            `).join('');
            tbody.innerHTML = rows;
        } else if (c.beneficiary_name) {
            tbody.innerHTML = `
                <tr>
                    <td>${c.beneficiary_name}</td>
                    <td>${c.beneficiary_relation || '-'}</td>
                    <td>${c.share_percent || '-'}</td>
                    <td>${c.national_id || '-'}</td>
                </tr>
            `;
        } else {
            tbody.innerHTML = `
                <tr><td colspan="4" class="text-center text-muted">Không có thông tin người thụ hưởng</td></tr>
            `;
        }

        new bootstrap.Modal(document.getElementById("claimDetailModal")).show();
    } catch (err) {
        Swal.close();
        console.error(err);
        window.showToast("error", "Không thể tải chi tiết yêu cầu!");
    }
};

const exportBtn = document.getElementById("exportExcelBtn");
if (exportBtn) {
    exportBtn.addEventListener("click", function () {
        const selectAllMode = localStorage.getItem('selectAllClaims') === 'true';
        const excludedIdsArray = Array.from(new Set(JSON.parse(localStorage.getItem('excludedClaimIds') || '[]')));
        let queryString = '';

        if (selectAllMode) {
            if (excludedIdsArray.length > 0)
                queryString = `excludeIds=${excludedIdsArray.join(',')}`;

            const filters = {
                search: document.querySelector("input[name='search']")?.value || '',
                status: document.querySelector("select[name='status']")?.value || '',
                claim_type: document.querySelector("select[name='claim_type']")?.value || '',
                from_date: document.querySelector("input[name='from_date']")?.value || '',
                to_date: document.querySelector("input[name='to_date']")?.value || ''
            };

            const params = [];
            for (const key in filters) {
                if (filters[key]) params.push(`${key}=${encodeURIComponent(filters[key])}`);
            }

            if (params.length > 0)
                queryString += (queryString ? '&' : '') + params.join('&');

            queryString += (queryString ? '&' : '') + 'exportAll=true';
        } else {
            const selectedIds = window.getSelectedClaimIds();
            if (!selectedIds || selectedIds.length === 0) {
                window.showToast("warning", "Vui lòng chọn ít nhất một yêu cầu để xuất!");
                return;
            }
            queryString = `ids=${selectedIds.join(',')}`;
        }

        window.location.href = '/Claims/ExportExcel' + (queryString ? '?' + queryString : '');
    });
}
