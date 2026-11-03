# Part 557 — Native async accounting and logical port close

Local Windows validation and final integrated source admissions are complete for the bounded scenarios below. Publication, exact committed checkout admission, exact-head CI, current review and merge remain separate required gates. API and full-contract deltas are **0 / 0**. The complete statistics contract is not accepted.

## Final integrated checkpoint

Reviewed metadata 1354 was applied under 5615 guards in 1361, without C# changes. All 13 focused source-plan admissions plus MFA and Resource529 pass in [1362](evidence/final-admissions/review.json). Final normal Debug1363 and Release1365 builds pass; all four actual pre/post maps contain the same 13902 source entries. The complete [canonical map](evidence/final-integration/canonical-source-map13902.json) is stored once; linked build reviews explicitly record the original full receipt hashes. Both raw build process receipts, logs and every error field remain retained. This is duplicate-map storage reduction, not a new build or changed source claim.

The final post-metadata selected regressions pass **97/97 in Debug1364** and **97/97 in Release1366**, each with the same 83 runtime files before and after: [Debug](evidence/final-affected-debug/review.json), [Release](evidence/final-affected-release/review.json). Catalog9601 remains only a catalog count; the whole suite has not run. Earlier native7/7-per-profile and all historical RED/raw evidence remain below and are not overwritten. Exact committed checkout proof, mandatory current-head CI, review and merge are still open.

## Changes

Persistent native async workers now participate through authenticated capture/apply/finish/release ownership. Source-tail work preserves the real initiating normal worker and physical trap lifetime. Native work stays in the child; parent activity is not substituted for child counters.

The concrete fixes include configured child async service/metadata handling (1195), physical pending-job/image/PDL retirement (1200), original `port_control` badarg metadata (1206), and separate logical versus physical port close (1317). Logical close and DOWN can complete while unrelated queued native work remains; the real row, image and PDL stay owned until physical completion. The original failed [cross-port result](evidence/cross-port-red/own.etf) is retained.

The runtime/BIF architecture remains C# with unchanged pure OTP Erlang/BEAM resources. No new dependency or public function API is introduced. Upstream fixture and license/provenance remain preserved.

## Validation

| Gate | Actual result | Evidence |
|---|---|---|
| Final native observer, Debug 1347 | 7/7 passed | [completion](evidence/native-debug/completion.json) |
| Final native observer, Release 1348 | 7/7 passed | [completion](evidence/native-release/completion.json) |
| Post-fix affected regression, Debug 1342 | 97/97 selected from catalog 9601 | [receipt](evidence/affected-debug/review.json) |
| Post-fix affected regression, Release 1351 | 97/97 selected from catalog 9601 | [receipt](evidence/affected-release/review.json) |
| Real force unload, 1341 | Five required events and physical cleanup passed; context limitation below | [peer review](evidence/force-unload/independent-peer.json) |
| Two-shard cohort prototype, 1262 | **FAILED**, 3600.047-second timeout, zero tests executed; not enabled | [failure](evidence/cohort-failed/review.json) |

The seven native cases cover fresh original A1/A2 comparisons, public queued-normal-ACK caller cancellation and VM close, component child exit and wrong epoch, and the strengthened cross-port close observer, including public `port_info`, `ports` and DDLL port-count checks. The final original and own runs execute the same strengthened source; the initial 1310 RED source remains separately preserved. The genuine C fixture remains unchanged. Original A1/A2 check 15/17 finite predicates, scalar outcomes and complete eight-frame invalid-control stacks using the declared singleton fixture-port role. Native final counts are 8 invoked/returned/freed with 7 ready callbacks for A1, and 11 with 10 ready callbacks for A2; jobs finish and only the controller state remains. The A1 two-worker and both zero-pool boundaries stay explicitly unexecuted. Original `+A0` clamps to one worker and is not proof of zero mode.

Full original and own ETF outputs, exception stacks, raw dynamic maps and stage logs are retained. Dynamic topology, counters, opaque port/reference values and native thread IDs are VM-relative; raw whole microstate outputs are not declared equal or normalized. The cancellation cases occur after remote-token release while the final normal ACK is queued; they do not prove cancellation during an in-flight RPC. Expected component transport/release/VM fault logs remain visible alongside physical child exit and lease cleanup.

Compiler fixture preparation uses a narrowly declared MSVC ancillary retirement policy. Its receipt retains `ForcedCleanup=true`, `NaturalDescendantsExit=false`, the exact owned image/hash, same-Job membership, actual termination code and final Job zero. This is explicit targeted retirement, not natural cleanup. All ordinary observer/runtime stages still require unforced completion. Earlier compiler cleanup failures remain failures.

## Force-unload comparison limit

Both VMs return `pending_driver` with their own monitor reference, report both port DOWN and linked EXIT reasons `driver_unloaded`, then driver DOWN `unloaded`. Both clean up physically. Fixed snapshots, port count and cleanup fields match. Raw event order and identities remain unchanged.

The post-unload `erl_ddll:info` error is `error:badarg` on both sides. Original capture has eight frames; the direct HOST invocation has six. Those six exactly match the original prefix; the original adds `attempt/1` and `capture/2`. This is not full-stack or whole-output equality. Aligning both invocation contexts remains open; no synthetic frames or suffix stripping is used.

## Handoff

Readiness remains 708 registered / 180 full / 504 pending / 24 known differences; `erlang` remains 351 present / 83 full / 255 pending / 13 known. The full 9601-case catalog has not run. Remaining statistics work includes in-flight cancellation, a real persistent poll provider, default topology and feature profiles, Linux DriverHost, and the remaining S0–S5 matrix.

Before publication, consume the guarded copy-list, complete exact committed checkout transport/source admission, verify the committed tree and require its mandatory CI plus current review. No result here promotes another PR or authorizes a merge.

## Publication source proof

The [candidate raw tree](publication/source-tree1372.json) matches all13902 final build inputs and344 changed source/documentation paths. The [configured affected checkout](publication/affected-checkout1373.json) passes284 raw-critical byte checks and all78 declared native input guards. Its six LF/CRLF differences remain explicit. These are source/transport checks, not another runtime execution or a full checkout build. Only documentation/evidence will be added after this candidate; the final commit and exact-headCI/review must still be verified.
