using Do_an_NoSQL.Database;
using Do_an_NoSQL.Helpers;
using Do_an_NoSQL.Models;
using Do_an_NoSQL.Models.ViewModels;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Driver;
using OfficeOpenXml;

[Authorize]
public class ProductsController : Controller
{
    private readonly MongoDbContext _context;


    public ProductsController(MongoDbContext context)
    {
        _context = context;
    }

    private IMongoCollection<Product> Collection => _context.Products;

    public IActionResult Index(
    int page = 1,
    int per_page = 10,
    string? search = null,
    string? type = null,
    decimal? sum_assured_value = null,  // Giá trị nhập để lọc theo giá bảo hiểm
    int? age_value = null,               // Giá trị nhập để lọc theo tuổi
    string sort = "date_desc"             // Sort theo thời gian hợp đồng
)
    {
        if (!RoleHelper.CanViewProducts(User))
        {
            return RedirectToAction("AccessDenied", "Auth");
        }
        var collection = _context.Products.AsQueryable();

        // ============================
        // Lọc theo 'Search' (tên sản phẩm)
        // ============================
        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            collection = collection.Where(x => x.Name.ToLower().Contains(search));
        }

        // ============================
        // Lọc theo 'Type' (loại sản phẩm)
        // ============================
        if (!string.IsNullOrEmpty(type))
        {
            collection = collection.Where(x => x.Type == type);
        }

        // ============================
        // Lọc theo giá bảo hiểm (1 input)
        // ============================
        if (sum_assured_value.HasValue)
        {
            var value = sum_assured_value.Value;
            collection = collection.Where(x => value >= x.MinSumAssured && value <= x.MaxSumAssured);
        }

        // ============================
        // Lọc theo tuổi (1 input)
        // ============================
        if (age_value.HasValue)
        {
            var age = age_value.Value;
            collection = collection.Where(x => age >= x.MinAge && age <= x.MaxAge);
        }

        // ============================
        // Sắp xếp theo 'TermYears' (thời gian)
        // ============================
        collection = sort switch
        {
            "term_asc" => collection.OrderBy(x => x.TermYears),
            "term_desc" => collection.OrderByDescending(x => x.TermYears),
            "date_asc" => collection.OrderBy(x => x.CreatedAt),
            "date_desc" => collection.OrderByDescending(x => x.CreatedAt),
            _ => collection.OrderByDescending(x => x.CreatedAt)
        };

        // ============================
        // Phân trang
        // ============================
        var list = collection.Skip((page - 1) * per_page).Take(per_page).ToList();
        var totalItems = collection.Count();

        // ============================
        // Dữ liệu trả về
        // ============================
        var dynamicList = list.Select(x => new
        {
            x.Id,
            x.ProductCode,
            x.Name,
            x.Type,
            x.MinSumAssured,
            x.MaxSumAssured,
            x.MinAge,
            x.MaxAge,
            x.TermYears
        }).Cast<dynamic>().ToList();

        var paged = PagedResult<dynamic>.Create(dynamicList, page, per_page, totalItems);

        // ============================
        // Truyền ViewBag
        // ============================
        ViewBag.Search = search;
        ViewBag.Type = new List<string>
    {
        "endowment", "term", "whole_life", "retirement", "health", "accident", "education"
    };
        ViewBag.SelectedType = type;
        ViewBag.SumAssuredValue = sum_assured_value;
        ViewBag.AgeValue = age_value;
        ViewBag.Sort = sort;

