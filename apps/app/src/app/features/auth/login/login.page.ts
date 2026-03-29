import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { IonButton, IonContent, IonSpinner } from '@ionic/angular/standalone';

import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../core/ui/toast.service';
import { environment } from '../../../../environments/environment';

@Component({
  standalone: true,
  selector: 'app-login-page',
  imports: [IonButton, IonContent, IonSpinner],
  templateUrl: './login.page.html',
  styles: [
    `
      .login-page {
        --panel-border: color-mix(in srgb, var(--ion-color-primary) 18%, white);
        align-items: center;
        background:
          radial-gradient(circle at top left, rgba(48, 180, 154, 0.25), transparent 34%),
          radial-gradient(circle at bottom right, rgba(255, 183, 77, 0.26), transparent 28%),
          linear-gradient(160deg, #f5f1e8 0%, #fffdfa 52%, #eef8f7 100%);
        display: grid;
        min-height: 100%;
        padding: 1.5rem;
      }

      .login-panel {
        backdrop-filter: blur(16px);
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid var(--panel-border);
        border-radius: 28px;
        box-shadow: 0 24px 60px rgba(30, 48, 70, 0.14);
        display: grid;
        gap: 1rem;
        margin: 0 auto;
        max-width: 34rem;
        padding: 2rem;
      }

      .actions {
        display: grid;
        gap: 0.75rem;
      }

      .eyebrow {
        color: #58726d;
        font-size: 0.85rem;
        font-weight: 700;
        letter-spacing: 0.12em;
        margin: 0;
        text-transform: uppercase;
      }

      h1 {
        color: #17221f;
        font-size: clamp(2.25rem, 8vw, 4rem);
        line-height: 0.95;
        margin: 0;
        text-wrap: balance;
      }

      .body {
        color: #4b625d;
        font-size: 1rem;
        line-height: 1.65;
        margin: 0;
      }

      .dev-note {
        color: #6c7f7a;
        font-size: 0.9rem;
        line-height: 1.5;
        margin: 0;
      }

      .loading-state {
        align-items: center;
        color: #35524c;
        display: flex;
        gap: 0.75rem;
      }

      .loading-state p {
        margin: 0;
      }
    `,
  ],
})
export class LoginPageComponent {
  readonly mockAuthEnabled = environment.enableMockAuth;

  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  protected readonly isAuthenticating = signal(false);
  protected readonly loadingMessage = signal('Redirecting to Google…');

  async continueWithGoogle(): Promise<void> {
    if (this.isAuthenticating()) {
      return;
    }

    this.loadingMessage.set('Redirecting to Google…');
    this.isAuthenticating.set(true);

    try {
      await this.authService.signInWithGoogle();
    } catch {
      this.isAuthenticating.set(false);
      await this.toastService.showError('Unable to start Google sign-in.');
    }
  }

  async continueWithEasyLogin(): Promise<void> {
    if (this.isAuthenticating()) {
      return;
    }

    this.loadingMessage.set('Signing you in with the development account…');
    this.isAuthenticating.set(true);

    try {
      await this.authService.signInWithEasyLogin();
      await this.router.navigateByUrl('/tabs/recipes', { replaceUrl: true });
    } catch {
      this.isAuthenticating.set(false);
      await this.toastService.showError('Unable to start Easy Login.');
    }
  }
}
