using System.Collections.Generic;

namespace HelpdeskLite.Models
{
    public class DashboardViewModel
    {
        public int TotalTickets { get; set; }
        public int NewToday { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; } = new();
    }
}