        return View(paged);
    }

    // ================================
    // CREATE (GET)
    // ================================
    [HttpGet]
    public IActionResult Create()
    {
        if (!RoleHelper.CanManageProducts(User))
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Bạn không có quyền thêm sản phẩm!";
            return RedirectToAction("Index");
        }
        LoadTypeOptions();
        ViewData["Mode"] = "create";
        return View("Form", new ProductCreateVM());
    }

    // ================================
    // CREATE (POST)
    // ================================
    [HttpPost]
    public IActionResult Create(ProductCreateVM vm)
    {
        if (!RoleHelper.CanManageProducts(User))
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Bạn không có quyền thêm sản phẩm!";
            return Json(new { success = false, reload = true });
        }
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

        try
        {
            var product = new Product
            {
                ProductCode = vm.ProductCode,
                Name = vm.Name,
                Type = vm.Type,
                Purpose = vm.Purpose,
                TermYears = vm.TermYears,
                PremiumRate = vm.PremiumRate,
                MinAge = vm.MinAge,
                MaxAge = vm.MaxAge,
                MinSumAssured = vm.MinSumAssured,
                MaxSumAssured = vm.MaxSumAssured,
                Riders = vm.Riders.Select(r => new Rider
                {
                    Code = r.Code,
                    Name = r.Name
                }).ToList(),
                LatePenaltyRate = vm.LatePenaltyRate,          // ✅ THÊM
                GracePeriodDays = vm.GracePeriodDays,          // ✅ THÊM
                CreatedAt = DateTime.UtcNow
            };
            _context.Products.InsertOne(product);

            return Json(new { success = true, message = $"Tạo sản phẩm {product.Name} thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi tạo sản phẩm: " + ex.Message });
        }
    }

    // ================================
    // EDIT (GET)
    // ================================
    //[HttpGet]
    //public IActionResult Edit(string id)
    //{
    //    var p = _context.Products.Find(x => x.Id == id).FirstOrDefault();
    //    if (p == null) return NotFound();

    //    // ✅ Truyền selected value để dropdown chọn đúng loại sản phẩm
    //    ViewBag.TypeOptions = new SelectList(new[]
    //    {
    //    new { Value = "endowment", Text = "Tích lũy" },
    //    new { Value = "term", Text = "Tử kỳ" },
    //    new { Value = "whole_life", Text = "Trọn đời" },
    //    new { Value = "retirement", Text = "Hưu trí" },
    //    new { Value = "health", Text = "Sức khỏe" },
    //    new { Value = "accident", Text = "Tai nạn" },
    //    new { Value = "education", Text = "Giáo dục" }
    //}, "Value", "Text", p.Type); // <== đây nè

    //    ViewData["Mode"] = "edit";

    //    var vm = new ProductCreateVM
    //    {
    //        Id = p.Id,
    //        ProductCode = p.ProductCode,
    //        Name = p.Name,
    //        Type = p.Type, // ✅ giữ lại giá trị
    //        Purpose = p.Purpose,
    //        TermYears = p.TermYears,
    //        PremiumRate = p.PremiumRate,
    //        MinAge = p.MinAge,
    //        MaxAge = p.MaxAge,
    //        MinSumAssured = p.MinSumAssured,
    //        MaxSumAssured = p.MaxSumAssured,
    //        Riders = p.Riders.Select(r => new RiderVM
    //        {
    //            Code = r.Code,
    //            Name = r.Name
    //        }).ToList()
    //    };

    //    return View("Form", vm);
    //}
    // ================================
    // EDIT (GET) - Line ~190
    // ================================
    [HttpGet]
    public IActionResult Edit(string id)
    {
        if (!RoleHelper.CanManageProducts(User))
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Bạn không có quyền sửa sản phẩm!";
            return RedirectToAction("Index");
        }
        var p = _context.Products.Find(x => x.Id == id).FirstOrDefault();
        if (p == null) return NotFound();

        ViewBag.TypeOptions = new SelectList(new[]
        {
        new { Value = "endowment", Text = "Tích lũy" },
        new { Value = "term", Text = "Tử kỳ" },
        new { Value = "whole_life", Text = "Trọn đời" },
        new { Value = "retirement", Text = "Hưu trí" },
        new { Value = "health", Text = "Sức khỏe" },
        new { Value = "accident", Text = "Tai nạn" },
        new { Value = "education", Text = "Giáo dục" }
    }, "Value", "Text", p.Type);

        ViewData["Mode"] = "edit";

        var vm = new ProductCreateVM
        {
            Id = p.Id,
            ProductCode = p.ProductCode,
            Name = p.Name,
            Type = p.Type,
            Purpose = p.Purpose,
            TermYears = p.TermYears,
            PremiumRate = p.PremiumRate,
            LatePenaltyRate = p.LatePenaltyRate,          // ✅ THÊM
            GracePeriodDays = p.GracePeriodDays,          // ✅ THÊM
            MinAge = p.MinAge,
            MaxAge = p.MaxAge,
            MinSumAssured = p.MinSumAssured,
            MaxSumAssured = p.MaxSumAssured,
            Riders = p.Riders.Select(r => new RiderVM
            {
                Code = r.Code,
                Name = r.Name
            }).ToList()
        };

        return View("Form", vm);
    }

    // ================================
    // EDIT (POST)
    // ================================
    [HttpPost]
    //public IActionResult Edit(string id, ProductCreateVM vm)
    //{
    //    if (!ModelState.IsValid)
    //        return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

    //    var update = Builders<Product>.Update
    //        .Set(x => x.ProductCode, vm.ProductCode)
    //        .Set(x => x.Name, vm.Name)
    //        .Set(x => x.Type, vm.Type)
    //        .Set(x => x.Purpose, vm.Purpose)
    //        .Set(x => x.TermYears, vm.TermYears)
    //        .Set(x => x.PremiumRate, vm.PremiumRate)
    //        .Set(x => x.MinAge, vm.MinAge)
    //        .Set(x => x.MaxAge, vm.MaxAge)
    //        .Set(x => x.MinSumAssured, vm.MinSumAssured)
    //        .Set(x => x.MaxSumAssured, vm.MaxSumAssured)
    //        .Set(x => x.Riders, vm.Riders.Select(r => new Rider { Code = r.Code, Name = r.Name }).ToList());

    //    _context.Products.UpdateOne(x => x.Id == id, update);

    //    return Json(new { success = true, message = $"Cập nhật sản phẩm {vm.Name} thành công!" });
    //}
    [HttpPost]
    public IActionResult Edit(string id, ProductCreateVM vm)
    {
        if (!RoleHelper.CanManageProducts(User))
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Bạn không có quyền sửa sản phẩm!";
            return RedirectToAction("Index");
        }
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

        var update = Builders<Product>.Update
            .Set(x => x.ProductCode, vm.ProductCode)
            .Set(x => x.Name, vm.Name)
            .Set(x => x.Type, vm.Type)
            .Set(x => x.Purpose, vm.Purpose)
            .Set(x => x.TermYears, vm.TermYears)
            .Set(x => x.PremiumRate, vm.PremiumRate)
            .Set(x => x.LatePenaltyRate, vm.LatePenaltyRate)      // ✅ THÊM
            .Set(x => x.GracePeriodDays, vm.GracePeriodDays)      // ✅ THÊM
            .Set(x => x.MinAge, vm.MinAge)
            .Set(x => x.MaxAge, vm.MaxAge)
            .Set(x => x.MinSumAssured, vm.MinSumAssured)
            .Set(x => x.MaxSumAssured, vm.MaxSumAssured)
            .Set(x => x.Riders, vm.Riders.Select(r => new Rider { Code = r.Code, Name = r.Name }).ToList());

        _context.Products.UpdateOne(x => x.Id == id, update);

        return Json(new { success = true, message = $"Cập nhật sản phẩm {vm.Name} thành công!" });
    }

    private void LoadTypeOptions()
    {
        ViewBag.TypeOptions = new SelectList(new[]
        {
            new { Value = "endowment", Text = "Tích lũy" },
            new { Value = "term", Text = "Tử kỳ" },
            new { Value = "whole_life", Text = "Trọn đời" },
            new { Value = "retirement", Text = "Hưu trí" },
            new { Value = "health", Text = "Sức khỏe" },
            new { Value = "accident", Text = "Tai nạn" },
            new { Value = "education", Text = "Giáo dục" }
        }, "Value", "Text");
    }

    [HttpPost]
    public IActionResult Delete(string id)
    {
        if (!RoleHelper.CanManageProducts(User))
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Bạn không có quyền xóa sản phẩm!";
            return Json(new { success = false, reload = true });
        }
        try
        {
            var result = _context.Products.DeleteOne(x => x.Id == id);

            if (result.DeletedCount > 0)
            {
                return Json(new
                {
                    success = true,
                    message = "Xóa sản phẩm thành công!"
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy sản phẩm cần xóa!"
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = "Lỗi khi xóa sản phẩm: " + ex.Message
            });
        }
    }



    [HttpPost]
        public IActionResult BulkDelete([FromBody] List<string> ids, bool deleteAll = false)
        {
            if (deleteAll)
            {
                var excludeIds = ids ?? new List<string>();
                var filter = Builders<Product>.Filter.Nin(x => x.Id, excludeIds);
                var result = _context.Products.DeleteMany(filter);

                return Json(new { success = true, message = $"Đã xóa {result.DeletedCount} sản phẩm (loại trừ {excludeIds.Count} sản phẩm)." });
            }
            else
            {
                var filter = Builders<Product>.Filter.In(x => x.Id, ids);
                var result = _context.Products.DeleteMany(filter);

                return Json(new { success = true, message = $"Đã xóa {result.DeletedCount} sản phẩm." });
            }
        }



    //[HttpGet]
    //public IActionResult GetProductDetail(string id)
    //{
    //    try
    //    {
    //        var product = _context.Products.Find(p => p.Id == id).FirstOrDefault();
    //        if (product == null)
    //            return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

    //        return Json(new
    //        {
    //            success = true,
    //            data = new
    //            {
    //                product.ProductCode,
    //                product.Name,
    //                product.Type,
    //                product.Purpose,
    //                product.TermYears,
    //                product.PremiumRate,
    //                product.MinAge,
    //                product.MaxAge,
    //                product.MinSumAssured,
    //                product.MaxSumAssured,
    //                product.Riders,
    //                product.CreatedAt
    //            }
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        return Json(new { success = false, message = "Lỗi khi tải chi tiết sản phẩm: " + ex.Message });
    //    }
    //}

    [HttpGet]
    public IActionResult GetProductDetail(string id)
    {
        try
        {
            var product = _context.Products.Find(p => p.Id == id).FirstOrDefault();
            if (product == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            return Json(new
            {
                success = true,
                data = new
                {
                    product.ProductCode,
                    product.Name,
                    product.Type,
                    product.Purpose,
                    product.TermYears,
                    product.PremiumRate,
                    product.LatePenaltyRate,          // ✅ THÊM
                    product.GracePeriodDays,          // ✅ THÊM
                    product.MinAge,
                    product.MaxAge,
                    product.MinSumAssured,
                    product.MaxSumAssured,
                    product.Riders,
                    product.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi tải chi tiết sản phẩm: " + ex.Message });
        }
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "Product_Template.xlsx");
        if (!System.IO.File.Exists(filePath))
            return NotFound("Không tìm thấy file mẫu.");

        var bytes = System.IO.File.ReadAllBytes(filePath);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Product_Template.xlsx");
    }

    [HttpPost]
    public IActionResult ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Json(new { success = false, message = "Vui lòng chọn file Excel hợp lệ!" });

        try
        {
            var products = new List<Product>();
            var ridersDict = new Dictionary<string, List<Rider>>();
            var errors = new List<string>();

            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                {
                    // ===========================
                    // SHEET PRODUCTS
                    // ===========================
                    var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "Products") ?? workbook.Worksheet(1);
                    if (ws == null)
                        return Json(new { success = false, message = "Không tìm thấy sheet 'Products' trong file!" });

                    var rows = ws.RowsUsed().Skip(1);
                    foreach (var row in rows)
                    {
                        try
                        {
                            var code = row.Cell(1).GetValue<string>().Trim();
                            if (string.IsNullOrEmpty(code)) continue;

                            if (_context.Products.Find(x => x.ProductCode == code).Any())
                            {
                                errors.Add($"Mã sản phẩm '{code}' đã tồn tại trong hệ thống!");
                                continue;
                            }

                            var name = row.Cell(2).GetValue<string>().Trim();
                            var type = row.Cell(3).GetValue<string>().Trim().ToLower();
                            var termYears = row.Cell(4).GetValue<int>();
                            var premiumRate = row.Cell(5).GetValue<decimal>();
                            var minAge = row.Cell(6).GetValue<int>();
                            var maxAge = row.Cell(7).GetValue<int>();
                            var minSum = row.Cell(8).GetValue<decimal>();
                            var maxSum = row.Cell(9).GetValue<decimal>();
                            var purposes = row.Cell(10).GetValue<string>()
                                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim()).ToList();

                            if (minAge > maxAge)
                            {
                                errors.Add($"Sản phẩm '{code}': MinAge > MaxAge");
                                continue;
                            }

                            var product = new Product
                            {
                                ProductCode = code,
                                Name = name,
                                Type = type,
                                TermYears = termYears,
                                PremiumRate = premiumRate,
                                MinAge = minAge,
                                MaxAge = maxAge,
                                MinSumAssured = minSum,
                                MaxSumAssured = maxSum,
                                Purpose = purposes,
                                Riders = new List<Rider>(),
                                CreatedAt = DateTime.UtcNow
                            };

                            products.Add(product);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Lỗi dòng {row.RowNumber()}: {ex.Message}");
                        }
                    }

                    // ===========================
                    // SHEET RIDERS (nếu có)
                    // ===========================
                    var wsRiders = workbook.Worksheets.FirstOrDefault(w => w.Name == "Riders");
                    if (wsRiders != null)
                    {
                        var riderRows = wsRiders.RowsUsed().Skip(1);
                        foreach (var row in riderRows)
                        {
                            try
                            {
                                var productCode = row.Cell(1).GetValue<string>().Trim();
                                if (string.IsNullOrEmpty(productCode)) continue;

                                var riderCode = row.Cell(2).GetValue<string>().Trim();
                                var riderName = row.Cell(3).GetValue<string>().Trim();

                                if (string.IsNullOrEmpty(riderCode) || string.IsNullOrEmpty(riderName))
                                {
                                    errors.Add($"Sheet Riders - dòng {row.RowNumber()}: thiếu mã hoặc tên rider!");
                                    continue;
                                }

                                var rider = new Rider
                                {
                                    Code = riderCode,
                                    Name = riderName
                                };

                                if (!ridersDict.ContainsKey(productCode))
                                    ridersDict[productCode] = new List<Rider>();

                                ridersDict[productCode].Add(rider);
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Sheet Riders - dòng {row.RowNumber()}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Gán riders cho sản phẩm tương ứng
            foreach (var p in products)
            {
                if (ridersDict.ContainsKey(p.ProductCode))
                    p.Riders = ridersDict[p.ProductCode];
            }

            // Nếu có lỗi
            if (errors.Any())
            {
                return Json(new
                {
                    success = false,
                    message = $"Import thất bại! Có {errors.Count} lỗi.",
                    errors = errors.Take(10).ToList(),
                    totalErrors = errors.Count
                });
            }

            // Lưu vào DB
            if (products.Any())
                _context.Products.InsertMany(products);

            return Json(new
            {
                success = true,
                message = $"Import thành công {products.Count} sản phẩm!",
                count = products.Count
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi đọc file Excel: " + ex.Message });
        }
    }

    // ================================
    // HELPER METHODS
    // ================================
    private int ParseInt(string value, int row, string field, List<string> errors)
    {
        if (int.TryParse(value.Trim(), out int result))
            return result;

        errors.Add($"Dòng {row}: Trường '{field}' phải là số nguyên!");
        return 0;
    }

    private decimal ParseDecimal(string value, int row, string field, List<string> errors)
    {
        if (decimal.TryParse(value.Trim(), out decimal result))
            return result;

        errors.Add($"Dòng {row}: Trường '{field}' phải là số!");
        return 0;
    }

    [HttpGet]
    public IActionResult ExportExcel(
    string? ids,
    string? excludeIds,
    bool exportAll = false,
    string? search = null,
    string? type = null,
    decimal? sum_assured_value = null,
    int? age_value = 0,
    DateTime? from_date = null,
    DateTime? to_date = null)
    {
        if (!RoleHelper.CanViewProducts(User))
        {
            return StatusCode(403, "Bạn không có quyền xuất dữ liệu!");
        }
        try
        {
            var query = _context.Products.AsQueryable();

            // ======================
            // Áp dụng bộ lọc (nếu có)
            // ======================
            if (exportAll)
            {
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()) ||
                                             x.ProductCode.ToLower().Contains(search.ToLower()));

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(x => x.Type == type);

                if (sum_assured_value.HasValue)
                {
                    var val = sum_assured_value.Value;
                    query = query.Where(x => val >= x.MinSumAssured && val <= x.MaxSumAssured);
                }

                if (age_value > 0)
                    query = query.Where(x => age_value >= x.MinAge && age_value <= x.MaxAge);

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

            var products = query.ToList();

            // ======================
            // Xuất file Excel
            // ======================
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Products");

                // Header
                ws.Cell(1, 1).Value = "Mã sản phẩm";
                ws.Cell(1, 2).Value = "Tên sản phẩm";
                ws.Cell(1, 3).Value = "Loại";
                ws.Cell(1, 4).Value = "Thời hạn (năm)";
                ws.Cell(1, 5).Value = "Tỷ lệ phí (%)";
                ws.Cell(1, 6).Value = "Tuổi tối thiểu";
                ws.Cell(1, 7).Value = "Tuổi tối đa";
                ws.Cell(1, 8).Value = "Số tiền tối thiểu (₫)";
                ws.Cell(1, 9).Value = "Số tiền tối đa (₫)";
                ws.Cell(1, 10).Value = "Mục đích bảo hiểm";
                ws.Cell(1, 11).Value = "Sản phẩm bổ trợ (Riders)";
                ws.Cell(1, 12).Value = "Ngày tạo";

                // Style header
                var header = ws.Range(1, 1, 1, 12);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                header.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var p in products)
                {
                    ws.Cell(row, 1).Value = p.ProductCode;
                    ws.Cell(row, 2).Value = p.Name;
                    ws.Cell(row, 3).Value = p.Type;
                    ws.Cell(row, 4).Value = p.TermYears;
                    ws.Cell(row, 5).Value = p.PremiumRate;
                    ws.Cell(row, 6).Value = p.MinAge;
                    ws.Cell(row, 7).Value = p.MaxAge;
                    ws.Cell(row, 8).Value = p.MinSumAssured;
                    ws.Cell(row, 9).Value = p.MaxSumAssured;
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";
                    ws.Cell(row, 10).Value = p.Purpose != null ? string.Join(", ", p.Purpose) : "";
                    ws.Cell(row, 11).Value = p.Riders != null && p.Riders.Any()
                        ? string.Join("; ", p.Riders.Select(r => $"{r.Code} - {r.Name}"))
                        : "Không có";
                    ws.Cell(row, 12).Value = p.CreatedAt.ToString("dd/MM/yyyy");
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileName = $"Products_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi xuất Excel: {ex.Message}");
            return StatusCode(500, "Đã xảy ra lỗi khi xuất dữ liệu. Vui lòng thử lại.");
        }
    }
}