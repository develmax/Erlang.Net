## Part 322 — 2026-10-16 — Connected guest PID explicit exit controls

**Part status:** implementation and isolated SDK validation completed; final Main, native and readiness receipts must be bound before publication. Full `erlang` compatibility remains partial.

**Objective and scope:** make existing public `exit/2`, `exit/3`, `exit_signal/2` and `exit_signal/3` publish genuine explicit exit controls to an already-connected remote PID. Previously these routes could return success without publishing a packet. This increment preserves the existing public MFA set and does not add pending connection establishment or automatic connection setup.

### Changes

- Added runtime routing for captured-generation remote PID explicit exit output and serialization of EXIT2/ALTACT controls. Default calls publish ordinary explicit exit controls; complete option validation precedes publication, including invalid members, improper tails and duplicate priority handling.
- Preserved local process exit behavior and the public aliases. Connected output uses the existing producer and connection ownership mechanisms. Ordinary output retains its producer cancellation and generation checks; accepted async distribution output uses the previously bounded staged worker and survives producer death as connection-owned work.
- Added two direct public-route HOST checks and a separate lifecycle/profile/token/ownership HOST group. They inspect actual owned controller packets and process behavior, rather than substituting a fake remote reply. Existing link/unlink, send, group-leader and admission regressions remain part of validation.
- Added a separate original-only collector with eight explicit exit contracts, private foreground epmd and two loopback VMs. Its source and cleanup protocol are reviewed; this publication does not claim that the collector has executed. It preserves raw ETF, actual images and metadata when executed later.

### Decisions and compatibility

The implementation follows the pinned OTP `ad05823719d77c8faee87348ea39513d4e2f99c5` source and reuses the existing distribution codec, lifecycle fixture and output ownership infrastructure. No external implementation was imported. Native fixture C, ABI profiles, import policies and approved callback capabilities remain unchanged. Canonical native source plans include the four new runtime/serialization partials: 1107 legacy inputs and 1132 reallocation inputs.

Explicit exit is not sequence-traced send. It does not advance a send token, attach a token or emit a pseudo-send event. Output is EXIT2 (normal opcode 26 or legacy opcode 8) or ALTACT opcode 37 with EXIT flag 16 and, when requested, priority flag 1. The first source-only metadata recipe incorrectly mentioned token-bearing EXIT2_TT; that text was preserved and corrected in recipe revision 2 before execution. This was a disclosure error, not an application runtime failure.

The supported route is an already-connected remote PID with the captured entry generation. Pending/autoconnect, remote names and aliases, a live SDK network peer, general native wire equality and full inbound priority behavior remain incomplete. The separately source-audited blocked normal output wait gap remains follow-up 323: such waits do not yet fully admit owner control signals and cross-process system queries. This is a genuine runtime limitation, distinct from the historical idle HOST actor observation issue.

### Validation

| Check | Saved proof and command scope | Actual result |
| --- | --- | --- |
| Isolated source/build | Formatted 10 C# files, complete 2314-file isolated source authority, actual Tests target | Target build passed with zero warnings and errors; the compiled source remains raw-identical in the final composition. |
| SDK GREEN | [Combined guard](../validation/part-322/retained-local/connected-guest-exit322-combined-guard1/guard.json), actual focus driver against Tests/Runtime DLLs | Thirteen groups passed. Actual DLL identities and authored source rows were passively rehashed. |
| Meaningful historical RED | Same two public HOST files over the exact repaired 321 core, complete 2309-file source authority | Both `exit/2` and `exit/3` return success but fail the immediate real missing-packet assertion at line 26 with `InvalidOperationException`. The single historical target build, both actual run identities and both reports are retained; neither failure is a timeout or a missing API/reflection member. |
| First focus selection | Original focus report, same compiled DLL | Three new groups passed before a missing method suffix in an old multi-method HOST selector caused a reflection `NullReferenceException`. Correct selectors passed all thirteen with the same DLL; no runtime source or expected behavior was changed for this infrastructure correction. |
| Final Main/native/readiness | [Full guard](../validation/part-322/final-main/full-guard.json), [native guard](../validation/part-322/final-native/guard.json), [readiness](../validation/part-322/module-readiness.json) | [FINAL_GATES_PENDING] |
| Previous published 321 CI and observers | [Independent CI guard](../validation/part-322/retained-local/pack321-ci-independent-guard.json) | Exact published head `a1a7fe4b8ef81e3cefdd39bc9717ab6855376b4a` passed 6678 tests and eleven integration/package checks; all 262 published blobs were checked. The [native, reallocation and distribution observers](../validation/part-322/retained-local/pack321-four-terminal-independent-summary.json) also independently passed at this same published 321 head; only the ordinary differential workflow remains pending. Distribution evidence includes three actual compiled images on both original nodes, including the minimal bang image; This original-only collection does not test SDK preparation or invocation of that image. |

The new original322 observer has eight contracts; it is separate from local SDK HOST results and the unchanged trusted Windows C callback regression probes. No original VM or two-node wire comparison is run by the local full pipeline. Registry presence remains 509 total MFAs and 351 `erlang` MFAs, with zero presence delta; presence does not mean complete semantic compatibility.

### Handoff

Bind actual Main 6681/6681 tests, eleven integrations and three separate post-full trusted Windows roles before recording final gates. Run the readiness CLI against the immutable part-321 test report, requiring 509 current/509 previous registered MFAs and delta zero. The finalizer appends this complete entry to active 0049 within 32 KiB/400 lines, preserves closed 0048 and 0047, and updates seven progress/compatibility notes plus the four existing exit rows. See the [evidence receipt](../validation/part-322/evidence-receipt.json) for exact source/capture bindings.

After publication, run and independently inspect the original322 observer on its exact published head. Keep accepted async output, producer-owned ordinary waits, pending/autoconnect and follow-up 323 as separate scopes. Checkpoint title: connected guest explicit exit control publication. Current 322 remote outcomes are pending; no live peer or general wire-equality result is claimed here.
