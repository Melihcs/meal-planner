# MVP 2 — Household + Health Insight

```yaml
mvp: 2
name: 'Household + health insight'
description: 'Multiple members, dietary goals & body map'
ships_as: 'A household-ready nutrition planning tool'
prereqs:
  - 'MVP 1 complete'
summary:
  features: 5
  tasks: 37

features:
  - id: F1
    name: 'Household management'
    description: 'Create household, invite members, define roles'
    task_count: 10
    groups:
      - group: 'Backend'
        tasks:
          - id: F1-1
            task: 'Add Household, HouseholdMember, and HouseholdInvite tables via EF Core migration.'
            note: |
              Household:
                id UUID PK
                name TEXT not null (max 100)
                created_by UUID not null FK → UserProfile.id (restrict)
                created_at TIMESTAMPTZ not null (default now())
                updated_at TIMESTAMPTZ not null (default now())

              HouseholdMember:
                id UUID PK
                household_id UUID not null FK → Household.id (cascade delete)
                user_id UUID not null FK → UserProfile.id (cascade delete)
                role TEXT not null (default 'member') — check: ('owner','member')
                display_name TEXT not null (max 100, can differ from UserProfile.display_name)
                is_child BOOL not null (default false)
                age INT nullable (used for default calorie targets in F2-8)
                joined_at TIMESTAMPTZ not null (default now())
                Unique constraint: (household_id, user_id)

              HouseholdInvite:
                id UUID PK
                household_id UUID not null FK → Household.id (cascade delete)
                token TEXT not null unique (generate with RandomNumberGenerator, store as hex string, min 32 chars)
                created_by UUID not null FK → UserProfile.id
                created_at TIMESTAMPTZ not null (default now())
                expires_at TIMESTAMPTZ not null (default now() + interval '48 hours')
                used_at TIMESTAMPTZ nullable (null = not yet used)
                used_by UUID nullable FK → UserProfile.id

              Also add household_id UUID nullable FK → Household.id (set null on delete) to both
              the Recipe table and the WeeklyPlan table — needed for F1-4 and F1-6.

          - id: F1-2
            task: 'Implement HouseholdsController: POST /households, GET /households/mine, PUT /households/{id}, DELETE /households/{id}.'
            note: |
              POST /households
                Body: { name: string }
                Action: create Household row, then create HouseholdMember row for calling user with role = 'owner'
                Response 201: HouseholdDto

              GET /households/mine
                Response 200: HouseholdDto[] — all households the calling user is a member of

              PUT /households/{id}
                [HouseholdOwner policy] only
                Body: { name: string }
                Response 200: HouseholdDto

              DELETE /households/{id}
                [HouseholdOwner policy] only
                Cascade: set household_id = null on all Recipe and WeeklyPlan rows for household members,
                         delete all HouseholdMember rows, delete Household — user data is never deleted
                Response 204

              DTOs:
                HouseholdDto: { id, name, created_by, role (caller's role in this household),
                                members: MemberDto[] }
                MemberDto:    { id, user_id, display_name, role, is_child, age, joined_at }

          - id: F1-3
            task: 'Implement invite system: POST /households/{id}/invites and POST /households/join.'
            note: |
              POST /households/{id}/invites
                Any household member (not just owner) may generate an invite
                Action: create HouseholdInvite with token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                        expires_at = now() + 48h
                Response 201: { token, invite_url: "{APP_BASE_URL}/join?token={token}", expires_at }
                APP_BASE_URL is read from env (e.g. http://localhost:4200 in dev)

              POST /households/join
                Body: { token: string }
                Validation:
                  - Token exists → 404 if not found
                  - Not expired (expires_at > now()) → 400 "Invite has expired"
                  - Not already used (used_at is null) → 400 "Invite has already been used"
                  - Caller is not already a member → 409 "Already a member of this household"
                Action: INSERT HouseholdMember (role = 'member'), SET used_at = now(), used_by = caller's user_id
                Response 200: HouseholdDto

          - id: F1-4
            task: 'Update recipe fetching for plan generation to scope to the household: members can see all household recipes when generating a plan.'
            note: |
              Modify the query in PlanGeneratorService (and wherever GET /recipes is called for generation)
              to be household-aware:

              If calling user is in a household:
                Return recipes WHERE user_id IN (
                  SELECT user_id FROM HouseholdMember WHERE household_id = callerHouseholdId
                )
              If calling user has no household:
                Return recipes WHERE user_id = callerId (existing behaviour)

              Also update GET /recipes to accept an optional query param ?scope=household.
              When scope=household, apply the same logic.
              Default behaviour (no param) still returns only the caller's own recipes.

              This change must be applied before passing recipes to the Claude prompt in PlanGeneratorService.

          - id: F1-5
            task: 'Add GET /households/{id}/serving-multiplier — returns a decimal based on member composition.'
            note: |
              Formula:
                multiplier = adult_count + (child_count * 0.5)
                adult_count = COUNT(HouseholdMember) WHERE is_child = false AND household_id = {id}
                child_count = COUNT(HouseholdMember) WHERE is_child = true  AND household_id = {id}

              Examples:
                2 adults, 0 children → 2.0
                2 adults, 1 child    → 2.5
                1 adult,  2 children → 2.0

              Response: { multiplier: number, adult_count: number, child_count: number }

              Usage: the plan generation flow (POST /plans/generate) should call this and use
              the multiplier as the default servings value on each PlanSlot it creates.

          - id: F1-6
            task: 'Implement leave/delete household flow: POST /households/{id}/leave for members, cascade behaviour on owner delete.'
            note: |
              POST /households/{id}/leave
                Any authenticated household member may call this
                Guard: if calling user is the only owner → return 400
                       "Transfer ownership before leaving, or delete the household instead"
                Action: DELETE HouseholdMember row for (household_id, user_id)
                Auto-delete: if no HouseholdMember rows remain after the delete, delete the Household row
                Response 204

              Add POST /households/{id}/members/{memberId}/transfer-ownership (owner only):
                Sets target member's role = 'owner', sets caller's role = 'member'
                Response 200: updated HouseholdDto

              Cascade on DELETE /households/{id} (from F1-2):
                1. SET household_id = null ON Recipe WHERE user_id IN (household member user_ids)
                2. SET household_id = null ON WeeklyPlan WHERE user_id IN (household member user_ids)
                3. DELETE HouseholdMember WHERE household_id = {id}
                4. DELETE Household WHERE id = {id}
                User data (recipes, plans) is never deleted — only the household association is removed.

          - id: F1-7
            task: 'Add role-based authorization — [HouseholdOwner] policy for owner-only actions.'
            note: |
              In Program.cs, register a custom policy:
                builder.Services.AddAuthorization(options => {
                  options.AddPolicy("HouseholdOwner", policy =>
                    policy.Requirements.Add(new HouseholdOwnerRequirement()));
                });

              Create HouseholdOwnerRequirement : IAuthorizationRequirement (marker class, no props needed)

              Create HouseholdOwnerHandler : AuthorizationHandler<HouseholdOwnerRequirement>:
                - Read householdId from route values (context.Resource as HttpContext)
                - Query DB: does a HouseholdMember row exist WHERE household_id = routeId
                  AND user_id = currentUserId AND role = 'owner'?
                - If yes: context.Succeed(requirement)
                - If no: context.Fail()

              Register handler: builder.Services.AddScoped<IAuthorizationHandler, HouseholdOwnerHandler>()

              Apply [Authorize(Policy = "HouseholdOwner")] to:
                - DELETE /households/{id}
                - PUT /households/{id}
                - POST /households/{id}/members/{memberId}/remove
                - POST /households/{id}/members/{memberId}/transfer-ownership

              All other household endpoints use plain [Authorize] plus a manual membership check
              (verify caller is a member of the household in the route).

      - group: 'Frontend'
        tasks:
          - id: F1-8
            task: 'Household setup screen — create or join household post-signup.'
            note: |
              Route: /household/setup

              Post-login startup check (in AppComponent or an app-initializer):
                1. Call GET /households/mine
                2. If the response is an empty array AND localStorage key 'household_skipped' is not set:
                   Navigate to /household/setup
                3. If already has a household or skip flag is set: proceed to /tabs/recipes as normal

              Screen layout:
                - ion-segment at the top: "Create" | "Join"
                - Create tab:
                    ion-input for household name (required, max 100 chars)
                    "Create household" ion-button → POST /households → on success navigate to /tabs/recipes
                - Join tab:
                    ion-input for invite token or paste the full invite URL
                    (strip the URL to extract just the token if a full URL is pasted)
                    "Join household" ion-button → POST /households/join → on success navigate to /tabs/recipes
                - "Skip for now" text button at the bottom:
                    sets localStorage 'household_skipped' = '1', navigates to /tabs/recipes
                    The skip flag is checked on every startup — user will not be shown this screen again
                    until they clear the flag (e.g. from profile settings)

          - id: F1-9
            task: 'Member management screen — list members, invite link/QR code, remove member.'
            note: |
              Route: /tabs/profile/household (linked from the Profile tab)

              Sections:
                1. Household name header + edit icon (owner only) — tapping edit shows an inline ion-input

                2. Members list (ion-list):
                   Each ion-item: avatar circle (first letter of display_name), display_name,
                   role badge ("Owner" / "Member"), age if set, "Child" badge if is_child = true
                   Owner sees a "Remove" option per non-owner member (ion-item-sliding with delete action)
                   Tapping a member row navigates to F1-10 (member profile form)
                   POST /households/{id}/members/{memberId}/remove for the remove action

                3. Invite section:
                   "Generate invite link" ion-button → POST /households/{id}/invites
                   On success: show the invite_url in a copyable text field + a QR code canvas below it
                   QR code: npm install qrcode, then qrcode.toCanvas(canvasElement, invite_url)
                   Show expiry: "Expires in {hours}h" calculated from expires_at
                   "Copy link" button copies invite_url to clipboard (navigator.clipboard.writeText)

                4. "Leave household" ion-button (danger colour) at the bottom
                   Show ion-alert to confirm: "Are you sure you want to leave {householdName}?"
                   On confirm: POST /households/{id}/leave → on success navigate to /household/setup

          - id: F1-10
            task: 'Member profile form — display name, age, is_child toggle.'
            note: |
              Route: /tabs/profile/household/member/:memberId (navigated to from F1-9)

              This form edits HouseholdMember fields (not UserProfile).
              Any member can edit their own profile. Owner can edit any member's profile.

              Fields:
                - Display name: ion-input, required, max 100 chars
                - Age: ion-input type="number", optional, min 0 max 120
                - Is child: ion-toggle, label "Child member (under 13)"
                  When toggled on, age field label changes to "Child's age"

              Save button → PUT /households/{id}/members/{memberId}
                Body: { display_name, age, is_child }
                Response 200: MemberDto

              Add this endpoint to the backend: PUT /households/{id}/members/{memberId}
                Any household member can edit their own record. Owner can edit any record.
                Authorization: check caller is member of household AND (caller.user_id == member.user_id OR caller.role == 'owner')

              After save: show ToastService.showSuccess('Profile updated'), navigate back to F1-9.

  - id: F2
    name: 'Dietary profiles & constraints'
    description: 'Allergies, diet types, calorie targets per member'
    task_count: 8
    groups:
      - group: 'Backend'
        tasks:
          - id: F2-1
            task: 'Add DietaryProfile table via EF Core migration.'
            note: |
              DietaryProfile:
                id UUID PK
                member_id UUID not null FK → HouseholdMember.id (cascade delete)
                diet_type TEXT not null (default 'omnivore')
                  — check: ('omnivore','vegetarian','vegan','keto','paleo','gluten_free','custom')
                custom_diet_description TEXT nullable (required when diet_type = 'custom', max 500)
                calorie_target INT nullable (kcal/day)
                protein_target_g INT nullable
                carbs_target_g INT nullable
                fat_target_g INT nullable
                created_at TIMESTAMPTZ not null (default now())
                updated_at TIMESTAMPTZ not null (default now())
                Unique constraint: (member_id) — one profile per member, use upsert on save

              Add endpoints:
                GET  /households/{id}/members/{memberId}/dietary-profile → DietaryProfileDto (404 if none)
                PUT  /households/{id}/members/{memberId}/dietary-profile → upsert, return 200 DietaryProfileDto

          - id: F2-2
            task: 'Add Allergy table via EF Core migration.'
            note: |
              Allergy:
                id UUID PK
                member_id UUID not null FK → HouseholdMember.id (cascade delete)
                allergen TEXT not null
                  — check: ('gluten','dairy','nuts','eggs','shellfish','soy','fish','custom')
                custom_allergen TEXT nullable (required when allergen = 'custom', max 100)
                severity TEXT not null (default 'avoid') — check: ('avoid','intolerance')
                  severity is for UI display only — both are treated as hard constraints in plan generation
                created_at TIMESTAMPTZ not null (default now())
                Unique constraint: (member_id, allergen, custom_allergen)

              Add endpoints:
                GET    /households/{id}/members/{memberId}/allergies → Allergy[]
                POST   /households/{id}/members/{memberId}/allergies → body: { allergen, custom_allergen?, severity }
                DELETE /households/{id}/members/{memberId}/allergies/{allergyId}

          - id: F2-3
            task: 'Update Claude prompt builder in PlanGeneratorService to include per-member dietary constraints.'
            note: |
              In PlanGeneratorService.BuildSystemPrompt():

              Fetch all household members + their DietaryProfile + Allergy rows in a single JOIN query.
              Do not N+1 query — load everything up front.

              Append a constraints section to the system prompt, one entry per member:

                "Household member constraints (these are HARD constraints — violating them is not acceptable):
                 - {display_name}: {diet_type} diet{custom_text}. Allergic to: {allergen_list}.
                   Daily calorie target: {calorie_target} kcal (soft guideline, aim for approximately this).
                   Macros: {protein_target_g}g protein, {carbs_target_g}g carbs, {fat_target_g}g fat."

              If diet_type = 'custom': append "Custom diet notes: {custom_diet_description}"
              If calorie_target is null: omit the calorie target line for that member
              If no allergies: omit the allergy line for that member

              Mark allergens and diet type as HARD constraints in the prompt (use that exact word).
              Mark calorie/macro targets as soft guidelines.

              Example output for one member:
                "- Kuzey: vegan diet. Allergic to: nuts, shellfish.
                   Daily calorie target: 1800 kcal. Macros: 70g protein, 240g carbs, 55g fat."

          - id: F2-4
            task: 'Add POST /recipes/{id}/compatibility-check endpoint.'
            note: |
              POST /recipes/{id}/compatibility-check
                No body required — uses calling user's household context

              Logic:
                1. Load recipe with all RecipeIngredient rows (ingredient.name, ingredient.category)
                2. Load all HouseholdMember rows + their DietaryProfile + Allergy rows for the user's household
                   If user has no household, check only against the user's own UserProfile (no-op for now)

                3. For each member, check allergen conflicts:
                   Simple string matching — does any ingredient.name or ingredient.category contain the allergen?
                   Match map: 'gluten'    → ingredient.name contains "wheat","barley","rye","flour" (excl. "gluten-free")
                              'dairy'     → ingredient.category contains "Dairy" OR name contains "milk","cheese","cream","butter","yogurt"
                              'nuts'      → ingredient.category contains "Nuts" OR name contains "almond","cashew","walnut","peanut","pistachio"
                              'eggs'      → ingredient.name contains "egg"
                              'shellfish' → ingredient.name contains "shrimp","prawn","crab","lobster","scallop"
                              'soy'       → ingredient.name contains "soy","tofu","edamame"
                              'fish'      → ingredient.category contains "Seafood" OR name contains "salmon","tuna","cod","halibut"
                              'custom'    → skip automated check, always flag with message "Contains unknown allergen — verify manually"

                4. For each member, check diet conflicts:
                   vegan:       incompatible if any ingredient.category in ["Meat","Poultry","Seafood","Dairy","Eggs"]
                   vegetarian:  incompatible if any ingredient.category in ["Meat","Poultry","Seafood"]
                   gluten_free: use gluten allergen check above
                   keto:        incompatible if recipe total carbs_g > 30g per serving (from GET /recipes/{id}/nutrition)
                   omnivore / paleo / custom: no automated check

                5. Response:
                   {
                     compatible_members:   [{ member_id, display_name }],
                     incompatible_members: [{ member_id, display_name, reason: string }]
                   }

      - group: 'Frontend'
        tasks:
          - id: F2-5
            task: 'Dietary profile screen per member — diet type, allergens, calorie/macro targets.'
            note: |
              Route: /tabs/profile/household/member/:memberId/dietary-profile
              Linked from the member profile form (F1-10) via a "Dietary preferences" button.

              Load on init: GET /households/{id}/members/{memberId}/dietary-profile (may 404 — treat as empty form)
                            GET /households/{id}/members/{memberId}/allergies

              Sections:

              1. Diet type (ion-radio-group):
                 Options: Omnivore, Vegetarian, Vegan, Keto, Paleo, Gluten-free, Custom
                 If "Custom" selected: show ion-textarea below (required), placeholder "Describe your diet..."

              2. Allergens (ion-list with ion-checkbox):
                 One checkbox per standard allergen: Gluten, Dairy, Nuts, Eggs, Shellfish, Soy, Fish
                 Pre-check boxes based on loaded Allergy rows
                 Below the list: ion-input "Add custom allergen" + "Add" button
                 Custom allergens shown as ion-chip with × remove button

              3. Daily targets (ion-grid with ion-input per macro):
                 Calories (kcal), Protein (g), Carbs (g), Fat (g) — all optional
                 "Use defaults" button: calls the F2-8 defaults logic based on member's age/is_child,
                 fills inputs and shows a note "Based on general nutritional guidelines — adjust as needed"

              Save → PUT dietary-profile (upsert) + reconcile allergies (add new, delete removed).
              Show ToastService.showSuccess('Dietary profile saved').

          - id: F2-6
            task: 'Show compatibility warning on recipe detail screen.'
            note: |
              On the recipe detail screen (MVP 1 F3-5), after the recipe loads:
                Call POST /recipes/{id}/compatibility-check in parallel with other detail loading.

              If incompatible_members is non-empty:
                Show a horizontal ion-chip strip below the recipe title area.
                One amber (warning colour) chip per incompatible member:
                  "⚠ {display_name}"
                Tapping a chip opens an ion-alert:
                  Header: "{display_name} can't eat this"
                  Message: the reason string from the response
                  Button: "OK"

              If all members compatible: show nothing (no green "all clear" chip).

              Handle loading state: show 2-3 skeleton chips (ion-skeleton-text) while the check is pending.
              Handle error silently (log, do not show error to user — the check is informational only).

          - id: F2-7
            task: 'Weekly plan generation modal — show household constraints before generating.'
            note: |
              Replace the direct POST /plans/generate call with a flow that first opens an ion-modal.

              Modal content:
                1. Title: "Generate weekly plan"

                2. Household members summary:
                   ion-list, one row per member: display_name, diet_type badge, allergen count
                   (e.g. "Kuzey — Vegan — 2 allergens")
                   Tapping a row navigates to their dietary profile screen (opens in a new modal on top)

                3. Week selector:
                   ion-datetime-button or a simple date input for week_start
                   Default: next Monday (compute in TypeScript: find next Monday from today)
                   Validate: week_start must be a Monday

                4. Free-text preferences:
                   ion-textarea, label "Any preferences this week?"
                   placeholder "e.g. More Italian food, easy weeknight meals, use up the chicken in the fridge"
                   Optional — if blank, omit from the API body

                5. Generate button → POST /plans/generate { week_start, preferences? }
                   While pending: show a full-modal loading overlay with a spinner and
                   "Generating your plan with Claude AI..." text (can take 5-15 seconds)
                   On success: close modal, navigate to /tabs/plan/{newPlanId}/review (F3-6)
                   On error: dismiss the overlay, show ToastService.showError with Claude's error message

          - id: F2-8
            task: 'Pre-populate sensible default calorie/macro targets when creating a dietary profile.'
            note: |
              Default values table (based on general WHO/NHS reference values):

                is_child = true:
                  age < 4:    calories: 1200, protein: 35g,  carbs: 165g, fat: 45g
                  age 4–8:    calories: 1400, protein: 45g,  carbs: 195g, fat: 50g
                  age 9–13:   calories: 1700, protein: 55g,  carbs: 230g, fat: 60g
                  age 14–17:  calories: 2000, protein: 60g,  carbs: 265g, fat: 65g

                is_child = false (adult, age 18+):
                  calories: 2000, protein: 50g, carbs: 275g, fat: 65g

              Implement as a pure function getDefaultTargets(age: number | null, isChild: boolean) in both:
                - Frontend: used when user clicks "Use defaults" button in F2-5
                - Backend: called server-side when a new DietaryProfile is first created (PUT with null targets)
                  and the member has an age set — auto-populate targets if calorie_target is null

              Show a note below the inputs: "Based on general nutritional guidelines. Adjust to your needs."
              Never overwrite targets the user has already manually set.

  - id: F3
    name: 'Weekly nutrition tracking'
    description: 'Plan totals vs targets, per-day breakdown'
    task_count: 6
    groups:
      - group: 'Backend'
        tasks:
          - id: F3-1
            task: 'Add GET /plans/{id}/nutrition-summary — per-day and weekly totals vs member targets.'
            note: |
              Response shape:
              {
                week_totals: { calories, protein_g, carbs_g, fat_g, fibre_g },
                per_day: [
                  {
                    date: "2026-03-23",
                    calories, protein_g, carbs_g, fat_g, fibre_g,
                    per_member_vs_target: [
                      {
                        member_id, display_name,
                        calorie_target, calorie_actual,
                        protein_target_g, protein_actual_g,
                        carbs_target_g, carbs_actual_g,
                        fat_target_g, fat_actual_g,
                        status: "on_target" | "under" | "over"
                      }
                    ]
                  }
                ],
                per_member_week_averages: [
                  { member_id, display_name, avg_daily_calories, calorie_target, status }
                ],
                gaps: [
                  { date, member_id, display_name, type: "under_calories" | "over_calories", target, actual, percent_deviation }
                ]
              }

              status logic:
                "on_target" = actual is within 10% of target (above or below)
                "under"     = actual is more than 10% below target
                "over"      = actual is more than 20% above target

              Aggregation:
                For each PlanSlot: scale RecipeIngredient nutrients by (slot.servings / recipe.servings)
                Sum across all slots for the day.
                MVP simplification: divide total household calories evenly among adult members
                (per-member portion tracking is a future feature).
                If member has no calorie_target: omit that member from per_member_vs_target.

              gaps[] population:
                Flag a day/member pair when calorie_actual is > 20% below or above calorie_target.
                Only populated for members who have a calorie_target set.

          - id: F3-2
            task: 'Nutrient gap detection is included in the gaps[] array of GET /plans/{id}/nutrition-summary — see F3-1 for the full spec.'
            note: |
              This task is fulfilled as part of F3-1. The gaps[] array in the nutrition-summary response
              covers all gap detection logic. No separate endpoint needed.

              Ensure gaps[] is populated before returning the response — do not make it a separate async call.
              If calorie_target is null for a member, skip that member entirely in gap detection.

          - id: F3-3
            task: 'Add NutritionSnapshot table and save snapshot when a plan is confirmed.'
            note: |
              NutritionSnapshot table (EF Core migration):
                id UUID PK
                plan_id UUID not null FK → WeeklyPlan.id (cascade delete)
                household_id UUID not null FK → Household.id (cascade delete)
                week_start DATE not null
                snapshot_json JSONB not null (stores the full GET /plans/{id}/nutrition-summary response)
                created_at TIMESTAMPTZ not null (default now())
                Unique constraint: (plan_id) — one snapshot per confirmed plan

              Add PUT /plans/{id}/status endpoint:
                Body: { status: 'confirmed' | 'draft' }
                Updates WeeklyPlan.status
                When status transitions to 'confirmed':
                  1. Call GET /plans/{id}/nutrition-summary internally
                  2. Insert/update NutritionSnapshot with the result
                Response 200: WeeklyPlanDto

              GET /nutrition/history:
                Query params: ?limit=12 (default 12)
                Returns the last {limit} NutritionSnapshot rows for the calling user's household,
                ordered by week_start DESC.
                Parse snapshot_json and return only per_member_week_averages[] from each snapshot
                (not the full snapshot — keep the response small for trend charting).

      - group: 'Frontend'
        tasks:
          - id: F3-4
            task: 'Weekly nutrition panel on plan screen — collapsible macro bars vs targets.'
            note: |
              Add an ion-accordion below the week grid on the plan screen (MVP 1 F6-7).

              Accordion header: "This week's nutrition" + a colour-coded dot (green/amber/red)
              based on the worst status in per_member_week_averages[].

              Accordion content:
                One row per macro: Calories, Protein, Carbs, Fat.
                Each row: macro label | ion-progress-bar (value = actual/target, max 1.0) | "Xg / Yg" text

                Colour the progress bar via the color attribute:
                  "success"  (green)  = status is "on_target"
                  "warning"  (amber)  = status is "under" or "over" but < 30% deviation
                  "danger"   (red)    = deviation >= 30%

                If household has multiple members, show a per-member breakdown as a nested
                ion-accordion inside the main one (collapsed by default).

              Data source: load GET /plans/{id}/nutrition-summary once when the plan screen loads.
              Cache the result in a PlanNutritionService signal — F3-4, F3-5, and F3-6 all reuse it.

          - id: F3-5
            task: 'Per-day nutrition tooltip — tap a day header to see that day breakdown.'
            note: |
              On the plan screen weekly grid, make each day column header (Mon, Tue, Wed...) an
              interactive element (add role="button", cursor pointer).

              On tap, open an ion-popover on desktop/web or an ion-modal (50% height) on mobile.

              Content:
                - Day heading (e.g. "Monday, March 23")
                - Calories: large number, with target below if set (e.g. "1,840 kcal / 2,000 target")
                - Macro breakdown: Protein | Carbs | Fat in a 3-column grid
                - If household: per-member vs-target table (member name, actual, target, status icon ✓/⚠/✗)

              Data: pull from the per_day[] array already loaded for F3-4. No extra API call.
              Dismiss on backdrop tap or popover click-outside.

          - id: F3-6
            task: 'Plan generation result screen — show nutrition summary before user confirms the plan.'
            note: |
              After POST /plans/generate succeeds, navigate to /tabs/plan/:planId/review
              (do not go directly to the plan grid).

              Route: /tabs/plan/:planId/review

              Screen content:
                1. Heading: "Your plan is ready"
                2. Nutrition summary card (use the same nutrition panel component from F3-4,
                   but expanded by default — not inside an accordion)
                3. Gap alerts (if gaps[] is non-empty):
                   ion-card (colour: warning): "Nutrition gaps detected"
                   List the affected days and members inline
                4. Two buttons:
                   - "Regenerate" → POST /plans/generate again (same preferences/week_start),
                     this replaces the current plan (old plan rows deleted by the API), re-renders the screen
                   - "Confirm plan" → PUT /plans/{planId}/status { status: 'confirmed' },
                     navigate to /tabs/plan (plan grid screen)

              Load GET /plans/{planId}/nutrition-summary immediately when this screen mounts.
              Show AppLoadingSpinnerComponent while loading.

  - id: F4
    name: 'Cross-device real-time sync'
    description: 'SignalR live updates, optimistic UI, offline detection'
    task_count: 5
    groups:
      - group: 'Backend'
        tasks:
          - id: F4-1
            task: 'Set up ASP.NET Core SignalR hub for real-time plan and shopping list updates.'
            note: |
              SignalR is built into ASP.NET Core — no extra NuGet package needed for the server.

              Create api/Hubs/PlanHub.cs:
                public class PlanHub : Hub
                {
                  // Client calls this after connecting to subscribe to their household's updates
                  public async Task JoinHousehold(string householdId)
                  {
                    // Validate: check DB that the caller (Context.UserIdentifier) is a member
                    var userId = Context.UserIdentifier; // set via JWT "sub" claim
                    bool isMember = await _householdService.IsMember(userId, householdId);
                    if (!isMember) throw new HubException("Not a member of this household");
                    await Groups.AddToGroupAsync(Context.ConnectionId, householdId);
                  }
                }

              Register in Program.cs:
                builder.Services.AddSignalR();
                app.MapHub<PlanHub>("/hubs/plan");

              Server-to-client messages (emit from service layer, not controllers):
                Inject IHubContext<PlanHub> into PlanSlotService and ShoppingListService.
                After any mutation, call:
                  await _hubContext.Clients.Group(householdId)
                    .SendAsync("PlanSlotUpdated", planSlotDto);
                  await _hubContext.Clients.Group(householdId)
                    .SendAsync("ShoppingItemUpdated", shoppingItemDto);
                  await _hubContext.Clients.Group(householdId)
                    .SendAsync("PlanStatusChanged", planId, newStatus);

              Pass householdId through the service layer — store it on WeeklyPlan (already has user_id,
              use that to look up the household). Add a helper GetHouseholdIdForPlan(planId).

          - id: F4-2
            task: 'Configure SignalR JWT authentication so connections are authorized.'
            note: |
              SignalR WebSocket connections cannot send HTTP headers, so the JWT must be passed
              as a query parameter. Configure JwtBearer to accept it:

              In Program.cs, inside the AddAuthentication().AddJwtBearer() config block:
                options.Events = new JwtBearerEvents
                {
                  OnMessageReceived = context =>
                  {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                      context.Token = accessToken;
                    return Task.CompletedTask;
                  }
                };

              This means the Angular client will connect to:
                /hubs/plan?access_token={jwt}

              The Hub's Context.UserIdentifier will be set to the "sub" claim value (the user's id).
              Configure this by setting options.ClaimsIdentity.UserNameClaimType = "sub" in JWT validation
              options, or by setting Hub.Context.UserIdentifier manually via IUserIdProvider.

              Create a NameIdentifierUserIdProvider : IUserIdProvider that returns the "sub" claim.
              Register: builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>()

      - group: 'Frontend'
        tasks:
          - id: F4-3
            task: 'Create RealtimeService Angular injectable using @microsoft/signalr.'
            note: |
              Install: npm install @microsoft/signalr

              Create RealtimeService (providedIn: 'root'):

                private connection: signalR.HubConnection | null = null;

                connect(householdId: string): void {
                  const token = this.authService.getToken();
                  this.connection = new signalR.HubConnectionBuilder()
                    .withUrl(`${this.apiUrl}/hubs/plan?access_token=${token}`)
                    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // retry intervals ms
                    .build();

                  this.connection.on('PlanSlotUpdated', (slot: PlanSlotDto) => {
                    this.planService.updateSlotInSignal(slot);
                  });
                  this.connection.on('ShoppingItemUpdated', (item: ShoppingItemDto) => {
                    this.shoppingService.updateItemInSignal(item);
                  });
                  this.connection.on('PlanStatusChanged', (planId: string, status: string) => {
                    this.planService.updatePlanStatus(planId, status);
                  });

                  this.connection.start()
                    .then(() => this.connection!.invoke('JoinHousehold', householdId))
                    .catch(err => console.error('SignalR connection failed:', err));
                }

                disconnect(): void {
                  this.connection?.stop();
                  this.connection = null;
                }

              Call connect() in the plan screen (F6-7) and shopping list screen (F7-4) ngOnInit,
              after confirming the user has a household.
              Call disconnect() in ngOnDestroy of those same screens.

              When a SignalR event arrives, update the Angular signal in-place — do NOT re-fetch
              the entire resource. Find the item by id in the signal array and replace it.

          - id: F4-4
            task: 'Optimistic updates on shopping list item check-off.'
            note: |
              In ShoppingListService, implement the toggle with optimistic update:

                toggleItem(listId: string, item: ShoppingItem): void {
                  const previousValue = item.is_checked;
                  // 1. Optimistically update the signal immediately
                  item.is_checked = !previousValue;

                  // 2. Fire the API call in the background
                  this.http.patch(`/shopping-lists/${listId}/items/${item.id}`,
                    { is_checked: item.is_checked })
                  .subscribe({
                    next: () => { /* signal is already correct, do nothing */ },
                    error: () => {
                      // 3. Revert on failure
                      item.is_checked = previousValue;
                      this.toast.showError('Could not update item — please try again');
                    }
                  });
                }

              The shopping list screen component calls this method on (ionChange) of each checkbox.
              Do not disable the checkbox while the request is pending — let the user interact freely.

          - id: F4-5
            task: 'Offline detection banner — show when offline, queue shopping list mutations.'
            note: |
              Create OfflineService (Angular injectable, providedIn: 'root'):

                isOffline = signal(!navigator.onLine);

                constructor() {
                  window.addEventListener('online',  () => {
                    this.isOffline.set(false);
                    this.flushQueue();
                  });
                  window.addEventListener('offline', () => this.isOffline.set(true));
                }

              In AppComponent template: conditionally show an ion-toolbar or ion-banner
              when OfflineService.isOffline() is true:
                <ion-banner *ngIf="offlineService.isOffline()">
                  You're offline — changes will sync when reconnected
                </ion-banner>
              Style it with color="warning", fixed position at top.

              Mutation queue (shopping list only for MVP):
                In ShoppingListService, before any PATCH, check isOffline():
                  If offline: push { url, method, body } to pendingMutations signal array,
                              apply the optimistic update (same as F4-4),
                              do NOT fire the HTTP call yet
                  If online:  proceed normally

                flushQueue() in OfflineService:
                  Execute pending mutations sequentially (one at a time, in order),
                  clear each from the queue after it succeeds.
                  If a queued mutation fails after reconnection: show a single toast
                  "Some offline changes failed to sync" and clear the queue.

              Note: full offline-first with SQLite cache is out of scope for MVP 2.
              Only shopping list check-offs are queued. Other mutations (recipe edits, plan changes)
              show ToastService.showError('You are offline') if attempted while offline.

  - id: F5
    name: 'Body map'
    description: 'Organ coverage from weekly nutrients, interactive SVG map'
    priority: 'flagship feature'
    task_count: 8
    groups:
      - group: 'Backend'
        tasks:
          - id: F5-1
            task: 'Seed NutrientOrganMapping table with all nutrient-organ pairs.'
            note: |
              Seed in OnModelCreating via modelBuilder.Entity<NutrientOrganMapping>().HasData(...)
              or in a separate data migration. Use fixed Guid PKs so re-running migrations is idempotent.

              Seed at minimum these 35 pairs (nutrient_name must match the keys used in F5-2 mapping):

              nutrient_name       | organ_name       | body_system
              --------------------|------------------|------------------
              omega_3             | Brain            | Nervous system
              omega_3             | Heart            | Cardiovascular
              omega_3             | Lungs            | Respiratory
              vitamin_c           | Skin             | Integumentary
              vitamin_c           | Lungs            | Respiratory
              vitamin_c           | Immune system    | Immune
              calcium             | Bones            | Skeletal
              calcium             | Teeth            | Skeletal
              calcium             | Heart            | Cardiovascular
              fibre               | Gut              | Digestive
              fibre               | Heart            | Cardiovascular
              iron                | Blood            | Cardiovascular
              iron                | Brain            | Nervous system
              vitamin_d           | Bones            | Skeletal
              vitamin_d           | Immune system    | Immune
              vitamin_d           | Muscles          | Muscular
              magnesium           | Heart            | Cardiovascular
              magnesium           | Muscles          | Muscular
              magnesium           | Brain            | Nervous system
              potassium           | Heart            | Cardiovascular
              potassium           | Kidneys          | Urinary
              potassium           | Muscles          | Muscular
              zinc                | Immune system    | Immune
              zinc                | Skin             | Integumentary
              zinc                | Eyes             | Sensory
              vitamin_a           | Eyes             | Sensory
              vitamin_a           | Skin             | Integumentary
              vitamin_a           | Immune system    | Immune
              vitamin_b12         | Brain            | Nervous system
              vitamin_b12         | Blood            | Cardiovascular
              folate              | Brain            | Nervous system
              folate              | Blood            | Cardiovascular
              protein             | Muscles          | Muscular
              selenium            | Liver            | Digestive
              iodine              | Thyroid          | Endocrine

          - id: F5-2
            task: 'Create BodyMapService.ComputeCoverage(weeklyPlanId) — aggregates nutrients and returns per-organ coverage.'
            note: |
              Step 1 — Aggregate nutrients from the plan:
                For each PlanSlot in the plan:
                  For each RecipeIngredient in slot.recipe.ingredients:
                    scaledAmount = ingredient.nutritionValue * (slot.servings / recipe.servings)
                Accumulate into totalNutrients: Dictionary<string, decimal>

              Step 2 — Map Edamam nutrient keys to our nutrient_name values:
                Create a static NutrientKeyMapper with these mappings (Edamam key → nutrient_name):
                  "CA"    → "calcium"
                  "FE"    → "iron"
                  "MG"    → "magnesium"
                  "ZN"    → "zinc"
                  "K"     → "potassium"
                  "FAPU"  → "omega_3"       (PUFA as omega-3 proxy)
                  "VITC"  → "vitamin_c"
                  "VITD"  → "vitamin_d"
                  "VITA_RAE" → "vitamin_a"
                  "VITB12" → "vitamin_b12"
                  "FOLFD" → "folate"
                  "FIBTG" → "fibre"
                  "PROCNT" → "protein"
                  "SE"    → "selenium"
                  "ID"    → "iodine"

              Step 3 — Per-organ coverage score:
                Define weekly RDA reference values (7 × daily RDA):
                  calcium: 7000mg, iron: 56mg (male) / 105mg (female — use 105mg as conservative default),
                  magnesium: 2800mg, zinc: 77mg, potassium: 23100mg, omega_3: 9800mg (1.4g/day),
                  vitamin_c: 560mg, vitamin_d: 420IU, vitamin_a: 5250μg RAE, vitamin_b12: 16.8μg,
                  folate: 2800μg DFE, fibre: 175g, protein: 350g (50g/day baseline),
                  selenium: 385μg, iodine: 1050μg

                For each organ in NutrientOrganMapping (distinct organ_name values):
                  Get all nutrient_names mapped to this organ.
                  For each mapped nutrient:
                    nutrientScore = min(100, (totalNutrients[nutrient_name] / weeklyRDA[nutrient_name]) * 100)
                  organCoverageScore = average of all nutrientScores for this organ
                  is_covered = organCoverageScore >= 60

                contributing_recipes: for each organ, collect the recipe titles whose ingredients
                contributed the most to the organ's highest-scoring nutrient (top 3 recipes max).

              Return: List<OrganCoverageResult>
                { organ_name, body_system, is_covered, coverage_score (0–100),
                  contributing_nutrients: string[], contributing_recipes: RecipeSummaryDto[] }

          - id: F5-3
            task: 'Add GET /plans/{id}/body-map endpoint.'
            note: |
              Response shape:
              {
                organs: [
                  {
                    organ_name: "Brain",
                    body_system: "Nervous system",
                    is_covered: true,
                    coverage_score: 78,
                    contributing_nutrients: ["omega_3", "vitamin_b12"],
                    contributing_recipes: [{ id, title, cover_colour }]
                  }
                ],
                overall_coverage_percent: 72,   // percent of organs where coverage_score >= 60
                low_coverage_organs: ["Liver", "Eyes"]  // organs with score < 40
              }

              Call BodyMapService.ComputeCoverage(planId).
              Check OrganCoverageCache first (F5-4) — if fresh cache exists, deserialise and return it.
              If no cache: compute, store result in cache, then return.
              "Fresh" = cached_at is within 1 hour OR no PlanSlot has changed since cached_at.

          - id: F5-4
            task: 'Add OrganCoverageCache table and invalidation logic.'
            note: |
              OrganCoverageCache table (EF Core migration):
                id UUID PK
                plan_id UUID not null FK → WeeklyPlan.id (cascade delete)
                cached_at TIMESTAMPTZ not null (default now())
                result_json JSONB not null
                Unique constraint: (plan_id)

              Use INSERT ... ON CONFLICT (plan_id) DO UPDATE to upsert the cache.

              Cache invalidation — whenever a PlanSlot is inserted, updated, or deleted:
                DELETE FROM OrganCoverageCache WHERE plan_id = affectedPlanId

              Implement in PlanSlotService (which already has IHubContext injected):
                After any slot mutation, call: await _cacheService.InvalidateBodyMap(planId)
                CacheInvalidationService is a simple service that deletes the cache row.

              Do not invalidate on shopping list changes — shopping items do not affect nutrition coverage.

      - group: 'Frontend'
        tasks:
          - id: F5-5
            task: 'Body map screen — SVG human outline with tappable organ regions.'
            note: |
              Route: /tabs/plan/body-map
              Accessible via a "Body Map" button on the plan screen.

              Load on init: GET /plans/{currentPlanId}/body-map

              SVG implementation:
                - Embed an inline SVG of a simplified front-facing human body outline
                - Use a creative-commons SVG as a base (e.g. search Wikimedia Commons for
                  "human body outline SVG") or create a simplified geometric version for MVP
                  (head = ellipse, torso = rectangle, limbs = rectangles — labelled, not photorealistic)
                - Each organ region must be a <path> or <g> element with an id matching organ_name:
                    id="Brain", id="Heart", id="Lungs", id="Gut", id="Liver", id="Bones",
                    id="Skin", id="Eyes", id="Muscles", id="Immune system", id="Blood",
                    id="Kidneys", id="Thyroid", id="Teeth"
                - Angular component (OrganBodyMapComponent) receives organs[] as @Input
                  On ngAfterViewInit, iterate organs[] and for each:
                    const el = svgEl.querySelector(`#${organ.organ_name}`)
                    if (el) el.setAttribute('fill', this.getOrganFill(organ))

                Organ colour map (store as a static constant):
                  Brain: '#9C27B0', Heart: '#F44336', Lungs: '#2196F3', Gut: '#FF9800',
                  Liver: '#795548', Bones: '#78909C', Skin: '#FFC107', Eyes: '#00BCD4',
                  Muscles: '#4CAF50', 'Immune system': '#E91E63', Blood: '#D32F2F',
                  Kidneys: '#FF7043', Thyroid: '#26A69A', Teeth: '#ECEFF1'

                Fill logic:
                  coverage_score >= 80: organ colour at full opacity
                  coverage_score 60–79: organ colour at 0.65 opacity
                  coverage_score 40–59: '#FFB300' (amber)
                  coverage_score < 40:  '#9E9E9E' (muted grey)

                Add click/tap handler on each named element → emit selected organ_name
                → opens the organ detail panel (F5-6)

          - id: F5-6
            task: 'Organ detail panel — slide-up panel on organ tap.'
            note: |
              Open as an ion-modal with breakpoints: [0, 0.5, 0.9], initialBreakpoint: 0.5
              Pass the selected OrganCoverageResult as a component input.

              Panel content:
                1. Header: organ_name (h2), body_system (subtitle)
                2. Coverage score: large centred number (e.g. "78%") with a CSS circular progress ring
                   (use a <svg> circle with stroke-dashoffset for the arc — no extra library needed)
                   Colour: green (#4CAF50) >= 60, amber (#FF9800) 40–59, red (#F44336) < 40
                3. Contributing nutrients: ion-chip list of contributing_nutrients[]
                4. Contributing recipes: horizontal ion-scroll of recipe cards
                   Each card: cover_colour chip (coloured square) + recipe title
                   Tapping a card: close the modal, navigate to /tabs/recipes/:recipeId
                5. If coverage_score < 60: show a tip card:
                   "Try adding more {contributing_nutrients[0]} to your plan.
                    Good sources include {static tip per nutrient — hardcode a short tips map}"

                   Tips map (static dictionary, one entry per nutrient_name):
                     omega_3: "salmon, sardines, walnuts, flaxseeds"
                     vitamin_c: "bell peppers, broccoli, citrus fruits, strawberries"
                     calcium: "dairy, fortified plant milk, leafy greens, almonds"
                     fibre: "beans, lentils, oats, vegetables, whole grains"
                     iron: "red meat, lentils, spinach, fortified cereals"
                     (add one entry per nutrient_name in the seed data)

          - id: F5-7
            task: 'Nutrition gap alert card at the top of the body map screen.'
            note: |
              Condition: low_coverage_organs[] from GET /plans/{id}/body-map is non-empty (score < 40)

              Show an ion-card above the SVG:
                Colour: warning (amber)
                Header icon: ⚠ (ion-icon name="warning-outline")
                Title: "Your plan is missing key nutrients"
                Body: "The following body systems have low coverage this week:
                       {comma-separated list of low_coverage_organs}"
                No action button needed — user can tap the organs directly on the map below

              If low_coverage_organs is empty: show nothing above the SVG (no green "all covered" card).

          - id: F5-8
            task: 'Add data source disclaimer info button on the body map screen.'
            note: |
              Add an ion-button (fill="clear", slot="end" in the page toolbar) with
              ion-icon name="information-circle-outline".

              On tap, call this.alertController.create({
                header: 'About this data',
                message: 'Nutrient-organ relationships shown here are based on general nutritional
                          science and are for informational purposes only. Individual nutritional
                          needs vary based on age, sex, health conditions, and other factors.
                          Consult a registered dietitian or healthcare provider for personalised
                          dietary advice.',
                buttons: ['OK']
              }).then(a => a.present());

              This is a legal/safety requirement — do not skip it.

build_order:
  - step: 1
    feature: F1
    note: 'Household and member models must exist before dietary profiles can reference members.
           F1-1 migration must run first — it adds household_id to Recipe and WeeklyPlan tables.'
  - step: 2
    feature: F2
    note: 'Dietary profiles needed before plan generation can apply member constraints (F2-3).
           Complete F2-1 and F2-2 (migrations) before F2-3 (prompt update).'
  - step: 3
    feature: F3
    note: 'Nutrition tracking builds on plan + profile data from F1 and F2.
           F3-3 (NutritionSnapshot) requires the PUT /plans/{id}/status endpoint — build that first.'
  - step: 4
    feature: F4
    note: 'Real-time sync can be layered on once core data flows (F1–F3) are stable and tested.
           F4-1 and F4-2 (backend SignalR) before F4-3 (Angular client).'
  - step: 5
    feature: F5
    note: 'Body map requires the NutrientOrganMapping seed data (F5-1) and a confirmed plan with
           nutrition data from F3. Do F5-1 (seed) and F5-4 (cache table migration) before F5-2/F5-3.'
```
