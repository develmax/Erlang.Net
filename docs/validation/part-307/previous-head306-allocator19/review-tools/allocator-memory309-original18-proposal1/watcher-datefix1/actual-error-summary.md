Root-reported first actual watcher failure after the already accepted single dispatch:

`watch-get-only.ps1` line26 attempted `DateTimeOffset.Parse(run.created_at)` after `Invoke-RestMethod` had returned a DateTime. Its culture-dependent string representation `10/15/2026 14:37:01` failed to parse under the active Russian culture. No snapshot was completed by that invocation.

This summary records the actual error information supplied by root; it is not a captured exception stack. The exact previous watcher source is retained as `watch-before.ps1`. The dispatch attempt/result files remain unchanged. Only the GET watcher timestamp conversion is repaired; no repeat POST is allowed.
