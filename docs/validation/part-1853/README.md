# Part 1853 — Preserve the matched call-result projection in X registers

The compiler orders independent outgoing assignments while preserving
reads of the forwarded result. It clears a saved slot only after the
single-use proof covers its reachable lifetime. The result and checked
nested tuple remain in the live X prefix through the heap check.
The terminal projection and result tuple then reuse the result register.
The repeated failure-arm use keeps its original slot.

Pinned OTP SSA code-generation listings establish the matching
instruction order before trimming. The patch reuses the C# stack-use
proof, compact operand encoding and initialized frame handling. No
dependency or public MFA was added.

The independent nine-module sample increases from 6 Equal to 7 Equal.
It has 2 Different, 0 Unsupported and 0 SourceRejected with no losses.
The exact 4,012-byte Windows release file is megaco_tcp_sup.beam.
Both compared images have SHA-256
763AC8A08073103F8C111D36FDA9641741F8416F96577C2CED5BDED5F3D05866.
Pinned ERTS confirms loading, initialization, successful and missing
lookups, and malformed nested-payload outcomes. The permanent regression
detects the prior register and root layout.

The integrated candidate passes all 35 focused cases. The exact
source archive is separate from its generated outputs. The linked
release sample and full recompilation follow independently.

[Source, compiler and test hashes](source-freeze.json) ·
[Focused tests](focused.json) ·
[Isolated release proof](isolated-megaco-README.md) ·
[Isolated identities](isolated-megaco-identity.json).

## Integrated release pilot

The 36-module compiler selection takes 22.89 seconds, reports
14 Equal, 22 Different, 0 Unsupported and 0 SourceRejected, and
confirms the new megaco_tcp_sup file without any lost matches.
All 21 changed files prepare in pinned ERTS. The diagnostic
finds 54 changed normalized functions, 16 newly equal and none
lost. Eleven raw sections reach equality with no losses.

The exact compiler and corpus application are frozen. The
uncached comparison started on four disjoint groups weighted
with the measured Part 1845 module runtimes.

[Pilot raw report](pilot.json) · [Matched file status](pilot-status-changes.json) ·
[Normalized functions](pilot-summary.terms) · [Raw sections](pilot-section-delta.json) ·
[Load results](pilot-loader.terms) · [Uncached launch identity](full-launch.json).

## Full uncached Windows comparison

All 1,366 release modules were freshly compiled in four disjoint
groups. The result is 407 Equal, 959 Different, 0 Unsupported and
0 SourceRejected. megaco_tcp_sup.beam is the only new whole-file
match. All previous 406 matches remain. The full run takes 846.85
seconds. The prior full run took 602.52 seconds, an observed
difference of 244.34 seconds. The current run overlaps the standard
solution build and other focused development. The timing does not
establish a controlled compiler throughput comparison.

The integrated output changes 471 files. Sixty-four raw sections
gain equality, including 55 Dbgi and three LitT chunks. No raw
section loses equality. The remaining 959 files all still differ in
Code. The raw full section inventory provides the per-module
intersection with Type, Line and constant table differences.

Pinned ERTS prepares 468 of the changed files. Three on-load
modules use ordinary loading. SHA-256 execution for crypto,
certificate-cache operations for pubkey_os_cacerts and the zstd
compression roundtrip all agree with the release and prior package.
The input verification confirms 3,016 release files, 265 supplemental
inputs, 89 generated ASN.1 files and the clean pinned upstream checkout.
The build profile collector verifies 1,366 equivalent module inputs
against the linked preceding package. Standard solution build passes.

[Full corpus](full-otp-corpus.json) · [Full timings](full-timing.json) ·
[Timing delta](full-timing-comparison.json) · [File change](full-status-changes.json) ·
[Raw section delta](full-section-delta.json) · [Remaining sections](full-residual-sections.json) ·
[Native admission](full-loader.terms) · [Verified inputs](input-verification.json) ·
[Resolved build profiles](windows-build-profiles.json.gz).

The normalized comparison covers 1,207 changed function bodies.
Fifty-seven reach exact normalized equality and zero lose it. Eighty-eight
comparable frame profiles move closer to OTP and none move further away.
The Tooling gate passes in 362.29 seconds. The full local runtime
catalog completes in 2,908.46 seconds with 9,783 passing and one
failing case. The original Jaro resource-contract case returns
the same timeout and application stack as in the isolated
Part 1839 baseline. Canonical readiness retains its previously
passed input.

[Function summary](full-normalized-summary.terms) ·
[Per-function delta](full-normalized-delta.terms) ·
[Tooling verification](tooling-receipt.json).

[Ranked residual causes](remaining-causes.md) ·
[Controlled timing proposal](timing-audit.md) ·
[Runtime gate identity](repository-full-start.json) ·
[Runtime receipt](repository-full-receipt.json) ·
[Full runtime report](repository-full.json.gz) ·
[Raw runtime stdout](repository-full.json.stdout.log.gz) ·
[Raw runtime stderr](repository-full.json.stderr.log.gz) ·
[Recorded failure](runtime-failures.json) ·
[Raw hashes and baseline comparison](runtime-evidence.json).
