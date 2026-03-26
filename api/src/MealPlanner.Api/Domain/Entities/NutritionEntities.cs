namespace MealPlanner.Api.Domain.Entities;

public sealed class NutrientOrganMapping
{
    public Guid Id { get; set; }
    public string NutrientName { get; set; } = string.Empty;
    public string OrganName { get; set; } = string.Empty;
    public string BodySystem { get; set; } = string.Empty;
    public string? ImpactDescription { get; set; }
    public string? SourceCitation { get; set; }
}
