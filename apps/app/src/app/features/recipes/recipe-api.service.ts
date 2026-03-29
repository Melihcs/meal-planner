import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_URL } from '../../core/config/api-url.token';
import type {
  RecipeDetail,
  RecipeIngredient,
  RecipeIngredientPayload,
  RecipeStep,
  RecipeStepPayload,
  RecipeSummary,
  RecipeUpsertPayload,
} from './recipe.models';

@Injectable({ providedIn: 'root' })
export class RecipeApiService {
  private readonly apiUrl = inject(API_URL);
  private readonly http = inject(HttpClient);

  async getRecipes(): Promise<RecipeSummary[]> {
    return firstValueFrom(this.http.get<RecipeSummary[]>(`${this.apiUrl}/recipes`));
  }

  async getRecipe(recipeId: string): Promise<RecipeDetail> {
    return firstValueFrom(this.http.get<RecipeDetail>(`${this.apiUrl}/recipes/${recipeId}`));
  }

  async createRecipe(payload: RecipeUpsertPayload): Promise<RecipeDetail> {
    return firstValueFrom(this.http.post<RecipeDetail>(`${this.apiUrl}/recipes`, payload));
  }

  async updateRecipe(recipeId: string, payload: RecipeUpsertPayload): Promise<RecipeDetail> {
    return firstValueFrom(this.http.put<RecipeDetail>(`${this.apiUrl}/recipes/${recipeId}`, payload));
  }

  async deleteRecipe(recipeId: string): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/recipes/${recipeId}`));
  }

  async addIngredient(recipeId: string, payload: RecipeIngredientPayload): Promise<RecipeIngredient> {
    return firstValueFrom(
      this.http.post<RecipeIngredient>(`${this.apiUrl}/recipes/${recipeId}/ingredients`, payload),
    );
  }

  async updateIngredient(
    recipeId: string,
    ingredientId: string,
    payload: RecipeIngredientPayload,
  ): Promise<RecipeIngredient> {
    return firstValueFrom(
      this.http.put<RecipeIngredient>(
        `${this.apiUrl}/recipes/${recipeId}/ingredients/${ingredientId}`,
        payload,
      ),
    );
  }

  async deleteIngredient(recipeId: string, ingredientId: string): Promise<void> {
    await firstValueFrom(
      this.http.delete<void>(`${this.apiUrl}/recipes/${recipeId}/ingredients/${ingredientId}`),
    );
  }

  async addStep(recipeId: string, payload: RecipeStepPayload): Promise<RecipeStep> {
    return firstValueFrom(this.http.post<RecipeStep>(`${this.apiUrl}/recipes/${recipeId}/steps`, payload));
  }

  async updateStep(recipeId: string, stepId: string, payload: RecipeStepPayload): Promise<RecipeStep> {
    return firstValueFrom(
      this.http.put<RecipeStep>(`${this.apiUrl}/recipes/${recipeId}/steps/${stepId}`, payload),
    );
  }

  async reorderSteps(recipeId: string, stepIds: string[]): Promise<RecipeStep[]> {
    return firstValueFrom(
      this.http.put<RecipeStep[]>(`${this.apiUrl}/recipes/${recipeId}/steps/reorder`, { stepIds }),
    );
  }

  async deleteStep(recipeId: string, stepId: string): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/recipes/${recipeId}/steps/${stepId}`));
  }
}
