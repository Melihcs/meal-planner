import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonButton } from '@ionic/angular/standalone';

import type { RecipeIngredientDraft } from '../recipe.models';

@Component({
  standalone: true,
  selector: 'app-ingredient-builder',
  imports: [FormsModule, IonButton],
  templateUrl: './ingredient-builder.component.html',
  styles: [
    `
      .builder-card {
        background: rgba(255, 255, 255, 0.86);
        border: 1px solid rgba(85, 117, 109, 0.14);
        border-radius: 24px;
        box-shadow: 0 18px 40px rgba(26, 38, 47, 0.07);
        display: grid;
        gap: 1rem;
        padding: 1.25rem;
      }

      .header {
        display: grid;
        gap: 0.35rem;
      }

      .eyebrow {
        color: #58726d;
        font-size: 0.78rem;
        font-weight: 700;
        letter-spacing: 0.12em;
        margin: 0;
        text-transform: uppercase;
      }

      h2 {
        color: #17221f;
        font-size: 1.3rem;
        margin: 0;
      }

      .subcopy {
        color: #5a6d68;
        line-height: 1.6;
        margin: 0;
      }

      .ingredient-list {
        display: grid;
        gap: 1rem;
      }

      .ingredient-row {
        background: rgba(241, 247, 245, 0.95);
        border: 1px solid rgba(85, 117, 109, 0.12);
        border-radius: 18px;
        display: grid;
        gap: 0.85rem;
        padding: 1rem;
      }

      .field-grid,
      .nutrition-grid {
        display: grid;
        gap: 0.75rem;
      }

      .field-grid {
        grid-template-columns: minmax(0, 2fr) repeat(2, minmax(0, 1fr));
      }

      .nutrition-grid {
        grid-template-columns: repeat(5, minmax(0, 1fr));
      }

      label {
        color: #31403d;
        display: grid;
        font-size: 0.88rem;
        font-weight: 600;
        gap: 0.4rem;
      }

      input {
        background: white;
        border: 1px solid rgba(85, 117, 109, 0.18);
        border-radius: 12px;
        color: #17221f;
        font: inherit;
        min-height: 2.75rem;
        padding: 0.7rem 0.85rem;
      }

      .row-actions {
        display: flex;
        justify-content: space-between;
      }

      .nutrition-note {
        color: #6b7d79;
        font-size: 0.82rem;
        margin: 0;
      }

      @media (max-width: 720px) {
        .field-grid,
        .nutrition-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class IngredientBuilderComponent {
  readonly ingredients = input.required<RecipeIngredientDraft[]>();
  readonly ingredientsChange = output<RecipeIngredientDraft[]>();

  protected addIngredient(): void {
    this.ingredientsChange.emit([
      ...this.ingredients(),
      {
        draftId: crypto.randomUUID(),
        name: '',
        quantity: 1,
        unit: 'g',
        sortOrder: this.ingredients().length + 1,
        edamamFoodId: null,
        edamamMeasureUri: null,
        caloriesPer100G: null,
        proteinG: null,
        carbsG: null,
        fatG: null,
        fibreG: null,
        category: null,
      },
    ]);
  }

  protected removeIngredient(draftId: string): void {
    const nextIngredients = this.ingredients()
      .filter((ingredient) => ingredient.draftId !== draftId)
      .map((ingredient, index) => ({ ...ingredient, sortOrder: index + 1 }));

    this.ingredientsChange.emit(nextIngredients);
  }

  protected updateText(
    draftId: string,
    field: 'name' | 'unit' | 'category',
    value: string,
  ): void {
    this.patchIngredient(draftId, { [field]: value } as Partial<RecipeIngredientDraft>);
  }

  protected updateNumber(
    draftId: string,
    field:
      | 'quantity'
      | 'caloriesPer100G'
      | 'proteinG'
      | 'carbsG'
      | 'fatG'
      | 'fibreG',
    value: string,
  ): void {
    this.patchIngredient(draftId, {
      [field]: value === '' ? null : Number(value),
    } as Partial<RecipeIngredientDraft>);
  }

  protected trackByDraftId(_index: number, ingredient: RecipeIngredientDraft): string {
    return ingredient.draftId;
  }

  private patchIngredient(draftId: string, patch: Partial<RecipeIngredientDraft>): void {
    this.ingredientsChange.emit(
      this.ingredients().map((ingredient) =>
        ingredient.draftId === draftId ? { ...ingredient, ...patch } : ingredient,
      ),
    );
  }
}
