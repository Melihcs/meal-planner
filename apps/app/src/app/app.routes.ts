import type { Route, Routes } from '@angular/router';

import {
  authCanActivateChildGuard,
  authCanMatchGuard,
  guestOnlyCanMatchGuard,
} from './core/auth/auth.guard';

const shellPage = (data: {
  title: string;
  eyebrow: string;
  headline: string;
  description: string;
}): Route => ({
  loadComponent: () =>
    import('./features/shell/shell-page.component').then((module) => module.ShellPageComponent),
  data,
});

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'tabs/recipes',
  },
  {
    path: 'auth',
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'login',
      },
      {
        path: 'login',
        canMatch: [guestOnlyCanMatchGuard],
        loadComponent: () =>
          import('./features/auth/login/login.page').then((module) => module.LoginPageComponent),
      },
      {
        path: 'callback',
        loadComponent: () =>
          import('./features/auth/callback/auth-callback.page').then(
            (module) => module.AuthCallbackPageComponent,
          ),
      },
    ],
  },
  {
    path: 'tabs',
    canMatch: [authCanMatchGuard],
    canActivateChild: [authCanActivateChildGuard],
    loadComponent: () =>
      import('./features/tabs/tabs-shell.component').then((module) => module.TabsShellComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'recipes',
      },
      {
        path: 'recipes',
        children: [
          {
            path: '',
            pathMatch: 'full',
            ...shellPage({
              title: 'Recipes',
              eyebrow: 'Recipes tab',
              headline: 'Recipe library and drafting land here.',
              description:
                'This route anchors recipe browsing, creation, and details for the rest of MVP 1.',
            }),
          },
          {
            path: 'new',
            ...shellPage({
              title: 'New recipe',
              eyebrow: 'Recipes flow',
              headline: 'Recipe creation starts here.',
              description:
                'Future recipe form tasks attach to this route instead of reshaping the shell later.',
            }),
          },
          {
            path: ':recipeId',
            ...shellPage({
              title: 'Recipe detail',
              eyebrow: 'Recipes flow',
              headline: 'Recipe details, nutrition, and actions live here.',
              description:
                'Subsequent recipe tasks can add detail, nutrition, comments, and saves on this route.',
            }),
          },
        ],
      },
      {
        path: 'plan',
        children: [
          {
            path: '',
            pathMatch: 'full',
            ...shellPage({
              title: 'Plan',
              eyebrow: 'Planning tab',
              headline: 'Meal planning starts from a single home screen.',
              description:
                'Plan generation, reviews, and body-map tools all hang from this route tree.',
            }),
          },
          {
            path: 'body-map',
            ...shellPage({
              title: 'Body map',
              eyebrow: 'Planning flow',
              headline: 'Nutrient and body-system planning tools plug in here.',
              description:
                'MVP 2 uses this route for the body map overlay and health-focused planning aids.',
            }),
          },
          {
            path: ':planId/review',
            ...shellPage({
              title: 'Plan review',
              eyebrow: 'Planning flow',
              headline: 'Generated plans get reviewed and confirmed here.',
              description:
                'Plan generation and revision tasks can target this route without changing navigation.',
            }),
          },
        ],
      },
      {
        path: 'history',
        children: [
          {
            path: '',
            pathMatch: 'full',
            ...shellPage({
              title: 'History',
              eyebrow: 'History tab',
              headline: 'Completed meals and cooking history live here.',
              description:
                'This tab stays reserved for review, analytics, and meal-history tasks later in the roadmap.',
            }),
          },
        ],
      },
      {
        path: 'profile',
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/profile/profile.page').then((module) => module.ProfilePageComponent),
          },
          {
            path: 'household',
            children: [
              {
                path: '',
                pathMatch: 'full',
                ...shellPage({
                  title: 'Household',
                  eyebrow: 'Profile flow',
                  headline: 'Household setup and membership will live here.',
                  description:
                    'MVP 2 household creation, joining, and management screens attach here.',
                }),
              },
              {
                path: 'member/:memberId',
                ...shellPage({
                  title: 'Member profile',
                  eyebrow: 'Profile flow',
                  headline: 'Household member details belong here.',
                  description:
                    'Future member-profile routes can expand here without disturbing the tab shell.',
                }),
              },
              {
                path: 'member/:memberId/dietary-profile',
                ...shellPage({
                  title: 'Dietary profile',
                  eyebrow: 'Profile flow',
                  headline: 'Dietary needs and compatibility settings fit here.',
                  description:
                    'Dietary profile editing is reserved on this route for later MVP 2 tasks.',
                }),
              },
            ],
          },
        ],
      },
      // MVP 3 adds the Discover tab once search and social discovery ship.
    ],
  },
  {
    path: '**',
    redirectTo: 'tabs/recipes',
  },
];
