# Supplemental recursive cleanup comparison

Two bounded, guest-observable cases close a specific gap left by the part329 ordinary corpus: external kill while named or anonymous recursion has 256 pending `try ... after` frames. Both wait for the leaf's ready message, send kill, require the monitored `killed` result and observe no cleanup message for 100 milliseconds. Expected outcome is `{ok,{killed,no_cleanup}}`.

The existing host controls independently establish CLR source-image leases and actual Unwound ordering. These supplemental sources do not claim such internal objects exist in OTP, and do not compare physical stack, allocator footprint or performance. The finite quiet window is recorded explicitly; this is not a proof over every interleaving.

The collector directly references approved existing subject DLLs; it has no project reference that rebuilds or changes the subject. Run original mode only with the OTP-29.1.1 source-built runtime. The driver verifies the exact original source commit and installed OTP_VERSION before original execution, binds source and DLL identities, owns deadlines and writes fresh process receipts.

Local inspection found no `erl` in PATH, no `../artifacts/oracle/builds`, and no installed WSL distribution. Source inspection is not original execution. The first collector build failed with CS4007 because an array comparison created a Span before an awaited read. Revision2 awaits the read into a separate byte array before comparison. This was a collector compilation failure, not a guest runtime failure. The corrected collector built with zero warnings and both SDK cases passed against the existing part329 focus5 subject binaries; original OTP execution remains pending.

This tooling is published under `tools/Erlang.RecursiveCleanupObservations`; the standalone workflow lives at `.github/workflows/recursive-cleanup.yml`. The proposal copy in this directory binds the identical workflow bytes in the source freeze. The workflow checks out collector tooling from its own workflow commit and the exact published subject separately; it compares the latter's compiler/runtime DLLs against newly built pinned OTP. Its default subject is the published part329 commit supplied by the parent reviewer.

The formatted source files are bound by exact UTF-8 bytes in `source-freeze.json`. The project filename matches its tool directory while AssemblyName remains SupplementalCleanup for the reviewed driver. A local SDK invocation uses the existing focus5 approved binary directory and passes its candidate root as subject-root without verify-subject-git; the subject SHA remains the published329 identity and exact compiler/runtime DLL correspondence must be checked by the parent reviewer. CI uses verify-subject-git.

Actual pre-promotion SDK receipts (original reports retained under artifacts, not duplicated here):

- `named`: expected and actual `{ok,{killed,no_cleanup}}`; source SHA256 `DAFD7307B1FA93F54B7519DCD933BBF944FFC25551A9A04D314A7CD6BC9577E4`; report SHA256 `DE65624755336F945614B707608A5DE15CF5EB02835593974484BD554A7CCFFA`.
- `anonymous`: expected and actual `{ok,{killed,no_cleanup}}`; source SHA256 `67560C058C98A0C42EFBAA8D2BB680F8077AA5F682F49CDE7888419E8E1F623D`; report SHA256 `A987C3B5FF093A56ABB824BF5C8BAAB4FB8352689C1C4536FCAC616DFE882D6E`.
