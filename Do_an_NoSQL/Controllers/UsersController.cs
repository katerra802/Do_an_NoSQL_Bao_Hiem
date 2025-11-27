using ClosedXML.Excel;
using Do_an_NoSQL.Database;
using Do_an_NoSQL.Helpers;
using Do_an_NoSQL.Models;
using Do_an_NoSQL.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OfficeOpenXml;
using System.Linq;
using System.Text;

namespace Do_an_NoSQL.Controllers
{
    public class UsersController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(MongoDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================
        // INDEX
        // ================================
        public IActionResult Index(
    int page = 1,
    int? per_page = null,
    int pageSize = 10,
    string? search = null,
    string? role = null,
    string? status = null)
        {
            if (!PermissionHelper.CanManageUsers(User, _context))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }
            var query = _context.Users.AsQueryable();
            pageSize = per_page ?? pageSize;
            // --- Tìm kiếm ---
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(search) ||
                    x.FullName.ToLower().Contains(search) ||
                    x.Email.ToLower().Contains(search));
            }

            // --- Lọc theo vai trò ---
            if (!string.IsNullOrEmpty(role))
                query = query.Where(x => x.RoleCode == role);

            // --- Lọc theo trạng thái ---
            if (!string.IsNullOrEmpty(status))
                query = query.Where(x => x.Status == status);

            // --- Tổng số bản ghi ---
            var totalItems = query.Count();

            // --- Lấy trang dữ liệu ---
            var users = query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // --- Lấy danh sách vai trò ---
            var roles = _context.Roles.Find(_ => true).ToList();

            // --- Gắn tên vai trò vào từng user ---
            var items = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.Email,
                RoleName = roles.FirstOrDefault(r => r.Code == u.RoleCode)?.Name ?? "(Không xác định)",
                u.Status
            }).ToList<dynamic>();

            // --- Tạo model phân trang bằng helper ---
            var model = PagedResult<dynamic>.Create(items, page, pageSize, totalItems);

            // --- Gửi dữ liệu phụ ra View ---
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.Roles = roles;

            return View(model);
        }


        // ================================
        // XEM CHI TIẾT
        // ================================
        [HttpGet]
        public IActionResult GetUserDetails(string id)
        {
            var user = _context.Users.Find(x => x.Id == id).FirstOrDefault();
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            var role = _context.Roles.Find(x => x.Code == user.RoleCode).FirstOrDefault();

            // 🔹 Lấy danh sách quyền của role
            var rolePerm = _context.RolePermissions.Find(x => x.RoleCode == user.RoleCode).FirstOrDefault();
            var permissions = new List<object>();

            if (rolePerm != null)
            {
                var allPerms = _context.Permissions
                    .Find(p => rolePerm.Permissions.Contains(p.Code))
                    .ToList();

                permissions = allPerms.Select(p => new
                {
                    code = p.Code,
                    name = p.Name,
                    module = p.Module
                }).ToList<object>();
            }

            return Json(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    full_name = user.FullName,
                    email = user.Email,
                    role = role?.Name ?? "(Không xác định)",
                    status = user.Status,
                    permissions
                }
            });
        }


        // ================================
        // CREATE USER (THÊM NGƯỜI DÙNG)
        // ================================
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Mode"] = "create";  // Chế độ tạo mới
            ViewBag.Roles = _context.Roles.Find(_ => true).ToList();  // Lấy danh sách Roles
            return View("Form", new UserCreateVM());  // Trả về form tạo mới
        }

        // ================================
        // POST: Create User (Thêm người dùng)
        // ================================
        [HttpPost]
        public IActionResult Create(UserCreateVM model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

            var existingUser = _context.Users.Find(x => x.Username == model.Username).FirstOrDefault();
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại!");
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại!" });
            }

            var existingEmail = _context.Users.Find(x => x.Email == model.Email).FirstOrDefault();
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng!");
                return Json(new { success = false, message = "Email đã được sử dụng!" });
            }

            var user = new User
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                RoleCode = model.RoleCode,
                Status = model.Status ?? "active",  // Default status is active
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.InsertOne(user);  // Thêm người dùng mới vào MongoDB

            TempData["ToastMessage"] = "Thêm người dùng thành công!";
            TempData["ToastType"] = "success";  // Lưu thông báo

            return RedirectToAction("Index");
        }

        // ================================
        // GET: Edit User (Chỉnh sửa người dùng)
        // ================================
        [HttpGet]
        public IActionResult Edit(string id)
        {
            var user = _context.Users.Find(x => x.Id == id).FirstOrDefault();
            if (user == null)
                return NotFound();  // Không tìm thấy người dùng, trả về lỗi 404

            ViewData["Mode"] = "edit";  // Chế độ chỉnh sửa
            ViewBag.Roles = _context.Roles.Find(_ => true).ToList();  // Lấy danh sách Roles

            var userEditVM = new UserCreateVM
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                RoleCode = user.RoleCode,
                Status = user.Status
            };

            return View("Form", userEditVM);  // Trả về form chỉnh sửa
        }

        // ================================
        // POST: Edit User (Chỉnh sửa người dùng)
        // ================================
        [HttpPost]
        public IActionResult Edit(string id, UserCreateVM model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

            var existingUser = _context.Users.Find(x => x.Username == model.Username && x.Id != id).FirstOrDefault();
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại!");
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại!" });
            }

            var existingEmail = _context.Users.Find(x => x.Email == model.Email && x.Id != id).FirstOrDefault();
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng!");
                return Json(new { success = false, message = "Email đã được sử dụng!" });
            }

            var update = Builders<User>.Update
                .Set(x => x.Username, model.Username)
                .Set(x => x.FullName, model.FullName)
                .Set(x => x.Email, model.Email)
                .Set(x => x.RoleCode, model.RoleCode)
                .Set(x => x.Status, model.Status);

            _context.Users.UpdateOne(x => x.Id == id, update);  // Cập nhật người dùng

            TempData["ToastMessage"] = "Cập nhật người dùng thành công!";
            TempData["ToastType"] = "success";  // Lưu thông báo

            return Json(new { success = true, message = "Cập nhật người dùng thành công!" });
        }



        // ================================
        // XÓA 1 NGƯỜI DÙNG
        // ================================
        [HttpPost]
        public IActionResult Delete([FromBody] DeleteUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Id))
                    return Json(new { success = false, message = "ID người dùng không hợp lệ!" });

                // Kiểm tra user có tồn tại không
                var user = _context.Users.Find(x => x.Id == request.Id).FirstOrDefault();
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });

                // Thực hiện xóa
                var deleteResult = _context.Users.DeleteOne(x => x.Id == request.Id);

                if (deleteResult.DeletedCount > 0)
                {
                    _logger.LogInformation("Đã xóa người dùng: {Username} (ID: {UserId})", user.Username, user.Id);
                    return Json(new { success = true, message = "Xóa người dùng thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa người dùng. Vui lòng thử lại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa người dùng với ID: {UserId}", request?.Id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa người dùng!" });
            }
        }

        // ================================
        // XÓA HÀNG LOẠT
        // ================================
        [HttpPost]
        public IActionResult BulkDelete([FromBody] List<string> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                    return Json(new { success = false, message = "Danh sách ID rỗng!" });

                // Xóa nhiều bản ghi
                var filter = Builders<User>.Filter.In(x => x.Id, ids);
                var deleteResult = _context.Users.DeleteMany(filter);

                if (deleteResult.DeletedCount > 0)
                {
                    _logger.LogInformation("Đã xóa {Count} người dùng", deleteResult.DeletedCount);
                    return Json(new { success = true, message = $"Đã xóa thành công {deleteResult.DeletedCount} người dùng!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không có người dùng nào được xóa!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa hàng loạt người dùng");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa người dùng!" });
            }
        }

        // ================================
        // CẬP NHẬT TRẠNG THÁI
        // ================================
        [HttpPost]
        public IActionResult UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Id) || string.IsNullOrEmpty(request?.NewStatus))
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

                // Kiểm tra trạng thái hợp lệ
                if (request.NewStatus != "active" && request.NewStatus != "inactive")
                    return Json(new { success = false, message = "Trạng thái không hợp lệ!" });

                // Tìm user
                var user = _context.Users.Find(x => x.Id == request.Id).FirstOrDefault();
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });

                // Cập nhật trạng thái
                var filter = Builders<User>.Filter.Eq(x => x.Id, request.Id);
                var update = Builders<User>.Update
                    .Set(x => x.Status, request.NewStatus);

                var updateResult = _context.Users.UpdateOne(filter, update);

                if (updateResult.ModifiedCount > 0)
                {
                    _logger.LogInformation("Đã cập nhật trạng thái người dùng {Username} thành {Status}", user.Username, request.NewStatus);
                    return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không có thay đổi nào được thực hiện!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái người dùng {UserId}", request?.Id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật trạng thái!" });
            }
        }

        // ================================
        // REQUEST MODELS
        // ================================
        public class DeleteUserRequest
        {
            public string Id { get; set; }
        }

        public class UpdateStatusRequest
        {
            public string Id { get; set; }
            public string NewStatus { get; set; }
        }

        // ================================
        // IMPORT EXCEL NGƯỜI DÙNG
        // ================================
        [HttpPost]
        public IActionResult ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file Excel hợp lệ!" });

            var users = new List<User>();
            var errors = new List<string>();
            int skipped = 0;

            try
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "Users") ?? workbook.Worksheet(1);
                        if (ws == null)
                            return Json(new { success = false, message = "Không tìm thấy sheet 'Users' trong file!" });

                        var rows = ws.RowsUsed().Skip(1);
                        foreach (var row in rows)
                        {
                            try
                            {
                                var username = row.Cell(1).GetValue<string>().Trim();
                                var fullName = row.Cell(2).GetValue<string>().Trim();
                                var email = row.Cell(3).GetValue<string>().Trim();
                                var roleCode = row.Cell(4).GetValue<string>().Trim();
                                var status = row.Cell(5).GetValue<string>().Trim();

                                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
                                {
                                    errors.Add($"Dòng {row.RowNumber()}: Thiếu tên đăng nhập hoặc email!");
                                    continue;
                                }

                                // Nếu user/email đã tồn tại thì bỏ qua, không báo lỗi
                                if (_context.Users.Find(x => x.Username == username || x.Email == email).Any())
                                {
                                    skipped++;
                                    continue;
                                }

                                users.Add(new User
                                {
                                    Username = username,
                                    FullName = fullName,
                                    Email = email,
                                    RoleCode = roleCode,
                                    Status = string.IsNullOrEmpty(status) ? "active" : status
                                });
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Lỗi dòng {row.RowNumber()}: {ex.Message}");
                            }
                        }
                    }
                }

                if (users.Any())
                    _context.Users.InsertMany(users);

                string message = $"Import thành công {users.Count} người dùng!";
                if (skipped > 0) message += $" ({skipped} người dùng đã tồn tại và được bỏ qua)";

                if (errors.Any())
                    return Json(new { success = true, message, warning = $"Có {errors.Count} lỗi nhỏ." });

                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi đọc file Excel: " + ex.Message });
            }
        }


        // ================================
        // EXPORT EXCEL NGƯỜI DÙNG
        // ================================
        [HttpGet]
        public IActionResult ExportExcel(
    string? ids,
    string? excludeIds,
    bool exportAll = false,
    string? search = null,
    string? role = null,
    string? status = null)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                if (exportAll)
                {
                    if (!string.IsNullOrEmpty(search))
                        query = query.Where(x =>
                            x.Username.ToLower().Contains(search.ToLower()) ||
                            x.FullName.ToLower().Contains(search.ToLower()) ||
                            x.Email.ToLower().Contains(search.ToLower()));

                    if (!string.IsNullOrEmpty(role))
                        query = query.Where(x => x.RoleCode == role);

                    if (!string.IsNullOrEmpty(status))
                        query = query.Where(x => x.Status == status);

                    if (!string.IsNullOrEmpty(excludeIds))
                    {
                        var excludeList = excludeIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                        query = query.Where(x => !excludeList.Contains(x.Id));
                    }
                }
                else if (!string.IsNullOrEmpty(ids))
                {
                    var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                    query = query.Where(x => idList.Contains(x.Id));
                }

                var users = query.ToList();

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Users");

                    ws.Cell(1, 1).Value = "Tên đăng nhập";
                    ws.Cell(1, 2).Value = "Họ tên";
                    ws.Cell(1, 3).Value = "Email";
                    ws.Cell(1, 4).Value = "Vai trò";
                    ws.Cell(1, 5).Value = "Trạng thái";

                    var header = ws.Range(1, 1, 1, 5);
                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor = XLColor.LightGray;
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var u in users)
                    {
                        ws.Cell(row, 1).Value = u.Username;
                        ws.Cell(row, 2).Value = u.FullName;
                        ws.Cell(row, 3).Value = u.Email;
                        ws.Cell(row, 4).Value = u.RoleCode;
                        ws.Cell(row, 5).Value = u.Status == "active" ? "Hoạt động" : "Ngưng hoạt động";
                        row++;
                    }

                    ws.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var fileName = $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi Export Excel User: {ex.Message}");
                return StatusCode(500, "Đã xảy ra lỗi khi xuất dữ liệu.");
            }
        }


        // ================================
        // TẢI FILE MẪU NGƯỜI DÙNG
        // ================================
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "User_Template.xlsx");
            if (!System.IO.File.Exists(filePath))
                return NotFound("Không tìm thấy file mẫu.");

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "User_Template.xlsx");
        }
    }
}
