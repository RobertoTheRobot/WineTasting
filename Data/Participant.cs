using System.ComponentModel.DataAnnotations;

namespace WineTasting.Data;

public class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    [Required, MaxLength(100)]
    public string Alias { get; set; } = "";

    public bool IsFinished { get; set; }

    public List<Rating> Ratings { get; set; } = [];
}
