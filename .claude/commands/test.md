Run this project's NUnit test suite (`Live2DCardRogue.Tests.EditMode` / `Live2DCardRogue.Tests.PlayMode`) via the live Unity Editor, per `CLAUDE.md`'s "Running tests" section:

1. Confirm a connected Editor with `unity status` (state must be `ready`). If none is connected, say so and stop — do not fall back to the standalone `unity test` CLI command while the Editor is open, it will fail with `COMMAND_FAILED`.
2. Run:
   ```
   unity command run_tests --mode all --filter "Live2DCardRogue.Tests" --filter_type assembly --timeout 180 --format json
   ```
3. EditMode results come back in that response. If the message mentions PlayMode tests running asynchronously, poll `unity command test_status --format json` (a couple of seconds apart) until `status` is `completed`, then read its `summary`/`results`.
4. Report a concise pass/fail summary (total/passed/failed for each of EditMode and PlayMode). For any failure, show the test's `FullName` and `Message` (and ask before investigating further/fixing anything unless already asked to).

$ARGUMENTS
