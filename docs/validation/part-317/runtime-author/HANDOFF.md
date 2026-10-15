# Public memory/0,1: source-only candidate

Candidate: `candidate/`. Source freeze `20F71DC931205AF6BB1C4879418198DD5E767B8636399B1746AAEF511C0D0A6F`; complete 2257-file closure `B23EC556E22FC228B78651D64D9B6E0BE65A884829180148D768CA151D358421`. Twenty-eight C# paths: sixteen added and twelve changed. All 2229 other parent files are byte-identical, including native code, ABI/profiles, collectors, source plans and workflows. No build, tests, native execution, VM execution or Main changes occurred.

Parent is the actual working Main316 source `57F9F381…` / 2241-file closure `87E2A60C…`, copied and individually rehashed before editing. The freeze preserves its original working-parent role. Parent subsequently reported published316 `6d7a430f55a43e488257f654e72a53b3b7f96ada`; the separate parent-binding receipt records this without rewriting the historical freeze. Published315 `32c39423…` is not mislabeled as the compiled316 source.

## Actual implementation

`CoreModules` registers both public arities, routed through one captured `ClrMemorySnapshot`. The ordered nine keys, integer/list selection, duplicate identity, empty list, badarg versus improper-tail function_clause, and unsupported maximum follow the approved design. A default fresh runtime has a positive supported payload profile. There is no artificial disable switch, fake GC partition or always-notsup path.

Scope is the shared loaded SDK Terms/Runtime **type universe**, across its weakly observed runtimes. Unrelated application heaps and separately loaded SDK versions/ALCs are excluded. `ObservedPayloadStorage` weakly keys actual backing identities; its values contain numeric bytes/category only. Primordial canonical atom strings and duplicate strings retained by Atom.Name are observed. Binary backing is counted once even when shared with another wrapper or observed by the code hook. Binary has priority over code. Sizes use actual string/array lengths, sizeof(char), Unsafe.SizeOf<T> and BigInteger arithmetic.

Code hooks precede readonly wrapping of the descriptor and prepared-body arrays. PrepareBeam owns an input copy, and BeamStructureReader makes another actual copy retained by chunk slices: both are observed by actual allocation identity, not by measurement copies. GC-observed bytes can remain counted while their actual backing remains alive; purge does not promise an immediate GC decrease.

ProcessSlotTable uses a lazily growing List<Pid?>, with occupied identity admission, released-cell reuse and unchanged ordered sparse scans. Its real capacity cells contribute to processes; occupied cells contribute to processes_used. Each mailbox contributes its actual owned inline Entry payload; CLR node headers and arbitrary referenced term graphs are excluded.

System is atom + binary + code + current joined native backing-request extents; total adds processes. ETS is the empty source set of the currently absent SDK ETS implementation, not a claim of working ETS. Exclusions also include CLR headers, hash bookkeeping, process-context/dictionary object bodies, JIT/assembly storage, OS port implementation storage and accounting telemetry. This is not physical, GC-total or ERTS-byte equivalence.

## Lock and lifetime audit

- Weak runtime registry gate only copies live keys. It is released before taking any runtime gate or awaiting any source.
- Runtime gate captures slot numbers and Mailbox references, then ends before mailbox gates and native capture. No runtime-to-mailbox nested lock or native await exists.
- Mailbox gate only reads messages.Count and inline Entry size; it executes no admission callback or signals.
- Atom canonical admission can acquire the backing registry gate. Registry capture calls no atom-registration/runtime/mailbox code while holding that gate, and checks cancellation on every entry. No reverse lock edge is introduced.
- Native capture reuses the existing cancellable domain method and reads the nullable domain field. It does not create missing domains. Existing native ownership/locking is unchanged.
- Weak-key values have no key, runtime, image or process references. A query transiently roots sampled keys/runtimes until capture settles. Source captures are locally coherent; the combined sample is deliberately not a stop-the-world/global-epoch snapshot.

## Authored permanent tests

One HOST group (`host/clr-observed-payload-memory`) plus two direct `mfa/erlang:memory/{0,1}` routes. Coverage includes positive nine-key output, selection/order/duplicates/empty/errors; actual copied and adopted binary identities, ByteView alias, equal-content distinct buffers, untracked external export copy, duplicate atom strings; weak binary and prepared-image collection; retained prepared code arrays and binary/category priority; process admission/termination/reuse and actual mailbox enqueue/removal; bounded real native 7/zero/grow/shrink/free and two-runtime local contributions; concurrent producer/query partition invariants; actual query cancellation while a native domain remains held, including a runtime-gate operation while the query awaits it. Native helper owners are existing actual backend adapters, not fabricated snapshots or private-field writes.

No test assumes stable global before/after totals: GC and other runtimes may change them. Identity assertions use actual backing metadata and keep-alive roots. Exact mailbox/native deltas use the local runtime capture. Two existing tests change only legitimate export expectations: SystemAllocatorPublicHostChecks no longer requires memory absence; halt registration counts become 509/511. No readiness-count expectation, ordinary oracle exclusion or full-options shape changes.

Root must independently review, format/token-check, update reviewed-source plans as appropriate, build and run actual focus/full validation. No pass claim is attached to these authored tests.

## Native source references retained

Approved design freeze `7B4430A7…` contains exact source bindings. Pinned OTP `ad05823719d77c8faee87348ea39513d4e2f99c5`, erlang.erl:12980–12982 states amounts are not atomic; 13021–13038 fixes output order; 13064–13085 fixes atom/list/outer/improper-tail behavior. Original exact file copies remain in `artifacts/allocator-reducer313-foundation-candidate/pinned-source/`, with hashes in the design. Numeric backend values remain an explicit CLR adaptation; keys/order/errors/duplicate behavior and arithmetic invariants can be paired independently.
