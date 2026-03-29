import { CdkDrag, CdkDragHandle, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonButton } from '@ionic/angular/standalone';
import type { CdkDragDrop } from '@angular/cdk/drag-drop';

import type { RecipeStepDraft, RecipeStepType } from '../recipe.models';

@Component({
  standalone: true,
  selector: 'app-step-builder',
  imports: [CdkDrag, CdkDragHandle, CdkDropList, FormsModule, IonButton],
  templateUrl: './step-builder.component.html',
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

      .step-list {
        display: grid;
        gap: 1rem;
      }

      .step-card {
        background: rgba(241, 247, 245, 0.95);
        border: 1px solid rgba(85, 117, 109, 0.12);
        border-radius: 18px;
        display: grid;
        gap: 0.85rem;
        padding: 1rem;
      }

      .step-topline {
        align-items: center;
        display: flex;
        gap: 0.75rem;
        justify-content: space-between;
      }

      .step-meta {
        align-items: center;
        display: flex;
        gap: 0.75rem;
      }

      .step-number {
        align-items: center;
        background: #173c37;
        border-radius: 999px;
        color: white;
        display: inline-grid;
        font-size: 0.8rem;
        font-weight: 700;
        height: 2rem;
        justify-items: center;
        width: 2rem;
      }

      .drag-handle {
        background: none;
        border: 0;
        color: #4e6661;
        cursor: grab;
        font: inherit;
        font-size: 1.1rem;
        padding: 0.25rem 0.45rem;
      }

      .field-grid {
        display: grid;
        gap: 0.75rem;
        grid-template-columns: 1.8fr repeat(3, minmax(0, 1fr));
      }

      label {
        color: #31403d;
        display: grid;
        font-size: 0.88rem;
        font-weight: 600;
        gap: 0.4rem;
      }

      textarea,
      select,
      input {
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

      .dependency-note {
        color: #6b7d79;
        font-size: 0.82rem;
        margin: 0;
      }

      @media (max-width: 780px) {
        .field-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class StepBuilderComponent {
  readonly steps = input.required<RecipeStepDraft[]>();
  readonly stepsChange = output<RecipeStepDraft[]>();

  protected addStep(): void {
    this.emitSteps([
      ...this.steps(),
      {
        draftId: crypto.randomUUID(),
        instruction: '',
        stepType: 'sequential',
        timerSeconds: null,
        backgroundStepId: null,
        backgroundStepDraftId: null,
        stepNumber: this.steps().length + 1,
      },
    ]);
  }

  protected removeStep(draftId: string): void {
    const nextSteps = this.steps().filter((step) => step.draftId !== draftId);
    this.emitSteps(nextSteps);
  }

  protected reorderSteps(event: CdkDragDrop<RecipeStepDraft[]>): void {
    const nextSteps = [...this.steps()];
    moveItemInArray(nextSteps, event.previousIndex, event.currentIndex);
    this.emitSteps(nextSteps);
  }

  protected updateInstruction(draftId: string, instruction: string): void {
    this.patchStep(draftId, { instruction });
  }

  protected updateStepType(draftId: string, stepType: RecipeStepType): void {
    const patch: Partial<RecipeStepDraft> = {
      stepType,
      timerSeconds: stepType === 'timed' ? 300 : null,
    };

    this.patchStep(draftId, patch);
  }

  protected updateTimerSeconds(draftId: string, timerSeconds: string): void {
    this.patchStep(draftId, {
      timerSeconds: timerSeconds === '' ? null : Number(timerSeconds),
    });
  }

  protected updateBackgroundStep(draftId: string, backgroundStepDraftId: string): void {
    this.patchStep(draftId, {
      backgroundStepDraftId: backgroundStepDraftId || null,
    });
  }

  protected backgroundOptionsFor(step: RecipeStepDraft): RecipeStepDraft[] {
    return this.steps().filter(
      (candidate) => candidate.draftId !== step.draftId && candidate.stepType === 'background',
    );
  }

  protected trackByDraftId(_index: number, step: RecipeStepDraft): string {
    return step.draftId;
  }

  private patchStep(draftId: string, patch: Partial<RecipeStepDraft>): void {
    this.emitSteps(
      this.steps().map((step) => (step.draftId === draftId ? { ...step, ...patch } : step)),
    );
  }

  private emitSteps(steps: RecipeStepDraft[]): void {
    const stepTypesByDraftId = new Map(steps.map((step) => [step.draftId, step.stepType]));

    const nextSteps = steps.map((step, index) => ({
      ...step,
      stepNumber: index + 1,
      backgroundStepDraftId:
        step.backgroundStepDraftId && stepTypesByDraftId.get(step.backgroundStepDraftId) === 'background'
          ? step.backgroundStepDraftId
          : null,
    }));

    this.stepsChange.emit(nextSteps);
  }
}
