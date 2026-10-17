# Part 354: common failure-origin capture and cancellation cleanup

Pinned reference: OTP-29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`).

The separately accepted canonical attempt 4 passes **7,343 tests, eleven integration outcomes, two packages and twelve bounded child controls**. All 7,342 preceding registered names remain in order. The original successful 7,313-test baseline is preserved, with thirty permanent checks added. [Independent audit](root-delivery/actual-report.json), [raw command receipt](full/raw/full.exit.json), [test report](full/raw/artifacts/tests.json), [source composition](root-delivery/composition-plan.json).

Changes implement origin-time logical error capture and backtrace-depth handling, repair apply/RPC target frames, validate the default direct load_nif filename domain, and allow eligible canceled unbound continuations to move to an idle online scheduler. Binding/offline restrictions remain intact. A deterministic real-actor control fails before the cleanup fix and passes after it; both processes settle normally. The exact historical interleaving of the earlier port-stop failure was not captured.

Earlier failed compilations, focused/full attempts and environment-paired port observations remain under `history/`, `source-history/` and `root-proof/`. Attempt 3 passed every test but failed readiness because sixteen evidence records lacked required Scope fields; it is not a full pass. The correction adds those fields without weakening the validator. Original tested source bytes remain archived. The [publication whitespace proof](publication-whitespace/actual-proof.json) establishes that eleven blank-line whitespace removals preserve all 23 compiled fixture values; runtime/package bytes and native plans remain unchanged.

Readiness is generated from the actual accepted report after retaining the raw registry and restoring reviewed descriptive Limits. Registry presence remains **587 APIs across fourteen modules**: **16 fully compatible contracts, 30 known differences, 541 requiring complete verification**. These counts do not establish whole-module or performance parity. [Command-level status](../../API_COMPATIBILITY_INDEX.md).

Original OTP comparisons for the new portable cases and same-head CI are pending publication. Managed checks are not native ABI or original OTP execution. Valid default native loading, unsupported heap/GC/MQD options and further stack/runtime contracts remain unfinished.

Git text representation: [the explicit resource mapping](publication-source-representations.json) separates captured Windows package-source bytes from staged Git text. Three unchanged license files and OTP-PORTS have equal TextLF content but different raw newline representations. Runtime C# source guards remain raw-exact. No cross-platform package byte-identity claim is made.
