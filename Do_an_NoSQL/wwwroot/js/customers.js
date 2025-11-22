document.addEventListener("DOMContentLoaded", function () {

    // ================================
    // QUẢN LÝ CHECKBOX QUA NHIỀU TRANG
    // ================================
    let selectedAll = false;
    let selectedIds = new Set(JSON.parse(localStorage.getItem('selectedCustomerIds') || '[]'));
    let excludedIds = new Set(JSON.parse(localStorage.getItem('excludedCustomerIds') || '[]'));

    const checkAll = document.getElementById("checkAll");
    const rowCheckboxes = document.querySelectorAll(".row-check");

    function restoreCheckboxState() {
        selectedAll = localStorage.getItem('selectAllCustomer') === 'true';
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
            localStorage.setItem('selectAllCustomer', 'true');
            localStorage.setItem('excludedCustomerIds', JSON.stringify([...excludedIds]));
        } else {
            localStorage.removeItem('selectAllCustomer');
            localStorage.removeItem('excludedCustomerIds');
            localStorage.setItem('selectedCustomerIds', JSON.stringify([...selectedIds]));
        }
    }

    if (checkAll) {
        checkAll.addEventListener("change", function () {
            if (this.checked) {
                selectedAll = true;
                excludedIds.clear();
                selectedIds.clear();
                localStorage.setItem('selectAllCustomer', 'true');
                rowCheckboxes.forEach(cb => cb.checked = true);
            } else {
                selectedAll = false;
                excludedIds.clear();
                selectedIds.clear();
                localStorage.removeItem('selectAllCustomer');
                localStorage.removeItem('selectedCustomerIds');
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

    window.getSelectedCustomerIds = function () {
        const checked = Array.from(document.querySelectorAll(".row-check:checked")).map(c => c.value);
        return localStorage.getItem('selectAllCustomer') === 'true'
            ? null
            : (checked.length ? checked : Array.from(selectedIds));
    };

    window.clearCustomerSelection = function () {
        selectedAll = false;
        selectedIds.clear();
        excludedIds.clear();
        localStorage.removeItem('selectAllCustomer');
        localStorage.removeItem('selectedCustomerIds');
        localStorage.removeItem('excludedCustomerIds');
    };

    // ================================
    // BỘ LỌC TỰ ĐỘNG SUBMIT
    // ================================
    const filterForm = document.getElementById("filterForm");
    if (filterForm) {
        filterForm.querySelectorAll("select, input[type=date], input[name='search']")
            .forEach(el => {
                el.addEventListener("change", () => {
                    window.clearCustomerSelection();
                    filterForm.submit();
                });
            });
    }

    // ================================
    // IMPORT EXCEL KHÁCH HÀNG 
    // ================================
    window.handleImportCustomerExcel = async function () {
        const fileInput = document.getElementById("importCustomerInput");
        const file = fileInput.files[0];

        if (!file) {
            window.showToast("warning", "Vui lòng chọn tệp Excel!");
            return;
        }

        const validExtensions = [".xlsx", ".xls"];
        const fileName = file.name.toLowerCase();
        if (!validExtensions.some(ext => fileName.endsWith(ext))) {
            window.showToast("error", "File không hợp lệ! Chỉ chấp nhận .xlsx hoặc .xls");
            return;
        }

        Swal.fire({
            title: "Đang xử lý file...",
            html: "Vui lòng đợi trong giây lát",
            didOpen: () => Swal.showLoading(),
            allowOutsideClick: false
        });

        try {
            const formData = new FormData();
            formData.append("file", file);

            const res = await fetch("/Customers/ImportExcel", {
                method: "POST",
                body: formData
            });

            const data = await res.json();
            Swal.close();

            if (data.success) {
                localStorage.setItem("toastAfterReload", JSON.stringify({
                    type: "success",
                    message: data.message
                }));
                document.getElementById("importCustomerForm").reset();
                location.reload();
            } else {
                const detailErrors = data.errors ? "<br>" + data.errors.map(e => `• ${e}`).join("<br>") : "";
                localStorage.setItem("toastAfterReload", JSON.stringify({
                    type: "error",
                    message: `${data.message}${detailErrors}`
                }));
                location.reload();
            }
        } catch (err) {
            Swal.close();
            console.error(err);
            localStorage.setItem("toastAfterReload", JSON.stringify({
                type: "error",
                message: "Không thể kết nối máy chủ!"
            }));
            location.reload();
        }
    };


    document.getElementById("importCustomerBtn")?.addEventListener("click", () => {
        document.getElementById("importCustomerInput").click();
    });

    document.getElementById("importCustomerInput")?.addEventListener("change", e => {
        const fileName = e.target.files[0] ? e.target.files[0].name : "Chưa chọn tệp";
        document.getElementById("selectedCustomerFile").textContent = fileName;
    });

    // ================================
    // EXPORT EXCEL KHÁCH HÀNG
    // ================================
    const exportBtn = document.getElementById("exportCustomerExcelBtn");
    if (exportBtn) {
        exportBtn.addEventListener("click", function () {
            const selectAllMode = localStorage.getItem('selectAllCustomer') === 'true';
            const excludedIdsArray = Array.from(new Set(JSON.parse(localStorage.getItem('excludedCustomerIds') || '[]')));
            let queryString = '';

            if (selectAllMode) {
                if (excludedIdsArray.length > 0)
                    queryString = `excludeIds=${excludedIdsArray.join(',')}`;

                const filters = {
                    search: document.querySelector("input[name='search']")?.value || '',
                    gender: document.querySelector("select[name='gender']")?.value || '',
                    source: document.querySelector("select[name='source']")?.value || '',
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
                const selectedIds = window.getSelectedCustomerIds();
                if (!selectedIds || selectedIds.length === 0) {
                    window.showToast("warning", "Vui lòng chọn ít nhất một khách hàng để xuất!");
                    return;
                }
                queryString = `ids=${selectedIds.join(',')}`;
            }

            window.location.href = '/Customers/ExportExcel' + (queryString ? '?' + queryString : '');
        });
    }

});


// ================================
// XÓA KHÁCH HÀNG ĐƠN LẺ
// ================================
window.confirmDeleteCustomer = function (customerId) {
    window.confirmAction({
        title: 'Xác nhận xóa',
        message: 'Bạn có chắc chắn muốn xóa khách hàng này không?',
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
                const res = await fetch(`/Customers/Delete/${customerId}`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" }
                });

                if (res.ok) {
                    const data = await res.json();

                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message || "Khách hàng đã được xóa thành công!"
                    }));

                    window.clearCustomerSelection();
                    location.reload();
                } else {
                    Swal.close();
                    window.showToast("error", "Không thể xóa khách hàng. Vui lòng thử lại.");
                }
            } catch (err) {
                Swal.close();
                window.showToast("error", "Lỗi kết nối đến máy chủ.");
            }
        }
    });
};

