import { Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

@Component({
  standalone: true,
  selector: 'app-empty-state',
  imports: [IonIcon],
  templateUrl: './app-empty-state.component.html',
  styles: [
    `
      .empty-state {
        align-items: center;
        color: #4b625d;
        display: grid;
        gap: 0.85rem;
        justify-items: center;
        margin: 0 auto;
        max-width: 28rem;
        padding: 1.5rem;
        text-align: center;
      }

      .icon-shell {
        align-items: center;
        background: rgba(59, 111, 82, 0.09);
        border-radius: 999px;
        color: var(--ion-color-primary);
        display: inline-grid;
        height: 3.5rem;
        justify-items: center;
        width: 3.5rem;
      }

      ion-icon {
        font-size: 1.5rem;
      }

      .message {
        font-size: 1rem;
        line-height: 1.7;
        margin: 0;
      }
    `,
  ],
})
export class AppEmptyStateComponent {
  readonly message = input.required<string>();
  readonly icon = input<string | null>(null);
}
