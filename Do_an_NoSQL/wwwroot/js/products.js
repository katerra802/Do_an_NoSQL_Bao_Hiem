document.addEventListener("DOMContentLoaded", function () {

    // ================================
    // QUẢN LÝ CHECKBOX QUA NHIỀU TRANG
    // ================================
    let selectedAll = false;
    let selectedIds = new Set(JSON.parse(localStorage.getItem('selectedProductIds') || '[]'));
    let excludedIds = new Set(JSON.parse(localStorage.getItem('excludedProductIds') || '[]'));

    const checkAll = document.getElementById("checkAll");
    const rowCheckboxes = document.querySelectorAll(".row-check");

    function restoreCheckboxState() {
        selectedAll = localStorage.getItem('selectAllProduct') === 'true';
        if (selectedAll) {
            checkAll.checked = true;
            rowCheckboxes.forEach(cb => cb.checked = !excludedIds.has(cb.value));
        } else {
            rowCheckboxes.forEach(cb => {
                cb.checked = selectedIds.has(cb.value);
            });
            checkAll.checked = Array.from(rowCheckboxes).every(cb => cb.checked);
        }
    }

    function saveCheckboxState() {
        if (selectedAll) {
            localStorage.setItem('selectAllProduct', 'true');
            localStorage.setItem('excludedProductIds', JSON.stringify([...excludedIds]));
        } else {
            localStorage.removeItem('selectAllProduct');
            localStorage.removeItem('excludedProductIds');
            localStorage.setItem('selectedProductIds', JSON.stringify([...selectedIds]));
        }
    }

    if (checkAll) {
        checkAll.addEventListener("change", function () {
            if (this.checked) {
                selectedAll = true;
                excludedIds.clear();
                selectedIds.clear();
                localStorage.setItem('selectAllProduct', 'true');
                rowCheckboxes.forEach(cb => cb.checked = true);
            } else {
                selectedAll = false;
                excludedIds.clear();
                selectedIds.clear();
                localStorage.removeItem('selectAllProduct');
                localStorage.removeItem('selectedProductIds');
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
    window.getSelectedProductIds = function () {
        const checked = Array.from(document.querySelectorAll(".row-check:checked")).map(c => c.value);
        return localStorage.getItem('selectAllProduct') === 'true' ? null : checked.length ? checked : Array.from(selectedIds);
    };

    window.clearProductSelection = function () {
        selectedAll = false;
        selectedIds.clear();
        excludedIds.clear();
        localStorage.removeItem('selectAllProduct');
        localStorage.removeItem('selectedProductIds');
        localStorage.removeItem('excludedProductIds');
    };

    // ================================
    // BỘ LỌC TỰ ĐỘNG SUBMIT
    // ================================
    const filterForm = document.getElementById("filterForm");
    if (filterForm) {
        filterForm.querySelectorAll("select, input[type=date], input[name='sum_assured_value'], input[name='age_value']")
            .forEach(el => {
                el.addEventListener("change", () => {
                    window.clearProductSelection();
                    filterForm.submit();
                });
            });
    }

    // ================================
    // XUẤT EXCEL
    // ================================
    const exportBtn = document.getElementById("exportExcelBtn");
    if (exportBtn) {
        exportBtn.addEventListener("click", function () {
            const selectAllMode = localStorage.getItem('selectAllProduct') === 'true';
            const excludedIdsArray = Array.from(new Set(JSON.parse(localStorage.getItem('excludedProductIds') || '[]')));
            let queryString = '';

            if (selectAllMode) {
                if (excludedIdsArray.length > 0) {
                    queryString = `excludeIds=${excludedIdsArray.join(',')}`;
                }

                const filters = {
                    search: document.querySelector("input[name='search']")?.value || '',
                    type: document.querySelector("select[name='type']")?.value || '',
                    sum_assured_value: document.querySelector("input[name='sum_assured_value']")?.value || '',
                    age_value: document.querySelector("input[name='age_value']")?.value || ''
                };

                const params = [];
                for (const key in filters) {
                    if (filters[key]) params.push(`${key}=${encodeURIComponent(filters[key])}`);
                }

                if (params.length > 0)
                    queryString += (queryString ? '&' : '') + params.join('&');

                queryString += (queryString ? '&' : '') + 'exportAll=true';
            } else {
                const selectedIds = window.getSelectedProductIds();
                if (!selectedIds || selectedIds.length === 0) {
                    window.showToast("warning", "Vui lòng chọn ít nhất một sản phẩm để xuất!");
                    return;
                }
                queryString = `ids=${selectedIds.join(',')}`;
            }

            window.location.href = '/Products/ExportExcel' + (queryString ? '?' + queryString : '');
        });
    }

});



// ================================
// XÓA SẢN PHẨM ĐƠN LẺ
// ================================
window.confirmDeleteProduct = function (productId) {
    window.confirmAction({
        title: 'Xác nhận xóa',
        message: 'Bạn có chắc chắn muốn xóa sản phẩm này không?',
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
                const res = await fetch(`/Products/Delete/${productId}`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" }
                });

                if (res.ok) {
                    const data = await res.json();

                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: data.success ? "success" : "error",
                        message: data.message
                    }));

                    window.clearProductSelection();
                    location.reload();
                } else {
                    Swal.close();
                    window.showToast("error", "Không thể xóa sản phẩm. Vui lòng thử lại.");
                }
            } catch (err) {
                Swal.close();
                window.showToast("error", "Lỗi kết nối đến máy chủ.");
            }
        }
    });
};