// ================================
// XÓA HÀNG LOẠT KHÁCH HÀNG
// ================================
window.bulkDeleteCustomers = async function () {
    const selectAllMode = localStorage.getItem('selectAllCustomer') === 'true';
    const selectedIds = window.getSelectedCustomerIds();
    const excludedIds = new Set(JSON.parse(localStorage.getItem('excludedCustomerIds') || '[]'));

    if (!selectAllMode && (!selectedIds || selectedIds.length === 0)) {
        window.showToast("warning", "Vui lòng chọn ít nhất 1 khách hàng để xóa!");
        return;
    }

    let countText = selectAllMode
        ? `toàn bộ khách hàng${excludedIds.size > 0 ? ` (trừ ${excludedIds.size} khách hàng)` : ""}`
        : `${selectedIds.length} khách hàng`;

    const message = `Bạn có chắc chắn muốn xóa <strong>${countText}</strong> không?`;

    window.confirmAction({
        title: "Xác nhận hành động",
        message: message,
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
                let url = "/Customers/BulkDelete";
                let body;

                if (selectAllMode) {
                    url += "?deleteAll=true";
                    body = JSON.stringify({ excludeIds: [...excludedIds] });
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

                    window.clearCustomerSelection();
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message || "Xóa khách hàng thành công!"
                    }));
                    location.reload();
                } else {
                    Swal.close();
                    window.showToast("error", "Không thể xóa khách hàng. Vui lòng thử lại.");
                }
            } catch (err) {
                Swal.close();
                window.showToast("error", "Kết nối đến máy chủ thất bại.");
            }
        }
    });
};


