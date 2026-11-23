# Part 1839 — Restore selector and tail argument placement

The linked candidate retains lexical preparation of closed values only
when a direct local tail preserves its incoming argument positions and
appends at least two closed arguments. Reordered inputs keep the ordinary
call scheduling. A proof that a private parameter is a specific atom can
replace it in a direct selector argument when the selector sends it to a
different register. Same-register arguments keep the existing alias.

The 15-module selection retained 5 Equal and 10 Different, with zero
Unsupported, SourceRejected, new byte matches and lost byte matches.
ERTS prepared all six changed images. Normalized instructions regained
all ten matches lost in Part 1837. The two ASN.1 length functions also
gained normalized equality. The 17 named regression cases passed on
the final formatted compiler. The first focused retake failed because
its assertion constrained an unrelated frame width. The regression
now compares the selector's argument, line and call instructions.

The formatting tool passed on the changed C# paths. The intermediate
compiler build into an empty isolated directory failed because the
assembly-dependency writer could not find Erlang.Runtime.dll. The rebuilt
isolated directory was hydrated from the preceding dependency payload
and compiled successfully. The uncached full comparison uses that
final compiler and four disjoint partitions of the release.

[Scoped corpus](cohort.json) · [Scoped timing](cohort.json.timings.json) ·
[Changed images](cohort-delta.json) · [ERTS preparation](cohort-loader.terms) ·
[Normalized functions](cohort-normalized.terms) · [Final focused cases](final-focused.json) ·
[Initial assertion failure](focused.log) · [Source identity](source-overlay.json) ·
[Full launch identity](full-launcher-start.json).

## Uncached Windows release

The candidate compiled all 1,366 distinct Windows OTP 29.1.1 paths in
545.37 seconds. It retained 404 Equal and 962 Different, with zero
Unsupported and zero SourceRejected. No whole-image match was gained
or lost. All seven changed images passed ERTS preparation. Twelve
normalized functions gained equality. Ten restore Part 1837 losses,
and two match through the joined ASN.1 length proof. The Type sections
of DSS and PKCS-3 became byte-identical. No previously equal section
among the seven images regressed. Two changed snmpa_trap functions
remain unequal.

The solution build first exhausted drive C:. The retake used isolated
temporary files on D: and built with zero warnings and errors. Tooling
passed. The exact compiler and test assembly passed the five case
provider preflight. The initial full regression resolved fixtures against the primary checkout
when it launched from the D: payload. It was stopped after two fixture
provider errors. The same assembly launched through the current worktree
junction passed the direct provider test and restarted the 9,767-case
regression. Both launch receipts and the initial output are retained. Readiness retains the previous accepted evidence.

[Full corpus](full-otp-corpus.json) · [Elapsed times](full-timing.json) ·
[Status changes](full-status-changes.json) · [Image hashes](full-changed-images.json) ·
[ERTS preparation](full-loader.terms) · [Section gains](full-section-delta.json) ·
[Normalized functions](full-normalized-delta.terms) ·
[Original build failure](standard-build.log) · [Build retake](standard-build-retake.log) ·
[Tooling](tooling.log) · [Runtime launch](exact-full-start.json).


[Initial regression output](exact-full.json.stdout.log) ·
[Final-path preflight](repository-provider-preflight.json) ·
[Corrected regression launch](repository-full-start.json).

Against the completed Part 1833 comparison, the final Part 1839
candidate gains 66 normalized functions with no losses. The original
diagnostic ran out of cache storage on C:. It was retaken with a
fresh disassembly cache on D:. The corpus compilation reports and
exact image comparison are unchanged.

[Cumulative terms](against-part-1833-normalized.terms.gz) ·
[Cumulative counts](against-part-1833-normalized-summary.json).

## Full local regression

The corrected run passed all 9,767 distinct cases in 2,659.41 seconds.
It used the same compiler and test hashes as the initial attempt. The
initial run and its two wrong-checkout fixture errors remain retained.
The stderr from the successful run includes guest process diagnostics
emitted by the tested observation cases.

[Passed report](repository-full.json.gz) · [Raw stdout](repository-full.json.stdout.log.gz) ·
[Raw stderr](repository-full.json.stderr.log.gz) · [Timing and exit](repository-full-receipt.json).

The readiness CLI used the immutable 9,687-case comparison snapshot.
It records 733 directly tested registered MFAs across 21 modules and
zero registry change. The complete per-module acceptance and remaining
scope are in the generated readiness table.

[Readiness](../../MODULE_READINESS.md) · [Receipt](readiness-receipt.json).
