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