// ================================
// XÓA HÀNG LOẠT SẢN PHẨM
// ================================
window.bulkDeleteProducts = async function () {
    const selectAllMode = localStorage.getItem('selectAllProduct') === 'true';
    const selectedIds = window.getSelectedProductIds();
    const excludedIds = new Set(JSON.parse(localStorage.getItem('excludedProductIds') || '[]'));

    if (!selectAllMode && (!selectedIds || selectedIds.length === 0)) {
        window.showToast("warning", "Vui lòng chọn ít nhất 1 sản phẩm để xóa!");
        return;
    }

    let countText = selectAllMode
        ? `toàn bộ sản phẩm${excludedIds.size > 0 ? ` (trừ ${excludedIds.size} sản phẩm)` : ""}`
        : `${selectedIds.length} sản phẩm`;

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
                let url = "/Products/BulkDelete";
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

                    window.clearProductSelection();
                    localStorage.setItem("toastAfterReload", JSON.stringify({
                        type: "success",
                        message: data.message || "Xóa sản phẩm thành công!"
                    }));
                    location.reload();
                } else {
                    Swal.close();
                    window.showToast("error", "Không thể xóa sản phẩm. Vui lòng thử lại.");
                }
            } catch (err) {
                Swal.close();
                window.showToast("error", "Kết nối đến máy chủ thất bại.");
            }
        }
    });
};


//CREATE, EDIT
document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("ridersContainer");
    const addBtn = document.getElementById("addRiderBtn");

    // Kiểm tra nếu các phần tử tồn tại (chỉ áp dụng cho trang Create và Edit)
    if (!container || !addBtn) return;  // Nếu không có phần tử, dừng lại ở đây

    // Thêm Rider mới
    addBtn.addEventListener("click", function () {
        const index = container.querySelectorAll(".rider-item").length;
        const html = `
            <div class="rider-item d-flex gap-2 align-items-center mb-2">
                <input type="text" name="Riders[${index}].Code" class="input-input flex-fill" placeholder="Mã Rider" required />
                <input type="text" name="Riders[${index}].Name" class="input-input flex-fill" placeholder="Tên Rider" required />
                <button type="button" class="btn btn-sm btn-outline-danger remove-rider">
                    <i class="fa-solid fa-xmark"></i>
                </button>
            </div>
        `;
        container.insertAdjacentHTML("beforeend", html);
    });

    // Xóa Rider
    container.addEventListener("click", function (e) {
        if (e.target.closest(".remove-rider")) {
            e.target.closest(".rider-item").remove();
        }
    });
});


