import { Component, input, output } from '@angular/core';
import { IonButton } from '@ionic/angular/standalone';

@Component({
  standalone: true,
  selector: 'app-error-state',
  imports: [IonButton],
  templateUrl: './app-error-state.component.html',
  styles: [
    `
      .error-state {
        align-items: center;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(158, 67, 54, 0.16);
        border-radius: 24px;
        box-shadow: 0 16px 40px rgba(72, 37, 28, 0.08);
        display: grid;
        gap: 1rem;
        justify-items: center;
        margin: 0 auto;
        max-width: 28rem;
        padding: 1.5rem;
        text-align: center;
      }

      .copy {
        display: grid;
        gap: 0.5rem;
      }

      .eyebrow {
        color: #9e4336;
        font-size: 0.82rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        margin: 0;
        text-transform: uppercase;
      }

      .message {
        color: #3a2a25;
        font-size: 1rem;
        line-height: 1.6;
        margin: 0;
      }
    `,
  ],
})
export class AppErrorStateComponent {
  readonly message = input.required<string>();
  readonly retry = output<void>();
}
