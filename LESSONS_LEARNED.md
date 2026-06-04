# Lessons Learned

## Unity Batch Mode

- Unity licensing can fail from sandboxed command execution because the Editor cannot connect to the `Unity.Licensing.Client` IPC channel.
- When running Unity command-line checks from Codex, use `sandbox_permissions: require_escalated` for the Unity command so licensing IPC works normally.
- If Unity logs `Unsupported protocol version '1.18.1'`, check for Unity Hub's older `UnityLicensingClient_V1` on the generic `LicenseClient-ubik` pipe.
- Keep Unity Hub closed during batch test/build runs when possible, because Hub can respawn the older V1 licensing client and race the Editor-bundled Unity 6.4 licensing client.
- If a sandboxed Unity command launches `Unity.Licensing.Client` but then logs `Timed-out after 60.00s, waiting for channel`, rerun the Unity command unsandboxed/escalated before spending time on project or license-file debugging.
- Unity cannot batch-open a project path that is already open in the Unity Editor. If the Editor is open, run batch validation against a temporary copy of the project instead.
- For this project, Unity Test Runner did not run when `-quit` was passed. Omit `-quit`; the Test Runner exits the Editor itself when the run finishes.
- A working EditMode command is:

```bash
/Applications/Unity/Hub/Editor/6000.4.9f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /private/tmp/4xgame-batch-probe \
  -runTests \
  -testPlatform editmode \
  -testResults /private/tmp/4xgame-batch-probe/Logs/editmode-results.xml \
  -logFile /private/tmp/4xgame-batch-probe/Logs/editmode-test.log
```

- Before running that command, sync the project into the temporary copy while excluding generated folders:

```bash
rsync -a --delete \
  --exclude Library \
  --exclude Logs \
  --exclude Temp \
  --exclude obj \
  --exclude .git \
  /Users/ubik/dev/4xgame/ \
  /private/tmp/4xgame-batch-probe/
```

## Unity Test Discovery

- Command-line EditMode tests should live in a test assembly definition. Plain test scripts may compile in the Editor but still not produce command-line test results.
- Runtime code should also live in an assembly definition so the test assembly can reference it explicitly.
- The current assembly split is:
  - `LaneSurvivor.Runtime` for gameplay, data, and UI scripts.
  - `LaneSurvivor.Editor` for editor-only scene builder tools.
  - `LaneSurvivor.Tests.EditMode` for EditMode tests.
- If Unity exits with code `0` but does not create a results XML file, inspect the log for actual `Running tests for ExecutionSettings` and `Saving results to:` lines. A clean batch open/quit is not the same as a test run.

## Autoreview

- The `autoreview` skill in the steipete skills checkout is a symlink to the OpenClaw shared skills repo:

```text
skills/autoreview -> ../../agent-skills/skills/autoreview
```

- The sibling repo must exist at:

```text
/Users/ubik/Documents/Codex/2026-05-18/agent-skills
```

- `autoreview --engine codex` sends the review bundle to the Codex/OpenAI service. Ask for explicit approval before running it on private repo content.
- Review each already-committed change with `--mode commit --commit <sha>`, and review new local fixes with `--mode local`.
- Treat findings as advisory. Verify the real code path before changing anything.
- Rerun autoreview after review-triggered fixes until it reports no accepted/actionable findings.

## Phase 1 Gameplay

- Lane-based gameplay needs lane checks everywhere lateral placement matters. Gates, zombie breaches, and shooter targeting must all use the same lane tolerance concept.
- UI buttons and global touch gestures can double-handle the same tap. Suppress raw gesture handling only over the lane button rectangles, not over every UI element, or HUD text creates dead zones.
- Floating feedback makes the prototype much easier to understand without adding production art. Short labels such as `MISS`, `DODGED`, gate values, and squad losses clarify cause and effect.
- Keep the first prototype data-driven but small. A tiny `LevelDefinition` with lane positions, gates, zombies, and tuning values is enough for useful iteration.
- Placeholder geometry is fine for Phase 1, but visual distinction matters: colored gates, lane markers, and a visible finish line are cheap and improve playability immediately.

## Visual Bug Verification

- After Unity gameplay, camera, rendering, scene layout, or visual/UI code changes, always run a full visual gameplay recording pass on the iOS Simulator before reporting the change as done.
- For visual bugs, do not claim a fix from a single screenshot, a short GIF, or a convenient time slice.
- Produce full-run proof before calling the bug fixed:
  - A full video recording from before the relevant interaction through the end of the run.
  - A whole-run contact sheet sampled across the full recording.
  - Dense frame windows around the exact reported failure moments.
  - A short pass/fail statement for each reported symptom.
- When the user reports a timing-specific visual issue, verify the frame immediately before, during, and immediately after the reported moment.
- If a contact sheet reveals a different readability issue while fixing the original bug, treat it as part of the same visual polish pass instead of calling the original issue done.
- For simulator verification, confirm the deployed app was rebuilt from the current export before recording, and name the final video/contact-sheet paths in the report.

## Remote Work

- If the Mac is locked or asleep, app-state and GUI inspection may fail even when shell commands still work.
- Keep the Mac awake during long remote Unity work. A useful local command is:

```bash
caffeinate -dimsu
```

- `caffeinate -dimsu` prevents display sleep, idle sleep, disk sleep, and system sleep while the command is running.
- For remote validation, prefer command-line proof over GUI state when the screen may be locked.

## Git And Workflow

- The Git working tree may need escalated filesystem access for `git add` and `git commit` because sandboxed writes to `.git/index.lock` can fail.
- Networked Git commands such as `git push` need escalated execution when sandbox DNS/network access is restricted.
- Commit test-infrastructure fixes separately from gameplay polish so regressions are easier to bisect.
