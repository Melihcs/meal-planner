import { provideBrowserGlobalErrorListeners, type ApplicationConfig } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideIonicAngular } from '@ionic/angular/standalone';

import { API_URL } from './core/config/api-url.token';
import { appHttpInterceptors } from './core/http/app-http-interceptors';
import { environment } from '../environments/environment';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors(appHttpInterceptors)),
    provideIonicAngular(),
    {
      provide: API_URL,
      useValue: environment.apiUrl,
    },
  ],
};
