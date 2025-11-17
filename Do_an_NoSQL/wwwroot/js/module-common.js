// =============================
//  CONFIRM POPUP (GLOBAL)
// =============================
window.confirmAction = function ({
    html,
    message,
    title = "Xác nhận hành động",
    confirmButtonText = "Xác nhận",
    cancelButtonText = "Hủy bỏ",
    icon = "warning",
    iconColor = "#dc2626",
    dangerText,
    preConfirm,
    onConfirm,
}) {
    const contentHtml = html
        ? html
        : `
            <p style="margin-top:4px; margin-bottom: 4px; font-size:15px; color:#444;">
                ${message || "Bạn có chắc chắn muốn thực hiện thao tác này?"}
            </p>
            ${dangerText
            ? `<p style="color:#dc2626; font-weight:500;">${dangerText}</p>`
            : ""
        }
        `;

    Swal.fire({
        title,
        html: contentHtml,
        icon,
        iconColor,
        showCancelButton: true,
        buttonsStyling: false,
        confirmButtonText,
        cancelButtonText,
        reverseButtons: true,
        allowOutsideClick: false,
        customClass: {
            popup: "custom-swal-popup",
            confirmButton: "custom-swal-confirm-btn",
            cancelButton: "custom-swal-cancel-btn",
        },

        // ✅ lấy giá trị trước khi Swal đóng
        preConfirm: preConfirm || (() => {
            const select = document.getElementById("bulkStatusSelect");
            return select ? select.value : null;
        }),
    }).then((result) => {
        if (result.isConfirmed) {
            onConfirm && onConfirm(result.value);
        }
    });
};


/* TOAST */
window.showToast = function (type = "success", message = "Thao tác thành công!") {
    const colors = {
        success: { bg: "#f0fdf4", text: "#14532d", border: "#16a34a" },
        error: { bg: "#fef2f2", text: "#7f1d1d", border: "#dc2626" },
        warning: { bg: "#fffbeb", text: "#78350f", border: "#f59e0b" },
        info: { bg: "#eff6ff", text: "#1e3a8a", border: "#3b82f6" },
    };
    const c = colors[type] || colors.info;

    const Toast = Swal.mixin({
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        background: "white",
        iconColor: c.border,
        customClass: {
            popup: "custom-toast-popup",
            title: "custom-toast-title",
            timerProgressBar: "custom-toast-progress",
        },
        didOpen: (toast) => {
            const style = document.createElement("style");
            style.textContent = `
                @keyframes slideInRight {
                    0% { transform: translateX(100%); opacity: 0; }
                    60% { transform: translateX(-15px); opacity: 1; }
                    80% { transform: translateX(8px); }
                    100% { transform: translateX(0); }
                }
                @keyframes glowProgress {
                    0%,100% { box-shadow: 0 0 6px ${c.border}80; }
                    50% { box-shadow: 0 0 14px ${c.border}cc; }
                }
            `;
            document.head.appendChild(style);
        },
    });

    Toast.fire({
        icon: type,
        title: `<span style="color:${c.text};">${message}</span>`,
        didRender: (toast) => {
            // Apply consistent CSS via classes
            const popup = toast.querySelector(".custom-toast-popup");
            if (popup) {
                popup.style.animation = "slideInRight 0.6s cubic-bezier(.34,1.56,.64,1)";
                popup.style.border = `1px solid ${c.border}55`;
                popup.style.borderLeft = `5px solid ${c.border}`;
                popup.style.borderRadius = "10px";
                popup.style.background = c.bg;
                popup.style.boxShadow = "0 6px 16px rgba(0,0,0,0.08)";
                popup.style.padding = "12px 18px";
                popup.style.display = "flex";
                popup.style.alignItems = "center";
                popup.style.fontFamily = "'Segoe UI', Roboto, sans-serif";
            }

            const title = toast.querySelector(".custom-toast-title");
            if (title) {
                title.style.marginLeft = "10px";
                title.style.fontWeight = "600";
                title.style.fontSize = "15px";
            }

            const progress = toast.querySelector(".custom-toast-progress");
            if (progress) {
                progress.style.height = "4px";
                progress.style.borderRadius = "50px";
                progress.style.background = `linear-gradient(90deg, ${c.border}, ${c.border}aa, ${c.border})`;
                progress.style.animation = "glowProgress 2s ease-in-out infinite";
            }
        },
    });
};

window.addEventListener("DOMContentLoaded", () => {
    const savedToast = localStorage.getItem("toastAfterReload");

    if (savedToast) {
        try {
            const { type, message } = JSON.parse(savedToast);
            window.showToast(type, message); 
        } catch (e) {
            console.error("Lỗi đọc toast sau reload:", e);
        }
        localStorage.removeItem("toastAfterReload");  
    }
});