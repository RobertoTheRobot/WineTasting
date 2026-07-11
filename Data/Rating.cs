using System.ComponentModel.DataAnnotations;

namespace WineTasting.Data;

public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ParticipantId { get; set; }
    public Participant? Participant { get; set; }

    public Guid WineId { get; set; }
    public Wine? Wine { get; set; }

    [Range(0, 5)] public int Aroma { get; set; }
    [Range(0, 5)] public int Body { get; set; }
    [Range(0, 5)] public int Flavor { get; set; }
    [Range(0, 5)] public int Smoothness { get; set; }
    [Range(0, 5)] public int Overall { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? GuessedOrigin { get; set; }
}
