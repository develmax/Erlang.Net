# Independent review of Part 447 probe1

Read-only review of Program.cs, beam_ddll_identity447.erl, ddll_tcp447.erl and run-root.py. No execution or runtime/shared edits. Review applies to the bytes read, before any successor preparation.

## Sound aspects

- Two actual OTP OS server processes are created, and the managed consumer communicates with both using genuine packet4 loopback TCP. The transport statement expressly excludes Erlang distribution handshake; retain that exact limitation.
- The foreign reference is actually produced by peer.Call(new_ref), not a fixed fabricated ETF. The local queued reference travels through the peer decode/encode functions; both returned wire encodings must exactly equal their actual original encodings.
- Both runtimes execute the same compiled worker image for setup, DDLL operations, public setnode/2 and actual kernel death. The common worker catches class/reason/full stack. Equal recursively compares every tuple/list field and only permits explicitly captured own-reference pairings; stack frames are not dropped or normalized wholesale.
- Raw typed and ETF files are written before comparison; raw equality and protocol equality are separately recorded. Global backtrace depth is set on the real worker runtimes. Actual native child PhysicalExit, PhysicalCleanup and Completion, fixture disposal and zero leases are required.
- Original preparation errors prevent dependent comparison. Comparison profiles have separate groups and continue after a group failure. Provider inventory is scanned before/finally after, and input bindings are checked at every stage.

## Changes recommended before acceptance

1. beam_ddll_identity447:stop_node/0 currently returns identity immediately after receiving kernel DOWN. OTP node teardown has a scheduled set_node_not_alive continuation (dist.c:854-882). Receiving DOWN alone should not be treated as the inactive-state fence. Reuse the bounded public is_alive=false wait from tools/Erlang.LocalNodeAdmissionContrasts/local_node_admission_probe.erl before returning identity. Record actual node/nonode and creation0; do not coerce the state.
2. ddll_tcp447 stop branch sends Worker!stop, ACKs stopped and halts without joining the worker. Add monitor-before-stop and await its normal DOWN before the server ACK/normal halt. Native unload already has a genuine DDLL notification fence, but this gives an explicit actual worker completion proof as well.
3. run-root.py cleanup can stop attempting other owned original processes if process.terminate()/process.wait(timeout=10) raises inside the finally loop. Wrap each process cleanup independently, retain errors, and always attempt both processes. Use the existing bounded owned-tree termination convention as the final fallback; failed forced cleanup must remain a failure. Avoid masking an earlier comparison failure with a cleanup exception.
4. Make queue nonvacuity explicit. Expected/actual equality at foreign-caller-preserves-queue compares counts, but the consumer never asserts the source expected count is1, and never checks the empty awaiting list after the creator cancels. Assert actual original and managed before count1, after count0, and unchanged counts after stale-wire rejection. A helper returning normalized count0 for [] is sufficient; do not reinterpret malformed info. Use separate monitors for direct/current-wire checks when claiming independent real removal, since repeated demonitor on an already completed local reference legitimately returns true.

## Scope statements to retain

- This is real VM / loopback TCP ETF interoperability, not Erlang distribution handshake, EPMD or net_kernel boot interoperability.
- Public setnode with registered minimal real kernel actor and its actual death exercises VM identity changes; it is not the standard kernel/application boot proof.
- next-node-admission uses a different node name. It does not prove same-name/creation restart collision avoidance with a retained external old epoch or real distribution reconnect. Keep those as separate remaining tests if not added.
- peer-rejects-primary-reference compares two calls executed on the original peer with each runtime's actual returned reference. foreign-reference-full-stack is the cross-original/managed DDLL stack comparison. The distinction matters when reporting result counts.
- Probe result rows are scenario/step observations, not added canonical API contracts or canonical suite tests. Binding counts do not equal independent reference-lifecycle checks.
