# 134 Dialog fixes from the manual checklist (I-033..I-039)
- I-033 Modal dialogs take keyboard focus when shown (first field focused); while a modal is open the main window ignores keys; Enter = OK, Esc = Cancel reliably.
- I-034 Room dialog Objects box: syntax hint must not shift layout (overlay/popup or reserved space); Esc closes the hint only when it is open; remove Tab-to-indent (decided by user) so Tab moves focus.
- I-035 App Settings: tab headers reachable by keyboard; Tab order runs top-to-bottom and wraps sensibly (no local Cycle traps).
- I-036 Colour chooser: nothing clipped, expanded/HSV view wide enough, OK and Cancel buttons.
- I-037 Remove the "(not active yet)" hints — all six settings are wired.
- I-038 Every save picker confirms overwrite (`ShowOverwritePrompt = true`); if the platform picker cannot, ask in-app.
- I-039 Image export: relabel "Current zoom" so it says it scales the whole map by the current zoom.
