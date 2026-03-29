import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  standalone: true,
  selector: 'app-profile-page',
  imports: [IonButton, IonContent, IonHeader, IonTitle, IonToolbar, RouterLink],
  templateUrl: './profile.page.html',
  styles: [
    `
      .profile-page {
        background:
          radial-gradient(circle at top left, rgba(48, 180, 154, 0.18), transparent 34%),
          linear-gradient(180deg, #fffef9 0%, #eef8f7 100%);
        display: grid;
        min-height: 100%;
        padding: 1rem 1rem calc(1rem + var(--ion-safe-area-bottom) + 5rem);
      }

      .profile-card {
        align-content: center;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(79, 122, 112, 0.16);
        border-radius: 28px;
        box-shadow: 0 18px 42px rgba(30, 48, 70, 0.08);
        display: grid;
        gap: 0.95rem;
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
        font-size: clamp(2rem, 8vw, 3.2rem);
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

      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
      }
    `,
  ],
})
export class ProfilePageComponent {
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  async signOut(): Promise<void> {
    await this.authService.signOut();
    void this.toastService.showInfo('Signed out.');
    window.location.replace('/auth/login');
  }
}
