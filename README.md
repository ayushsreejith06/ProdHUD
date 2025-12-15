# ProdHUD

ProdHUD is a translucent, pass-through desktop heads-up display to keep your workflow moving without getting in your way. Hold the `Fn` key to temporarily make the windows interactive and opaque; release it to return to pass-through.

## Feature overview
- Overlay HUD with default pass-through and `Fn`-based interaction toggle.
- Playback controls: back track, play/pause, skip track via system media controls.
- Pomodoro timer: current task field, countdown, 10-segment XP/progress bar, dynamic buttons (start → pause → stop/continue), and configurable work/break lengths.
- Task manager: nested folders (up to 3 levels), checklist tasks with quick-add on Enter, and a fallback “General Task” used by the pomodoro when nothing is selected.

## Interaction model
- **Pass-through by default:** HUD does not intercept mouse clicks; the desktop beneath stays usable.
- **Hold `Fn` to interact:** While held, windows become opaque and accept mouse/keyboard. Releasing `Fn` restores pass-through and translucency.
- **Keyboard flow:** Enter commits quick-add task; media keys or on-screen controls manage playback; pomodoro buttons cycle Start ↔ Pause and Stop/Continue.

## Windows and behaviors
- **Playback controls**
  - Buttons: Back, Play/Pause, Skip.
  - Expected to drive system media session (e.g., OS-level media controls).
  - Stays minimal to avoid occluding the screen.

- **Pomodoro timer**
  - Current task text field (populated from selected task; falls back to General Task).
  - Countdown timer with stateful controls: Start → Pause → Stop/Continue.
  - 10-segment XP/progress bar; the active segment glows for the current interval.
  - Settings view includes:
    - Work (pomodoro) length
    - Short break length
    - Long break length
    - Number of pomodoros before long break
    - Cancel and Confirm actions
  - Goal: stay operable while pass-through is active, relying mostly on keyboard.

- **Task manager**
  - Checklist UI with a quick-add row (checkbox + text input). Enter saves a non-empty task.
  - New folder button creates a folder under the active folder/context (max depth: 3).
  - Selecting a task feeds the pomodoro “current task”; if none selected, use General Task.
  - Centralized view to keep navigation shallow and prevent deep nesting.

## Installation and setup
Implementation details are still being finalized. A likely path will be:
1) Clone the repo.
2) Install dependencies (TBD: choose runtime/framework).
3) Run the dev build (TBD: script/name).
4) Package for distribution (TBD: target OS builds).

When the stack is locked, this section will list exact commands and prerequisites.

## Usage notes
- Keep `Fn` pressed only when you need to interact; otherwise enjoy full pass-through.
- Pair the task manager with the pomodoro to keep context visible without switching apps.
- For media, the HUD should mirror hardware media keys—use whichever is closer.

## Roadmap (draft)
- Finalize tech stack and build pipeline.
- Wire playback to OS media sessions.
- Implement pomodoro state machine and persistence of settings.
- Implement task storage (local persistence) and folder navigation.
- Polish visuals (opacity transitions, glow states) and accessibility.

## Contributing
Draft contribution flow; feel free to open issues or PRs as the UX solidifies. Please include clear repro steps for bugs and note platform/OS details.

## Project status
This repository is in active design/iteration. Implementation details, builds, and platform specifics will be added as features land.
