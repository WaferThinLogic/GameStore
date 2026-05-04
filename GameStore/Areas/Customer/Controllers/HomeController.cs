using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;

namespace GameStore.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly GameStoreDbContext _context;

        public HomeController(GameStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _context.Games
                .Include(g => g.Category)
                .OrderByDescending(g => g.Id)
                .Take(8)
                .ToListAsync();
            return View(games);
        }

        public async Task<IActionResult> Browse(int? categoryId, string? searchQuery, int? minPrice, int? maxPrice)
        {
            IQueryable<Game> games = _context.Games.Include(g => g.Category);

            if (categoryId.HasValue)
            {
                games = games.Where(g => g.CategoryId == categoryId);
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                games = games.Where(g => g.Title.Contains(searchQuery) || g.Description.Contains(searchQuery));
            }

            if (minPrice.HasValue)
            {
                games = games.Where(g => g.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                games = games.Where(g => g.Price <= maxPrice.Value);
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchQuery = searchQuery;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(await games.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }
    }
}