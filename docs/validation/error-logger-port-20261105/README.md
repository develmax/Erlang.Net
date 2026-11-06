# Original local checkpoint — error_logger focused acceptance — 2026-11-05

Baseline: OTP29.1.1 / `ad05823719d77c8faee87348ea39513d4e2f99c5`. Implementation preceded validation. Original error_logger source and BEAM are retained; the static audit confirms exact original source bytes and29/29 explicit signatures (28 C# library functions and one shared native warning primitive).

## Final results

| Check | Managed | Original BEAM on .NET |
| --- | --- | --- |
| Complete values/errors/full-stack/effect corpus |143/143|143/143|
| Unchanged upstream error_logger_SUITE testcase functions |10/10|10/10|
| Selected backend survives legacy proc_lib registration |pass|pass|
| Callback ETS/ETF restoration and TTY default filters |pass|pass|
| Real log-file output and off-heap manager lifecycle |pass|pass|

[Machine acceptance](final/acceptance.json) checks all final source hashes against [the audit](final/source-audit.json), all recorded runtime assembly hashes against [the frozen runner](final/frozen-runner-identities.json), every expected observation and backend-registration preservation. The observer BEAM is identical on the official oracle and both .NET alternatives. Each backend runs with its own process working directory and artifacts directory; reported path values remain identical.

[Managed primary](final/Managed-report.json), [BEAM primary](final/Beam-report.json), [Managed upstream](final/Managed-suite-report.json), [BEAM upstream](final/Beam-suite-report.json), [official provider/source provenance](final/provenance.json), [complete build](final/final-build.log).

Final build:0 warnings/errors, no source exclusions or build adapters. Earlier experimental exclusions and compile errors are historical and do not establish acceptance. The final ordinary build uses the shared source tree, isolated output directories and `-m:1`. Source layout was applied with the SDK/Roslyn repository tool, then compiled and replayed. Full regression, validate.ps1 and canonical readiness regeneration were deliberately not run.

## Commands

From the repository root:

```powershell
pwsh -NoProfile -File tools/validate-error-logger-original.ps1 -Output artifacts/error-logger-20261105-6 -CaptureOnly
dotnet build tools/Validation/Erlang.ErrorLoggerCompatibility -m:1 --nologo -p:UseArtifactsOutput=true -p:ArtifactsPath=D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/error-logger-build/output
py -3 tools/Validation/audit-error-logger-source.py
```

Copy the resulting runner/dependencies into the private frozen runner directory before replay. The final commands use absolute repository/corpus/runner paths. For each backend run `dotnet <runner>/Erlang.ErrorLoggerCompatibility.dll <repository> <corpus> Managed|Beam` and repeat with `suite`, from `artifacts/error-logger-managed-work` or `artifacts/error-logger-beam-work`. Both working directories contain an `artifacts` subdirectory. These are the final source-bound outputs copied into `final/`; no local package is published.

## Preserved failures and scope

- [Raw host delegates in ETS](history/raw-delegate-ets-failure/): the original attempt killed Logger when TTY filters were stored. Canonical external functions and managed stored-function recipes fix this; successful callback_storage/tty_default rows also cover ETF restoration and filter removal.
- [Legacy tags and frames](history/legacy-tags-and-error-frames/):120/140 before correcting error_msg's legacy `error` tag and string_p/error_info frames. Non-tail source frames and nested invalid depth have a further permanent contract.
- [File dependencies](history/file-dependency-diagnosis/): raw_file_io was initially omitted. The subsequent142-row attempt omitted calendar: its file handler disappeared on the first actual write. Both are real original dependency images in the final runner; no service stub is substituted.
- [Earlier142-row acceptance](history/accepted-142-before-extra-frame-contract/): preserved separately from the final143-row corpus and assembly identity. First141/142 captures and later full142/142 captures are distinguished by their filenames.
- [Official emulator testcase failure](history/official-emulator-failure/): the unmodified suite expects an error string including a node name, while the official non-distributed VM emits its no-node variant. This failure is not counted as an accepted upstream testcase. The primary corpus independently verifies the emulator metadata path. Distributed other_node and platform-specific via_logger_process require separate environments.
- [Earlier builds](history/builds/): retain concurrent draft compiler errors, output collisions, and the failed redirected-output build. They are superseded only by the complete final build receipt.

This is complete implementation/API coverage and finite Windows acceptance, not universal100% OTP certification. Full distribution, all resource/failure/scheduling interleavings, other platforms, complete release-manifest startup and original module-info/local-fun byte identity remain outside this receipt. C# callbacks have their actual assembly-specific identity. No performance claim or live state migration is made.

The working tree already contained interleaved library/runtime changes. No commit, push, PR, merge or hosted-CI success is claimed; publication must isolate this scope without bundling unrelated work.

[Publication replay on main](publication/README.md).
