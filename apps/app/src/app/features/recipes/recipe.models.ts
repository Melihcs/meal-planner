export interface RecipeSummary {
  id: string;
  title: string;
  description: string | null;
  servings: number;
  cuisine: string | null;
  coverColour: string;
  visibility: string;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  ingredientCount: number;
  stepCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface RecipeIngredient {
  id: string;
  ingredientId: string;
  name: string;
  quantity: number;
  unit: string;
  sortOrder: number;
  caloriesPer100G: number | null;
  proteinG: number | null;
  carbsG: number | null;
  fatG: number | null;
  fibreG: number | null;
  edamamFoodId: string | null;
  edamamMeasureUri: string | null;
  category: string | null;
}

export interface RecipeStep {
  id: string;
  stepNumber: number;
  instruction: string;
  stepType: RecipeStepType;
  timerSeconds: number | null;
  backgroundStepId: string | null;
}

export interface RecipeDetail extends RecipeSummary {
  ingredients: RecipeIngredient[];
  steps: RecipeStep[];
}

export interface RecipeUpsertPayload {
  title: string;
  description: string | null;
  servings: number;
  cuisine: string | null;
  coverColour: string | null;
  visibility: string;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
}

export interface RecipeIngredientPayload {
  name: string;
  quantity: number;
  unit: string;
  sortOrder: number | null;
  edamamFoodId: string | null;
  edamamMeasureUri: string | null;
  caloriesPer100G: number | null;
  proteinG: number | null;
  carbsG: number | null;
  fatG: number | null;
  fibreG: number | null;
  category: string | null;
}

export type RecipeStepType = 'sequential' | 'background' | 'timed';

export interface RecipeStepPayload {
  instruction: string;
  stepType: RecipeStepType;
  timerSeconds: number | null;
  backgroundStepId: string | null;
}

export interface RecipeIngredientDraft extends RecipeIngredientPayload {
  draftId: string;
  id?: string;
}

export interface RecipeStepDraft extends RecipeStepPayload {
  draftId: string;
  id?: string;
  stepNumber: number;
  backgroundStepDraftId: string | null;
}

export interface RecipeNutritionSummary {
  calories: number;
  proteinG: number;
  carbsG: number;
  fatG: number;
  fibreG: number;
}

export interface RecipeCard extends RecipeSummary {
  nutrition: RecipeNutritionSummary;
}
