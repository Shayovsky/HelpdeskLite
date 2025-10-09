using HelpdeskLite.Data;
using HelpdeskLite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace HelpdeskLite.Controllers
{
    [Authorize(Roles = "Agent,Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var tickets = _context.Tickets.ToList();

            var model = new DashboardViewModel
            {
                TotalTickets = tickets.Count,
                NewToday = tickets.Count(t => t.CreatedAt.Date == DateTime.Today),
                StatusCounts = tickets
                    .GroupBy(t => t.Status)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return View(model);
        }
    }
}
