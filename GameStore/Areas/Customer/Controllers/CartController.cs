using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;

namespace GameStore.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly GameStoreDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(GameStoreDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var cart = await _context.Carts
                .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

            if (cart == null)
            {
                cart = new Cart { ApplicationUserId = user.Id };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int gameId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return NotFound();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

            if (cart == null)
            {
                cart = new Cart { ApplicationUserId = user.Id };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = cart.CartItems?.FirstOrDefault(ci => ci.GameId == gameId);
            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    GameId = gameId,
                    Quantity = 1,
                    Price = game.Price
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity++;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            if (quantity <= 0)
            {
                return await RemoveFromCart(cartItemId);
            }

            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var cart = await _context.Carts
                .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var model = new CheckoutViewModel
            {
                FirstName = user.FullName?.Split(' ').FirstOrDefault() ?? string.Empty,
                LastName = user.FullName?.Split(' ').LastOrDefault() ?? string.Empty,
                Address = user.Address ?? string.Empty,
                City = user.City ?? string.Empty,
                State = user.State ?? string.Empty,
                PostalCode = user.PostalCode ?? string.Empty,
                Country = user.Country ?? string.Empty,
                Phone = user.PhoneNumber,
                Email = user.Email ?? string.Empty,
                Cart = cart
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                model.Cart = await _context.Carts
                    .Include(c => c.CartItems!)
                        .ThenInclude(ci => ci.Game)
                    .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);
                return View("Checkout", model);
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems!)
                    .ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            // Calculate totals
            var subTotal = cart.CartItems.Sum(ci => ci.Price * ci.Quantity);
            var tax = subTotal * 0.08m; // 8% tax
            var shippingCost = 5.99m;
            var totalAmount = subTotal + tax + shippingCost;

            // Create order
            var order = new Order
            {
                ApplicationUserId = user.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                PostalCode = model.PostalCode,
                Country = model.Country,
                Phone = model.Phone,
                Email = model.Email,
                SubTotal = subTotal,
                Tax = tax,
                ShippingCost = shippingCost,
                TotalAmount = totalAmount,
                OrderDate = DateTime.Now,
                PaymentStatus = "Completed",
                OrderStatus = "Processing"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create order details
            foreach (var cartItem in cart.CartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    GameId = cartItem.GameId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Price,
                    TotalPrice = cartItem.Price * cartItem.Quantity
                };
                _context.OrderDetails.Add(orderDetail);

                // Decrease stock
                if (cartItem.Game != null)
                {
                    cartItem.Game.Stock -= cartItem.Quantity;
                }
            }

            // Remove cart items
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            // Update user address
            user.FullName = $"{model.FirstName} {model.LastName}";
            user.Address = model.Address;
            user.City = model.City;
            user.State = model.State;
            user.PostalCode = model.PostalCode;
            user.Country = model.Country;
            user.PhoneNumber = model.Phone;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(OrderConfirmation), new { orderId = order.Id });
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Game)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == user.Id);

            if (order == null) return NotFound();

            return View(order);
        }

        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Game)
                .Where(o => o.ApplicationUserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Game)
                .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == user.Id);

            if (order == null) return NotFound();

            return View(order);
        }
    }

    public class CheckoutViewModel
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string? State { get; set; }

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public Cart? Cart { get; set; }
    }
}
