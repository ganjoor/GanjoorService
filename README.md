# User panel Phase 0 + Phase 1 — file package

This contains the FULL, current content of every file touched by Phase 0
(shared components/foundations) and Phase 1 (nav grouping + dashboard)
combined. Since you already have Phase 0 applied, copying these over is
a no-op for the Phase-0-only files and just adds the Phase 1 changes.

Copy each file below into the same relative path under your
`GanjoorService/GanjooRazor/` folder, overwriting what's there.

## New files (Phase 0)
- wwwroot/css/user-panel.css
- wwwroot/js/user-panel.js
- Areas/User/Pages/Shared/_Pagination.cshtml
- Areas/User/Pages/Shared/_EmptyState.cshtml
- Areas/User/Pages/Shared/_Toasts.cshtml
- Areas/User/Pages/Shared/_ConfirmModal.cshtml

## Modified files (Phase 0 + Phase 1, combined into their final state)
- Pages/Shared/_UserPanelLayout.cshtml
  (Phase 0: loads user-panel.css/js, includes toast/modal hosts.
   Phase 1: navbar regrouped into dropdowns.)
- Areas/User/Pages/Notifications.cshtml
  (Phase 0 demo: uses the new pagination/empty-state partials,
   upConfirm/upToast instead of confirm()/alert().)
- Areas/User/Pages/Index.cshtml
  (Phase 1: dashboard greeting, quick-links grid, forms wrapped in cards.)

## After copying
1. Diff against what you have now (or just eyeball it — none of these
   files are huge) to make sure nothing local got clobbered.
2. Build.
3. Load `/User/Index` and `/User/Notifications` and click around:
   nav dropdowns, the confirm-modal on delete, toasts on notification
   actions, the quick-links grid.
4. Commit and push however you normally would.

No backend/API/.csproj changes in either phase — these are all under
`wwwroot/` or `Areas/User/Pages/` (Razor views + one stylesheet + one
script).
