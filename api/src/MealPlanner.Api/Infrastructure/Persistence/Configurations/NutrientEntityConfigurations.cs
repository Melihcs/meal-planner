namespace MealPlanner.Api.Infrastructure.Persistence.Configurations;

using MealPlanner.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class NutrientOrganMappingConfiguration : IEntityTypeConfiguration<NutrientOrganMapping>
{
    public void Configure(EntityTypeBuilder<NutrientOrganMapping> builder)
    {
        builder.ToTable("nutrient_organ_mapping");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.NutrientName)
            .HasColumnName("nutrient_name")
            .IsRequired();

        builder.Property(entity => entity.OrganName)
            .HasColumnName("organ_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.BodySystem)
            .HasColumnName("body_system")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.ImpactDescription)
            .HasColumnName("impact_description");

        builder.Property(entity => entity.SourceCitation)
            .HasColumnName("source_citation");

        builder.HasIndex(entity => entity.NutrientName)
            .IsUnique();
    }
}
