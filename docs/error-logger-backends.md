# error_logger backends

The explicit C# alternative implements all 28 library exports of OTP29.1.1 `error_logger`; the 29th export, `warning_map/0`, uses the existing runtime-owned native primitive. The unchanged original Erlang source and BEAM remain under `src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/`.

Select `RuntimeModuleSelection.Managed("error_logger")` through `RuntimeLibrarySelection` / `ProcessRuntime.CreateAsync`, or select `RuntimeModuleSelection.Beam("error_logger", image, dependencies)` with the pinned image and the explicit dependency graph. The same selection is available through the library manifest and MSBuild library items. Selection is a startup operation; live state migration is not implemented. Ordinary kernel boot must still supply Logger and its supervisor.

The C# module calls the same MFA dependencies as upstream: `logger`, `gen_event`, `supervisor`, `logger_handler_watcher`, `application`, `lists`, `io_lib` and `logger_filters`. They may run from their original BEAM images on .NET. File/TTY handlers remain separate original modules; their actual closure includes `calendar`, `raw_file_io`, `file_io_server` and native file services. C# does not execute the original `error_logger` body or silently fall back to it. Production does not require external ERTS.

## Implemented behavior

- Standard/custom error, warning and information messages/reports, including malformed message arguments, undefined report types, metadata/domain selection and legacy `error` tagging.
- Logger handler callbacks, report callbacks of arity one/two, precedence, callback errors/crashes, warning remapping and emulator metadata.
- Supervisor-owned event-manager start/stop, off-heap mailbox option, handler addition/deletion/enumeration, real UTF-8 log files, TTY filters and format depth/term limiting.
- Canonical external function pointers and runtime-owned local callback recipes. Callbacks survive mailbox/ETS transfer and ETF restoration without retaining a source process or delegate environment. Their C# code identity is assembly-specific; no original BEAM fingerprint is fabricated.
- Source-aware local function/case errors and connected error stacks. Legacy partial registration skips a fully installed error_logger alternative.

## Validation boundary

[Focused evidence](validation/error-logger-port-20261105/README.md) records source/assembly/corpus identities, complete comparisons and earlier failures. The same observer BEAM executes on official OTP and both .NET alternatives. It compares values, full exception stacks and effects; it also runs selected unchanged upstream tests. The original suite's emulator testcase fails in the non-distributed official oracle and is retained as a separate unaccepted attempt.

API coverage and finite Windows observations do not certify universal 100% OTP equivalence. Distributed nodes, complete release startup manifests, all failure/resource/scheduling interleavings, other operating systems, original module-info/local-fun byte identity and performance remain separate acceptance work. Full regression and canonical readiness regeneration are deliberately excluded by the user's focused-check instruction.
