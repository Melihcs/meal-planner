import type { RecipeIngredient, RecipeNutritionSummary } from './recipe.models';

export function calculateRecipeNutrition(ingredients: RecipeIngredient[]): RecipeNutritionSummary {
  return ingredients.reduce<RecipeNutritionSummary>(
    (summary, ingredient) => {
      const factor = ingredient.quantity / 100;

      summary.calories += (ingredient.caloriesPer100G ?? 0) * factor;
      summary.proteinG += (ingredient.proteinG ?? 0) * factor;
      summary.carbsG += (ingredient.carbsG ?? 0) * factor;
      summary.fatG += (ingredient.fatG ?? 0) * factor;
      summary.fibreG += (ingredient.fibreG ?? 0) * factor;

      return summary;
    },
    {
      calories: 0,
      proteinG: 0,
      carbsG: 0,
      fatG: 0,
      fibreG: 0,
    },
  );
}

export function formatNutritionValue(value: number): string {
  return value.toFixed(value >= 10 ? 0 : 1);
}