document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById('productForm');

    // 🔒 Nếu trang không có form (ví dụ Index), thì dừng tại đây
    if (!form) return;

    form.addEventListener('submit', function (e) {
        e.preventDefault(); // Ngừng hành động submit mặc định của form

        const formData = new FormData(form);

        fetch(form.action, {
            method: "POST",
            body: formData,
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    localStorage.setItem('toastAfterReload', JSON.stringify({
                        type: 'success',
                        message: data.message
                    }));
                    window.location.href = '/Products/Index';
                } else {
                    alert(data.message);
                }
            })
            .catch(() => {
                alert("Có lỗi xảy ra khi lưu sản phẩm.");
            });
    });
});

window.viewProductDetails = async function (id) {
    Swal.fire({
        title: "Đang tải chi tiết...",
        didOpen: () => Swal.showLoading(),
        allowOutsideClick: false
    });

    try {
        const res = await fetch(`/Products/GetProductDetail?id=${id}`);
        const data = await res.json();
        Swal.close();

        if (!data.success) {
            window.showToast("error", data.message || "Không thể tải dữ liệu!");
            return;
        }

        const p = data.data;
        $("#modalProductCode").text(`Mã sản phẩm: ${p.productCode}`);

        const purposes = p.purpose?.length
            ? p.purpose.map(x => `<span class="purpose-chip">${x}</span>`).join("")
            : "<span class='text-muted'>Không có thông tin</span>";

        const riders = p.riders?.length
            ? `
                <div class="table-responsive">
                    <table class="table table-bordered align-middle">
                        <thead class="table-light">
                            <tr>
                                <th>Mã sản phẩm bổ trợ</th>
                                <th>Tên sản phẩm bổ trợ</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${p.riders.map(r => `
                                <tr>
                                    <td>${r.code || '-'}</td>
                                    <td>${r.name || '-'}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            `
            : "<p class='text-muted fst-italic'>Không có sản phẩm bổ trợ.</p>";

        const html = `
            <div class="row g-3">
                <div class="col-md-6">
                    <label class="form-label fw-semibold">Tên sản phẩm</label>
                    <input type="text" class="input-input bg-white" value="${p.name || '-'}" readonly>
                </div>
                <div class="col-md-6">
                    <label class="form-label fw-semibold">Loại sản phẩm</label>
                    <input type="text" class="input-input bg-white" value="${translateType(p.type)}" readonly>
                </div>
                <div class="col-md-12">
                    <label class="form-label fw-semibold">Mục đích bảo hiểm</label>
                    <div class="purpose-wrap">${purposes}</div>
                </div>
                <div class="col-md-6">
                    <label class="form-label fw-semibold">Độ tuổi áp dụng</label>
                    <input type="text" class="input-input bg-white" value="${p.minAge} - ${p.maxAge} tuổi" readonly>
                </div>
                <div class="col-md-6">
                    <label class="form-label fw-semibold">Thời hạn hợp đồng (năm)</label>
                    <input type="text" class="input-input bg-white" value="${p.termYears}" readonly>
                </div>
                <div class="col-md-6">
                    <label class="form-label fw-semibold">Số tiền bảo hiểm tối thiểu</label>
                    <input type="text" class="input-input bg-white" value="${p.minSumAssured.toLocaleString('vi-VN')} ₫" readonly>
                </div>
                <div class="col-md-6">
                    <label class="form-label fw-semibold">Số tiền bảo hiểm tối đa</label>
                    <input type="text" class="input-input bg-white" value="${p.maxSumAssured.toLocaleString('vi-VN')} ₫" readonly>
                </div>
                <div class="col-12">
                    <label class="form-label fw-semibold">Tỷ lệ phí bảo hiểm (%)</label>
                    <input type="text" class="input-input bg-white" value="${p.premiumRate}" readonly>
                </div>
                <div class="col-12">
                    <label class="form-label fw-semibold">Sản phẩm bổ trợ (Riders)</label>
                    ${riders}
                </div>
            </div>
        `;

        $("#productDetailBody").html(html);
        $("#productDetailModal").modal("show"); // 👉 Dùng jQuery modal thay vì new bootstrap.Modal
    }
    catch (err) {
        Swal.close();
        console.error(err);
        window.showToast("error", "Lỗi khi tải chi tiết sản phẩm!");
    }
};

function translateType(type) {
    switch (type) {
        case "endowment": return "Bảo hiểm tích lũy";
        case "term": return "Bảo hiểm tử kỳ";
        case "whole_life": return "Bảo hiểm trọn đời";
        case "retirement": return "Bảo hiểm hưu trí";
        case "health": return "Bảo hiểm sức khỏe";
        case "accident": return "Bảo hiểm tai nạn";
        case "education": return "Bảo hiểm giáo dục";
        default: return "Không xác định";
    }
}

// ================================
// IMPORT EXCEL
// ================================
window.handleImportExcel = async function () {
    const fileInput = document.getElementById("excelFile");
    const file = fileInput.files[0];

    if (!file) {
        window.showToast("warning", "Vui lòng chọn file Excel!");
        return;
    }

    // Validate file type
    const validExtensions = ['.xlsx', '.xls'];
    const fileName = file.name.toLowerCase();
    const isValid = validExtensions.some(ext => fileName.endsWith(ext));

    if (!isValid) {
        window.showToast("error", "File không hợp lệ! Chỉ chấp nhận .xlsx hoặc .xls");
        return;
    }

    // Show loading
    Swal.fire({
        title: "Đang xử lý file...",
        html: "Vui lòng đợi trong giây lát",
        didOpen: () => Swal.showLoading(),
        allowOutsideClick: false
    });

    try {
        const formData = new FormData();
        formData.append("file", file);

        const response = await fetch("/Products/ImportExcel", {
            method: "POST",
            body: formData
        });

        const data = await response.json();

        await Swal.close();

        if (data.success) {
            // Đóng modal import
            const importModal = bootstrap.Modal.getInstance(document.getElementById("importModal"));
            if (importModal) importModal.hide();

            // Hiển thị thông báo thành công
            await Swal.fire({
                icon: "success",
                title: "Import thành công!",
                html: `<p class="mb-0">Đã import <strong>${data.count}</strong> sản phẩm vào hệ thống.</p>`,
                confirmButtonText: "OK"
            });

            // Reset form và reload trang
            document.getElementById("importForm").reset();
            location.reload();
        } else {
            // Hiển thị lỗi
            showImportErrors(data);
        }
    } catch (err) {
        Swal.close();
        console.error("Import error:", err);
        window.showToast("error", "Lỗi kết nối đến máy chủ!");
    }
};

// Khi click chọn tệp
document.getElementById("importExcelBtn").addEventListener("click", function () {
    document.getElementById("importExcelInput").click();
});

// Hiển thị tên tệp được chọn
document.getElementById("importExcelInput").addEventListener("change", function (e) {
    const fileName = e.target.files[0] ? e.target.files[0].name : "Chưa chọn tệp";
    document.getElementById("selectedFileName").textContent = fileName;
});

document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("importExcelForm");
    if (!form) return;

    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        const file = document.getElementById("importExcelInput").files[0];
        if (!file) {
            window.showToast("warning", "Vui lòng chọn tệp Excel!");
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        Swal.fire({
            title: "Đang import dữ liệu...",
            didOpen: () => Swal.showLoading(),
            allowOutsideClick: false
        });

        try {
            const res = await fetch("/Products/ImportExcel", {
                method: "POST",
                body: formData
            });

            Swal.close();

            if (!res.ok) {
                window.showToast("error", `Máy chủ trả về lỗi ${res.status}`);
                return;
            }

            const data = await res.json();

            if (data.success) {
                localStorage.setItem("toastAfterReload", JSON.stringify({
                    type: "success",
                    message: data.message || "Import thành công!"
                }));

                // Reload trang
                location.reload();
            }  else {
                window.showToast("error", data.message || "Import thất bại!");
            }
        } catch (err) {
            Swal.close();
            console.error(err);
            window.showToast("error", "Không thể kết nối máy chủ!");
        }
    });
});
