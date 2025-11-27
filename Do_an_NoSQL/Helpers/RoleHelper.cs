using Do_an_NoSQL.Database;
using MongoDB.Driver;
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

        // Cache permissions
        private static Dictionary<string, List<string>> _rolePermissionsCache = new();

        public static bool HasRole(ClaimsPrincipal user, params string[] roles)
        {
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            return userRole != null && roles.Contains(userRole);
        }

        // ✅ THÊM METHOD HasPermission
        public static bool HasPermission(ClaimsPrincipal user, MongoDbContext context, params string[] permissions)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var roleCode = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleCode))
                return false;

            // Admin có tất cả quyền
            if (roleCode == "ADMIN")
                return true;

            // Lấy permissions từ cache hoặc DB
            var userPermissions = GetRolePermissions(context, roleCode);
            return permissions.Any(p => userPermissions.Contains(p));
        }

        private static List<string> GetRolePermissions(MongoDbContext context, string roleCode)
        {
            if (_rolePermissionsCache.ContainsKey(roleCode))
                return _rolePermissionsCache[roleCode];

            var rolePermission = context.RolePermissions
                .Find(rp => rp.RoleCode == roleCode)
                .FirstOrDefault();

            var permissions = rolePermission?.Permissions ?? new List<string>();
            _rolePermissionsCache[roleCode] = permissions;

            return permissions;
        }

        public static void ClearCache()
        {
            _rolePermissionsCache.Clear();
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

        public static bool CanManageApplication(ClaimsPrincipal user, MongoDbContext context)
        {
            return HasRole(user, ADMIN, ADVISOR);
        }

        public static bool CanViewApplication(ClaimsPrincipal user, MongoDbContext context)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER);
        }

        // === POLICY ===
        public static bool CanAccessPolicies(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER);
        }

        // ✅ THÊM: UNDERWRITER CÓ THỂ XEM POLICIES
        public static bool CanViewPolicy(ClaimsPrincipal user, MongoDbContext context)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER);
        }

        // ✅ THÊM: UNDERWRITER CÓ THỂ QUẢN LÝ POLICIES
        public static bool CanManagePolicy(ClaimsPrincipal user, MongoDbContext context)
        {
            return HasRole(user, ADMIN, UNDERWRITER);
        }

        // === CLAIM ===
        public static bool CanAccessClaims(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, UNDERWRITER, CSKH);
        }

        public static bool CanManageClaim(ClaimsPrincipal user, MongoDbContext context)
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
        public static bool CanViewProducts(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER);
        }

        public static bool CanManageProducts(ClaimsPrincipal user)
        {
            return HasRole(user, ADMIN);
        }

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

        // === REPORTS ===
        // ✅ THÊM: Export Report
        public static bool CanExportReport(ClaimsPrincipal user, MongoDbContext context)
        {
            return HasRole(user, ADMIN, ADVISOR, UNDERWRITER, ACCOUNTANT);
        }
    }
}