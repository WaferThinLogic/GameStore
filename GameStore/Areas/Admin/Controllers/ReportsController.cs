using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;

namespace GameStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly GameStoreDbContext _context;

        public ReportsController(GameStoreDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Reports/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var model = new ReportsDashboardViewModel();

            // Stock monitoring
            var games = await _context.Games
                .Include(g => g.Category)
                .Include(g => g.Supplier)
                .ToListAsync();

            model.TotalProducts = games.Count;
            model.TotalStockValue = games.Sum(g => g.Stock * g.CostPrice);
            model.LowStockItems = games.Where(g => g.Stock <= g.MinStockThreshold).ToList();
            model.OutOfStockItems = games.Where(g => g.Stock == 0).ToList();

            // Purchase order stats
            var purchaseOrders = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .ToListAsync();

            model.PendingPOs = purchaseOrders.Count(p => p.Status == "Draft" || p.Status == "Submitted");
            model.TotalPOValue = purchaseOrders.Sum(p => p.TotalAmount);

            // Sales stats (from orders)
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ToListAsync();

            model.TotalSales = orders.Sum(o => o.TotalAmount);
            model.TotalOrders = orders.Count;

            // Profitability
            var orderDetails = await _context.OrderDetails
                .Include(od => od.Game)
                .ToListAsync();

            decimal totalRevenue = orderDetails.Sum(od => od.TotalPrice);
            decimal totalCost = orderDetails.Sum(od => od.Quantity * (od.Game?.CostPrice ?? 0));
            model.TotalProfit = totalRevenue - totalCost;
            model.ProfitMargin = totalRevenue > 0 ? (model.TotalProfit / totalRevenue) * 100 : 0;

            return View(model);
        }

        // GET: Admin/Reports/StockMonitoring
        public async Task<IActionResult> StockMonitoring(int? categoryId, int? supplierId)
        {
            var query = _context.Games
                .Include(g => g.Category)
                .Include(g => g.Supplier)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(g => g.CategoryId == categoryId.Value);
            }

            if (supplierId.HasValue)
            {
                query = query.Where(g => g.SupplierId == supplierId.Value);
            }

            var games = await query.OrderBy(g => g.Stock).ToListAsync();

            var model = new StockMonitoringViewModel
            {
                Games = games,
                Categories = await _context.Categories.ToListAsync(),
                Suppliers = await _context.Suppliers.ToListAsync(),
                SelectedCategoryId = categoryId,
                SelectedSupplierId = supplierId,
                LowStockThreshold = 5
            };

            return View(model);
        }

        // GET: Admin/Reports/SupplierPerformance
        public async Task<IActionResult> SupplierPerformance()
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.Games)
                .Include(s => s.PurchaseOrders)
                .ToListAsync();

            var model = new List<SupplierPerformanceViewModel>();

            foreach (var supplier in suppliers)
            {
                var purchaseOrders = supplier.PurchaseOrders?.ToList() ?? new List<PurchaseOrder>();
                var games = supplier.Games?.ToList() ?? new List<Game>();

                var performance = new SupplierPerformanceViewModel
                {
                    SupplierId = supplier.Id,
                    SupplierName = supplier.Name,
                    TotalOrders = purchaseOrders.Count,
                    CompletedOrders = purchaseOrders.Count(p => p.Status == "Received"),
                    PendingOrders = purchaseOrders.Count(p => p.Status == "Draft" || p.Status == "Submitted" || p.Status == "Confirmed"),
                    TotalProducts = games.Count,
                    TotalOrderValue = purchaseOrders.Sum(p => p.TotalAmount),
                    OnTimeDeliveryRate = purchaseOrders.Any(p => p.ReceivedDate.HasValue)
                        ? (decimal)purchaseOrders.Count(p => p.ReceivedDate.HasValue && p.ExpectedDeliveryDate.HasValue && p.ReceivedDate <= p.ExpectedDeliveryDate) / purchaseOrders.Count(p => p.ReceivedDate.HasValue) * 100
                        : 0,
                    AverageOrderValue = purchaseOrders.Any() ? purchaseOrders.Average(p => p.TotalAmount) : 0
                };

                model.Add(performance);
            }

            return View(model.OrderByDescending(s => s.TotalOrderValue));
        }

        // GET: Admin/Reports/Profitability
        public async Task<IActionResult> Profitability(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Now.AddMonths(-6);
            endDate ??= DateTime.Now;

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Game)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var model = new ProfitabilityViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            // Calculate revenue and costs
            foreach (var order in orders)
            {
                foreach (var detail in order.OrderDetails ?? new List<OrderDetail>())
                {
                    var revenue = detail.TotalPrice;
                    var cost = detail.Quantity * (detail.Game?.CostPrice ?? 0);
                    var profit = revenue - cost;

                    model.TotalRevenue += revenue;
                    model.TotalCost += cost;
                    model.TotalProfit += profit;

                    // By category
                    var categoryId = detail.Game?.CategoryId ?? 0;
                    if (!model.RevenueByCategory.ContainsKey(categoryId))
                    {
                        model.RevenueByCategory[categoryId] = 0;
                        model.CostByCategory[categoryId] = 0;
                        model.ProfitByCategory[categoryId] = 0;
                    }
                    model.RevenueByCategory[categoryId] += revenue;
                    model.CostByCategory[categoryId] += cost;
                    model.ProfitByCategory[categoryId] += profit;

                    // By month
                    var monthKey = $"{order.OrderDate.Year}-{order.OrderDate.Month:D2}";
                    if (!model.RevenueByMonth.ContainsKey(monthKey))
                    {
                        model.RevenueByMonth[monthKey] = 0;
                        model.CostByMonth[monthKey] = 0;
                        model.ProfitByMonth[monthKey] = 0;
                    }
                    model.RevenueByMonth[monthKey] += revenue;
                    model.CostByMonth[monthKey] += cost;
                    model.ProfitByMonth[monthKey] += profit;
                }
            }

            model.ProfitMargin = model.TotalRevenue > 0 ? (model.TotalProfit / model.TotalRevenue) * 100 : 0;

            // Get category names for display
            var categories = await _context.Categories.ToListAsync();
            model.CategoryNames = categories.ToDictionary(c => c.Id, c => c.Name);

            return View(model);
        }

        // GET: Admin/Reports/PriceHistory
        public async Task<IActionResult> PriceHistory(int? gameId)
        {
            var query = _context.PriceHistories
                .Include(p => p.Game)
                    .ThenInclude(g => g!.Category)
                .AsQueryable();

            if (gameId.HasValue)
            {
                query = query.Where(p => p.GameId == gameId.Value);
            }

            var priceHistories = await query.OrderByDescending(p => p.EffectiveDate).ToListAsync();

            var model = new PriceHistoryViewModel
            {
                PriceHistories = priceHistories,
                Games = await _context.Games.OrderBy(g => g.Title).ToListAsync(),
                SelectedGameId = gameId
            };

            return View(model);
        }
    }

    public class ReportsDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public decimal TotalStockValue { get; set; }
        public List<Game> LowStockItems { get; set; } = new();
        public List<Game> OutOfStockItems { get; set; } = new();
        public int PendingPOs { get; set; }
        public decimal TotalPOValue { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    public class StockMonitoringViewModel
    {
        public List<Game> Games { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
        public int? SelectedCategoryId { get; set; }
        public int? SelectedSupplierId { get; set; }
        public int LowStockThreshold { get; set; }
    }

    public class SupplierPerformanceViewModel
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal OnTimeDeliveryRate { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class ProfitabilityViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
        public Dictionary<string, decimal> CostByMonth { get; set; } = new();
        public Dictionary<string, decimal> ProfitByMonth { get; set; } = new();
        public Dictionary<int, decimal> RevenueByCategory { get; set; } = new();
        public Dictionary<int, decimal> CostByCategory { get; set; } = new();
        public Dictionary<int, decimal> ProfitByCategory { get; set; } = new();
        public Dictionary<int, string> CategoryNames { get; set; } = new();
    }

    public class PriceHistoryViewModel
    {
        public List<PriceHistory> PriceHistories { get; set; } = new();
        public List<Game> Games { get; set; } = new();
        public int? SelectedGameId { get; set; }
    }
}