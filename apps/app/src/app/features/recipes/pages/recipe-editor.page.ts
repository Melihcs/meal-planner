import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

import { ToastService } from '../../../core/ui/toast.service';
import { AppErrorStateComponent, AppLoadingSpinnerComponent } from '../../../shared/ui';
import { IngredientBuilderComponent } from '../components/ingredient-builder.component';
import { StepBuilderComponent } from '../components/step-builder.component';
import { RecipeApiService } from '../recipe-api.service';
import type {
  RecipeDetail,
  RecipeIngredientDraft,
  RecipeIngredientPayload,
  RecipeStepDraft,
  RecipeStepType,
  RecipeUpsertPayload,
} from '../recipe.models';

@Component({
  standalone: true,
  selector: 'app-recipe-editor-page',
  imports: [
    AppErrorStateComponent,
    AppLoadingSpinnerComponent,
    IngredientBuilderComponent,
    IonButton,
    IonContent,
    IonHeader,
    IonTitle,
    IonToolbar,
    ReactiveFormsModule,
    RouterLink,
    StepBuilderComponent,
  ],
  templateUrl: './recipe-editor.page.html',
  styles: [
    `
      .editor-page {
        background:
          radial-gradient(circle at top left, rgba(48, 180, 154, 0.18), transparent 32%),
          radial-gradient(circle at top right, rgba(255, 194, 102, 0.18), transparent 24%),
          linear-gradient(180deg, #fffef8 0%, #eef8f7 100%);
        display: grid;
        gap: 1rem;
        min-height: 100%;
        padding: 1rem 1rem calc(6rem + var(--ion-safe-area-bottom));
      }

      .form-shell,
      .draft-card {
        background: rgba(255, 255, 255, 0.88);
        border: 1px solid rgba(85, 117, 109, 0.12);
        border-radius: 24px;
        box-shadow: 0 18px 40px rgba(26, 38, 47, 0.08);
        padding: 1.15rem;
      }

      .form-shell {
        display: grid;
        gap: 1rem;
      }

      .eyebrow {
        color: #58726d;
        font-size: 0.8rem;
        font-weight: 700;
        letter-spacing: 0.12em;
        margin: 0 0 0.4rem;
        text-transform: uppercase;
      }

      h1,
      h2 {
        color: #182321;
        margin: 0;
      }

      .subcopy,
      .helper,
      .error-text {
        margin: 0;
      }

      .subcopy,
      .helper {
        color: #566965;
        line-height: 1.6;
      }

      .field-grid {
        display: grid;
        gap: 0.85rem;
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .timing-grid {
        display: grid;
        gap: 0.85rem;
        grid-template-columns: repeat(4, minmax(0, 1fr));
      }

      label {
        color: #31403d;
        display: grid;
        font-size: 0.88rem;
        font-weight: 600;
        gap: 0.4rem;
      }

      input,
      textarea,
      select {
        background: white;
        border: 1px solid rgba(85, 117, 109, 0.18);
        border-radius: 12px;
        color: #17221f;
        font: inherit;
        min-height: 2.75rem;
        padding: 0.7rem 0.85rem;
      }

      textarea {
        min-height: 7rem;
        resize: vertical;
      }

      .draft-card {
        display: grid;
        gap: 0.85rem;
      }

      .tag-row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
      }

      .tag-pill {
        align-items: center;
        background: #edf5f2;
        border-radius: 999px;
        color: #21403a;
        display: inline-flex;
        font-size: 0.88rem;
        gap: 0.5rem;
        padding: 0.5rem 0.85rem;
      }

      .tag-pill button {
        background: none;
        border: 0;
        color: inherit;
        cursor: pointer;
        font: inherit;
        padding: 0;
      }

      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        justify-content: space-between;
      }

      .error-text {
        color: #9e4336;
        font-size: 0.82rem;
      }

      @media (max-width: 840px) {
        .field-grid,
        .timing-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class RecipeEditorPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly recipeApi = inject(RecipeApiService);
  private readonly toastService = inject(ToastService);
  private readonly routeRecipeId = this.route.snapshot.paramMap.get('recipeId');

  protected readonly loading = signal(this.routeRecipeId !== null);
  protected readonly hasError = signal(false);
  protected readonly saving = signal(false);
  protected readonly ingredients = signal<RecipeIngredientDraft[]>([]);
  protected readonly steps = signal<RecipeStepDraft[]>([]);
  protected readonly originalRecipe = signal<RecipeDetail | null>(null);
  protected readonly dietTags = signal<string[]>([]);

  protected readonly recipeForm = this.formBuilder.group({
    title: this.formBuilder.nonNullable.control('', [Validators.required, Validators.minLength(2)]),
    description: this.formBuilder.nonNullable.control(''),
    servings: this.formBuilder.nonNullable.control(2, [Validators.required, Validators.min(1)]),
    cuisine: this.formBuilder.nonNullable.control(''),
    coverColour: this.formBuilder.nonNullable.control('#E8F5E9', [Validators.required]),
    visibility: this.formBuilder.nonNullable.control('private'),
    prepTimeMinutes: this.formBuilder.control<number | null>(null),
    cookTimeMinutes: this.formBuilder.control<number | null>(null),
    dietTagDraft: this.formBuilder.nonNullable.control(''),
  });

  protected readonly isEditMode = this.routeRecipeId !== null;

  constructor() {
    if (this.routeRecipeId) {
      void this.loadRecipe(this.routeRecipeId);
      return;
    }

    this.loading.set(false);
  }

  protected setIngredients(ingredients: RecipeIngredientDraft[]): void {
    this.ingredients.set(ingredients);
  }

  protected setSteps(steps: RecipeStepDraft[]): void {
    this.steps.set(steps);
  }

  protected retry(): void {
    if (!this.routeRecipeId) {
      return;
    }

    void this.loadRecipe(this.routeRecipeId);
  }

  protected addDietTag(): void {
    const value = this.recipeForm.controls.dietTagDraft.value.trim();
    if (!value) {
      return;
    }

    if (!this.dietTags().includes(value)) {
      this.dietTags.set([...this.dietTags(), value]);
    }

    this.recipeForm.controls.dietTagDraft.setValue('');
  }

  protected removeDietTag(tag: string): void {
    this.dietTags.set(this.dietTags().filter((candidate) => candidate !== tag));
  }

  protected async saveRecipe(): Promise<void> {
    if (this.recipeForm.invalid) {
      this.recipeForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    try {
      const payload = this.buildRecipePayload();
      const savedRecipe = this.routeRecipeId
        ? await this.recipeApi.updateRecipe(this.routeRecipeId, payload)
        : await this.recipeApi.createRecipe(payload);

      await this.syncIngredients(savedRecipe.id);
      await this.syncSteps(savedRecipe.id);

      await this.toastService.showSuccess(
        this.routeRecipeId ? 'Recipe updated.' : 'Recipe created.',
      );

      await this.router.navigateByUrl(`/tabs/recipes/${savedRecipe.id}`, { replaceUrl: true });
    } catch {
      await this.toastService.showError('Recipe could not be saved.');
    } finally {
      this.saving.set(false);
    }
  }

  private async loadRecipe(recipeId: string): Promise<void> {
    this.loading.set(true);
    this.hasError.set(false);

    try {
      const recipe = await this.recipeApi.getRecipe(recipeId);
      this.originalRecipe.set(recipe);
      this.recipeForm.patchValue({
        title: recipe.title,
        description: recipe.description ?? '',
        servings: recipe.servings,
        cuisine: recipe.cuisine ?? '',
        coverColour: recipe.coverColour,
        visibility: recipe.visibility,
        prepTimeMinutes: recipe.prepTimeMinutes,
        cookTimeMinutes: recipe.cookTimeMinutes,
      });
      this.ingredients.set(
        recipe.ingredients.map((ingredient) => ({
          draftId: ingredient.id,
          id: ingredient.id,
          name: ingredient.name,
          quantity: ingredient.quantity,
          unit: ingredient.unit,
          sortOrder: ingredient.sortOrder,
          edamamFoodId: ingredient.edamamFoodId,
          edamamMeasureUri: ingredient.edamamMeasureUri,
          caloriesPer100G: ingredient.caloriesPer100G,
          proteinG: ingredient.proteinG,
          carbsG: ingredient.carbsG,
          fatG: ingredient.fatG,
          fibreG: ingredient.fibreG,
          category: ingredient.category,
        })),
      );
      this.steps.set(
        recipe.steps.map((step) => ({
          draftId: step.id,
          id: step.id,
          stepNumber: step.stepNumber,
          instruction: step.instruction,
          stepType: step.stepType,
          timerSeconds: step.timerSeconds,
          backgroundStepId: step.backgroundStepId,
          backgroundStepDraftId: step.backgroundStepId,
        })),
      );
    } catch {
      this.hasError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  private buildRecipePayload(): RecipeUpsertPayload {
    return {
      title: this.recipeForm.controls.title.value.trim(),
      description: this.nullIfBlank(this.recipeForm.controls.description.value),
      servings: Number(this.recipeForm.controls.servings.value),
      cuisine: this.nullIfBlank(this.recipeForm.controls.cuisine.value),
      coverColour: this.nullIfBlank(this.recipeForm.controls.coverColour.value),
      visibility: this.recipeForm.controls.visibility.value,
      prepTimeMinutes: this.recipeForm.controls.prepTimeMinutes.value,
      cookTimeMinutes: this.recipeForm.controls.cookTimeMinutes.value,
    };
  }

  private async syncIngredients(recipeId: string): Promise<void> {
    const originalIngredients = this.originalRecipe()?.ingredients ?? [];
    const nextIngredients = this.ingredients().map((ingredient, index) => ({
      ...ingredient,
      sortOrder: index + 1,
    }));

    const nextIds = new Set(nextIngredients.flatMap((ingredient) => (ingredient.id ? [ingredient.id] : [])));

    for (const ingredient of originalIngredients) {
      if (!nextIds.has(ingredient.id)) {
        await this.recipeApi.deleteIngredient(recipeId, ingredient.id);
      }
    }

    for (const ingredient of nextIngredients) {
      const payload = this.toIngredientPayload(ingredient);
      if (ingredient.id) {
        await this.recipeApi.updateIngredient(recipeId, ingredient.id, payload);
      } else {
        await this.recipeApi.addIngredient(recipeId, payload);
      }
    }
  }

  private async syncSteps(recipeId: string): Promise<void> {
    const originalSteps = this.originalRecipe()?.steps ?? [];
    const nextSteps = this.steps().map((step, index) => ({ ...step, stepNumber: index + 1 }));
    const nextIds = new Set(nextSteps.flatMap((step) => (step.id ? [step.id] : [])));

    for (const step of originalSteps) {
      if (!nextIds.has(step.id)) {
        await this.recipeApi.deleteStep(recipeId, step.id);
      }
    }

    const stepIdsByDraftId = new Map<string, string>();
    for (const step of nextSteps) {
      if (step.id) {
        stepIdsByDraftId.set(step.draftId, step.id);
      }
    }

    for (const step of nextSteps) {
      if (step.id) {
        continue;
      }

      const createdStep = await this.recipeApi.addStep(recipeId, {
        instruction: this.sanitizedInstruction(step),
        stepType: step.stepType,
        timerSeconds: this.normalizedTimer(step.stepType, step.timerSeconds),
        backgroundStepId: null,
      });

      stepIdsByDraftId.set(step.draftId, createdStep.id);
    }

    for (const step of nextSteps) {
      const stepId = step.id ?? stepIdsByDraftId.get(step.draftId);
      if (!stepId) {
        continue;
      }

      const backgroundStepId = step.backgroundStepDraftId
        ? stepIdsByDraftId.get(step.backgroundStepDraftId) ?? null
        : null;

      await this.recipeApi.updateStep(recipeId, stepId, {
        instruction: this.sanitizedInstruction(step),
        stepType: step.stepType,
        timerSeconds: this.normalizedTimer(step.stepType, step.timerSeconds),
        backgroundStepId,
      });
    }

    const orderedStepIds = nextSteps
      .map((step) => step.id ?? stepIdsByDraftId.get(step.draftId))
      .filter((stepId): stepId is string => Boolean(stepId));

    if (orderedStepIds.length > 0) {
      await this.recipeApi.reorderSteps(recipeId, orderedStepIds);
    }
  }

  private toIngredientPayload(ingredient: RecipeIngredientDraft): RecipeIngredientPayload {
    return {
      name: ingredient.name.trim(),
      quantity: ingredient.quantity,
      unit: ingredient.unit.trim(),
      sortOrder: ingredient.sortOrder,
      edamamFoodId: this.nullIfBlank(ingredient.edamamFoodId),
      edamamMeasureUri: this.nullIfBlank(ingredient.edamamMeasureUri),
      caloriesPer100G: ingredient.caloriesPer100G,
      proteinG: ingredient.proteinG,
      carbsG: ingredient.carbsG,
      fatG: ingredient.fatG,
      fibreG: ingredient.fibreG,
      category: this.nullIfBlank(ingredient.category),
    };
  }

  private sanitizedInstruction(step: RecipeStepDraft): string {
    return step.instruction.trim() || `Step ${step.stepNumber}`;
  }

  private normalizedTimer(stepType: RecipeStepType, timerSeconds: number | null): number | null {
    return stepType === 'timed' ? timerSeconds ?? 300 : null;
  }

  private nullIfBlank(value: string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    const trimmedValue = value.trim();
    return trimmedValue === '' ? null : trimmedValue;
  }
}
