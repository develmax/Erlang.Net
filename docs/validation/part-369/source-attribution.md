# Source and adaptation record

Authority: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. Read the sibling repository through pinned Git objects; its newer working checkout is not the baseline.

The binary module follows `lib/stdlib/src/binary.erl`, `erts/emulator/beam/erl_bif_binary.c` and the pinned binary match/append/storage code. The source proposals and independent original admission bind their exact blobs. Twenty-two actual native BIF declarations and nine Erlang wrappers are registered separately. Managed loops preserve reached validation, callback order, error helpers and source continuations; a byte trie preserves earliest, longest matching and nonoverlapping advancement.

Storage uses genuine owned byte arrays, immutable logical offsets/lengths, retained views and writer tokens. Match/serialization/sharing operations pin actual backing; no original capacity or reference identifier is injected. Compiled pattern handles are live typed resources owned by each runtime. Source-to-BEAM hex helper identities and error locations come from the independently built original binary module, rather than guessed source names.

Allocation optimization, physical heap addresses, absolute resource ceilings, native scheduling and speed parity are separate compiler/platform obligations. Observable backing state is evaluated against genuine own VM state; arbitrary numeric differences are not waived. Existing unsupported infrastructure remains visible in the broader component readiness. This part does not certify all compiler, port or NIF APIs.

Reuse existing .NET libraries, term/resource ownership and cooperative process infrastructure; add no package or production BEAM engine. Original OTP Linux execution in GitHub CI is development validation infrastructure; actual Main runs separately on Windows. Preserve upstream Apache-2.0 notices and existing third-party attribution. Add no source banners. The owner's `LICENSE.local.txt` remains uncommitted and unpublished.

Source attribution alone does not establish complete compatibility. Per-MFA branch arguments, independently admitted whole original comparisons, actual code/resource identity evidence, negative controls, dependency regressions and final-source validation are required together.
