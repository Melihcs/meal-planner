# MVP 1 — Core Loop

```yaml
mvp: 1
name: 'Core loop'
description: 'Single user, full cooking & planning experience'
ships_as: 'A fully usable solo meal planning app'
prereqs: []
summary:
  features: 8
  tasks: 63

features:
  - id: F1
    name: 'Project setup & infrastructure'
    description: 'Monorepo, DB schema, CI — do this first, everything depends on it'
    task_count: 15
    groups:
      - group: 'Repo & tooling'
        tasks:
          - id: F1-1
            task: 'Scaffold monorepo with pnpm workspaces and Turborepo. Create the following workspace packages: apps/app (Angular + Ionic), apps/electron (Electron shell), api/ (ASP.NET Core 10), packages/types (shared TypeScript DTOs).'
            note: |
              Pinned versions:
                Angular 21, @ionic/angular 8.x (8.8.1), @capacitor/core 8.x (8.2.0), Electron 34.x
                Node 24 LTS, pnpm 10.x, TypeScript 6.0
                .NET 10 (LTS), EF Core 10
              Notes on versions:
                - Capacitor 8 uses Swift Package Manager (SPM) by default for iOS — follow the Capacitor 8 migration guide for iOS setup
                - TypeScript 6.0 (released March 2026) includes deprecations aimed at the upcoming Go-based TS 7 compiler — heed deprecation warnings from day one
                - Electron: use the latest LTS-supported stable channel; check https://releases.electronjs.org for current stable
                - Angular 21 uses standalone components, signals, and Vitest as the default test runner — no NgModules
              Bootstrap commands:
                pnpm create turbo@latest
                ionic start app blank --type=angular --capacitor (inside apps/)
                dotnet new webapi -n api (inside api/)
              Folder layout after scaffold:
                apps/app/        → Angular + Ionic source
                apps/electron/   → Electron main + preload
                api/             → ASP.NET Core API
                packages/types/  → shared TS interfaces (DTOs)
                turbo.json       → pipeline: build, lint, test
          - id: F1-2
            task: 'Configure ESLint, Prettier, and EditorConfig across all packages'
          - id: F1-3
            task: 'Set up Docker Compose for local dev: PostgreSQL + the .NET API container'
            note: 'Single docker-compose up should give a fully running local environment.'

      - group: 'Configuration'
        tasks:
          - id: F1-4
            task: 'Configure Google OAuth credentials and create .env.example at repo root listing every required variable. Create apps/app/src/environments/environment.ts and environment.prod.ts.'
            note: |
              Google Cloud Console setup (do this before any auth code):
                1. Go to console.cloud.google.com → APIs & Services → Credentials → Create OAuth 2.0 Client ID (Web application type).
                2. Authorized redirect URIs:
                     http://localhost:5000/auth/callback        (local dev)
                     https://yourdomain.com/auth/callback       (production)
                3. Copy the Client ID and Client Secret into your .env file.
                4. In OAuth consent screen: set app name, support email, and add scope: openid, email, profile.

              Required environment variables:
                # API (.env)
                GOOGLE_CLIENT_ID=xxx.apps.googleusercontent.com
                GOOGLE_CLIENT_SECRET=xxx
                JWT_SECRET=xxx                         (min 32 chars, used to sign your own JWTs)
                JWT_ISSUER=https://yourdomain.com
                JWT_AUDIENCE=meal-planner
                JWT_EXPIRY_DAYS=7
                EDAMAM_APP_ID=xxx
                EDAMAM_APP_KEY=xxx
                ANTHROPIC_API_KEY=xxx
                FIREBASE_PROJECT_ID=xxx               # MVP 4, define now
                DATABASE_URL=postgresql://...
                # Angular (apps/app/src/environments/environment.ts)
                apiUrl: 'http://localhost:5000'
              environment.ts and environment.prod.ts must export the full object.
              Never commit real values — .env stays in .gitignore.

      - group: 'Database'
        tasks:
          - id: F1-5
            task: 'Write EF Core migrations for all MVP 1 tables. Create one migration file per logical group (core, recipes, planning, shopping, nutrient-mapping).'
            note: |
              Full column specs for every table. CookingLog is added separately in F8-1 — do not create it here.

              UserProfile:
                id UUID PK (do NOT auto-generate — set to the Google 'sub' claim on first login, then reuse on every subsequent login)
                display_name TEXT not null (max 100)
                avatar_url TEXT nullable
                created_at TIMESTAMPTZ not null (default now())
                updated_at TIMESTAMPTZ not null (default now())

              Ingredient (Edamam food catalog cache):
                id UUID PK
                name TEXT not null
                edamam_food_id TEXT nullable unique (null = user-defined ingredient not in Edamam)
                category TEXT nullable (e.g. "Vegetables", "Dairy" — from Edamam food category field)
                created_at TIMESTAMPTZ not null (default now())

              Recipe:
                id UUID PK
                user_id UUID not null FK → UserProfile.id (cascade delete)
                title TEXT not null (max 200)
                description TEXT nullable
                servings INT not null (default 1, check > 0)
                cuisine TEXT nullable (max 100)
                cover_colour TEXT not null (default '#E8F5E9', hex string)
                visibility TEXT not null (default 'private') — check: ('private','public')
                prep_time_minutes INT nullable
                cook_time_minutes INT nullable
                created_at TIMESTAMPTZ not null (default now())
                updated_at TIMESTAMPTZ not null (default now())
                Index: (user_id, created_at DESC) for recipe list queries

              RecipeIngredient:
                id UUID PK
                recipe_id UUID not null FK → Recipe.id (cascade delete)
                ingredient_id UUID not null FK → Ingredient.id (restrict delete)
                quantity DECIMAL(10,3) not null (check > 0)
                unit TEXT not null (max 50, e.g. "g", "cup", "tbsp")
                sort_order INT not null (default 0)
                calories_per_100g DECIMAL(8,2) nullable
                protein_g DECIMAL(8,2) nullable
                carbs_g DECIMAL(8,2) nullable
                fat_g DECIMAL(8,2) nullable
                fibre_g DECIMAL(8,2) nullable
                edamam_measure_uri TEXT nullable (stored for Edamam re-fetch if nutrition is missing)
                edamam_food_id TEXT nullable (denormalized copy from Ingredient for Edamam re-fetch)

              RecipeStep:
                id UUID PK
                recipe_id UUID not null FK → Recipe.id (cascade delete)
                step_number INT not null (1-based, check > 0)
                instruction TEXT not null
                step_type TEXT not null — check: ('sequential','background','timed')
                timer_seconds INT nullable (required when step_type = 'timed')
                background_step_id UUID nullable FK → RecipeStep.id self-ref (set null on delete)
                Unique constraint: (recipe_id, step_number)

              WeeklyPlan:
                id UUID PK
                user_id UUID not null FK → UserProfile.id (cascade delete)
                week_start DATE not null (always a Monday — enforce in the service layer)
                status TEXT not null (default 'draft') — check: ('draft','confirmed')
                notes TEXT nullable
                generated_at TIMESTAMPTZ nullable (set when Claude returns a result)
                created_at TIMESTAMPTZ not null (default now())
                updated_at TIMESTAMPTZ not null (default now())
                Unique constraint: (user_id, week_start) — one plan per user per week

              PlanSlot:
                id UUID PK
                plan_id UUID not null FK → WeeklyPlan.id (cascade delete)
                recipe_id UUID not null FK → Recipe.id (restrict delete)
                slot_date DATE not null
                meal_type TEXT not null — check: ('breakfast','lunch','dinner','snack')
                servings INT not null (default 1, check > 0)
                rationale TEXT nullable (Claude's explanation for why this meal was chosen)
                sort_order INT not null (default 0)

              ShoppingList:
                id UUID PK
                user_id UUID not null FK → UserProfile.id (cascade delete)
                plan_id UUID nullable FK → WeeklyPlan.id (set null on delete) — null = standalone list
                created_at TIMESTAMPTZ not null (default now())
                updated_at TIMESTAMPTZ not null (default now())

              ShoppingItem:
                id UUID PK
                list_id UUID not null FK → ShoppingList.id (cascade delete)
                ingredient_id UUID nullable FK → Ingredient.id (set null on delete)
                name TEXT not null (max 200, denormalized from Ingredient.name or free-text for manual items)
                quantity DECIMAL(10,3) nullable
                unit TEXT nullable (max 50)
                category TEXT nullable (used for UI grouping: "Produce", "Dairy", "Meat", "Pantry")
                is_checked BOOL not null (default false)
                is_manual BOOL not null (default false — true = user-added, not derived from plan)
                sort_order INT not null (default 0)

              NutrientOrganMapping (schema only — data is seeded in MVP 2 F1-1):
                id UUID PK
                nutrient_name TEXT not null unique
                organ_name TEXT not null (max 100)
                body_system TEXT not null (max 100)
                impact_description TEXT nullable
                source_citation TEXT nullable

              Implementation notes:
                - UserProfile.id must be set to the Google 'sub' claim — no DB sequence
                - Add an updated_at EF Core SaveChanges interceptor (or trigger) to auto-update timestamps on all tables that have updated_at
                - All UUID PKs: use Guid in C# with ValueGeneratedOnAdd() in EF config
                - Restrict vs cascade: Recipe/Step/Ingredient rows referenced by other users' data in later MVPs use restrict — do not delete if still referenced

      - group: 'CI'
        tasks:
          - id: F1-6
            task: 'Set up GitHub Actions: build + lint on PR for both frontend and API'

      - group: 'Error handling & observability'
        tasks:
          - id: F1-7
            task: 'Add global exception handling middleware that catches unhandled exceptions and returns ProblemDetails JSON'
            note: 'Map common exceptions: NotFound → 404, Validation → 400, Unauthorized → 401. Use structured logging (Serilog recommended).'
          - id: F1-8
            task: 'Add GET /health endpoint that checks DB connectivity and returns 200/503'
            note: 'Used for Docker health checks and CI smoke tests.'
          - id: F1-9
            task: 'Add input validation on all request DTOs using FluentValidation or DataAnnotations — return 400 with field-level error messages'

      - group: 'Frontend foundation'
        tasks:
          - id: F1-10
            task: 'Set up app.config.ts with all providers: provideRouter(routes), provideHttpClient(withInterceptors([...])), provideIonicAngular(). Create the API_URL injection token from environment.apiUrl.'
          - id: F1-11
            task: 'Build the app shell — root Ionic tabs layout with 4 tabs: Recipes (/tabs/recipes), Plan (/tabs/plan), History (/tabs/history), Profile (/tabs/profile). Auth guard wraps the /tabs route. Unauthenticated users land on /auth/login. Tab icons use Ionicons.'
            note: 'Define the full route structure in app.routes.ts now so every subsequent screen task knows exactly where to add its route. Discover tab is added in MVP 3 — leave a placeholder comment.'
          - id: F1-12
            task: 'Create three shared UI components used by every page: AppLoadingSpinnerComponent (centered ion-spinner), AppErrorStateComponent (@Input message: string, @Output retry event, ion-button), AppEmptyStateComponent (@Input message: string, optional @Input icon). Export all three from a SharedModule or as standalone components.'
          - id: F1-13
            task: "Create ToastService — thin wrapper around Ionic's ToastController. Methods: showSuccess(message), showError(message), showInfo(message). Used by all pages instead of direct ToastController calls."
          - id: F1-14
            task: "Add HTTP error interceptor — Angular HttpInterceptorFn that catches API errors and calls ToastService: 401 → sign out user and redirect to /auth/login, 403 → show 'Access denied' toast, 422 → pass through (forms handle validation errors inline), 500+ → show 'Something went wrong' toast. Register in app.config.ts."
          - id: F1-15
            task: "Bootstrap the Electron app — apps/electron/src/main.ts creates a BrowserWindow (1200×800, min 900×600) that loads the Angular dev server in development (http://localhost:4200) or the built index.html in production. apps/electron/src/preload.ts exposes window.electronAPI = { platform: 'electron' } via contextBridge. Add electron-builder config for packaging."

  - id: F2
    name: 'Authentication'
    description: 'Google OAuth handled entirely by the .NET API — no third-party auth service'
    task_count: 6
    groups:
      - group: 'Backend'
        tasks:
          - id: F2-1
            task: "Configure Google OAuth and JWT issuance in the .NET API. Install Microsoft.AspNetCore.Authentication.Google. On successful Google callback: upsert UserProfile (using Google 'sub' as id), issue a signed JWT (System.IdentityModel.Tokens.Jwt) containing user_id and email, redirect to the Angular app with the token."
            note: |
              OAuth flow:
                GET /auth/google           → Challenge(GoogleDefaults.AuthenticationScheme) — redirects user to Google
                GET /auth/callback         → Google redirects here after login → validate, upsert UserProfile, issue JWT, redirect to frontend

              JWT config (read from env):
                Issuer:   JWT_ISSUER
                Audience: JWT_AUDIENCE
                Secret:   JWT_SECRET (HMAC-SHA256)
                Expiry:   JWT_EXPIRY_DAYS (default 7)
                Claims:   sub (UserProfile.id), email, name, picture

              Redirect after login:
                Web/Electron: redirect to http://localhost:4200/auth/callback?token=xxx (dev) or https://yourdomain.com/auth/callback?token=xxx (prod)
                Mobile: redirect to com.mealplanner://auth/callback?token=xxx
                Pass the callback base URL as a 'redirect_uri' query param on the initial /auth/google call so the API knows where to send the token back.
          - id: F2-2
            task: "Configure JwtBearer middleware globally — validate the API's own JWTs on every request. Add [Authorize] to all controllers. Create CurrentUserService that reads user_id and email from HttpContext.User claims."

      - group: 'Frontend'
        tasks:
          - id: F2-3
            task: 'Implement AuthService — no third-party SDK needed. Methods: signInWithGoogle() opens the OAuth flow, handleCallback(token) stores the JWT, signOut() clears it, getToken() reads it. Token stored in localStorage on web/Electron and Capacitor Preferences on mobile.'
            note: |
              Platform-specific OAuth initiation:
                Web/Electron: navigate to API_URL + '/auth/google?redirect_uri=' + encodeURIComponent(window.location.origin + '/auth/callback')
                Mobile (Capacitor): use @capacitor/browser Browser.open() with the same URL but redirect_uri = 'com.mealplanner://auth/callback'. Listen for the deep link via App.addListener('appUrlOpen'), extract the token from the URL, call handleCallback(token).
              Register the custom URL scheme com.mealplanner in capacitor.config.ts and in iOS Info.plist / Android AndroidManifest.xml.
              An /auth/callback Angular route handles the web/Electron case: reads ?token= from the URL, calls handleCallback(token), navigates to /tabs/recipes.
          - id: F2-4
            task: "Build the login screen — single 'Continue with Google' ion-button that calls AuthService.signInWithGoogle(). No form fields. Show a loading spinner while the OAuth flow is in progress."
          - id: F2-5
            task: 'Create an AuthGuard — unauthenticated users (no stored token) redirect to /auth/login, authenticated users land on /tabs/recipes.'
          - id: F2-6
            task: "Add HTTP auth interceptor — Angular HttpInterceptorFn that calls AuthService.getToken() and adds 'Authorization: Bearer {token}' to every outgoing API request. Register alongside the error interceptor in app.config.ts withInterceptors([authInterceptor, errorInterceptor])."
            note: 'Call getToken() at intercept time on every request — never cache the value at startup. Depends on AuthService (F2-3).'

  - id: F3
    name: 'Recipe management'
    description: 'CRUD, ingredients, tags, step authoring'
    task_count: 9
    groups:
      - group: 'API endpoints'
        tasks:
          - id: F3-1
            task: 'Implement RecipesController: GET /recipes, GET /recipes/{id}, POST /recipes, PUT /recipes/{id}, DELETE /recipes/{id}'
          - id: F3-2
            task: 'Implement RecipeIngredientsController: add, update, remove ingredients on a recipe'
            note: 'Ingredient entry triggers Edamam lookup (F4). Store edamam_food_id and cached nutrition on RecipeIngredient.'
          - id: F3-3
            task: 'Implement RecipeStepsController: add, reorder, update, delete steps with step_type, timer_seconds, and background_step_id'

      - group: 'Frontend screens'
        tasks:
          - id: F3-4
            task: 'Recipe list screen — shows all user recipes as cards with title, cover colour, and macro summary'
          - id: F3-5
            task: 'Recipe detail screen — full view with ingredients table, nutrition summary, step list, and action buttons (edit, cook, delete)'
          - id: F3-6
            task: 'Recipe create/edit form — title, description, servings, cuisine, diet tags'
          - id: F3-7
            task: 'Ingredient input component — type a name, auto-search Edamam (F4), select a match, enter quantity + unit'
          - id: F3-8
            task: 'Step builder component — add steps, set type (sequential / background / timed), set timer duration, link background dependency via dropdown'
          - id: F3-9
            task: 'Drag-to-reorder step list using @angular/cdk/drag-drop (CdkDragDrop). Works on web, Capacitor, and Electron.'

  - id: F4
    name: 'Nutrition auto-calculation'
    description: 'Edamam integration, caching, per-recipe totals'
    task_count: 5
    groups:
      - group: 'Backend'
        tasks:
          - id: F4-1
            task: 'Create EdamamService — typed HttpClient wrapper for Food Database API: SearchFood(query) and GetNutrients(foodId, measure, quantity)'
            note: 'Store API key in appsettings.json / environment variable. Use in-memory cache for duplicate lookups. Add Polly retry policy for transient failures and rate limiting.'
          - id: F4-2
            task: 'On ingredient save: call Edamam, store calories, protein_g, carbs_g, fat_g, fibre_g per 100g on RecipeIngredient'
            note: 'If Edamam fails, save the ingredient without nutrition and flag it for retry.'
          - id: F4-3
            task: 'Add GET /recipes/{id}/nutrition endpoint — aggregates ingredient macros scaled by quantity, returns per-serving and total values'

      - group: 'Frontend'
        tasks:
          - id: F4-4
            task: 'Ingredient search component — debounced input calls GET /ingredients/search?q=, shows dropdown of Edamam matches with food name and category'
          - id: F4-5
            task: 'Nutrition summary widget — reusable component showing calorie ring + macro bars, used on recipe detail and weekly plan screens'

  - id: F5
    name: 'Cooking mode'
    description: 'Step engine, background cards, countdown alarm'
    task_count: 7
    groups:
      - group: 'State machine'
        tasks:
          - id: F5-1
            task: 'Build CookingSessionService Angular injectable — manages currentStepIndex, backgroundTasks[] (active background steps with done status), and activeTimers[] using Angular signals'
          - id: F5-2
            task: 'Implement step advancement logic: sequential → wait for Next; background → push to floating card list and advance; timed → check dependency, start countdown'
          - id: F5-3
            task: 'Implement dependency gate — when a timed step has a background_step_id, block advancement until that background task is marked done. Show inline prompt.'

      - group: 'UI & alarm'
        tasks:
          - id: F5-4
            task: 'Cooking mode screen — full-screen step view with instruction text, Next button, and a persistent floating strip for active background tasks at the bottom'
          - id: F5-5
            task: 'Countdown timer component — large display, shows MM:SS, animates last 10 seconds, triggers alarm when zero'
          - id: F5-6
            task: 'Alarm service — @capacitor-community/native-audio or Howler.js on mobile for looping audio, Web Audio API on web/Electron. Alarm persists until user taps Stop. Wrap in AlarmService Angular injectable so the UI does not care about platform.'
          - id: F5-7
            task: 'Cooking complete summary screen — shown when user finishes all steps. Displays recipe name, total cook time, and option to return to recipe detail or recipe list. Auto-logs the cooking session to CookingLog (F8).'

  - id: F6
    name: 'AI weekly plan generation'
    description: 'Claude API integration, plan grid, manual swap'
    priority: 'top priority'
    task_count: 8
    groups:
      - group: 'Backend'
        tasks:
          - id: F6-1
            task: 'Add Anthropic.SDK NuGet package, create PlanGeneratorService with a typed prompt builder'
            note: 'Prompt includes: recipe list (id, title, tags, macros, diet flags), user preferences string, cooking history (last 4 weeks from CookingLog), and a JSON schema the model must return. Cooking history helps Claude avoid suggesting recently cooked meals.'
          - id: F6-2
            task: 'Define the expected JSON response schema: { days: [{ date, slots: [{ meal_type, recipe_id, servings, rationale }] }] }'
            note: "Use Claude's structured output / JSON mode. Validate the response against the schema before saving."
          - id: F6-3
            task: 'Implement POST /plans/generate — accepts optional preferences string, calls Claude, persists result as WeeklyPlan + PlanSlot rows, returns the full plan'
          - id: F6-4
            task: 'Implement PUT /plans/{id}/slots/{slotId} — manual swap endpoint to replace a single meal slot with a different recipe'
          - id: F6-5
            task: "Implement POST /plans/{id}/regenerate-day — regenerates a single day's meals while keeping the rest of the week intact"
          - id: F6-6
            task: 'Add error handling for Claude API failures — retry with exponential backoff (max 3 attempts), user-friendly error message if API is down or returns invalid JSON'

      - group: 'Frontend'
        tasks:
          - id: F6-7
            task: 'Weekly plan screen — 7-column grid (or vertical list on mobile) showing breakfast / lunch / dinner per day. Each slot shows recipe name, cover colour, and calorie count.'
          - id: F6-8
            task: 'Slot swap UI — tap a slot to open a recipe picker bottom sheet. Confirm replaces that slot via the API. Also expose Regenerate day per day header.'

  - id: F7
    name: 'Shopping list'
    description: 'Auto-generated from plan, categorised, check-off'
    task_count: 5
    groups:
      - group: 'Backend'
        tasks:
          - id: F7-1
            task: "Implement ShoppingListService — aggregates all RecipeIngredient rows across the plan's slots, sums quantities per ingredient+unit, groups by food category from Edamam metadata"
          - id: F7-2
            task: 'Implement POST /plans/{id}/shopping-list (generate) and GET /shopping-lists/{id}. Add PATCH /shopping-lists/{id}/items/{itemId} for check-off toggle and manual quantity edit.'
          - id: F7-3
            task: 'Add unit conversion normalization — when aggregating, normalize compatible units (e.g. 500ml + 2 cups). Define conversion table for common cooking units (cups, tbsp, tsp, ml, l, g, kg, oz, lb). Incompatible units listed as separate line items.'

      - group: 'Frontend'
        tasks:
          - id: F7-4
            task: 'Shopping list screen — grouped by category (Produce, Dairy, Meat, Pantry), each item shows ingredient name, total quantity, unit, and a checkbox'
          - id: F7-5
            task: 'Add extra item input — free-text field at the bottom of each category to manually add items not in the plan'

  - id: F8
    name: 'Cooking history'
    description: 'Track what was cooked, when, and feed into the AI planner to avoid repetition'
    task_count: 8
    groups:
      - group: 'Backend'
        tasks:
          - id: F8-1
            task: 'Add CookingLog table via EF Core migration: id, user_id FK, recipe_id FK, plan_slot_id FK (nullable — null if cooked outside a plan), cooked_at, servings_cooked, rating (1-5 nullable, quick post-cook rating), notes TEXT (nullable)'
          - id: F8-2
            task: 'Implement CookingLogController: POST /cooking-log (manual log entry), GET /cooking-log (paginated, newest first), GET /cooking-log/stats (summary stats)'
          - id: F8-3
            task: 'Auto-log on cooking mode completion — when the cooking complete screen (F5-7) fires, automatically create a CookingLog entry with recipe_id, plan_slot_id (if cooking from a plan), and cooked_at timestamp'
          - id: F8-4
            task: 'Add GET /cooking-log/recent?weeks=4 — returns recipe IDs and dates cooked in the last N weeks, used by PlanGeneratorService to feed history into the Claude prompt'
          - id: F8-5
            task: "Update PlanGeneratorService prompt builder — include cooking history from the last 4 weeks as context. Instruct Claude: 'The user recently cooked these recipes — avoid repeating them unless the user explicitly requests it.'"

      - group: 'Frontend'
        tasks:
          - id: F8-6
            task: 'Post-cook rating prompt — on the cooking complete screen (F5-7), show optional 1-5 star quick rating and a notes field before logging. Skip button available.'
          - id: F8-7
            task: 'Cooking history screen — chronological list of cooked meals with recipe name, date, rating (if given), and notes. Tapping a row navigates to the recipe detail.'
          - id: F8-8
            task: 'Cooking stats summary widget — shows on the history screen or profile: total recipes cooked, unique recipes, most-cooked recipe, current cooking streak (consecutive weeks with at least one cook).'

build_order:
  - step: 1
    feature: F1
    note: 'Must be fully complete before anything else starts. F1-10 through F1-15 (app shell, shared components, interceptors, Electron) are part of setup — do them before any feature screens.'
  - step: 2
    feature: F2
    note: 'Auth gates all other features. F2-6 (auth interceptor) depends on F2-3 (AuthService) — do it immediately after. Make sure Google OAuth credentials are in .env before testing F2-1.'
  - step: 3
    feature: 'F3 (API only)'
    note: 'Recipe endpoints needed before nutrition can be wired up'
  - step: 4
    feature: F4
    note: 'Edamam integration'
  - step: 5
    feature: 'F3 (frontend)'
    note: 'Screens can now be built with real data'
  - step: 6
    feature: F5
    note: 'Cooking mode is independent of F4/F6, can run in parallel'
  - step: 7
    feature: 'F8 (Backend: F8-1 through F8-4)'
    note: 'CookingLog table and API needed before planner can use history'
  - step: 8
    feature: F6
    note: 'Planner requires recipes with nutrition + cooking history'
  - step: 9
    feature: 'F8 (Frontend: F8-6 through F8-8)'
    note: 'History screens can be built after cooking mode and planner are working'
  - step: 10
    feature: F7
    note: 'Shopping list is derived from the plan'
```
