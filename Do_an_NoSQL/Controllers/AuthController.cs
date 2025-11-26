using Do_an_NoSQL.Database;
using Do_an_NoSQL.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SystemClaim = System.Security.Claims.Claim;
using SystemClaimType = System.Security.Claims.ClaimTypes;
using SystemClaimsIdentity = System.Security.Claims.ClaimsIdentity;
using SystemClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Do_an_NoSQL.Controllers
{
    public class AuthController : Controller
    {
        private readonly MongoDbContext _context;

        public AuthController(MongoDbContext context)
        {
            _context = context;
        }

        // GET: Auth/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // Nếu đã đăng nhập, chuyển về trang chủ
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            try
            {
                // Tìm user theo username
                // Tìm user theo username
                var user = _context.Users
                    .Find(u => u.Username == username && u.Status == "active") // ✅ Dùng Status thay vì IsActive
                    .FirstOrDefault();

                if (user == null || user.Password != password)
                {
                    TempData["ErrorMessage"] = "Tên đăng nhập hoặc mật khẩu không đúng!";
                    return View();
                }

                // Hoặc nếu muốn dùng IsActive sau khi lấy từ DB:
                if (user == null || !user.IsActive || user.Password != password)
                {
                    TempData["ErrorMessage"] = "Tên đăng nhập hoặc mật khẩu không đúng!";
                    return View();
                }

                // Lấy thông tin role
                var role = _context.Roles
                    .Find(r => r.Code == user.RoleCode)
                    .FirstOrDefault();

                if (role == null)
                {
                    TempData["ErrorMessage"] = "Vai trò không hợp lệ!";
                    return View();
                }

                // Tạo claims
                var claims = new List<SystemClaim>
                {
                    new SystemClaim(SystemClaimType.NameIdentifier, user.Id),
                    new SystemClaim(SystemClaimType.Name, user.Username),
                    new SystemClaim("FullName", user.FullName ?? ""),
                    new SystemClaim(SystemClaimType.Email, user.Email ?? ""),
                    new SystemClaim(SystemClaimType.Role, user.RoleCode),
                    new SystemClaim("RoleName", role.Name)
                };

                var claimsIdentity = new SystemClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Remember me
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new SystemClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Redirect về returnUrl hoặc trang chủ
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View();
            }
        }

        // POST: Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // GET: Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}