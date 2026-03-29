import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

import { ToastService } from '../../../core/ui/toast.service';
import { AppEmptyStateComponent, AppErrorStateComponent, AppLoadingSpinnerComponent } from '../../../shared/ui';
import { RecipeApiService } from '../recipe-api.service';
import { calculateRecipeNutrition, formatNutritionValue } from '../recipe-nutrition';
import type { RecipeDetail } from '../recipe.models';

@Component({
  standalone: true,
  selector: 'app-recipe-detail-page',
  imports: [
    AppEmptyStateComponent,
    AppErrorStateComponent,
    AppLoadingSpinnerComponent,
    IonButton,
    IonContent,
    IonHeader,
    IonTitle,
    IonToolbar,
    RouterLink,
  ],
  templateUrl: './recipe-detail.page.html',
  styles: [
    `
      .recipe-page {
        background:
          radial-gradient(circle at top left, rgba(48, 180, 154, 0.18), transparent 30%),
          linear-gradient(180deg, #fffef9 0%, #eef8f7 100%);
        display: grid;
        gap: 1rem;
        min-height: 100%;
        padding: 1rem 1rem calc(6rem + var(--ion-safe-area-bottom));
      }

      .hero,
      .panel {
        background: rgba(255, 255, 255, 0.88);
        border: 1px solid rgba(85, 117, 109, 0.12);
        border-radius: 24px;
        box-shadow: 0 18px 40px rgba(26, 38, 47, 0.08);
        padding: 1.15rem;
      }

      .hero {
        display: grid;
        gap: 1rem;
      }

      .cover-band {
        border-radius: 18px;
        min-height: 7rem;
      }

      .hero-topline {
        align-items: start;
        display: grid;
        gap: 1rem;
        grid-template-columns: 1fr auto;
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

      h1 {
        font-size: clamp(2rem, 6vw, 3.4rem);
        line-height: 0.95;
      }

      .hero-description,
      .step-copy,
      .ingredient-copy {
        color: #536763;
        line-height: 1.7;
        margin: 0;
      }

      .hero-meta,
      .nutrition-grid,
      .detail-grid {
        display: grid;
        gap: 0.75rem;
      }

      .hero-meta {
        grid-template-columns: repeat(4, minmax(0, 1fr));
      }

      .meta-card,
      .nutrition-card {
        background: #f4f8f7;
        border-radius: 18px;
        display: grid;
        gap: 0.2rem;
        padding: 0.8rem;
      }

      .meta-label,
      .nutrition-label {
        color: #68807a;
        font-size: 0.76rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .meta-value,
      .nutrition-value {
        color: #17312d;
        font-size: 1rem;
        font-weight: 700;
      }

      .hero-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
      }

      .detail-grid {
        grid-template-columns: minmax(0, 1.15fr) minmax(0, 0.85fr);
      }

      .nutrition-grid {
        grid-template-columns: repeat(5, minmax(0, 1fr));
      }

      table {
        border-collapse: collapse;
        width: 100%;
      }

      th,
      td {
        border-bottom: 1px solid rgba(85, 117, 109, 0.12);
        padding: 0.75rem 0;
        text-align: left;
      }

      ol {
        display: grid;
        gap: 0.85rem;
        margin: 0;
        padding-left: 1.2rem;
      }

      li {
        color: #24312e;
        padding-left: 0.2rem;
      }

      .step-meta {
        color: #617571;
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-top: 0.25rem;
      }

      @media (max-width: 840px) {
        .hero-topline,
        .detail-grid,
        .hero-meta,
        .nutrition-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class RecipeDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly recipeApi = inject(RecipeApiService);
  private readonly toastService = inject(ToastService);

  protected readonly recipe = signal<RecipeDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly hasError = signal(false);
  protected readonly nutrition = computed(() =>
    this.recipe() ? calculateRecipeNutrition(this.recipe()!.ingredients) : null,
  );
  protected readonly formatNutritionValue = formatNutritionValue;
  protected readonly recipeId = this.route.snapshot.paramMap.get('recipeId') ?? '';

  constructor() {
    void this.loadRecipe();
  }

  protected retry(): void {
    void this.loadRecipe();
  }

  protected async deleteRecipe(): Promise<void> {
    const recipe = this.recipe();
    if (!recipe || !window.confirm(`Delete "${recipe.title}"?`)) {
      return;
    }

    try {
      await this.recipeApi.deleteRecipe(recipe.id);
      await this.toastService.showInfo('Recipe deleted.');
      await this.router.navigateByUrl('/tabs/recipes', { replaceUrl: true });
    } catch {
      await this.toastService.showError('Recipe could not be deleted.');
    }
  }

  protected async startCooking(): Promise<void> {
    await this.toastService.showInfo('Cooking mode lands in a later task.');
  }

  private async loadRecipe(): Promise<void> {
    this.loading.set(true);
    this.hasError.set(false);

    try {
      const recipe = await this.recipeApi.getRecipe(this.recipeId);
      this.recipe.set(recipe);
    } catch {
      this.hasError.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}
