using Do_an_NoSQL.Database;
using System.Security.Claims;

namespace Do_an_NoSQL.Helpers
{
    public static class RoleHelper
    {
        public const string ADMIN = "ADMIN";
        public const string ADVISOR = "ADVISOR";
        public const string UNDERWRITER = "UNDERWRITER";
        public const string ACCOUNTANT = "ACCOUNTANT";
        public const string CSKH = "CSKH";

        public static bool HasRole(ClaimsPrincipal user, params string[] roles)
        {
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            return userRole != null && roles.Contains(userRole);
        }

        // === CUSTOMER ===
        public static bool CanAccessCustomers(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR);
        }

        // === APPLICATION ===
        public static bool CanAccessApplications(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER);
        }

        public static bool CanApproveApplications(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, UNDERWRITER);
        }

        // === POLICY ===
        public static bool CanAccessPolicies(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER);
        }

        // === CLAIM ===
        public static bool CanAccessClaims(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, UNDERWRITER, CSKH);
        }

        // === PAYMENT ===
        public static bool CanViewPayment(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR, ACCOUNTANT, CSKH);
        }

        public static bool CanManagePayment(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ACCOUNTANT);
        }

        public static bool CanAccessPayments(ClaimsPrincipal user)
        {
            return CanViewPayment(user);
        }

        public static bool CanAccessScheduleTab(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN);
        }

        public static bool CanAccessHistoryTab(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR, ACCOUNTANT, CSKH);
        }

        public static bool CanAccessPayoutTab(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ACCOUNTANT, CSKH);
        }

        // === PRODUCTS ===
        // ✅ ADVISOR CÓ THỂ XEM PRODUCTS
        public static bool CanViewProducts(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR);
        }

        // ✅ CHỈ ADMIN QUẢN LÝ PRODUCTS
        public static bool CanManageProducts(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN);
        }

        // ✅ Giữ lại method cũ để tương thích
        public static bool CanAccessProducts(ClaimsPrincipal user)
        {
            return CanViewProducts(user);
        }

        // === ADVISORS ===
        public static bool CanAccessAdvisors(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN);
        }

        // === USERS ===
        public static bool CanManageUsers(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN);
        }
    }
}