// ================================
//CREATE
// ================================
document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById('customerForm');
    if (!form) return;

    form.addEventListener('submit', function (e) {
        e.preventDefault(); // Ngừng hành động submit mặc định

        const formData = new FormData(form);

        fetch(form.action, {
            method: "POST",
            body: formData,
        })
            .then(response => response.json())
            .then(data => {
                console.log(data);

                if (data.success) {
                    localStorage.setItem('toastAfterReload', JSON.stringify({
                        type: 'success',
                        message: data.message || 'Tạo khách hàng thành công!'
                    }));

                    window.location.href = '/Customers/Index';
                } else {
                    localStorage.setItem('toastAfterReload', JSON.stringify({
                        type: 'error',
                        message: data.message || 'Dữ liệu không hợp lệ, vui lòng kiểm tra lại!'
                    }));
                    location.reload();
                }
            })
            .catch(error => {
                console.error(error);

                localStorage.setItem('toastAfterReload', JSON.stringify({
                    type: 'error',
                    message: 'Có lỗi xảy ra khi lưu khách hàng, vui lòng thử lại sau.'
                }));
                location.reload();
            });
    });
});

document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById('customerForm');
    if (!form) return;

    form.addEventListener('submit', function (e) {
        e.preventDefault();

        const name = form.querySelector("[name='FullName']").value.trim();
        if (!name) {
            window.showToast("warning", "Vui lòng nhập tên khách hàng!");
            return;
        }

        const formData = new FormData(form);

        fetch(form.action, {
            method: "POST",
            body: formData,
        })
            .then(response => response.json())
            .then(data => {
                console.log(data);

                if (data.success) {
                    localStorage.setItem('toastAfterReload', JSON.stringify({
                        type: 'success',
                        message: data.message || 'Cập nhật khách hàng thành công!'
                    }));

                    window.location.href = '/Customers/Index';
                } else {
                    localStorage.setItem('toastAfterReload', JSON.stringify({
                        type: 'error',
                        message: data.message || 'Không thể cập nhật, vui lòng kiểm tra lại!'
                    }));
                    location.reload();
                }
            })
            .catch(error => {
                console.error(error);
                localStorage.setItem('toastAfterReload', JSON.stringify({
                    type: 'error',
                    message: 'Lỗi kết nối máy chủ, vui lòng thử lại sau.'
                }));
                location.reload();
            });
    });
});

window.viewCustomerDetails = async function (customerId) {
    Swal.fire({
        title: "Đang tải chi tiết...",
        didOpen: () => Swal.showLoading(),
        allowOutsideClick: false
    });

    try {
        const res = await fetch(`/Customers/GetCustomerDetails?id=${customerId}`);
        const data = await res.json();
        Swal.close();

        if (!data.success || !data.customer) {
            window.showToast("error", data.message || "Không thể tải thông tin khách hàng!");
            return;
        }

        const c = data.customer;

        // Thông tin chung
        document.getElementById("customerDetailHeader").innerText = `${c.full_name} (${c.customer_code})`;
        document.getElementById("detailCustomerCode").value = c.customer_code || "-";
        document.getElementById("detailCustomerName").value = c.full_name || "-";
        document.getElementById("detailDob").value = c.dob ? new Date(c.dob).toLocaleDateString("vi-VN") : "-";
        document.getElementById("detailGender").value = c.gender === "M" ? "Nam" : "Nữ";
        document.getElementById("detailNationalId").value = c.national_id || "-";
        document.getElementById("detailPhone").value = c.phone || "-";
        document.getElementById("detailEmail").value = c.email || "-";
        document.getElementById("detailOccupation").value = c.occupation || "-";
        document.getElementById("detailIncome").value = c.income?.toLocaleString("vi-VN") + " ₫" || "-";
        document.getElementById("detailAddress").value = c.address || "-";
        document.getElementById("detailSource").value = c.source || "-";
        document.getElementById("detailHealth").value = c.health_info || "-";
        document.getElementById("detailCreatedAt").value = c.created_at ? new Date(c.created_at).toLocaleDateString("vi-VN") : "-";

        // Thống kê
        document.getElementById("statApplications").innerText = c.policy_applications || 0;
        document.getElementById("statPolicies").innerText = c.policies || 0;
        document.getElementById("statClaims").innerText = c.claims || 0;
        document.getElementById("statTotalPaid").innerText = c.total_paid?.toLocaleString("vi-VN") + " ₫" || "0 ₫";

        new bootstrap.Modal(document.getElementById("customerDetailModal")).show();
    } catch (err) {
        Swal.close();
        console.error(err);
        window.showToast("error", "Không thể kết nối máy chủ!");
    }
};
