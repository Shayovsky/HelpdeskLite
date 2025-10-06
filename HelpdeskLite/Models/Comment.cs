using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

public class Comment
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Treść komentarza jest wymagana")]
    [StringLength(500, ErrorMessage = "Komentarz może mieć maksymalnie 500 znaków")]
    public string Content { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public IdentityUser User { get; set; } = null!;
}
