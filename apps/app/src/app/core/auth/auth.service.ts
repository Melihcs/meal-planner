import { HttpClient } from '@angular/common/http';
import { App } from '@capacitor/app';
import { Browser } from '@capacitor/browser';
import { Capacitor } from '@capacitor/core';
import { Preferences } from '@capacitor/preferences';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { API_URL } from '../config/api-url.token';

const AUTH_TOKEN_STORAGE_KEY = 'meal-planner.auth-token';
const MOBILE_AUTH_CALLBACK_URI = 'com.mealplanner://auth/callback';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = inject(API_URL);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly token = signal<string | null>(null);
  private readonly hydrated = signal(false);

  readonly isAuthenticatedState = computed(() => this.token() !== null);

  constructor() {
    void this.hydrateToken();
    this.registerMobileCallbackListener();
    void this.handleLaunchUrl();
  }

  async signInWithGoogle(): Promise<void> {
    const redirectUri = this.isNativeMobile()
      ? MOBILE_AUTH_CALLBACK_URI
      : `${window.location.origin}/auth/callback`;

    const authUrl = `${this.apiUrl}/auth/google?redirect_uri=${encodeURIComponent(redirectUri)}`;

    if (this.isNativeMobile()) {
      await Browser.open({ url: authUrl });
      return;
    }

    window.location.assign(authUrl);
  }

  async handleCallback(token: string): Promise<void> {
    await this.persistToken(token);
  }

  async signInWithEasyLogin(): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<DevelopmentLoginResponse>(`${this.apiUrl}/auth/dev-login`, {}),
    );

    await this.handleCallback(response.token);
  }

  async signOut(): Promise<void> {
    await this.persistToken(null);
  }

  async getToken(): Promise<string | null> {
    return this.ensureHydrated();
  }

  async isAuthenticated(): Promise<boolean> {
    return (await this.getToken()) !== null;
  }

  private async ensureHydrated(): Promise<string | null> {
    if (!this.hydrated()) {
      await this.hydrateToken();
    }

    return this.token();
  }

  private async hydrateToken(): Promise<void> {
    const storedToken = await this.readStoredToken();
    this.token.set(storedToken);
    this.hydrated.set(true);
  }

  private async persistToken(token: string | null): Promise<void> {
    this.token.set(token);
    this.hydrated.set(true);

    if (this.isNativeMobile()) {
      if (token === null) {
        await Preferences.remove({ key: AUTH_TOKEN_STORAGE_KEY });
        return;
      }

      await Preferences.set({ key: AUTH_TOKEN_STORAGE_KEY, value: token });
      return;
    }

    if (typeof localStorage === 'undefined') {
      return;
    }

    if (token === null) {
      localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
      return;
    }

    localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token);
  }

  private async readStoredToken(): Promise<string | null> {
    if (this.isNativeMobile()) {
      const { value } = await Preferences.get({ key: AUTH_TOKEN_STORAGE_KEY });
      return value;
    }

    if (typeof localStorage === 'undefined') {
      return null;
    }

    return localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
  }

  private registerMobileCallbackListener(): void {
    if (!this.isNativeMobile()) {
      return;
    }

    void App.addListener('appUrlOpen', ({ url }) => {
      void this.handleMobileCallback(url);
    });
  }

  private async handleLaunchUrl(): Promise<void> {
    if (!this.isNativeMobile()) {
      return;
    }

    const launchUrl = await App.getLaunchUrl();
    if (!launchUrl?.url) {
      return;
    }

    await this.handleMobileCallback(launchUrl.url);
  }

  private async handleMobileCallback(url: string): Promise<void> {
    if (!url.startsWith(MOBILE_AUTH_CALLBACK_URI)) {
      return;
    }

    const parsedUrl = new URL(url);
    const token = parsedUrl.searchParams.get('token');
    if (!token) {
      await this.router.navigateByUrl('/auth/login', { replaceUrl: true });
      return;
    }

    await this.handleCallback(token);
    await Browser.close();
    await this.router.navigateByUrl('/tabs/recipes', { replaceUrl: true });
  }

  private isNativeMobile(): boolean {
    const platform = Capacitor.getPlatform();
    return platform === 'ios' || platform === 'android';
  }
}

interface DevelopmentLoginResponse {
  token: string;
}
