# string port checkpoint — 2026-11-03

**Acceptance status: incomplete.** [Machine-readable status](status.json),
[work journal](journal.md), and [module scope](../../string-backends.md).

| Observation | Manual C# string | Original string on .NET |
| --- | ---: | ---: |
| Actual original exports exercised | 71/71 | 71/71 |
| Returned values matched | 4,193/4,193 | 4,193/4,193 |
| Exception class/reason matched | 1,033/1,033 | 1,033/1,033 |
| Complete exception stacks matched | 3/1,033 | 1,020/1,033 |
| Binary-backing observations matched | 13/13 | 13/13 |

Both complete comparisons correctly fail the strict acceptance gate because of
the stack differences. No complete-module or full-MFA promotion is made. Existing
default registration remains unchanged. The current C# alternative is explicit
`StringModule.Register`, not a new default or startup-selector option.

## Evidence

- `final/`: gzip-compressed original/actual ETF, observer BEAM, complete reports,
  command logs, and provider/runner provenance. Decompression recovers exact bytes;
  no exception frames or inputs have been filtered out.
- `prior/`: preserved initial failures and the subsequent expanded comparison
  before the final binary-sharing correction and backing observations.
- `build/`: successful normal test-project build, five retained regression groups,
  isolated build recipes/logs, 71-export smoke, and the unrelated stdlib source
  archive at `07da777f6`. The isolated build's initial three warnings and corrected
  zero-warning build remain separate records.
- `source/`: exact string source overlay, validator, fixture and entry-point inputs
  for this checkpoint. The ordinary Erlang module remains unchanged in its owning
  original-library project.
- `runtime/frozen-validation-runtime.zip`: the actual isolated validator and
  runtime composition used for the final comparison. This is a local validation
  artifact, not a public package or a current combined-source build claim.
- `inventory.json`: SHA-256 and byte length of every evidence file except the
  inventory itself.

The shared checkout entered a separate integration merge with runtime/docs
conflicts. The frozen composition uses retained runtime assemblies, an unrelated
stdlib snapshot from `07da777f6`, and the current manual string overlay. It does
not establish that the incoming combined runtime builds or passes these tests.
The retained core DLL hashes, not a guessed source revision, identify that runtime.

## Re-run

On the admitted development environment, extract the runtime archive under a new
ignored artifacts directory, then use the maintained entry point:

```powershell
pwsh -NoProfile -File tools/validate-string-original.ps1 `
  -RunnerDll artifacts/string-replay/runtime/Erlang.StringCompatibility.dll `
  -Output artifacts/string-replay/results
```

The output directory must not already exist. This reacquires independent official
OTP outcomes, verifies the selected provider closure before/after execution, runs
both alternatives, and returns failure while complete-stack differences remain.
The default entry point builds the current source instead of selecting a retained
validator. Resolve the unrelated integration before claiming that build gate.

## Remaining acceptance

Implement source-equivalent helper/native exception frames, integrate explicit
startup selection, and run combined-source/canonical regression/readiness and
broader upstream/Unicode/resource/scheduler/backtrace-depth/platform checks.
The user's 100% compatibility target is not closed by this checkpoint.
