using System.ComponentModel.DataAnnotations;

public class Attachment
{
    public int Id { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
}
