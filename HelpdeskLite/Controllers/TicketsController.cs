using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public TicketsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Tickets
    public async Task<IActionResult> Index(string search, string statusFilter, string priorityFilter, string sortOrder)
    {
        var userId = _userManager.GetUserId(User);
        var tickets = _context.Tickets.AsQueryable();

        // normal user can only see their own tickets
        if (!User.IsInRole("Agent") && !User.IsInRole("Admin"))
        {
            tickets = tickets.Where(t => t.CreatedBy == userId);
        }

        // search
        if (!string.IsNullOrEmpty(search))
        {
            tickets = tickets.Where(t =>
                t.Title.Contains(search) ||
                t.Description.Contains(search));
        }

        // status filter
        if (!string.IsNullOrEmpty(statusFilter))
        {
            tickets = tickets.Where(t => t.Status == statusFilter);
        }

        // priority filter
        if (!string.IsNullOrEmpty(priorityFilter))
        {
            tickets = tickets.Where(t => t.Priority == priorityFilter);
        }

        // sorting
        tickets = sortOrder switch
        {
            "date_asc" => tickets.OrderBy(t => t.CreatedAt),
            "date_desc" => tickets.OrderByDescending(t => t.CreatedAt),
            _ => tickets.OrderByDescending(t => t.CreatedAt)
        };

        return View(await tickets.ToListAsync());
    }

    // GET: Tickets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var ticket = await _context.Tickets
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (ticket == null || ticket.CreatedBy != _userManager.GetUserId(User))
            return Unauthorized();

        return View(ticket);
    }

    // GET: Tickets/Create
    public IActionResult Create() => View();

    // POST: Tickets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,Priority")] Ticket ticket, List<IFormFile> files)
    {
        if (ModelState.IsValid)
        {
            ticket.CreatedBy = _userManager.GetUserId(User)!;
            ticket.CreatedAt = DateTime.Now;
            ticket.Status = "New";

            _context.Add(ticket);
            await _context.SaveChangesAsync();

            // file attachments
            if (files != null && files.Count > 0)
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var attachment = new Attachment
                        {
                            FileName = file.FileName,
                            FilePath = "/uploads/" + fileName,
                            TicketId = ticket.Id
                        };

                        _context.Attachments.Add(attachment);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        return View(ticket);
    }

    // GET: Tickets/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null || ticket.CreatedBy != _userManager.GetUserId(User))
            return Unauthorized();

        return View(ticket);
    }

    // POST: Tickets/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Status,Priority")] Ticket ticket)
    {
        if (id != ticket.Id) return NotFound();

        var dbTicket = await _context.Tickets.FindAsync(id);
        if (dbTicket == null || dbTicket.CreatedBy != _userManager.GetUserId(User))
            return Unauthorized();

        if (ModelState.IsValid)
        {
            dbTicket.Title = ticket.Title;
            dbTicket.Description = ticket.Description;
            dbTicket.Status = ticket.Status;
            dbTicket.Priority = ticket.Priority;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(ticket);
    }

    // POST: Tickets/AddComment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int ticketId, string content)
    {
        if (!ModelState.IsValid)
        {
            var ticketInvalid = await _context.Tickets
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticketInvalid == null) return NotFound();

            return RedirectToAction("Details", new { id = ticketId });
        }

        var comment = new Comment
        {
            Content = content,
            TicketId = ticketId,
            UserId = _userManager.GetUserId(User)!,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    // POST: Tickets/ChangeStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, string status)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();

        ticket.Status = status;

        if (status == "Resolved")
            ticket.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Tickets/AssignToMe
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToMe(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();

        ticket.AssignedToId = _userManager.GetUserId(User);
        ticket.Status = "InProgress";
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Tickets/Download/5
    public async Task<IActionResult> Download(int id)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment == null) return NotFound();

        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
        var contentType = "application/octet-stream";

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return File(bytes, contentType, attachment.FileName);
    }

    // GET: Tickets/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var ticket = await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
        if (ticket == null) return NotFound();

        return View(ticket);
    }

    // POST: Tickets/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
