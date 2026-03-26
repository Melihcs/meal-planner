# MVP 4 — Social Layer

```yaml
mvp: 4
name: 'Social layer'
description: 'Friends, sharing, comments & suggestions'
ships_as: 'The full social recipe platform'
prereqs:
  - 'MVP 1 complete'
  - 'MVP 2 complete'
  - 'MVP 3 complete'
summary:
  features: 4
  tasks: 47

features:
  - id: F1
    name: 'Friends & social graph'
    description: 'Send/accept requests, friends-only visibility tier, block/mute'
    task_count: 12
    groups:
      - group: 'Backend'
        tasks:
          - id: F1-1
            task: 'Add Friendship table: requester_id FK, addressee_id FK, status (pending/accepted/declined), created_at. Unique constraint on (requester_id, addressee_id). Check constraint: requester_id != addressee_id.'
          - id: F1-2
            task: 'Implement FriendshipsController: POST /friendships (send request), PATCH /friendships/{id} (accept/decline), DELETE /friendships/{id} (unfriend), GET /friendships (my friends + pending)'
          - id: F1-3
            task: 'Add user search: GET /users/search?q= — searches display names of users with at least one public recipe. Returns name, avatar, public recipe count, friendship status with requester.'
          - id: F1-4
            task: "Activate friends visibility tier: update RLS so visibility='friends' recipes are readable by accepted friends of the owner only"
          - id: F1-5
            task: 'Add friends feed: GET /feed — recently published public + friends-only recipes from accepted friends, sorted by published_at desc, paginated (cursor-based recommended)'
          - id: F1-6
            task: 'Add block/mute user functionality — BlockedUser table (blocker_id FK, blocked_id FK, created_at). POST /users/{id}/block (also removes any existing friendship), DELETE /users/{id}/block, GET /users/blocked.'
            note: 'Blocked users cannot: send friend requests, comment on your recipes, submit suggestions, appear in your feed. Update all relevant queries to exclude blocked users.'

      - group: 'Frontend'
        tasks:
          - id: F1-7
            task: 'Friends screen — two tabs: Friends list (accepted), Pending (incoming + outgoing requests). Each entry shows avatar, name, mutual recipe count.'
          - id: F1-8
            task: 'User search modal — search by display name, see profile preview, send friend request button'
          - id: F1-9
            task: 'Add Friends visibility option to recipe edit screen (was private/public, now private/friends/public)'
          - id: F1-10
            task: "Friends feed tab — chronological list of new recipes from friends, same card design as Discover. Shows 'from [friend name]' label. Empty state: 'Add friends to see their recipes here'."
          - id: F1-11
            task: 'Block user option — accessible from public profile screen overflow menu. Confirmation dialog explaining what blocking does. Blocked users screen in settings.'
          - id: F1-12
            task: 'Unfriend confirmation dialog — explain that removing a friend also removes access to friends-only recipes'

  - id: F2
    name: 'Comments'
    description: 'Optional per recipe, author-moderated'
    task_count: 10
    groups:
      - group: 'Backend'
        tasks:
          - id: F2-1
            task: 'Add RecipeComment table: recipe_id FK, user_id FK, body TEXT (max 1000 chars), created_at, updated_at, is_deleted BOOL, is_edited BOOL. Add comments_enabled BOOL column to Recipe.'
          - id: F2-2
            task: 'Implement RecipeCommentsController: GET /recipes/{id}/comments (paginated), POST /recipes/{id}/comments (create), PATCH /recipes/{id}/comments/{commentId} (edit within 15 min), DELETE /recipes/{id}/comments/{commentId} (soft delete)'
            note: 'Author can delete any comment on their recipe. Commenter can delete their own. Soft delete — body replaced with [deleted], record kept for thread integrity. Edit sets is_edited = true.'
          - id: F2-3
            task: 'Add basic profanity/spam guard: simple word-list check on comment body before save (also on edits). Return 422 with a clear message if triggered.'
            note: 'Configurable word list stored in config, not hardcoded. Basic filter — not ML-based.'
          - id: F2-4
            task: 'Add rate-limiting on comments — max 5 comments per user per recipe per hour, max 30 per user across all recipes per hour. Return 429 if exceeded.'

      - group: 'Frontend'
        tasks:
          - id: F2-5
            task: "Comments section on recipe detail — shows up to 10 comments inline, 'View all' expands to full list. Only visible when comments_enabled = true."
          - id: F2-6
            task: 'Comment input — text field + post button at bottom of comments section, for authenticated users only. Shows avatar of commenter. Character count indicator approaching 1000 limit.'
          - id: F2-7
            task: 'Comment moderation — long-press a comment to reveal delete option (own comments or own recipe). Confirm dialog before delete.'
          - id: F2-8
            task: "Comment editing — tap own comment to edit (within 15 min of posting). Show 'edited' label next to timestamp on edited comments."
          - id: F2-9
            task: "Comments toggle on recipe edit screen — 'Allow comments' switch. Disabling hides existing comments from public view but does not delete them."
          - id: F2-10
            task: 'Block check on comment display — hide comments from blocked users without showing [deleted] placeholder'

  - id: F3
    name: 'Private recipe suggestions'
    description: 'Submit suggestion to author, author accepts or rejects'
    task_count: 8
    groups:
      - group: 'Backend'
        tasks:
          - id: F3-1
            task: 'Add RecipeSuggestion table: recipe_id FK, suggester_id FK, body TEXT (max 2000 chars), status (pending/accepted/rejected), author_note TEXT, created_at'
          - id: F3-2
            task: 'Implement POST /recipes/{id}/suggestions — any authenticated user can submit a suggestion on a public recipe. One pending suggestion per user per recipe max. Check blocked status.'
          - id: F3-3
            task: 'Implement GET /recipes/{id}/suggestions — visible to recipe author only. Returns all suggestions with status.'
          - id: F3-4
            task: 'Implement PATCH /recipes/{id}/suggestions/{suggestionId} — author-only: set status to accepted/rejected, optionally add a note. Triggers notification to suggester.'
          - id: F3-5
            task: "Add GET /suggestions/mine — user sees their own submitted suggestions and their current status (but not other users' suggestions on the same recipe)"

      - group: 'Frontend'
        tasks:
          - id: F3-6
            task: 'Suggest a change button on public recipe detail — opens a bottom sheet with a text area and character count (2000 limit). Disabled if user already has a pending suggestion on this recipe.'
          - id: F3-7
            task: 'Suggestions inbox on recipe edit screen — list of pending suggestions with accept/reject buttons and an optional reply note field'
          - id: F3-8
            task: "My suggestions screen — list of suggestions the user has submitted, grouped by status (pending / accepted / rejected) with the author's reply note if provided"

  - id: F4
    name: 'Notifications'
    description: 'In-app + push for all social events'
    task_count: 17
    groups:
      - group: 'Backend'
        tasks:
          - id: F4-1
            task: 'Add Notification table: user_id FK, type (enum), payload JSONB, is_read BOOL, created_at. Types: friend_request, friend_accepted, new_comment, suggestion_received, suggestion_accepted, suggestion_rejected.'
            note: 'Added suggestion_received type — the recipe author needs to know when someone submits a suggestion.'
          - id: F4-2
            task: 'Create NotificationService — called from all relevant controllers (FriendshipsController, CommentsController, SuggestionsController) to write notification rows. Fire-and-forget pattern — notifications should never block the main request.'
          - id: F4-3
            task: 'Implement GET /notifications (paginated, unread first), PATCH /notifications/{id}/read, POST /notifications/read-all'
          - id: F4-4
            task: 'Add PushToken table: user_id FK, token, platform (ios/android/web). Expose POST /push-tokens and DELETE /push-tokens/{token}.'
          - id: F4-5
            task: "Integrate FCM (Firebase Cloud Messaging) server-side: after writing a notification row, send a push message to the recipient's stored FCM tokens"
            note: 'Use FirebaseAdmin NuGet package. FCM handles Android, iOS (via APNs bridge), and web push in one API. Handle registration errors (token not found → remove from DB).'
          - id: F4-6
            task: 'Enable Supabase Realtime on Notification table so the in-app badge updates live without polling. RLS: users can only subscribe to their own notifications.'
          - id: F4-7
            task: "Add notification batching for comments — if a recipe gets 10+ comments in an hour, batch into a single notification ('Your recipe [name] got 12 new comments'). Check if a comment notification for the same recipe was sent in the last 30 minutes; if so, update it instead of creating a new one."
          - id: F4-8
            task: 'Add per-recipe notification mute — POST /recipes/{id}/mute-notifications, DELETE /recipes/{id}/mute-notifications. NotificationService checks mute status before creating notifications.'
            note: 'Useful when a recipe goes viral and the author is overwhelmed with notifications.'

      - group: 'Frontend'
        tasks:
          - id: F4-9
            task: 'Register push token on app launch using @capacitor/push-notifications (mobile only). Request permission, get FCM token, POST to /push-tokens. On web/Electron, request Web Notifications API permission for desktop alerts — Supabase Realtime handles in-app badge updates without a token. Delete FCM token on sign-out.'
          - id: F4-10
            task: 'Notification bell in the header — red badge with unread count, driven by Supabase Realtime subscription on the notification table'
          - id: F4-11
            task: 'Notifications screen — grouped by Today / Earlier. Each row shows icon for type, human-readable message, timestamp, and tapping navigates to the relevant recipe or friendship screen.'
          - id: F4-12
            task: 'Notification preferences screen — toggles per notification type (friend requests, friend accepted, new comments, suggestion updates). Store preferences in UserProfile as a JSONB column notification_preferences.'
          - id: F4-13
            task: 'Mute notifications button on recipe detail — accessible from overflow menu on own recipes. Toggle icon shows muted state.'

      - group: 'Notification wiring'
        note: 'Ensure NotificationService is called from each of these flows'
        tasks:
          - id: F4-14
            task: 'Wire friend_request notification — trigger when POST /friendships creates a pending request'
          - id: F4-15
            task: 'Wire friend_accepted notification — trigger when PATCH /friendships/{id} accepts a request'
          - id: F4-16
            task: 'Wire new_comment notification — trigger when POST /recipes/{id}/comments creates a comment (respect batching and mute)'
          - id: F4-17
            task: 'Wire suggestion_received, suggestion_accepted, suggestion_rejected notifications — trigger on suggestion submit and author response'

build_order:
  - step: 1
    feature: F1
    note: 'Social graph must exist before friends feed, comments, or suggestions make sense'
  - step: 2
    feature: F2
    note: 'Comments are independent of suggestions — can be built in parallel with F3'
  - step: 3
    feature: F3
    note: 'Suggestions can be built in parallel with F2'
  - step: 4
    feature: F4
    note: 'Notifications wire into all three features above — build last'

security_notes:
  - 'RLS policies for friends visibility must be reviewed manually — test with multiple user accounts'
  - 'Suggestion visibility is strictly author-only — test with multiple user accounts before shipping'
  - 'Push token storage must be scoped per user — never expose tokens across users'
  - 'Blocked users must be excluded from all social interactions — test blocking thoroughly'
  - 'Comment and suggestion content is user-generated — sanitize inputs and validate lengths'
```
