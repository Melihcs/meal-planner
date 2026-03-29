namespace MealPlanner.Api.Infrastructure.Persistence.Configurations;

using MealPlanner.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlan>
{
    public void Configure(EntityTypeBuilder<WeeklyPlan> builder)
    {
        builder.ToTable("weekly_plan");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(255);

        builder.Property(entity => entity.WeekStart)
            .HasColumnName("week_start");

        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasDefaultValue("draft")
            .IsRequired();

        builder.Property(entity => entity.Notes)
            .HasColumnName("notes");

        builder.Property(entity => entity.GeneratedAt)
            .HasColumnName("generated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(entity => entity.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(entity => entity.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasIndex(entity => new { entity.UserId, entity.WeekStart })
            .IsUnique();

        builder.HasOne(entity => entity.User)
            .WithMany(user => user.WeeklyPlans)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_weekly_plan_status",
                "status IN ('draft', 'confirmed')");
        });
    }
}

internal sealed class PlanSlotConfiguration : IEntityTypeConfiguration<PlanSlot>
{
    public void Configure(EntityTypeBuilder<PlanSlot> builder)
    {
        builder.ToTable("plan_slot");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.PlanId)
            .HasColumnName("plan_id");

        builder.Property(entity => entity.RecipeId)
            .HasColumnName("recipe_id");

        builder.Property(entity => entity.SlotDate)
            .HasColumnName("slot_date");

        builder.Property(entity => entity.MealType)
            .HasColumnName("meal_type")
            .IsRequired();

        builder.Property(entity => entity.Servings)
            .HasColumnName("servings")
            .HasDefaultValue(1);

        builder.Property(entity => entity.Rationale)
            .HasColumnName("rationale");

        builder.Property(entity => entity.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.HasOne(entity => entity.Plan)
            .WithMany(plan => plan.Slots)
            .HasForeignKey(entity => entity.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Recipe)
            .WithMany(recipe => recipe.PlanSlots)
            .HasForeignKey(entity => entity.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_plan_slot_servings_positive", "servings > 0");
            tableBuilder.HasCheckConstraint(
                "ck_plan_slot_meal_type",
                "meal_type IN ('breakfast', 'lunch', 'dinner', 'snack')");
        });
    }
}
