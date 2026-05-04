using GameStore.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameStore.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(GameStoreDbContext context)
        {
            await SeedCategoriesAsync(context);
            await SeedSuppliersAsync(context);
            await SeedGamesAsync(context);
        }

        private static async Task SeedCategoriesAsync(GameStoreDbContext context)
        {
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Action", Description = "Fast-paced games with combat and challenges" },
                    new Category { Name = "Adventure", Description = "Explore new worlds and embark on epic journeys" },
                    new Category { Name = "RPG", Description = "Role-playing games with character progression" },
                    new Category { Name = "Strategy", Description = "Plan and execute tactical gameplay" },
                    new Category { Name = "Sports", Description = "Competitive sports simulations" },
                    new Category { Name = "Racing", Description = "High-speed racing action" },
                    new Category { Name = "Puzzle", Description = "Challenge your mind with brain teasers" },
                    new Category { Name = "Horror", Description = "Spine-chilling scary games" },
                    new Category { Name = "Simulation", Description = "Realistic life and world simulations" },
                    new Category { Name = "Indie", Description = "Unique games from independent developers" }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSuppliersAsync(GameStoreDbContext context)
        {
            if (!context.Suppliers.Any())
            {
                var suppliers = new List<Supplier>
                {
                    new Supplier
                    {
                        Name = "GameTech Studios",
                        ContactName = "John Smith",
                        Email = "contact@gametech.com",
                        Phone = "555-0101",
                        Address = "123 Gaming Blvd",
                        City = "Los Angeles",
                        State = "CA",
                        PostalCode = "90001",
                        Country = "USA"
                    },
                    new Supplier
                    {
                        Name = "Digital Dreams Interactive",
                        ContactName = "Sarah Johnson",
                        Email = "sales@digitaldreams.com",
                        Phone = "555-0102",
                        Address = "456 Tech Park",
                        City = "San Francisco",
                        State = "CA",
                        PostalCode = "94102",
                        Country = "USA"
                    },
                    new Supplier
                    {
                        Name = "Pixel Perfect Games",
                        ContactName = "Mike Chen",
                        Email = "info@pixelperfect.com",
                        Phone = "555-0103",
                        Address = "789 Indie Lane",
                        City = "Seattle",
                        State = "WA",
                        PostalCode = "98101",
                        Country = "USA"
                    },
                    new Supplier
                    {
                        Name = "Epic Entertainment",
                        ContactName = "Lisa Brown",
                        Email = "orders@epicent.com",
                        Phone = "555-0104",
                        Address = "321 Adventure Ave",
                        City = "Austin",
                        State = "TX",
                        PostalCode = "73301",
                        Country = "USA"
                    }
                };

                context.Suppliers.AddRange(suppliers);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedGamesAsync(GameStoreDbContext context)
        {
            if (!context.Games.Any())
            {
                var categories = context.Categories.ToList();
                var suppliers = context.Suppliers.ToList();
                
                var games = new List<Game>
                {
                    new Game
                    {
                        Title = "Cyber Warrior 2077",
                        Description = "A futuristic action-adventure game set in a dystopian cyberpunk world. Fight through corporate warfare and uncover dark secrets.",
                        Price = 59.99m,
                        CostPrice = 30.00m,
                        MinStockThreshold = 20,
                        ImageUrl = "https://via.placeholder.com/400x200/6f42c1/ffffff?text=Cyber+Warrior",
                        CategoryId = categories.First(c => c.Name == "Action").Id,
                        SupplierId = suppliers.First(s => s.Name == "GameTech Studios").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-6),
                        Stock = 100
                    },
                    new Game
                    {
                        Title = "Dragon's Quest XI",
                        Description = "Embark on an epic RPG adventure through a fantasy world filled with dragons, magic, and ancient prophecies.",
                        Price = 49.99m,
                        CostPrice = 25.00m,
                        MinStockThreshold = 15,
                        ImageUrl = "https://via.placeholder.com/400x200/e94560/ffffff?text=Dragons+Quest",
                        CategoryId = categories.First(c => c.Name == "RPG").Id,
                        SupplierId = suppliers.First(s => s.Name == "Digital Dreams Interactive").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-12),
                        Stock = 75
                    },
                    new Game
                    {
                        Title = "Speed Racer Ultimate",
                        Description = "Experience the thrill of high-speed racing with stunning graphics and realistic physics.",
                        Price = 39.99m,
                        CostPrice = 20.00m,
                        MinStockThreshold = 25,
                        ImageUrl = "https://via.placeholder.com/400x200/20c997/ffffff?text=Speed+Racer",
                        CategoryId = categories.First(c => c.Name == "Racing").Id,
                        SupplierId = suppliers.First(s => s.Name == "Pixel Perfect Games").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-3),
                        Stock = 150
                    },
                    new Game
                    {
                        Title = "Empire Builder",
                        Description = "Build and manage your own empire in this deep strategy game. Conquer enemies and expand your territory.",
                        Price = 44.99m,
                        CostPrice = 22.50m,
                        MinStockThreshold = 15,
                        ImageUrl = "https://via.placeholder.com/400x200/ffc107/000000?text=Empire+Builder",
                        CategoryId = categories.First(c => c.Name == "Strategy").Id,
                        SupplierId = suppliers.First(s => s.Name == "Epic Entertainment").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-8),
                        Stock = 60
                    },
                    new Game
                    {
                        Title = "Haunted Mansion",
                        Description = "Survive the night in this terrifying horror game. Uncover the mysteries of the haunted mansion.",
                        Price = 29.99m,
                        CostPrice = 15.00m,
                        MinStockThreshold = 30,
                        ImageUrl = "https://via.placeholder.com/400x200/333333/ffffff?text=Haunted+Mansion",
                        CategoryId = categories.First(c => c.Name == "Horror").Id,
                        SupplierId = suppliers.First(s => s.Name == "Pixel Perfect Games").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-1),
                        Stock = 200
                    },
                    new Game
                    {
                        Title = "Soccer Pro 2026",
                        Description = "The most realistic soccer simulation game. Play as your favorite teams and win the championship.",
                        Price = 59.99m,
                        CostPrice = 30.00m,
                        MinStockThreshold = 50,
                        ImageUrl = "https://via.placeholder.com/400x200/17a2b8/ffffff?text=Soccer+Pro",
                        CategoryId = categories.First(c => c.Name == "Sports").Id,
                        SupplierId = suppliers.First(s => s.Name == "GameTech Studios").Id,
                        ReleaseDate = DateTime.Now,
                        Stock = 300
                    },
                    new Game
                    {
                        Title = "Mystery Island",
                        Description = "Explore a mysterious island filled with puzzles and hidden secrets. Can you escape?",
                        Price = 24.99m,
                        CostPrice = 12.50m,
                        MinStockThreshold = 15,
                        ImageUrl = "https://via.placeholder.com/400x200/6f42c1/ffffff?text=Mystery+Island",
                        CategoryId = categories.First(c => c.Name == "Puzzle").Id,
                        SupplierId = suppliers.First(s => s.Name == "Digital Dreams Interactive").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-4),
                        Stock = 80
                    },
                    new Game
                    {
                        Title = "Farm Life Simulator",
                        Description = "Live the peaceful life of a farmer. Grow crops, raise animals, and build your dream farm.",
                        Price = 34.99m,
                        CostPrice = 17.50m,
                        MinStockThreshold = 20,
                        ImageUrl = "https://via.placeholder.com/400x200/28a745/ffffff?text=Farm+Life",
                        CategoryId = categories.First(c => c.Name == "Simulation").Id,
                        SupplierId = suppliers.First(s => s.Name == "Epic Entertainment").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-6),
                        Stock = 120
                    },
                    new Game
                    {
                        Title = "Lost Adventures",
                        Description = "An indie adventure game with beautiful hand-drawn art and an emotional story.",
                        Price = 19.99m,
                        CostPrice = 10.00m,
                        MinStockThreshold = 10,
                        ImageUrl = "https://via.placeholder.com/400x200/e94560/ffffff?text=Lost+Adventures",
                        CategoryId = categories.First(c => c.Name == "Indie").Id,
                        SupplierId = suppliers.First(s => s.Name == "Pixel Perfect Games").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-2),
                        Stock = 50
                    },
                    new Game
                    {
                        Title = "Galactic Conquest",
                        Description = "Command your fleet and conquer the galaxy in this epic space strategy game.",
                        Price = 54.99m,
                        CostPrice = 27.50m,
                        MinStockThreshold = 20,
                        ImageUrl = "https://via.placeholder.com/400x200/6610f2/ffffff?text=Galactic+Conquest",
                        CategoryId = categories.First(c => c.Name == "Strategy").Id,
                        SupplierId = suppliers.First(s => s.Name == "GameTech Studios").Id,
                        ReleaseDate = DateTime.Now.AddMonths(-5),
                        Stock = 90
                    }
                };

                context.Games.AddRange(games);
                await context.SaveChangesAsync();
            }
        }
    }
}