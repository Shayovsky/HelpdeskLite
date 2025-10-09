using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Ticket
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title has max amount of 100 words")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(1000, ErrorMessage = "Description has max amount of 1000 words")]
    public string Description { get; set; } = string.Empty;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "New";

    [Required]
    public string Priority { get; set; } = "Normal";

    public string? AssignedToId { get; set; }

    // nowe pola dla automatycznego zamykania
    public DateTime? ResolvedAt { get; set; }
    public bool ClosedBySystem { get; set; } = false;
}

