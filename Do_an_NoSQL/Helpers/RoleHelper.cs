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

        public static bool CanAccessCustomers(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR);
        }

        public static bool CanAccessApplications(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER);
        }

        public static bool CanApproveApplications(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, UNDERWRITER);
        }

        public static bool CanAccessClaims(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, UNDERWRITER, CSKH);
        }

        public static bool CanAccessPolicies(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER);
        }

        public static bool CanAccessProducts(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN);
        }

        public static bool CanAccessAdvisors(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN);
        }

        public static bool CanAccessPayments(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ACCOUNTANT, CSKH);
        }

        public static bool CanAccessScheduleTab(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN); // Chỉ admin mới thấy tab lịch thanh toán
        }

        public static bool CanAccessHistoryTab(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ACCOUNTANT, CSKH);
        }

        public static bool CanAccessPayoutTab(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ACCOUNTANT, CSKH);
        }
    }
}