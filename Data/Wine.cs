using System.ComponentModel.DataAnnotations;

namespace WineTasting.Data;

public class Wine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public int Number { get; set; }

    public WineType Type { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(200)]
    public string Origin { get; set; } = "";

    [MaxLength(2000)]
    public string Description { get; set; } = "";

    [MaxLength(2000)]
    public string Characteristics { get; set; } = "";

    [MaxLength(500)]
    public string? ImagePath { get; set; }

    [MaxLength(500)]
    public string? ImagePath2 { get; set; }

    public decimal? Price { get; set; }

    public List<Rating> Ratings { get; set; } = [];
}
