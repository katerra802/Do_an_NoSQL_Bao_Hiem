using Microsoft.AspNetCore.Mvc;
using Do_an_NoSQL.Models;

namespace Do_an_NoSQL.Controllers
{
    public class CustomerController : Controller
    {
        // GET: Customer
        public IActionResult Index()
        {
            // TODO: Load customers from database
            return View();
        }

        // GET: Customer/Details/5
        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // TODO: Load customer from database
            // var customer = _customerService.GetById(id);
            // if (customer == null)
            // {
            //     return NotFound();
            // }

            // Temporary mock data
            var customer = new Customer
            {
                CustomerCode = id,
                FullName = "Nguyễn Văn An",
                Dob = new DateTime(1985, 3, 15),
                Gender = "Nam",
                NationalId = "079085001234",
                Occupation = "Kỹ sư phần mềm",
                Income = 25000000,
                Phone = "0901234567",
                Email = "nguyenvanan@email.com",
                Address = "123 Nguyễn Huệ, Quận 1, TP.HCM",
                HealthInfo = "Bình thường",
                CreatedAt = new DateTime(2024, 1, 1)
            };

            return View(customer);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                // TODO: Save to database
                TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customer/Edit/5
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // TODO: Load customer from database
            var customer = new Customer
            {
                CustomerCode = id,
                FullName = "Nguyễn Văn An"
            };

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Customer customer)
        {
            if (id != customer.CustomerCode)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // TODO: Update database
                TempData["SuccessMessage"] = "Cập nhật khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customer/Delete/5
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // TODO: Delete from database
            TempData["SuccessMessage"] = "Xóa khách hàng thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}