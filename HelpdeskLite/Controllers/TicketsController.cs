using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize] // tylko zalogowani
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
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var tickets = await _context.Tickets
            .Where(t => t.CreatedBy == userId)
            .ToListAsync();

        return View(tickets);
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
    public IActionResult Create()
    {
        return View();
    }

    // POST: Tickets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description")] Ticket ticket, List<IFormFile> files)
    {
        if (ModelState.IsValid)
        {
            ticket.CreatedBy = _userManager.GetUserId(User)!;
            ticket.CreatedAt = DateTime.Now;

            _context.Add(ticket);
            await _context.SaveChangesAsync();

            // obsługa załączników
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
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description")] Ticket ticket)
    {
        if (id != ticket.Id) return NotFound();

        var dbTicket = await _context.Tickets.FindAsync(id);
        if (dbTicket == null || dbTicket.CreatedBy != _userManager.GetUserId(User))
            return Unauthorized();

        if (ModelState.IsValid)
        {
            dbTicket.Title = ticket.Title;
            dbTicket.Description = ticket.Description;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(ticket);
    }

    // GET: Tickets/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var ticket = await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
        if (ticket == null || ticket.CreatedBy != _userManager.GetUserId(User))
            return Unauthorized();

        return View(ticket);
    }

    // POST: Tickets/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null || ticket.CreatedBy != _userManager.GetUserId(User))
            return Unauthorized();

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST: Tickets/AddComment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int ticketId, [Bind("Content")] Comment comment)
    {
        // znajdź ticket wraz z komentarzami i autorem komentarzy (do wyświetlenia przy błędzie)
        var ticket = await _context.Tickets
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return NotFound();

        // walidacja wejścia -> jeśli niepoprawne, wracamy do widoku Details z modelem ticket (błędy będą widoczne)
        if (!ModelState.IsValid)
        {
            // przekazujemy ten sam ticket (z komentarzami), żeby formularz i błędy były widoczne
            return View("Details", ticket);
        }

        // przypisz użytkownika i zwiąż z ticketem
        comment.TicketId = ticketId;
        comment.UserId = _userManager.GetUserId(User)!;
        comment.CreatedAt = DateTime.UtcNow;

        _context.Comments.Add(comment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // jeśli masz ILogger możesz logować
            ModelState.AddModelError("", "Wystąpił błąd przy zapisie komentarza.");
            return View("Details", ticket);
        }

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }


    // GET: Tickets/Download/5
    public async Task<IActionResult> Download(int id)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment == null)
            return NotFound();

        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
        var contentType = "application/octet-stream";

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return File(bytes, contentType, attachment.FileName);
    }
}
