using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

public class TicketCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketCleanupService> _logger;

    public TicketCleanupService(IServiceScopeFactory scopeFactory, ILogger<TicketCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var oldTickets = await context.Tickets
                        .Where(t => t.Status == "Resolved"
                                    && t.ResolvedAt != null
                                    && t.ResolvedAt < DateTime.UtcNow.AddDays(-7)
                                    && !t.ClosedBySystem)
                        .ToListAsync(stoppingToken);

                    foreach (var ticket in oldTickets)
                    {
                        ticket.Status = "Zamknięty";
                        ticket.ClosedBySystem = true;

                        //komentarz
                        context.Comments.Add(new Comment
                        {
                            TicketId = ticket.Id,
                            Content = "Zgłoszenie zostało automatycznie zamknięte przez system po 7 dniach.",
                            CreatedAt = DateTime.UtcNow,
                            UserId = null
                        });
                    }

                    if (oldTickets.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Automatycznie zamknięto {count} zgłoszeń", oldTickets.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas automatycznego zamykania zgłoszeń");
            }

            //czk 1 dzień
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
