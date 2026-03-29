import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

type ShellPageData = {
  title?: string;
  eyebrow?: string;
  headline?: string;
  description?: string;
};

@Component({
  standalone: true,
  selector: 'app-shell-page',
  imports: [IonContent, IonHeader, IonTitle, IonToolbar],
  templateUrl: './shell-page.component.html',
  styles: [
    `
      .shell-page {
        background:
          radial-gradient(circle at top left, rgba(48, 180, 154, 0.18), transparent 32%),
          radial-gradient(circle at bottom right, rgba(255, 183, 77, 0.18), transparent 26%),
          linear-gradient(180deg, #fffef9 0%, #eef8f7 100%);
        display: grid;
        min-height: 100%;
        padding: 1rem;
      }

      .shell-card {
        align-content: center;
        background: rgba(255, 255, 255, 0.8);
        border: 1px solid rgba(79, 122, 112, 0.16);
        border-radius: 28px;
        box-shadow: 0 18px 42px rgba(30, 48, 70, 0.08);
        display: grid;
        gap: 0.9rem;
        min-height: 100%;
        padding: clamp(1.5rem, 4vw, 2.5rem);
      }

      .eyebrow {
        color: #58726d;
        font-size: 0.82rem;
        font-weight: 700;
        letter-spacing: 0.12em;
        margin: 0;
        text-transform: uppercase;
      }

      h1 {
        color: #17221f;
        font-size: clamp(2rem, 8vw, 3.6rem);
        line-height: 1;
        margin: 0;
        text-wrap: balance;
      }

      .description {
        color: #4b625d;
        font-size: 1rem;
        line-height: 1.7;
        margin: 0;
        max-width: 40rem;
      }
    `,
  ],
})
export class ShellPageComponent {
  protected readonly page = inject(ActivatedRoute).snapshot.data as ShellPageData;
}
