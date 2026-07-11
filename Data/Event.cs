using System.ComponentModel.DataAnnotations;

namespace WineTasting.Data;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public bool IsRevealed { get; set; }

    public List<Wine> Wines { get; set; } = [];
    public List<Participant> Participants { get; set; } = [];
}
