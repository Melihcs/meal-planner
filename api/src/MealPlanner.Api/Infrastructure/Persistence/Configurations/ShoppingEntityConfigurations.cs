namespace MealPlanner.Api.Infrastructure.Persistence.Configurations;

using MealPlanner.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
    public void Configure(EntityTypeBuilder<ShoppingList> builder)
    {
        builder.ToTable("shopping_list");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(255);

        builder.Property(entity => entity.PlanId)
            .HasColumnName("plan_id");

        builder.Property(entity => entity.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(entity => entity.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasOne(entity => entity.User)
            .WithMany(user => user.ShoppingLists)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Plan)
            .WithMany(plan => plan.ShoppingLists)
            .HasForeignKey(entity => entity.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class ShoppingItemConfiguration : IEntityTypeConfiguration<ShoppingItem>
{
    public void Configure(EntityTypeBuilder<ShoppingItem> builder)
    {
        builder.ToTable("shopping_item");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.ListId)
            .HasColumnName("list_id");

        builder.Property(entity => entity.IngredientId)
            .HasColumnName("ingredient_id");

        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(10, 3);

        builder.Property(entity => entity.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50);

        builder.Property(entity => entity.Category)
            .HasColumnName("category");

        builder.Property(entity => entity.IsChecked)
            .HasColumnName("is_checked")
            .HasDefaultValue(false);

        builder.Property(entity => entity.IsManual)
            .HasColumnName("is_manual")
            .HasDefaultValue(false);

        builder.Property(entity => entity.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.HasOne(entity => entity.List)
            .WithMany(list => list.Items)
            .HasForeignKey(entity => entity.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Ingredient)
            .WithMany(ingredient => ingredient.ShoppingItems)
            .HasForeignKey(entity => entity.IngredientId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
