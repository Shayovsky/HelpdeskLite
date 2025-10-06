using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Ticket
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tytuł jest wymagany")]
    [StringLength(100, ErrorMessage = "Tytuł może mieć maksymalnie 100 znaków")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Opis jest wymagany")]
    [StringLength(1000, ErrorMessage = "Opis może mieć maksymalnie 1000 znaków")]
    public string Description { get; set; } = string.Empty;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty;
}
