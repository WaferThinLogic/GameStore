using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;

namespace GameStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PurchaseOrdersController : Controller
    {
        private readonly GameStoreDbContext _context;

        public PurchaseOrdersController(GameStoreDbContext context)
        {
            _context = context;
        }

        // GET: Admin/PurchaseOrders
        public async Task<IActionResult> Index(string? status)
        {
            var purchaseOrders = _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Game)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                purchaseOrders = purchaseOrders.Where(p => p.Status == status);
            }

            var orders = await purchaseOrders.OrderByDescending(p => p.OrderDate).ToListAsync();
            ViewBag.CurrentStatus = status;
            ViewBag.StatusOptions = new List<string> { "Draft", "Submitted", "Confirmed", "Received", "Cancelled" };

            return View(orders);
        }

        // GET: Admin/PurchaseOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var purchaseOrder = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                    .ThenInclude(i => i.Game)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchaseOrder == null) return NotFound();

            return View(purchaseOrder);
        }

        // GET: Admin/PurchaseOrders/Create
        public IActionResult Create()
        {
            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name");
            ViewBag.Games = new SelectList(_context.Games.Include(g => g.Supplier).Where(g => g.SupplierId != null).OrderBy(g => g.Title), "Id", "Title");
            return View();
        }

        // POST: Admin/PurchaseOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SupplierId,ExpectedDeliveryDate,Notes")] PurchaseOrder purchaseOrder, int[]? gameIds, int[]? quantities, decimal[]? unitCosts)
        {
            if (gameIds != null && gameIds.Length > 0)
            {
                purchaseOrder.OrderNumber = GenerateOrderNumber();
                purchaseOrder.OrderDate = DateTime.Now;
                purchaseOrder.Status = "Draft";

                _context.Add(purchaseOrder);
                await _context.SaveChangesAsync();

                purchaseOrder.PurchaseOrderItems = new List<PurchaseOrderItem>();
                decimal totalAmount = 0;

                for (int i = 0; i < gameIds.Length; i++)
                {
                    var game = await _context.Games.FindAsync(gameIds[i]);
                    if (game != null && quantities != null && unitCosts != null && i < quantities.Length && i < unitCosts.Length)
                    {
                        var item = new PurchaseOrderItem
                        {
                            GameId = gameIds[i],
                            Quantity = quantities[i],
                            UnitCost = unitCosts[i],
                            TotalCost = quantities[i] * unitCosts[i]
                        };
                        purchaseOrder.PurchaseOrderItems.Add(item);
                        totalAmount += item.TotalCost;
                    }
                }

                purchaseOrder.TotalAmount = totalAmount;
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", purchaseOrder.SupplierId);
            ViewBag.Games = new SelectList(_context.Games.Include(g => g.Supplier).Where(g => g.SupplierId != null).OrderBy(g => g.Title), "Id", "Title");
            return View(purchaseOrder);
        }

        // GET: Admin/PurchaseOrders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var purchaseOrder = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchaseOrder == null) return NotFound();

            if (purchaseOrder.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Only draft orders can be edited";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", purchaseOrder.SupplierId);
            ViewBag.Games = new SelectList(_context.Games.Include(g => g.Supplier).Where(g => g.SupplierId != null).OrderBy(g => g.Title), "Id", "Title");
            return View(purchaseOrder);
        }

        // POST: Admin/PurchaseOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SupplierId,ExpectedDeliveryDate,Notes,TotalAmount")] PurchaseOrder purchaseOrder, int[]? gameIds, int[]? quantities, decimal[]? unitCosts)
        {
            if (id != purchaseOrder.Id) return NotFound();

            var existingOrder = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingOrder == null) return NotFound();
            if (existingOrder.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Only draft orders can be edited";
                return RedirectToAction(nameof(Index));
            }

            existingOrder.SupplierId = purchaseOrder.SupplierId;
            existingOrder.ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate;
            existingOrder.Notes = purchaseOrder.Notes;

            // Remove existing items
            _context.PurchaseOrderItems.RemoveRange(existingOrder.PurchaseOrderItems);

            // Add new items
            existingOrder.PurchaseOrderItems = new List<PurchaseOrderItem>();
            decimal totalAmount = 0;

            if (gameIds != null && quantities != null && unitCosts != null)
            {
                for (int i = 0; i < gameIds.Length; i++)
                {
                    if (i < quantities.Length && i < unitCosts.Length)
                    {
                        var item = new PurchaseOrderItem
                        {
                            GameId = gameIds[i],
                            Quantity = quantities[i],
                            UnitCost = unitCosts[i],
                            TotalCost = quantities[i] * unitCosts[i]
                        };
                        existingOrder.PurchaseOrderItems.Add(item);
                        totalAmount += item.TotalCost;
                    }
                }
            }

            existingOrder.TotalAmount = totalAmount;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderExists(purchaseOrder.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/PurchaseOrders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var purchaseOrder = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchaseOrder == null) return NotFound();

            return View(purchaseOrder);
        }

        // POST: Admin/PurchaseOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
            if (purchaseOrder != null)
            {
                _context.PurchaseOrders.Remove(purchaseOrder);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/PurchaseOrders/Submit/5
        [HttpPost]
        public async Task<IActionResult> Submit(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchaseOrder == null) return NotFound();
            if (purchaseOrder.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Only draft orders can be submitted";
                return RedirectToAction(nameof(Index));
            }

            purchaseOrder.Status = "Submitted";
            purchaseOrder.SubmittedDate = DateTime.Now;

            // Record price history for each item
            foreach (var item in purchaseOrder.PurchaseOrderItems)
            {
                var game = await _context.Games.FindAsync(item.GameId);
                if (game != null)
                {
                    var priceHistory = new PriceHistory
                    {
                        GameId = game.Id,
                        CostPrice = item.UnitCost,
                        SellingPrice = game.Price,
                        EffectiveDate = DateTime.Now,
                        ChangeReason = $"PO #{purchaseOrder.OrderNumber}"
                    };
                    _context.PriceHistories.Add(priceHistory);

                    // Update game cost price
                    game.CostPrice = item.UnitCost;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.OrderNumber} submitted successfully";

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/PurchaseOrders/Confirm/5
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);

            if (purchaseOrder == null) return NotFound();
            if (purchaseOrder.Status != "Submitted")
            {
                TempData["ErrorMessage"] = "Only submitted orders can be confirmed";
                return RedirectToAction(nameof(Index));
            }

            purchaseOrder.Status = "Confirmed";
            purchaseOrder.ConfirmedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.OrderNumber} confirmed";

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/PurchaseOrders/Receive/5
        [HttpPost]
        public async Task<IActionResult> Receive(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchaseOrder == null) return NotFound();
            if (purchaseOrder.Status != "Confirmed" && purchaseOrder.Status != "Submitted")
            {
                TempData["ErrorMessage"] = "Only confirmed or submitted orders can be received";
                return RedirectToAction(nameof(Index));
            }

            // Update stock levels
            foreach (var item in purchaseOrder.PurchaseOrderItems)
            {
                var game = await _context.Games.FindAsync(item.GameId);
                if (game != null)
                {
                    game.Stock += item.Quantity;
                    item.QuantityReceived = item.Quantity;
                }
            }

            purchaseOrder.Status = "Received";
            purchaseOrder.ReceivedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.OrderNumber} received - stock updated";

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/PurchaseOrders/Cancel/5
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);

            if (purchaseOrder == null) return NotFound();

            purchaseOrder.Status = "Cancelled";

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.OrderNumber} cancelled";

            return RedirectToAction(nameof(Index));
        }

        private string GenerateOrderNumber()
        {
            return $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private bool PurchaseOrderExists(int id)
        {
            return _context.PurchaseOrders.Any(e => e.Id == id);
        }
    }
}