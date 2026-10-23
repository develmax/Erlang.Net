# Part445 — DDLL permanent, deferred reload failures and force modes

Pinned OTP29.1.1: ad05823719d77c8faee87348ea39513d4e2f99c5. Root merged Part444 as main ef3840c5; exact merge CI39914974411 passes8823/8823, all mandatory steps and both package/consumer checks. This is Git publication and CI package validation, not a NuGet release.

## Actual defect and correction

The genuine new native driver invokes driver_lock_driver on its real opened port from control, outside init/finish. While a real port holds a pending reload, the same observer owns loaded, unloaded and unloaded_only waiters. Before correction, all six complete source-profile comparisons fail only the permanent notification-order field:54/60 per-case values agree. Original order is loaded_monitor, reload_request, unloaded_monitor, only_monitor. Our combined traversal sent only_monitor, unloaded_monitor, loaded_monitor, reload_request. Independent review binds complete actual/expected raw/typed/own files and proves no other field differs.

RuntimeDdllParent422.LockDriver now performs three per-kind passes in pinned source order (erl_bif_ddll.c1022-1043): AwaitingLoad, AwaitingUnload, AwaitingUnloadOnly. Traversal within a kind, monitor reference/name identity, shared gate, process-reference clearing and permanent ownership remain intact. The source SHA changes exactly E290340C to CD1BAAF0; no other product file changes. Independent final review reverses only that block in memory and reproduces the entire pre-fix source hash.

## Genuine acceptance

| `permanent_transition_order` | Matches in all six profiles |
| `reload_missing_recover` | Matches in all six profiles |
| `reload_wrongname_recover` | Matches in all six profiles |
| `reload_initfail_recover` | Matches in all six profiles |
| `force_unload_option` | Matches in all six profiles |
| `force_loadflag_unload` | Matches in all six profiles |
| `force_loadflag_reload` | Matches in all six profiles |
| `force_multiowner_option` | Matches in all six profiles |
| `force_loadflag_owner_death` | Matches in all six profiles |
| `force_reload_pending_otherowner` | Matches in all six profiles |

Identical compiled Erlang source and exact same DLL bytes run on original OTP and our actual native child/parent backend. One actual native fixture is newly compiled; authenticated echo/wrongname/initfail DLLs and the stock facade from the earlier228 receipt are reused. The latter is not a228 rerun. Missing DLL, wrong-name and failed-init deferred reloads produce actual load_failure/open_error or source error atoms, remove failed state, then permit a fresh healthy load and real echo exchange. Force scenarios cover unload option, initial kill_ports flag, reload, multiple owners and actual last-owner death.

Ten rows in each of six independently isolated VM/Runtime-child groups (values/default/0/1/2/8) yield60 whole main observations. All six entire final main ETF files and complete typed terms match; no source golden construction, data rewriting or comparison-side normalization. Explicit profile metadata is actual system_flag readback/restoration. These normal-return mode results are not proof of every error stack or the entire depth-BIF contract. An explicit binary-name subprobe retains either its actual full exception/stack or actual accepted reference+notification; pinned OTP accepted it in this cohort.

Each group retains complete own ETF/typed logs with real refs/PIDs/messages/info. Opaque bits are not compared across independent VMs. Same-actor permanent notification order is compared; force port EXIT and DDLL notification are separate producers, so source main waits for both and reports each role. Actual arrival stream remains in own raw logs, without inventing a cross-producer ordering guarantee. Genuine ports, helper handshakes and process DOWN provide barriers; no sleeps or retry-to-absence loop. Permanent is retained until VM/runtime disposal, with no false wait for unloaded.

All six managed children prove actual PhysicalExit, PhysicalCleanup, Completion and zero owner leases. Final Runtime90EAF3A2 additionally passes previous Part44472 and retained36 whole original comparisons, all8 native/HOST regressions, affected195/195 in10 immutable blocks and standard79/79 with actual collector/persistent cleanup. Thus168 deterministic source rows were replayed on final assemblies;60 are new,108 retained. Provider6427 is scanned completely before/finally-after each new batch, all bound inputs stable; native/compiler/VM processes are owned and bounded. Independent routes continue after sibling failure when preparation succeeded.

First compiler and style initialization OOMs are preserved. The serial build retry and one-processor low-memory style retry pass without weakening checks or changing product behavior. An earlier boot execution session disappeared without an exit/comparison receipt; its child was absent on read-only inspection, but that attempt receives no success/cleanup credit. Fresh boot79 output supplies the actual passing lifecycle evidence. Actual CLR builds have zero warnings/errors; original Erlang observer retains three unused-helper warnings. Final source layout needs zero changes. Source1618/default708/full118 and classifications stay unchanged, deltas0/0. The standalone60 rows are not60 new canonical registrations; the catalog remains8823. Historical8813 readiness authority is distinct from final publication-head full CI, whose exact SHA/results are linked in the attached PR.

## Evidence and limits

Large JSON receipts use lossless gzip with deterministic metadata; archive-map.json binds both stored and original raw SHA/length. Git-index audit decompresses every stored receipt and verifies all source/evidence bytes. No earlier failure is overwritten. Source-only helper preparation/review is separate from Root actual native/VM execution. Recipes retain original workspace-relative authority/helper dependencies; vendor authority files are hash-referenced, not redundantly copied. This is provenance, not turnkey fresh-clone replay. LICENSE.local.txt remains excluded.

This closes these ten stated scenarios, not all DDLL contracts or all force combinations. Wider reference-origin/generation/owner concurrency, repeated permanent transitions, additional force cross-products and other OS/ABI/native-thread/physical profiles remain separate. Adjacent erlang port APIs and actual ERTS layout are not promoted by these results.
