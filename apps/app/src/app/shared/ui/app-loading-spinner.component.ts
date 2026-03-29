import { Component } from '@angular/core';
import { IonSpinner } from '@ionic/angular/standalone';

@Component({
  standalone: true,
  selector: 'app-loading-spinner',
  imports: [IonSpinner],
  templateUrl: './app-loading-spinner.component.html',
  styles: [
    `
      .loading-spinner {
        align-items: center;
        display: grid;
        justify-items: center;
        min-height: 100%;
        width: 100%;
      }

      ion-spinner {
        color: var(--ion-color-primary);
        height: 2rem;
        width: 2rem;
      }
    `,
  ],
})
export class AppLoadingSpinnerComponent {}
