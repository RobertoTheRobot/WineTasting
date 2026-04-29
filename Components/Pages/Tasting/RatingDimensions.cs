using MudBlazor;
using WineTasting.Data;

namespace WineTasting.Components.Pages.Tasting;

public enum RatingDimension
{
    Aroma,
    Body,
    Flavor,
    Smoothness,
    Overall,
}

public sealed record RatingDimensionInfo(
    RatingDimension Key,
    string Label,
    string Subtitle,
    string Tip,
    string Icon);

public static class RatingDimensions
{
    public static readonly IReadOnlyList<RatingDimensionInfo> All =
    [
        new(RatingDimension.Aroma, "Aroma", "The Nose",
            "How much do you like the smell? Does it jump out of the glass, or is it faint?",
            Icons.Material.Filled.Air),
        new(RatingDimension.Body, "Body", "The Feel",
            "Does it feel light like water, or heavy and rich like whole milk? This is about weight on the tongue.",
            Icons.Material.Filled.LineWeight),
        new(RatingDimension.Flavor, "Flavor", "The Taste",
            "Is the taste pleasant? Note any fruit, spice, or earthy notes you pick up.",
            Icons.Material.Filled.Restaurant),
        new(RatingDimension.Smoothness, "Smoothness", "The Finish",
            "Does it go down easy or feel harsh and bitter? A smooth, long-lasting finish often signals quality.",
            Icons.Material.Filled.Waves),
        new(RatingDimension.Overall, "Overall enjoyability", "Would you buy it?",
            "Forget the technicalities — would you actually want a glass of this again?",
            Icons.Material.Filled.ThumbUp),
    ];
}

public static class RatingExtensions
{
    public static int Get(this Rating r, RatingDimension d) => d switch
    {
        RatingDimension.Aroma => r.Aroma,
        RatingDimension.Body => r.Body,
        RatingDimension.Flavor => r.Flavor,
        RatingDimension.Smoothness => r.Smoothness,
        RatingDimension.Overall => r.Overall,
        _ => 0,
    };
}
