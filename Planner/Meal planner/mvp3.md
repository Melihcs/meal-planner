# MVP 3 — Discovery & Photos

```yaml
mvp: 3
name: 'Discovery & photos'
description: 'Go public, search & save other recipes'
ships_as: 'A public recipe platform you can browse and build on'
prereqs:
  - 'MVP 1 complete'
  - 'MVP 2 complete'
summary:
  features: 6
  tasks: 51

features:
  - id: F1
    name: 'Recipe photos'
    description: 'Upload, reorder, cover photo selection, image optimization'
    task_count: 8
    groups:
      - group: 'Backend / storage'
        tasks:
          - id: F1-1
            task: 'Configure Supabase Storage bucket recipe-photos — public read, authenticated write, max 5MB per file, accept jpeg/png/webp only'
          - id: F1-2
            task: 'Add RecipePhoto table migration: id, recipe_id FK, storage_path, url, thumbnail_url, order_index, is_cover, uploaded_at'
          - id: F1-3
            task: 'Implement POST /recipes/{id}/photos — accepts multipart upload, resizes to 1200px max width via SixLabors.ImageSharp, converts to WebP (quality 80%), generates thumbnail at 400px (quality 70%), uploads both to Supabase Storage, saves record'
            note: 'Add Cache-Control headers on Supabase Storage for CDN caching.'
          - id: F1-4
            task: 'Implement PATCH /recipes/{id}/photos/{photoId} (set as cover, update order) and DELETE /recipes/{id}/photos/{photoId} (removes from storage + DB)'

      - group: 'Frontend'
        tasks:
          - id: F1-5
            task: 'Photo picker component — uses @capacitor/camera on mobile (CameraSource.Prompt to choose camera or gallery), native file input on web/Electron. Shows upload progress bar via Angular HttpClient reportProgress. Limits to 6 photos per recipe.'
          - id: F1-6
            task: 'Photo strip on recipe edit screen — horizontal scroll of uploaded photos with drag-to-reorder, star button to set cover, trash to delete'
          - id: F1-7
            task: 'Update recipe card and detail screen to show cover photo. Use thumbnail URL for list views, full URL for detail. Show placeholder gradient when no photo.'
          - id: F1-8
            task: 'Client-side validation before upload: check file type (jpeg/png/webp only) and rough size (< 5MB). Show error message if validation fails.'

  - id: F2
    name: 'Recipe visibility system'
    description: 'Private / public toggle, RLS enforcement, publish checklist'
    task_count: 6
    groups:
      - group: 'Backend'
        tasks:
          - id: F2-1
            task: 'Activate the recipe.visibility column (stubbed in MVP 1). Update Supabase RLS policy: public recipes are readable by all, private recipes only by their owner/household.'
          - id: F2-2
            task: 'Update all recipe query methods in the API to filter by visibility + ownership. A public recipe GET by a stranger should work; a private one should return 403.'
          - id: F2-3
            task: 'Add published_at timestamp to Recipe — set when visibility first changes to public. Used for sorting in discovery feed.'
          - id: F2-4
            task: 'Add publish checklist validation — before a recipe can be set to public, validate it has a title, description, at least one ingredient, and at least one step. Return 422 with specific missing fields if validation fails.'

      - group: 'Frontend'
        tasks:
          - id: F2-5
            task: "Visibility toggle on recipe edit screen — private/public segmented control with a clear label ('visible to everyone' vs 'only you and your household')"
          - id: F2-6
            task: "Show visibility badge on recipe cards in the user's own list — lock icon for private, globe icon for public. Show publish checklist requirements with checkmarks when toggling to public."

  - id: F3
    name: 'Global recipe search'
    description: 'Full-text search, filters, result cards'
    priority: 'core of MVP 3'
    task_count: 10
    groups:
      - group: 'Backend'
        tasks:
          - id: F3-1
            task: 'Add search_vector tsvector generated column to Recipe, create GIN index. Vector covers title, description, tags, and ingredient names.'
            note: 'Ingredient names are denormalized into the vector at save time — do not join at search time.'
          - id: F3-2
            task: 'Implement GET /recipes/search with params: q (text), diet_type, max_calories, min_rating, page, page_size. Returns public recipes only.'
          - id: F3-3
            task: 'Add relevance ranking using ts_rank(search_vector, query). Start simple — tune weighting post-launch with real user data.'
            note: 'Premature optimization of ranking formulas adds complexity without data to justify it. Get basic relevance working first.'
          - id: F3-4
            task: 'Add GET /recipes/trending — top 20 public recipes by save count + rating in the last 30 days. Used as default state before user types.'
          - id: F3-5
            task: 'Add GET /recipes/search/suggestions — autocomplete endpoint returning matching recipe titles as the user types (debounced, min 2 chars)'
          - id: F3-6
            task: 'Add SearchLog table (query_text, result_count, user_id nullable, created_at). Log searches fire-and-forget on each search request.'
            note: 'Useful for understanding what users look for and finding content gaps.'

      - group: 'Frontend'
        tasks:
          - id: F3-7
            task: 'Discover tab — search bar at top, trending grid as default state, results replace grid on search. Infinite scroll pagination.'
          - id: F3-8
            task: 'Filter sheet — bottom sheet with diet type chips, calorie slider, min-rating star selector. Applied filters show as pills under the search bar.'
          - id: F3-9
            task: 'Search result card — cover photo, recipe title, author name + avatar, average rating stars, calorie count, diet type tags'
          - id: F3-10
            task: "No results state — when search returns zero results, show 'No recipes found' message with suggestions: 'Try a broader search' or show trending recipes. If filters are active: 'Try removing some filters'."

  - id: F4
    name: 'Save & collections'
    description: 'Save public recipes, personal library, use in plans'
    task_count: 6
    groups:
      - group: 'Backend'
        tasks:
          - id: F4-1
            task: 'Add SavedRecipe table: user_id FK, recipe_id FK, saved_at. Unique constraint on (user_id, recipe_id). Add denormalized save_count on Recipe table.'
          - id: F4-2
            task: "Implement POST /recipes/{id}/save, DELETE /recipes/{id}/save, GET /recipes/saved (user's saved library)"
          - id: F4-3
            task: 'Update PlanGeneratorService to include saved recipes alongside authored recipes when building the recipe pool for Claude'

      - group: 'Frontend'
        tasks:
          - id: F4-4
            task: 'Save button on recipe detail and search result cards — bookmark icon, toggles saved state, optimistic update'
          - id: F4-5
            task: 'Saved tab in recipe library — separate section from authored recipes, shows saved count, allows unsaving'
          - id: F4-6
            task: "Saved recipe detail view — read-only (cannot edit someone else's recipe), but can cook it, add to plan manually, or unsave"

  - id: F5
    name: 'Ratings & public profiles'
    description: 'Star ratings, author profile pages'
    task_count: 14
    groups:
      - group: 'Backend — ratings'
        tasks:
          - id: F5-1
            task: 'Add RecipeRating table: recipe_id FK, user_id FK, stars (1-5). Unique constraint on (recipe_id, user_id).'
          - id: F5-2
            task: 'Implement POST /recipes/{id}/rating (upsert), DELETE /recipes/{id}/rating. Maintain a denormalized avg_rating and rating_count on the Recipe row (updated via DB trigger).'
          - id: F5-3
            task: "Return the current user's rating alongside the recipe in GET /recipes/{id} so the star UI shows their existing vote"
          - id: F5-4
            task: 'Add rate-limiting on ratings — max 10 rating changes per user per minute. Return 429 if exceeded.'

      - group: 'Backend — profiles'
        tasks:
          - id: F5-5
            task: 'Add UserProfile table: user_id FK, display_name, avatar_url, bio. Expose GET /users/{id}/profile and PUT /users/me/profile.'
          - id: F5-6
            task: 'Add GET /users/{id}/recipes — public recipes by a given user, sorted by rating desc. Used on profile pages.'
          - id: F5-7
            task: 'Avatar upload: POST /users/me/avatar — upload to Supabase Storage avatars/ bucket, resize to 256x256, return URL'

      - group: 'Backend — moderation'
        tasks:
          - id: F5-8
            task: 'Add report/flag mechanism — POST /recipes/{id}/report with reason enum (inappropriate/spam/copyright/other + free text). Store in RecipeReport table. Add GET /admin/reports for future admin review queue.'
            note: 'Basic moderation foundation — just stores reports for now. Full admin UI can come later.'

      - group: 'Frontend'
        tasks:
          - id: F5-9
            task: 'Star rating component — 5-star tap interaction on recipe detail, shows average rating + count below, disabled for own recipes'
          - id: F5-10
            task: 'My profile screen — edit display name, bio, avatar upload. Shows authored public recipe count and total save count across all recipes.'
          - id: F5-11
            task: 'Public author profile screen — tap author name/avatar anywhere to view their profile: bio, public recipe grid sorted by rating'
          - id: F5-12
            task: 'Settings screen — account management: change email, change password (via Supabase), sign out, delete account'
          - id: F5-13
            task: 'Report recipe button — accessible from recipe detail overflow menu. Opens a sheet with reason selector and optional free text. Confirm submits the report.'
          - id: F5-14
            task: "Add 'Report this recipe' option to recipe detail overflow menu (three-dot menu or long-press). Show confirmation after report is submitted."

  - id: F6
    name: 'Recipe import from URL'
    description: 'Paste a recipe URL, scrape and parse it into a structured recipe using Claude'
    task_count: 7
    groups:
      - group: 'Backend'
        tasks:
          - id: F6-1
            task: 'Create RecipeScraperService — accepts a URL, fetches the HTML content, extracts recipe-related content (title, ingredients, steps, description, images)'
            note: 'Use HttpClient to fetch the page. Extract structured data from JSON-LD (schema.org/Recipe) first — most recipe sites embed it. Fall back to raw HTML parsing if JSON-LD is absent.'
          - id: F6-2
            task: 'Create RecipeImportService — takes scraped raw content and sends it to Claude for structured extraction. Claude returns: title, description, servings, cuisine, diet_tags, ingredients[] (name, quantity, unit), steps[] (instruction, step_type, timer_seconds)'
            note: "Use a detailed system prompt with the exact JSON schema. Claude handles messy HTML, inconsistent formatting, and ingredient parsing (e.g. '2 large eggs, beaten' → name: eggs, quantity: 2, unit: large). Validate the response against the schema."
          - id: F6-3
            task: 'Implement POST /recipes/import — accepts { url }. Calls RecipeScraperService then RecipeImportService. Creates a new Recipe in draft/private state with all parsed data. Returns the created recipe for user review.'
            note: 'The imported recipe is always private initially. The user must review and edit before publishing.'
          - id: F6-4
            task: 'After import, trigger Edamam nutrition lookup for each parsed ingredient — reuse existing EdamamService from MVP 1 F4. Match ingredient names to Edamam entries, store nutrition data.'
          - id: F6-5
            task: "Extract cover photo from the scraped page — download the primary recipe image, process it through the photo upload pipeline (F1-3: resize, WebP convert, thumbnail), attach as the recipe's cover photo."
            note: 'Not all recipe pages have extractable images. If no image found, skip gracefully.'

      - group: 'Frontend'
        tasks:
          - id: F6-6
            task: "Import recipe screen — URL input field with paste button, 'Import' action button. Shows loading state with progress message ('Fetching recipe...', 'Parsing ingredients...', 'Looking up nutrition...'). On success, navigates to the recipe edit form pre-filled with imported data."
          - id: F6-7
            task: 'Import review step — after import completes, show the recipe edit form pre-filled with all parsed data. Highlight fields that may need review (e.g. ingredients where Edamam match confidence is low). User can edit anything before saving.'

build_order:
  - step: 1
    feature: F2
    note: 'Visibility system must be live before anything is made public'
  - step: 2
    feature: F1
    note: 'Photos depend on visibility — no point uploading to private recipes'
  - step: 3
    feature: F5
    note: 'Profiles and ratings needed before search results are meaningful'
  - step: 4
    feature: F3
    note: 'Search requires public recipes, ratings, and profile data'
  - step: 5
    feature: F4
    note: 'Save depends on search — users need to find recipes before saving them'
  - step: 6
    feature: F6
    note: 'Recipe import uses photos (F1), nutrition (MVP 1 F4), and recipe CRUD — build after core features are stable'
```
