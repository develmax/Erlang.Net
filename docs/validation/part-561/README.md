# Part561 resource closure and string acceptance

Baseline: **OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5**,
Windows x64, .NET SDK10.0.400. Both C# and original Erlang/BEAM remain available.

| Check | Actual result |
| --- | ---: |
| Full test harness |9647/9647, zero failures |
| Focused CPU/string/copy/port groups |28/28 |
| C# original differential outcomes |8522/8522 |
| Original BEAM on .NET outcomes |8522/8522 |
| Exports declared and exercised per backend |71/71 |
| Returned values matched per backend |6364 |
| Errors and complete stacks matched per backend |2158 |
| Binary backing observations per backend |13/13 |
| Depth0,1,2,3,8,32 per backend |17/17 each,102 total |
| Readiness CLI, actual source/evidence admission |Pass |

See [summary](acceptance-summary.json), [full tests](tests.json),
[exact registry](supported-mfas.json), [focused controls](final-controls3.json),
[C#](managed-report.json), [BEAM](beam-report.json),
[depth profiles](depth-profiles.json) and [oracle provenance](provenance.json).
[Raw acceptance archive](final-string-acceptance.tar.gz) retains ETF, compiled
observer, logs, upstream44-testcase acquisition and exact replay receipts.
[Source](source-files.json) and [assembly](assemblies.json) hashes identify the
local replay independently of any later commit.

## Resource fixes and retained failures

Windows CPU observation opens the exact thread with limited query access and
uses GetThreadTimes through SafeFileHandle. It no longer enumerates all process
threads. Actual100ns user time, nested VM attribution and provider failure
reporting remain intact. Linux keeps its prior provider. The old five interrupted
cases first pass5/5 even on the unchanged old DLL in a fresh process:
[baseline replay](resource-repro-baseline.json). Thus the previous low-memory
snapshot and subsequent Roslyn stack overflow do not establish a deterministic
compiler defect or prove a causal relationship. Roslyn is not patched to hide it.
The accepted full run passes that same sequence and all later compilations;
OutOfMemory and Stack overflow do not recur.

C# scalar decoding now copies at most four UTF8 bytes, preserving unaligned
binary values and the original tail representation. The permanent16KiB test
asserts both backend values and a32MiB C# allocation ceiling. The
[old-DLL negative control](old-decoder-control2.json) returns the right length
but allocates137402688 bytes and is rejected; the new implementation passes.
A64KiB length case is also compared against actual original OTP in both backends.
The ceiling targets the C# regression, not BEAM interpreter allocation policy.
Earlier64KiB ordinary-actor controls retain their real budget/timeout failures
in final-controls.json and final-controls2.json; generic budgets are unchanged.

Part560's failed full runs, rejected broad map shortcut and1328-row crash remain
[immutable](../part-560/string/README.md). The first intermediate Part561 replay
is not final evidence: a large-input case was appended after acquisition. The
fresh final archive binds the changed source and final assemblies. No failure
is rewritten into a pass. Current host snapshots differ from the older low-memory
observation; they are observational, not root-cause proof.

## Readiness and scope

The source/evidence validator accepted the actual final report and733 default
MFAs (previous immutable snapshot708). Sixteen MFA and eight historical native
source bindings are refreshed after review; compatibility statuses and historical
milestones are unchanged. See [source review](source-binding-review.md),
[MFA binding changes](source-bindings-refreshed.json),
[native changes](native-source-bindings-refreshed.json), and the generated
[readiness](MODULE_READINESS.md). Native binding metadata explicitly identifies
this working-tree snapshot; it does not pretend to be published Main or exact
historical source equivalence.

Default string registration remains one shared primitive. The explicitly chosen
backends each expose71 APIs. These are complete finite-profile observations,
not a universal all-input/platform/resource proof. Scoped publication must bind
its actual code version and fresh head CI/review, preserving the pre-existing
shared map/ordsets work. No original Erlang deletion, fallback, default backend
switch or package publication occurs.
