import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonContent } from '@ionic/angular/standalone';

import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../core/ui/toast.service';
import { AppLoadingSpinnerComponent } from '../../../shared/ui';

@Component({
  standalone: true,
  selector: 'app-auth-callback-page',
  imports: [AppLoadingSpinnerComponent, IonContent],
  templateUrl: './auth-callback.page.html',
  styles: [
    `
      .callback-page {
        --background:
          radial-gradient(circle at top, rgba(48, 180, 154, 0.18), transparent 30%),
          linear-gradient(180deg, #fffef8 0%, #eef8f7 100%);
      }

      .callback-shell {
        align-content: center;
        color: #35524c;
        display: grid;
        gap: 1rem;
        justify-items: center;
        min-height: 100%;
      }

      p {
        font-size: 1rem;
        font-weight: 600;
        margin: 0;
      }
    `,
  ],
})
export class AuthCallbackPageComponent {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  constructor() {
    const token = this.route.snapshot.queryParamMap.get('token');

    queueMicrotask(() => {
      void this.completeCallback(token);
    });
  }

  private async completeCallback(token: string | null): Promise<void> {
    if (!token) {
      await this.toastService.showError('Missing auth token.');
      await this.router.navigateByUrl('/auth/login', { replaceUrl: true });
      return;
    }

    await this.authService.handleCallback(token);
    await this.router.navigateByUrl('/tabs/recipes', { replaceUrl: true });
  }
}
