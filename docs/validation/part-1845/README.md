# Part 1845 — Hoist shared tuple payloads and retain SNMP header provenance

An adjacent call result now shares the tuple payload across a typed
guarded alias and a nested tuple projection. The check order, selected
value and error input follow pinned OTP. The Windows SNMP corpus
profiles also resolve snmp_debug.hrl through the recorded -I../misc
search directory. The app, manager and agent makefiles supply this
option. Existing explicit recorded paths and a same-directory
include retain their values.

In the 43-module sample, 7 files are Equal and 36 Different. There
are zero Unsupported and SourceRejected results. Both ssl_dh_groups
and snmp_app_sup are newly byte-identical compared with Part 1839.
ERTS prepares all 33 changed files. Sixteen functions gain normalized
equality without a loss in this sample. The same two SNMP functions
agree with OTP on 14 isolated public-call outcomes. All 25 focused
tests pass. The dedicated header check has 24 matching file-attribute
observations, including the same-directory control. It recorded 21
mismatches on the preceding full baseline.

The uncached 1,366-module candidate uses four independent groups
balanced with the Part 1839 per-module timings. The module selection,
compiler identity and tool identity are retained.

[Corpus](cohort.json) · [Timings](cohort.json.timings.json) ·
[Normalized functions](normalized-delta.terms) · [Loader](loader.terms) ·
[Header regression](snmp-header-proof.json) · [SNMP execution](isolated-candidate-execution.log) ·
[Focused tests](focused.json) · [Full launch](full-launcher-start.json).

## Full uncached Windows comparison

The full candidate compiled 1,366 distinct modules in 602.52 seconds.
It reports 406 Equal, 960 Different, 0 Unsupported and 0 SourceRejected.
The new exact files are ssl_dh_groups and snmp_app_sup. No former exact
image was lost. Twenty-nine normalized functions gain equality.
Twenty-two additional sections reach byte equality and none regress.

Pinned ERTS prepares 349 of the 351 changed images in batch. The other
two are on-load modules, crypto and pubkey_os_cacerts. Both load as
ordinary code. Their SHA-256 results and certificate-cache operations
match the prior and release images. The input check verifies 3,016
release files, 265 supplemental inputs and 89 generated ASN.1 files.
It also verifies the exact OTP tag and clean upstream checkout.

The standard solution build passes. The first Tooling run exhausted
drive C: while copying Roslyn resources. It keeps its original
failure. The retake uses relocated build directories and passes in
189.58 seconds. The full .NET regression used the frozen source and
the repository provider path.

[Full report](full-otp-corpus.json) · [Time](full-timing.json) ·
[New matches](full-status-changes.json) · [Image hashes](full-changed-images.json) ·
[ERTS loader](full-loader.terms) · [Function deltas](full-normalized-delta.terms) ·
[Section deltas](full-section-delta.json) · [Inputs](full-input-verification.json) ·
[Resolved profile snapshot](full-windows-build-profiles.json.gz).

## Full local .NET regression

The serial exact-source run completed all 9,774 cases in 4,410.97
seconds. It passed 9,768 and failed six. Five cases reported a
timeout at the actor or test runner deadline. The HAMT assertion
retains a missing stacks route caused by its actor timeout.
The raw failures are part of this completed run.

The preceding completed package passed the same runtime cases.
Eight frozen runtime libraries and 55 resources relevant to the
first five recorded failures are byte-identical across both
packages. An isolated selected-case comparison is tracked separately.
The failed complete run does not update readiness.

[Test JSON](repository-full.json.gz) · [Standard output](repository-full.json.stdout.log.gz) ·
[Standard error](repository-full.json.stderr.log.gz) · [Receipt](repository-full-receipt.json) ·
[Failed cases](runtime-failures.json) · [HAMT raw timeout](hamt-deadline-failure.json) ·
[Runtime inputs](runtime-input-audit.json).

The isolated comparison executes the six case names on both frozen
packages with no competing .NET process when the experiment begins.
The two sets have identical outcomes. HAMT, dirty debug and original
catch pass. Original Jaro, maps/proplists and the alternating lifecycle
case still time out. This establishes that the remaining observed
failures also affect the previous compiler payload under this setup.
The full gate and readiness remain separate.

[Case selection](isolated-deadline-selection.json) ·
[Prior results](isolated-part1839-cases.json) ·
[Part 1845 results](isolated-part1845-cases.json).
