using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;

namespace GameStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly GameStoreDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(GameStoreDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? status, string? sortBy)
        {
            var orders = _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Game)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.OrderStatus == status);
            }

            // Sort
            sortBy ??= "OrderDate";
            orders = sortBy.ToLower() switch
            {
                "total" => orders.OrderByDescending(o => o.TotalAmount),
                "customer" => orders.OrderBy(o => o.FirstName),
                _ => orders.OrderByDescending(o => o.OrderDate)
            };

            var orderList = await orders.ToListAsync();
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSort = sortBy;
            ViewBag.StatusOptions = new List<string> { "Processing", "Shipped", "Completed", "Cancelled" };

            return View(orderList);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Game)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = status;
            
            if (status == "Completed" || status == "Cancelled")
            {
                order.PaymentStatus = status == "Completed" ? "Completed" : "Refunded";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Order #{id} status updated to {status}";
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePaymentStatus(int id, string paymentStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.PaymentStatus = paymentStatus;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Order #{id} payment status updated to {paymentStatus}";
            
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new OrderDashboardViewModel();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ToListAsync();

            model.TotalOrders = orders.Count;
            model.TotalRevenue = orders.Sum(o => o.TotalAmount);
            model.PendingOrders = orders.Count(o => o.OrderStatus == "Processing");
            model.CompletedOrders = orders.Count(o => o.OrderStatus == "Completed");

            // Recent orders
            model.RecentOrders = await _context.Orders
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            // Orders by status
            model.OrdersByStatus = orders
                .GroupBy(o => o.OrderStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            // Revenue by month (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            model.RevenueByMonth = orders
                .Where(o => o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new MonthRevenue
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            return View(model);
        }
    }

    public class OrderDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();
        public List<MonthRevenue> RevenueByMonth { get; set; } = new();
    }

    public class MonthRevenue
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}