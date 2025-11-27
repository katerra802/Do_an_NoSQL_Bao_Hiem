window.viewPaymentDetail = async function (id) {
    try {
        const res = await fetch(`/Payments/GetPaymentDetail/${id}`);
        if (!res.ok) {
            window.showToast("error", "Không thể tải chi tiết giao dịch!");
            return;
        }

        const data = await res.json();
        document.getElementById("detailPolicy").value = data.policy_no || "-";
        document.getElementById("detailDueDate").value = data.due_date
            ? new Date(data.due_date).toLocaleDateString('vi-VN') : "-";
        document.getElementById("detailPaidDate").value = data.paid_date
            ? new Date(data.paid_date).toLocaleDateString('vi-VN') : "-";
        document.getElementById("detailAmount").value = data.amount?.toLocaleString('vi-VN') || "0";
        document.getElementById("detailChannel").value = translatePaymentChannel(data.channel);
        document.getElementById("detailStatus").value = translatePaymentStatus(data.status);
        document.getElementById("detailRef").value = data.reference || "-";
        document.getElementById("detailPaymentType").value =
            translatePaymentType(data.payment_type);  // ✅ THÊM
        document.getElementById("detailPenalty").value =
            data.penalty_amount?.toLocaleString('vi-VN') || "0";  // ✅ THÊM

        new bootstrap.Modal(document.getElementById("paymentDetailModal")).show();
    } catch (err) {
        console.error(err);
        window.showToast("error", "Lỗi kết nối đến máy chủ!");
    }
};

window.viewPayoutDetail = async function (id) {
    if (!id) return;

    Swal.fire({
        title: "Đang tải...",
        didOpen: () => Swal.showLoading(),
        allowOutsideClick: false
    });

    try {
        const res = await fetch(`/Payments/GetPayoutDetail/${id}`);
        if (!res.ok) throw new Error("Fetch error");

        const data = await res.json();
        Swal.close();

        // Gán dữ liệu vào modal
        document.getElementById("payoutClaimNoHeader").innerText = `Yêu cầu: ${data.claim_no}`;
        document.getElementById("payoutClaimNo").value = data.claim_no || "-";
        document.getElementById("payoutPolicyNo").value = data.policy_no || "-";
        document.getElementById("payoutRequestedAmount").value = data.requested_amount?.toLocaleString("vi-VN") || "-";
        document.getElementById("payoutApprovedAmount").value = data.approved_amount?.toLocaleString("vi-VN") || "-";
        document.getElementById("payoutPaidAmount").value = data.paid_amount?.toLocaleString("vi-VN") || "-";
        document.getElementById("payoutMethod").value = translatePaymentMethod(data.pay_method);
        document.getElementById("payoutDate").value = data.paid_at
            ? new Date(data.paid_at).toLocaleDateString("vi-VN")
            : "-";
        document.getElementById("payoutReference").value = data.reference || "-";

        // Mở modal
        const modal = new bootstrap.Modal(document.getElementById("payoutDetailModal"));
        modal.show();
    } catch (err) {
        Swal.close();
        window.showToast("error", "Không thể tải dữ liệu chi tiết chi trả.");
    }
};
function translatePaymentMethod(method) {
    switch (method) {
        case "bank_transfer": return "Chuyển khoản ngân hàng";
        case "cash": return "Tiền mặt";
        case "credit_card": return "Thẻ tín dụng";
        case "online": return "Cổng thanh toán trực tuyến";
        default: return method || "-";
    }
}

function translatePaymentType(type) {
    const map = {
        "normal": "Thanh toán thường",
        "penalty": "Có phí phạt",
        "late_fee": "Phí trễ hạn"
    };
    return map[type] || type;
}

