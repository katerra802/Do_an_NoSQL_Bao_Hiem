document.addEventListener("DOMContentLoaded", function () {

    // ================================
    // CHỌN TẤT CẢ & LƯU TRẠNG THÁI
    // ================================
    let selectedIds = new Set();
    const checkAll = document.getElementById("checkAll");
    const rowChecks = document.querySelectorAll(".row-check");

    if (checkAll) {
        checkAll.addEventListener("change", function () {
            rowChecks.forEach(cb => cb.checked = this.checked);
            selectedIds = new Set(this.checked ? Array.from(rowChecks).map(x => x.value) : []);
        });
    }

    rowChecks.forEach(cb => {
        cb.addEventListener("change", function () {
            if (this.checked) selectedIds.add(this.value);
            else selectedIds.delete(this.value);
        });
    });

    // ================================
    // XÓA 1 NGƯỜI DÙNG
    // ================================
    window.confirmDeleteUser = function (id) {
        window.confirmAction({
            title: "Xác nhận xóa",
            message: "Bạn có chắc muốn xóa người dùng này?",
            icon: "warning",
            confirmButtonText: "Xóa",
            dangerText: "Hành động này không thể hoàn tác.",
            onConfirm: async () => {
                Swal.fire({ title: "Đang xóa...", didOpen: () => Swal.showLoading() });
                const res = await fetch(`/Users/Delete`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ id })
                });
                const data = await res.json();
                Swal.close();
                if (data.success) {
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message
                    }));
                    location.reload();
                } else {
                    window.showToast("error", data.message);
                }
            }
        });
    };

    // ================================
    // XÓA HÀNG LOẠT NGƯỜI DÙNG
    // ================================
    window.bulkDeleteUsers = function () {
        if (selectedIds.size === 0) {
            window.showToast("warning", "Vui lòng chọn ít nhất 1 người dùng để xóa!");
            return;
        }

        window.confirmAction({
            title: "Xác nhận xóa hàng loạt",
            message: `Bạn có chắc chắn muốn xóa <strong>${selectedIds.size}</strong> người dùng đã chọn?`,
            icon: "warning",
            dangerText: "Hành động này không thể hoàn tác.",
            confirmButtonText: "Xác nhận",
            onConfirm: async () => {
                Swal.fire({ title: "Đang xóa...", didOpen: () => Swal.showLoading() });

                const res = await fetch("/Users/BulkDelete", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify([...selectedIds])
                });

                const data = await res.json();
                Swal.close();

                if (res.ok) {
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message
                    }));
                    location.reload();
                } else {
                    window.showToast("error", "Không thể xóa người dùng!");
                }
            }
        });
    };

    // ================================
    // CẬP NHẬT TRẠNG THÁI NGƯỜI DÙNG
    // ================================
    window.updateUserStatus = function (id) {
        const options = `
            <option value="active">Hoạt động</option>
            <option value="inactive">Ngưng hoạt động</option>
        `;

        window.confirmAction({
            title: "Cập nhật trạng thái người dùng",
            html: `
                <label class="fw-semibold mb-2">Chọn trạng thái mới:</label>
                <select id="newStatusSelect" class="select-input">${options}</select>
            `,
            icon: "info",
            confirmButtonText: "Cập nhật",
            onConfirm: async () => {
                const newStatus = document.getElementById("newStatusSelect").value;
                Swal.fire({ title: "Đang cập nhật...", didOpen: () => Swal.showLoading() });
                const res = await fetch(`/Users/UpdateStatus`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ id, newStatus })
                });
                const data = await res.json();
                Swal.close();
                if (data.success) {
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message
                    }));
                    location.reload();
                } else {
                    window.showToast("error", data.message);
                }
            }
        });
    };

    // IMPORT EXCEL NGƯỜI DÙNG
    window.handleImportUserExcel = async function () {
        const fileInput = document.getElementById("importUserInput");
        const file = fileInput.files[0];
        if (!file) {
            window.showToast("warning", "Vui lòng chọn tệp Excel!");
            return;
        }

        Swal.fire({ title: "Đang xử lý...", didOpen: () => Swal.showLoading(), allowOutsideClick: false });

        try {
            const formData = new FormData();
            formData.append("file", file);

            const res = await fetch("/Users/ImportExcel", { method: "POST", body: formData });
            const data = await res.json();
            Swal.close();

            localStorage.setItem("toastAfterReload", JSON.stringify({
                type: data.success ? "success" : "error",
                message: data.message
            }));
            location.reload();
        } catch (err) {
            Swal.close();
            window.showToast("error", "Không thể kết nối máy chủ!");
        }
    };

    document.getElementById("importUserBtn")?.addEventListener("click", () => {
        document.getElementById("importUserInput").click();
    });
    document.getElementById("importUserInput")?.addEventListener("change", e => {
        document.getElementById("selectedUserFile").textContent = e.target.files[0]?.name || "Chưa chọn tệp";
    });

    // EXPORT EXCEL NGƯỜI DÙNG
    document.getElementById("exportUserExcelBtn")?.addEventListener("click", function () {
        const checkAll = document.getElementById("checkAll");
        const rowChecks = document.querySelectorAll(".row-check");
        const selected = Array.from(rowChecks).filter(c => c.checked).map(c => c.value);

        let queryString = "";

        if (checkAll && checkAll.checked) {
            // Nếu chọn tất cả
            const unchecked = Array.from(rowChecks).filter(c => !c.checked).map(c => c.value);
            queryString = new URLSearchParams({
                exportAll: true,
                excludeIds: unchecked.join(","),
                search: document.querySelector("input[name='search']").value || '',
                role: document.querySelector("select[name='role']").value || '',
                status: document.querySelector("select[name='status']").value || ''
            }).toString();
        } else if (selected.length > 0) {
            // Nếu chọn vài user
            queryString = new URLSearchParams({
                ids: selected.join(",")
            }).toString();
        } else {
            window.showToast("warning", "Vui lòng chọn ít nhất một người dùng để xuất!");
            return;
        }

        window.location.href = '/Users/ExportExcel?' + queryString;
    });
});

// ================================
// XEM CHI TIẾT NGƯỜI DÙNG
// ================================
window.viewUserDetails = function (userId) {
    $.get(`/Users/GetUserDetails/${userId}`, function (res) {
        if (!res.success) {
            alert(res.message);
            return;
        }

        const u = res.user;

        // Gán thông tin cơ bản
        $('#detailUsername').val(u.username);
        $('#detailFullName').val(u.full_name);
        $('#detailEmail').val(u.email);
        $('#detailRole').val(u.role);
        $('#detailStatus').val(u.status === 'active' ? 'Hoạt động' : 'Ngưng hoạt động');

        // Hiển thị danh sách quyền trong bảng
        const tbody = $('#userPermissionTableBody');
        tbody.empty();

        if (u.permissions && u.permissions.length > 0) {
            u.permissions.forEach(p => {
                tbody.append(`
                        <tr>
                            <td class="text-center">${p.code}</td>
                            <td class="text-center">${p.name}</td>
                            <td class="text-center">${p.module}</td>
                        </tr>
                    `);
            });
        } else {
            tbody.append(`
                    <tr>
                        <td colspan="3" class="text-center text-muted fst-italic">Không có quyền nào</td>
                    </tr>
                `);
        }

        $('#userDetailModal').modal('show');
    }).fail(function () {
        alert('Không thể tải thông tin người dùng!');
    });
};
