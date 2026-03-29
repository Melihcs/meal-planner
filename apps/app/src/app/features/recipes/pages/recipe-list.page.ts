import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

import { AppEmptyStateComponent, AppErrorStateComponent, AppLoadingSpinnerComponent } from '../../../shared/ui';
import { RecipeApiService } from '../recipe-api.service';
import { calculateRecipeNutrition, formatNutritionValue } from '../recipe-nutrition';
import type { RecipeCard, RecipeDetail, RecipeSummary } from '../recipe.models';

@Component({
  standalone: true,
  selector: 'app-recipe-list-page',
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
  templateUrl: './recipe-list.page.html',
  styles: [
    `
      .recipes-page {
        background:
          radial-gradient(circle at top left, rgba(48, 180, 154, 0.18), transparent 30%),
          radial-gradient(circle at top right, rgba(255, 194, 102, 0.18), transparent 25%),
          linear-gradient(180deg, #fffef8 0%, #eef8f7 100%);
        display: grid;
        gap: 1.25rem;
        min-height: 100%;
        padding: 1rem 1rem calc(6rem + var(--ion-safe-area-bottom));
      }

      .hero {
        align-items: end;
        display: grid;
        gap: 1rem;
        grid-template-columns: 1.7fr auto;
      }

      .eyebrow {
        color: #58726d;
        font-size: 0.82rem;
        font-weight: 700;
        letter-spacing: 0.12em;
        margin: 0 0 0.5rem;
        text-transform: uppercase;
      }

      h1 {
        color: #17221f;
        font-size: clamp(2rem, 6vw, 3.6rem);
        line-height: 0.95;
        margin: 0;
      }

      .hero-copy p:last-child {
        color: #536763;
        line-height: 1.7;
        margin: 0.85rem 0 0;
        max-width: 40rem;
      }

      .recipes-grid {
        display: grid;
        gap: 1rem;
        grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr));
      }

      .recipe-card {
        background: rgba(255, 255, 255, 0.88);
        border: 1px solid rgba(85, 117, 109, 0.12);
        border-radius: 24px;
        box-shadow: 0 18px 40px rgba(26, 38, 47, 0.08);
        display: grid;
        gap: 1rem;
        overflow: hidden;
        padding: 0;
        text-decoration: none;
      }

      .card-cover {
        min-height: 5rem;
      }

      .card-body {
        display: grid;
        gap: 0.85rem;
        padding: 0 1.15rem 1.15rem;
      }

      .card-meta {
        color: #5b706b;
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        font-size: 0.88rem;
      }

      .card-title {
        color: #182321;
        font-size: 1.3rem;
        margin: 0;
      }

      .card-description {
        color: #566965;
        line-height: 1.6;
        margin: 0;
      }

      .macro-grid {
        display: grid;
        gap: 0.6rem;
        grid-template-columns: repeat(4, minmax(0, 1fr));
      }

      .macro {
        background: #f4f8f7;
        border-radius: 16px;
        display: grid;
        gap: 0.2rem;
        padding: 0.7rem;
      }

      .macro-label {
        color: #68807a;
        font-size: 0.76rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .macro-value {
        color: #17312d;
        font-size: 1rem;
        font-weight: 700;
      }

      @media (max-width: 720px) {
        .hero {
          grid-template-columns: 1fr;
        }

        .macro-grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }
    `,
  ],
})
export class RecipeListPageComponent {
  private readonly recipeApi = inject(RecipeApiService);

  protected readonly cards = signal<RecipeCard[]>([]);
  protected readonly loading = signal(true);
  protected readonly hasError = signal(false);
  protected readonly formatNutritionValue = formatNutritionValue;

  constructor() {
    void this.loadRecipes();
  }

  protected retry(): void {
    void this.loadRecipes();
  }

  private async loadRecipes(): Promise<void> {
    this.loading.set(true);
    this.hasError.set(false);

    try {
      const recipes = await this.recipeApi.getRecipes();
      const cards = await this.loadRecipeCards(recipes);
      this.cards.set(cards);
    } catch {
      this.hasError.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadRecipeCards(recipes: RecipeSummary[]): Promise<RecipeCard[]> {
    const details = await Promise.all(recipes.map((recipe) => this.recipeApi.getRecipe(recipe.id)));
    const detailById = new Map(details.map((detail) => [detail.id, detail]));

    return recipes.map((recipe) => {
      const detail = detailById.get(recipe.id) as RecipeDetail;
      return {
        ...recipe,
        nutrition: calculateRecipeNutrition(detail.ingredients),
      };
    });
  }
}