// ✅ THÊM FUNCTION MỚI - Quick Payment
window.quickPayment = async function (paymentId, policyNo, amount) {
    const { value: formValues } = await Swal.fire({
        title: '<strong>Xác nhận thanh toán</strong>',
        html: `
            <div class="text-start">
                <div class="mb-3">
                    <label class="form-label fw-semibold">Mã hợp đồng:</label>
                    <input type="text" class="form-control" value="${policyNo}" readonly>
                </div>
                <div class="mb-3">
                    <label class="form-label fw-semibold">Số tiền:</label>
                    <input type="text" class="form-control" value="${parseFloat(amount).toLocaleString('vi-VN')} VNĐ" readonly>
                </div>
                <div class="mb-3">
                    <label class="form-label fw-semibold">Kênh thanh toán <span class="text-danger">*</span></label>
                    <select id="swal-channel" class="form-select">
                        <option value="">-- Chọn kênh --</option>
                        <option value="admin">Admin</option>
                        <option value="bank">Ngân hàng</option>
                        <option value="agent">Tư vấn viên</option>
                        <option value="office">Văn phòng</option>
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label fw-semibold">Phương thức thanh toán <span class="text-danger">*</span></label>
                    <select id="swal-method" class="form-select">
                        <option value="">-- Chọn phương thức --</option>
                        <option value="cash">Tiền mặt</option>
                        <option value="bank_transfer">Chuyển khoản</option>
                        <option value="credit_card">Thẻ tín dụng</option>
                        <option value="online">Thanh toán online</option>
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label fw-semibold">Mã tham chiếu</label>
                    <input type="text" id="swal-reference" class="form-control" placeholder="Nhập mã tham chiếu (tùy chọn)">
                </div>
            </div>
        `,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: '<i class="fa-solid fa-check"></i> Xác nhận thanh toán',
        cancelButtonText: '<i class="fa-solid fa-xmark"></i> Hủy',
        confirmButtonColor: '#E31E24',
        cancelButtonColor: '#6c757d',
        width: '600px',
        preConfirm: () => {
            const channel = document.getElementById('swal-channel').value;
            const method = document.getElementById('swal-method').value;
            const reference = document.getElementById('swal-reference').value;

            if (!channel || !method) {
                Swal.showValidationMessage('Vui lòng chọn đầy đủ kênh và phương thức thanh toán!');
                return false;
            }

            return { channel, method, reference };
        }
    });

    if (!formValues) return;

    Swal.fire({
        title: "Đang xử lý thanh toán...",
        didOpen: () => Swal.showLoading(),
        allowOutsideClick: false
    });

    try {
        const res = await fetch('/Payments/QuickPay', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({
                paymentId: paymentId,
                channel: formValues.channel,
                payMethod: formValues.method,
                reference: formValues.reference || null
            })
        });

        const data = await res.json();
        await Swal.close();

        // ✅ NẾU BACKEND TRẢ VỀ reload = true → RELOAD VÀ HIỆN TOAST TỪ TEMPDATA
        if (data.reload) {
            location.reload();
        }
    } catch (err) {
        await Swal.close();
        console.error('Payment error:', err);
        window.showToast('error', 'Lỗi kết nối đến máy chủ. Vui lòng thử lại!');
    }
};

// ✅ THÊM HELPER - Translate payment type
function translatePaymentType(type) {
    const map = {
        "initial": "Phí đầu kỳ",
        "normal": "Thanh toán thường",
        "penalty": "Có phí phạt",
        "late_fee": "Phí trễ hạn"
    };
    return map[type] || type || "-";
}


document.addEventListener("DOMContentLoaded", () => {
    const forms = ["#filterForm", "#filterForm2"];
    forms.forEach(formId => {
        const form = document.querySelector(formId);
        if (!form) return;

        // 🔹 Chỉ submit khi người dùng RỜI khỏi ô ngày
        form.querySelectorAll("input[name='from_date'], input[name='to_date']")
            .forEach(input => {
                let oldValue = input.value;
                input.addEventListener("blur", () => {
                    if (input.value !== oldValue) {
                        oldValue = input.value;
                        form.submit();
                    }
                });
            });

        // 🔹 Tự submit khi rời khỏi ô tìm kiếm (và có thay đổi)
        const searchInput = form.querySelector("input[name='search']");
        if (searchInput) {
            let oldValue = searchInput.value;
            searchInput.addEventListener("blur", () => {
                if (searchInput.value.trim() !== oldValue.trim()) {
                    oldValue = searchInput.value.trim();
                    form.submit();
                }
            });
        }
    });
});
