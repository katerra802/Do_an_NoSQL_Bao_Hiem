using Microsoft.AspNetCore.Mvc;
using Do_an_NoSQL.Models;

namespace Do_an_NoSQL.Controllers
{
    public class ProductsController : Controller
    {
        // GET: Products
        public IActionResult Index()
        {
            // TODO: Load products from database
            return View();
        }

        // GET: Products/Details/5
        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // TODO: Load product from database
            // var product = _productService.GetById(id);
            // if (product == null)
            // {
            //     return NotFound();
            // }

            // Temporary mock data
            var product = new Product
            {
                Id = id,
                ProductCode = "PRU001",
                Name = "PRU - Đầu Tư Vững Tiền",
                Type = "Bảo hiểm liên kết đơn vị",
                Purpose = new List<string> { "Đầu tư", "Tích lũy", "Bảo vệ tài chính" },
                TermYears = 20,
                MinAge = 18,
                MaxAge = 65,
                MinSumAssured = 100000000,
                MaxSumAssured = 5000000000,
                Riders = new List<Rider>
                {
                    new Rider { Code = "R001", Name = "Bảo hiểm tai nạn" },
                    new Rider { Code = "R002", Name = "Bảo hiểm sức khỏe" }
                },
                CreatedAt = DateTime.UtcNow
            };

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                // TODO: Save to database
                // product.ProductCode = GenerateProductCode();
                // product.CreatedAt = DateTime.UtcNow;
                // _productService.Create(product);

                TempData["SuccessMessage"] = "Thêm sản phẩm bảo hiểm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // TODO: Load product from database
            // var product = _productService.GetById(id);
            // if (product == null)
            // {
            //     return NotFound();
            // }

            // Temporary mock data
            var product = new Product
            {
                Id = id,
                ProductCode = "PRU001",
                Name = "PRU - Đầu Tư Vững Tiền",
                Type = "Bảo hiểm liên kết đơn vị",
                Purpose = new List<string> { "Đầu tư", "Tích lũy" },
                TermYears = 20,
                MinAge = 18,
                MaxAge = 65,
                MinSumAssured = 100000000,
                MaxSumAssured = 5000000000
            };

            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // TODO: Update database
                // _productService.Update(id, product);

                TempData["SuccessMessage"] = "Cập nhật sản phẩm bảo hiểm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // TODO: Delete from database
            // _productService.Delete(id);

            TempData["SuccessMessage"] = "Xóa sản phẩm bảo hiểm thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}