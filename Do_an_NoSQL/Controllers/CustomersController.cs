using ClosedXML.Excel;
using Do_an_NoSQL.Database;
using Do_an_NoSQL.Helpers;
using Do_an_NoSQL.Models;
using Do_an_NoSQL.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Do_an_NoSQL.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly MongoDbContext _context;

        public CustomersController(MongoDbContext context, ILogger<CustomersController> logger)
        {
            _context = context;
        }

        // ================================
        // GET: List Customers with filtering, sorting, and paging
        // ================================
        public IActionResult Index(
     int page = 1,
     int per_page = 10,
     string? search = null,
     string? gender = null,
     string? occupation = null,
     DateTime? from_date = null,
     DateTime? to_date = null,
     string sort = "created_at_desc"
 )
        {
            if (!PermissionHelper.CanViewCustomer(User, _context))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }
            var collection = _context.Customers.AsQueryable();

            // ============================
            // FILTER
            // ============================
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                collection = collection.Where(x =>
                    x.FullName.ToLower().Contains(search) ||
                    x.CustomerCode.ToLower().Contains(search)
                );
            }

            if (!string.IsNullOrEmpty(gender))
                collection = collection.Where(x => x.Gender == gender);

            if (!string.IsNullOrEmpty(occupation))
                collection = collection.Where(x => x.Occupation == occupation);

            if (from_date.HasValue)
                collection = collection.Where(x => x.CreatedAt >= from_date.Value);

            if (to_date.HasValue)
                collection = collection.Where(x => x.CreatedAt <= to_date.Value);

            // ============================
            // SORT
            // ============================
            collection = sort switch
            {
                "created_at_asc" => collection.OrderBy(x => x.CreatedAt),
                "created_at_desc" => collection.OrderByDescending(x => x.CreatedAt),
                "name_asc" => collection.OrderBy(x => x.FullName),
                "name_desc" => collection.OrderByDescending(x => x.FullName),
                "income_asc" => collection.OrderBy(x => x.Income),
                "income_desc" => collection.OrderByDescending(x => x.Income),
                _ => collection.OrderByDescending(x => x.CreatedAt)
            };

            // ============================
            // COUNT TRƯỚC KHI PHÂN TRANG
            // ============================
            var totalItems = collection.Count();

            // ============================
            // PHÂN TRANG + LOAD DỮ LIỆU
            // ============================
            var list = collection.Skip((page - 1) * per_page)
                                 .Take(per_page)
                                 .ToList(); // ✅ fix lỗi LINQ3 MongoDB

            // ============================
            // CHUYỂN THÀNH DYNAMIC
            // ============================
            var dynamicList = list.Select(x => new
            {
                x.Id,
                x.CustomerCode,
                x.FullName,
                x.Gender,
                x.Occupation,
                x.Income,
                x.Address,
                x.Phone,
                x.Email,
                x.NationalId,
                x.Dob,
                x.CreatedAt
            }).Cast<dynamic>().ToList();

            // ============================
            // GỌI PagedResult (CÓ totalItems)
            // ============================
            var paged = PagedResult<dynamic>.Create(dynamicList, page, per_page, totalItems);

            // ============================
            // VIEWBAG
            // ============================
            ViewBag.Search = search;
            ViewBag.Gender = gender;
            ViewBag.Occupation = occupation;
            ViewBag.FromDate = from_date;
            ViewBag.ToDate = to_date;
            ViewBag.Sort = sort;

            return View(paged);
        }


        [HttpGet]
        public IActionResult Create()
        {
            if (!PermissionHelper.CanManageCustomer(User, _context))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }
            ViewData["Mode"] = "create";
            return View("Form", new CustomerCreateVM());
        }

        [HttpPost]
        public IActionResult Create(CustomerCreateVM model)
        {
            if (!PermissionHelper.CanManageCustomer(User, _context))
            {
                return Json(new { success = false, message = "Bạn không có quyền thêm khách hàng!" });
            }
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

            var year = DateTime.Now.Year;
            var random = new Random();
            string customerCode;

            // Sinh mã KH không trùng
            do
            {
                int randomNumber = random.Next(1, 1000);
                customerCode = $"C{year}-{randomNumber:D3}";
            } while (_context.Customers.Find(x => x.CustomerCode == customerCode).Any());

            var customer = new Customer
            {
                CustomerCode = customerCode,
                FullName = model.FullName,
                Dob = model.Dob,
                Gender = model.Gender,
                NationalId = model.NationalId,
                Occupation = model.Occupation,
                Income = model.Income,
                Address = model.Address,
                Phone = model.Phone,
                Email = model.Email,
                HealthInfo = model.HealthInfo,
                Source = model.Source,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.InsertOne(customer);

            return Json(new { success = true, message = $"Tạo khách hàng {customer.FullName} ({customer.CustomerCode}) thành công!" });
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (!PermissionHelper.CanManageCustomer(User, _context))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }
            var c = _context.Customers.Find(x => x.Id == id).FirstOrDefault();
            if (c == null) return NotFound();

            ViewData["Mode"] = "edit";

            var vm = new CustomerCreateVM
            {
                Id = c.Id,
                FullName = c.FullName,
                Dob = c.Dob,
                Gender = c.Gender,
                NationalId = c.NationalId,
                Occupation = c.Occupation,
                Income = c.Income,
                Address = c.Address,
                Phone = c.Phone,
                Email = c.Email,
                HealthInfo = c.HealthInfo,
                Source = c.Source
            };

            ViewBag.Id = c.Id;
            ViewBag.Code = c.CustomerCode;

            return View("Form", vm);
        }

        [HttpPost]
        public IActionResult Edit(string id, CustomerCreateVM model)
        {
            if (!PermissionHelper.CanManageCustomer(User, _context))
            {
                return Json(new { success = false, message = "Bạn không có quyền sửa thông tin!" });
            }
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

            var update = Builders<Customer>.Update
                .Set(x => x.FullName, model.FullName)
                .Set(x => x.Dob, model.Dob)
                .Set(x => x.Gender, model.Gender)
                .Set(x => x.NationalId, model.NationalId)
                .Set(x => x.Occupation, model.Occupation)
                .Set(x => x.Income, model.Income)
                .Set(x => x.Address, model.Address)
                .Set(x => x.Phone, model.Phone)
                .Set(x => x.Email, model.Email)
                .Set(x => x.HealthInfo, model.HealthInfo)
                .Set(x => x.Source, model.Source);

            _context.Customers.UpdateOne(x => x.Id == id, update);

            return Json(new { success = true, message = $"Cập nhật khách hàng {model.FullName} thành công!" });
        }

        [HttpPost]
        public IActionResult Delete(string id)
        {
            if (!PermissionHelper.CanManageCustomer(User, _context))
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa khách hàng!" });
            }
            var result = _context.Customers.DeleteOne(x => x.Id == id);

            if (result.DeletedCount > 0)
                return Json(new { success = true, message = "Xóa khách hàng thành công!" });
            else
                return Json(new { success = false, message = "Không tìm thấy khách hàng cần xóa!" });
        }

        [HttpPost]
        public IActionResult BulkDelete([FromBody] List<string>? ids, bool deleteAll = false)
        {
            if (deleteAll)
            {
                // Nếu đang ở chế độ "chọn tất cả" => xóa tất cả trừ excludedIds
                var body = Request.Body;
                using var reader = new StreamReader(body);
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                var json = reader.ReadToEndAsync().Result;

                var excludeData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                var excludeIds = excludeData != null && excludeData.ContainsKey("excludeIds")
                    ? excludeData["excludeIds"]
                    : new List<string>();

                var filter = Builders<Customer>.Filter.Nin(x => x.Id, excludeIds);
                var result = _context.Customers.DeleteMany(filter);

                return Ok(new
                {
                    message = $"Đã xóa {result.DeletedCount} khách hàng (loại trừ {excludeIds.Count})."
                });
            }
            else
            {
                // Nếu chỉ xóa các ID được chọn
                if (ids == null || !ids.Any())
                    return BadRequest(new { message = "Không có khách hàng nào được chọn." });

                var filter = Builders<Customer>.Filter.In(x => x.Id, ids);
                var result = _context.Customers.DeleteMany(filter);

                return Ok(new { message = $"Đã xóa thành công {result.DeletedCount} khách hàng." });
            }
        }


        [HttpPost]
        public IActionResult ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file Excel hợp lệ!" });

            var customers = new List<Customer>();
            var errors = new List<string>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "Customers") ?? workbook.Worksheet(1);
                        if (ws == null)
                            return Json(new { success = false, message = "Không tìm thấy sheet 'Customers' trong file!" });

                        var rows = ws.RowsUsed().Skip(1);
                        foreach (var row in rows)
                        {
                            try
                            {
                                var fullName = row.Cell(1).GetValue<string>().Trim();
                                var dobStr = row.Cell(2).GetValue<string>().Trim();
                                var gender = row.Cell(3).GetValue<string>().Trim();
                                var nationalId = row.Cell(4).GetValue<string>().Trim();
                                var phone = row.Cell(5).GetValue<string>().Trim();
                                var email = row.Cell(6).GetValue<string>().Trim();
                                var occupation = row.Cell(7).GetValue<string>().Trim();
                                var income = row.Cell(8).GetValue<decimal>();
                                var address = row.Cell(9).GetValue<string>().Trim();
                                var healthInfo = row.Cell(10).GetValue<string>().Trim();
                                var source = row.Cell(11).GetValue<string>().Trim();

                                // ✅ Xử lý ngày sinh
                                DateTime dob;
                                if (!DateTime.TryParse(dobStr, out dob))
                                {
                                    if (row.Cell(2).TryGetValue(out double excelDate))
                                        dob = DateTime.FromOADate(excelDate);
                                    else
                                    {
                                        errors.Add($"Lỗi dòng {row.RowNumber()}: Ngày sinh không hợp lệ ({dobStr})!");
                                        continue;
                                    }
                                }

                                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone))
                                {
                                    errors.Add($"Dòng {row.RowNumber()}: Thiếu họ tên hoặc số điện thoại!");
                                    continue;
                                }

                                if (_context.Customers.Find(x => x.Phone == phone || x.NationalId == nationalId).Any())
                                {
                                    errors.Add($"Khách hàng '{fullName}' (CMND/CCCD: {nationalId}) đã tồn tại!");
                                    continue;
                                }

                                // ✅ Sinh mã khách hàng giống Create()
                                var year = DateTime.Now.Year;
                                var random = new Random();
                                string customerCode;
                                do
                                {
                                    int randomNumber = random.Next(1, 1000);
                                    customerCode = $"C{year}-{randomNumber:D3}";
                                } while (_context.Customers.Find(x => x.CustomerCode == customerCode).Any());

                                var customer = new Customer
                                {
                                    CustomerCode = customerCode,
                                    FullName = fullName,
                                    Dob = dob,
                                    Gender = gender,
                                    NationalId = nationalId,
                                    Phone = phone,
                                    Email = email,
                                    Occupation = occupation,
                                    Income = income,
                                    Address = address,
                                    HealthInfo = healthInfo,
                                    Source = source,
                                    CreatedAt = DateTime.UtcNow
                                };

                                customers.Add(customer);
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Lỗi dòng {row.RowNumber()}: {ex.Message}");
                            }
                        }
                    }
                }

                if (errors.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Import thất bại ({errors.Count} lỗi).",
                        errors = errors.Take(10).ToList(),
                        totalErrors = errors.Count
                    });
                }

                if (customers.Any())
                    _context.Customers.InsertMany(customers);

                return Json(new
                {
                    success = true,
                    message = $"Import thành công {customers.Count} khách hàng!",
                    count = customers.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi đọc file Excel: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportExcel(
            string? ids,
            string? excludeIds,
            bool exportAll = false,
            string? search = null,
            string? gender = null,
            string? source = null,
            DateTime? from_date = null,
            DateTime? to_date = null)
        {
            try
            {
                var query = _context.Customers.AsQueryable();

                // Lọc dữ liệu theo tham số
                if (exportAll)
                {
                    if (!string.IsNullOrEmpty(search))
                        query = query.Where(x => x.FullName.ToLower().Contains(search.ToLower()) ||
                                                 x.Phone.Contains(search) ||
                                                 x.NationalId.Contains(search));

                    if (!string.IsNullOrEmpty(gender))
                        query = query.Where(x => x.Gender == gender);

                    if (!string.IsNullOrEmpty(source))
                        query = query.Where(x => x.Source == source);

                    if (from_date.HasValue)
                        query = query.Where(x => x.CreatedAt >= from_date.Value);

                    if (to_date.HasValue)
                        query = query.Where(x => x.CreatedAt <= to_date.Value);

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

                var customers = query.ToList();

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Customers");

                    // Header
                    ws.Cell(1, 1).Value = "Họ và tên";
                    ws.Cell(1, 2).Value = "Ngày sinh";
                    ws.Cell(1, 3).Value = "Giới tính";
                    ws.Cell(1, 4).Value = "CMND/CCCD";
                    ws.Cell(1, 5).Value = "Số điện thoại";
                    ws.Cell(1, 6).Value = "Email";
                    ws.Cell(1, 7).Value = "Nghề nghiệp";
                    ws.Cell(1, 8).Value = "Thu nhập (₫)";
                    ws.Cell(1, 9).Value = "Địa chỉ";
                    ws.Cell(1, 10).Value = "Tình trạng sức khỏe";
                    ws.Cell(1, 11).Value = "Nguồn khách hàng";
                    ws.Cell(1, 12).Value = "Ngày tạo";

                    var header = ws.Range(1, 1, 1, 12);
                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor = XLColor.LightGray;
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Dữ liệu
                    int row = 2;
                    foreach (var c in customers)
                    {
                        ws.Cell(row, 1).Value = c.FullName;
                        ws.Cell(row, 2).Value = c.Dob.ToString("dd/MM/yyyy");
                        ws.Cell(row, 3).Value = c.Gender == "M" ? "Nam" : "Nữ";
                        ws.Cell(row, 4).Value = c.NationalId;
                        ws.Cell(row, 5).Value = c.Phone;
                        ws.Cell(row, 6).Value = c.Email;
                        ws.Cell(row, 7).Value = c.Occupation;
                        ws.Cell(row, 8).Value = c.Income;
                        ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";
                        ws.Cell(row, 9).Value = c.Address;
                        ws.Cell(row, 10).Value = c.HealthInfo;
                        ws.Cell(row, 11).Value = c.Source;
                        ws.Cell(row, 12).Value = c.CreatedAt.ToString("dd/MM/yyyy");
                        row++;
                    }

                    ws.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var fileName = $"Customers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi Export Excel Customer: {ex.Message}");
                return StatusCode(500, "Đã xảy ra lỗi khi xuất dữ liệu. Vui lòng thử lại.");
            }
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "Customer_Template.xlsx");
            if (!System.IO.File.Exists(filePath))
                return NotFound("Không tìm thấy file mẫu.");

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Customer_Template.xlsx");
        }

        [HttpGet]
        public IActionResult GetCustomerDetails(string id)
        {
            try
            {
                var c = _context.Customers.Find(x => x.Id == id).FirstOrDefault();
                if (c == null)
                    return Json(new { success = false, message = "Không tìm thấy khách hàng." });

                // Dùng customer_code thay vì _id
                var customerCode = c.CustomerCode;

                var appCount = _context.PolicyApplications.CountDocuments(x => x.CustomerId == customerCode);
                var policyCount = _context.Policies.CountDocuments(x => x.CustomerId == customerCode);
                var claimCount = _context.Claims.CountDocuments(x => x.CustomerId == customerCode);

                // Tổng tiền đã chi trả
                var totalPaid = _context.ClaimPayouts.AsQueryable()
                    .Join(_context.Claims.AsQueryable(),
                          payout => payout.ClaimNo,
                          claim => claim.ClaimNo,
                          (payout, claim) => new { payout, claim })
                    .Where(x => x.claim.CustomerId == customerCode)
                    .Sum(x => (decimal?)x.payout.PaidAmount) ?? 0;

                return Json(new
                {
                    success = true,
                    customer = new
                    {
                        id = c.Id,
                        customer_code = c.CustomerCode,
                        full_name = c.FullName,
                        dob = c.Dob,
                        gender = c.Gender,
                        national_id = c.NationalId,
                        phone = c.Phone,
                        email = c.Email,
                        occupation = c.Occupation,
                        income = c.Income,
                        address = c.Address,
                        source = c.Source,
                        health_info = c.HealthInfo,
                        created_at = c.CreatedAt,
                        policy_applications = appCount,
                        policies = policyCount,
                        claims = claimCount,
                        total_paid = totalPaid
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải chi tiết: " + ex.Message });
            }
        }

    }
}
