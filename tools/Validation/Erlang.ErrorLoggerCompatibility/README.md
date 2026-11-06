# Focused error_logger compatibility runner

Implementations are complete before validation. `tools/validate-error-logger-original.ps1` verifies the existing admitted OTP29.1.1 provider before and after capture, compiles the observers and unchanged upstream suite, and records their input/corpus identities. Supply a fresh `-Output` and optionally `-RunnerDll`; `-CaptureOnly` does not build/run .NET.

The runner accepts `repository corpus Managed|Beam [suite|case-substring]`. It loads real original Logger/gen_event/supervisor/application/file dependencies, starts the actual application controller, Logger and file services, and compares the same observer BEAM with the official ETF corpus. Backend ownership and the complete 29-export inventory are checked. Separate fresh-runtime registration checks verify that the legacy proc_lib registration preserves the selected error_logger image. No service replacement is installed.

The primary corpus has 143 observations. The upstream corpus calls 10 unchanged error_logger_SUITE test functions. `emulator/1` is excluded from accepted suite rows because it fails on the non-distributed official VM; its initial failure remains in the evidence. The distributed `other_node` and platform-specific `via_logger_process` require separate environments. Filesystem paths are relative, so concurrent backend runs must use separate working directories with an `artifacts` subdirectory.

Run `py -3 tools/Validation/audit-error-logger-source.py` from the repository root for baseline source identity and static surface coverage. API inventory does not imply semantic acceptance. No full regression or readiness refresh is part of this runner.
