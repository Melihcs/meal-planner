import { inject, Injectable } from '@angular/core';
import { ToastController, type ToastOptions } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  alertCircleOutline,
  checkmarkCircleOutline,
  informationCircleOutline,
} from 'ionicons/icons';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toastController = inject(ToastController);

  constructor() {
    addIcons({
      alertCircleOutline,
      checkmarkCircleOutline,
      informationCircleOutline,
    });
  }

  showSuccess(message: string): Promise<void> {
    return this.present({
      color: 'success',
      icon: 'checkmark-circle-outline',
      message,
    });
  }

  showError(message: string): Promise<void> {
    return this.present({
      color: 'danger',
      icon: 'alert-circle-outline',
      message,
    });
  }

  showInfo(message: string): Promise<void> {
    return this.present({
      color: 'primary',
      icon: 'information-circle-outline',
      message,
    });
  }

  private async present(options: {
    color: NonNullable<ToastOptions['color']>;
    icon: string;
    message: string;
  }): Promise<void> {
    const toast = await this.toastController.create({
      color: options.color,
      duration: 2500,
      icon: options.icon,
      message: options.message,
      position: 'top',
    });

    await toast.present();
  }
}
