# Part 1855 Windows OTP parity evidence

The frozen linked compiler extends the Part 1854 predecessor with
two source-derived decisions. The call-continuation lowering builds
a map field from the preceding call result and keeps the selector
and nested checked tuple live through heap allocation. The transient
frame pass keeps an expired highest Y slot in a dead X register
through a run of passive instructions before the first ordinary
call. The new path stops at intervening heap checks. The source
checkout, generator profiles and public MFA inventory retain their
pinned inputs and scope.

All 38 focused cases pass on the frozen payload. The first two
focused runs retain the original failures. Pinned beam_ssa_codegen
listings confirm that an initial combined allocation uses one stack
slot for the only value surviving the call and that the terminal
tuple and list release that one slot. A two-call case with no
value surviving its first call uses a zero-slot frame and a
zero-slot final external call, as in OTP. The three original
assertions now check those oracle operands. The first fixture
preserves the tested values after a forced collection. The
native evaluator's initial malformed map pattern is also retained.

The 35-image linked corpus pilot took 211.14 seconds with one worker.
It has 11 Equal, 24 Different, 0 Unsupported and 0 SourceRejected.
The only new match is the 3,108-byte ssh_channel_sup.beam. It has the
official SHA-256 730924FBF4A6D95E9318756115EC575F032B31D6E041FDAE8735913E07A851C6.
No previously equal file was lost. Twenty-two images changed and all
prepare in pinned ERTS. All eight tested role/reply observations and the
complete child specification checks agree across the previous, release
and current images. The first attempt at running the ordinary load
observer reused a probe for a different module. Its original error
is preserved and the corrected SSH observer results are linked below.

Four of 479 changed function bodies gain normalized instruction
equality with zero losses. Comparable frames shrink in 25 functions
and grow in none. Three whole sections gain equality, Attr, Code and
Line for ssh_channel_sup.

The uncached full comparison completes in 646.91 seconds across four
disjoint partitions. It confirms 408 Equal, 958 Different,
0 Unsupported and 0 SourceRejected, with one new exact image and no
losses. All 412 changed images prepare or load in pinned ERTS.
The elapsed time is 23.61 percent lower than the preceding measured
full run. The four shard assignments changed, and medians are
approximately 0.80 of the previous run for both identical and
changed output. These observations do not attribute the reduction
to compiler throughput.
The crypto on-load image loads normally and its SHA-256 result agrees
with the prior and release images. The other 411 changed images prepare.
Exact raw sections gain Type for httpd_cgi, Line for snmpa_agent_sup
and Attr, Code and Line for ssh_channel_sup. There are zero section
losses. Seven of 1,350 changed function bodies gain normalized
instruction equality, with zero losses. Comparable frame profiles
move closer in 411 functions and further away in two. Both outliers
now use one Y slot where OTP uses two. Instrument's missing
receive-marker lifetimes and tprof's early tuple construction
account for the different live values. The individual function
listings preserve the discrepancy.

The standard build passes in 270.83 seconds and Tooling passes in
249.54 seconds. The original five provider checks pass. The first
full managed attempt is stopped after 1,013.16 seconds when
nineteen tests report a missing local official-admission manifest.
The manifest hashes match between the previously exercised test
workspace and the pinned installer workspace. A verified local
junction restores the input. The expanded preflight passes all
24 cases in 489.73 seconds, including all nineteen affected
identities. The corrected full 9,787-case run then starts with
the same frozen compiler and test assembly and no competing
managed process at startup. The raw first attempt remains preserved. The
verification hashes 3,016 release files, 265 supplemental files
and 89 generated ASN.1 files against their manifests. All 1,366 build
profiles are equivalent. The initial path-only comparison rejects
LDAP's fresh temporary path. Both generated LDAP source and header
files have the same hashes, and the include roots and logical
source paths agree. Canonical readiness still uses the older passed
report.

[Source identity](source-freeze.json) · [Exact focused cases](focused.json) ·
[First focused attempt](first-focused.log) · [Second focused attempt](second-focused.log) ·
[Oracle and native frame fixture](combined-allocation/) ·
[Isolated pre-call frame results](pre-call-README.md) ·
[Isolated map lowering proof](ssh-map-README.md) ·
[Isolated terminal lowering proof](ssh-terminal-README.md).

[Linked pilot](pilot.json) · [Pilot clock](pilot-receipt.json) ·
[Native admission](pilot-loader.terms) · [SSH execution](ssh-execution-Candidate.terms) ·
[Normalized instruction summary](pilot-summary.terms) · [Raw section changes](pilot-section-delta.json) ·
[Uncached full launch](full-launch.json).

[Uncached full report](full-otp-corpus.json) · [Full timings](full-timing.json) ·
[Timing comparison](full-timing-comparison.json) ·
[Byte status changes](full-status-changes.json) · [Image hashes](full-changed-images.json) ·
[Full native admission](full-loader-summary.terms) · [Crypto observation](full-crypto-Candidate.terms) ·
[Full raw section delta](full-section-delta.json) · [Remaining sections](full-residual-sections.json) ·
[Normalized comparison](full-normalized.terms) · [Frame outliers](frame-distance/).

[Input verification](input-verification.json) · [Effective profile comparison](effective-profile-equivalence.json) ·
[Original path assertion](profile-equivalence.stderr.log) ·
[Standard build](standard-build-receipt.json) · [Tooling](tooling-receipt.json) ·
[Provider checks](provider-preflight.json) · [Runtime start](repository-full-start.json) ·
[First attempt failure](initial-invalid-workspace-inputs.json) ·
[First runtime stdout](initial-repository-full.json.stdout.log.gz) ·
[Fixture input correction](workspace-correction.json) ·
[Corrected preflight](corrected-provider-preflight.json) ·
[Corrected runtime start](corrected-repository-full-start.json) ·
[Remaining causes](remaining-causes.md).

## Corrected full managed regression

The frozen candidate completes 9,786 of 9,787 cases in 2,807.62
seconds. The only failure is the pre-existing generic deadline on
`runtime/beam/lifecycle388/original-alternating-nontail-and-full-trace`
at 15,068 ms. The same wrapper timeout was preserved in Part 1845
and in the earlier exact-source control, where an isolated repeat
passed. The original Jaro test passes here in 8,874 ms. The full gate
remains failed, so canonical readiness continues to use the last
complete passed snapshot.

[Identity and receipt](corrected-repository-full.receipt.json) ·
[Raw report](corrected-repository-full.json.gz) ·
[Stdout](corrected-repository-full.json.stdout.log.gz) ·
[Stderr](corrected-repository-full.json.stderr.log.gz) ·
[Earlier exact-source control](../integration-20261103/local-regression-9682/README.md).
