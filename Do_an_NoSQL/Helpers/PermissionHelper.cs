using System.Security.Claims;
using Do_an_NoSQL.Database;
using MongoDB.Driver;

namespace Do_an_NoSQL.Helpers
{
    public static class PermissionHelper
    {
        // Cache permissions cho mỗi role (tối ưu performance)
        private static Dictionary<string, List<string>> _rolePermissionsCache = new();

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
            // Check cache trước
            if (_rolePermissionsCache.ContainsKey(roleCode))
                return _rolePermissionsCache[roleCode];

            // Lấy từ DB
            var rolePermission = context.RolePermissions
                .Find(rp => rp.RoleCode == roleCode)
                .FirstOrDefault();

            var permissions = rolePermission?.Permissions ?? new List<string>();

            // Cache lại
            _rolePermissionsCache[roleCode] = permissions;

            return permissions;
        }

        // Clear cache khi cần (ví dụ sau khi update permissions)
        public static void ClearCache()
        {
            _rolePermissionsCache.Clear();
        }

        // === CUSTOMER PERMISSIONS ===
        public static bool CanViewCustomer(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "VIEW_CUSTOMER");

        public static bool CanManageCustomer(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "MANAGE_CUSTOMER");

        // === APPLICATION PERMISSIONS ===
        public static bool CanViewApplication(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "VIEW_APPLICATION");

        public static bool CanManageApplication(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "MANAGE_APPLICATION");

        public static bool CanApproveApplication(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "APPROVE_APPLICATION");

        // === POLICY PERMISSIONS ===
        public static bool CanViewPolicy(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "VIEW_POLICY");

        public static bool CanManagePolicy(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "MANAGE_POLICY");

        // === PAYMENT PERMISSIONS ===
        public static bool CanViewPayment(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "VIEW_PAYMENT");

        public static bool CanManagePayment(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "MANAGE_PAYMENT");

        // === CLAIM PERMISSIONS ===
        public static bool CanViewClaim(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "VIEW_CLAIM");

        public static bool CanManageClaim(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "MANAGE_CLAIM");

        public static bool CanManagePayout(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "MANAGE_PAYOUT");

        // === SYSTEM PERMISSIONS ===
        public static bool CanManageUsers(ClaimsPrincipal user, MongoDbContext context)
            => HasPermission(user, context, "MANAGE_USERS");
    }
}