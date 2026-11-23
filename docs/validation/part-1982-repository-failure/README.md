# Part 1982 repository validation attempt

Local `tools/validate.ps1 -Part All -TestRuntimeVersion 10.0.11` exited 1 after 2,987.76 seconds. The managed harness observed 10,374 passes and two failures. The retained raw reports include both full exceptions. The older complete repository checkpoint remains Part 1978.

- `HOST/library411/original-code-seven-interpreter-capability-profile` failed at the post-disposal dispatcher assertion in InterpreterCode411Checks.cs line 106.
- `HOST debug logger rejects stopping/retiring recipients and resets dead explicit target` failed at the held native callback liveness assertion in DebugLoggerLiveness420Checks.cs line 47.

The full uncached compiler result and its normalized/native evidence remain in [Part 1982](../part-1982/README.md). The mandatory integrated catalog, readiness and subsequent package checks did not run in this attempt. Both failures remain open pending independent diagnosis.
