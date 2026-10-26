# Part 502: logical process death and physical retirement

This increment corrects an inference in Part500. It does not add an API or another full-contract closure. Published Part500 sources, golden outputs, changelog and evidence remain frozen; this receipt explicitly supersedes the claim that its lookup-based dirty-status result proved physical native-body completion.

## What the controlled original proves

Pinned OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`, loads a genuine Windows x64 C NIF. Its dirty CPU callback publishes independent `entered`, `done` and Windows wait-result markers, then blocks on an actual native event. A different process kills its registered owner and starts an actual liveness query. Before releasing the event, the original reports:

```erlang
{{before_release,{1,0,-1},true,false,false,undefined,killed,{1,0,-1}},
 {after_release,{1,1,0}}}
```

Thus public alive=false, internal alive=false, name removal and actual DOWN occur while the native callback is still physically blocked. Explicit release then produces physical done=1 and wait result=0. The complete 107-byte returned ETF is preserved without normalization. The final collector verifies its original golden and cleanup, all6427 official provider files before and after, successful build/owned VM settlement and no timeout. See [original completion](original/completion.json), [raw returned ETF](original/returned.etf), [native C source](original/retirement_probe502.c), [source plan](original/source-plan.json) and [build receipt](original/build-receipt.json).

The source partition explains this result. `is_process_executing_dirty/1` uses `erts_proc_lookup`, which rejects an exiting process before examining its dirty flags. A false result therefore cannot establish physical completion. Public `is_process_alive/1` uses raw lookup and preserves signal order while an exiting entry remains, but returns false when that table entry is gone. OTP unregisters the name and deletes the process-table entry in logical `EXIT_FREE`; physical dirty process storage may survive afterward. [Pinned source excerpts and digests](source/part500-retirement-claim-review.json) preserve the exact code behind this correction.

## Managed correction and three different controls

The runtime now tracks dirty-native work and allocator auxiliary requests as physical retirement. Existing logical storage cleanup still precedes liveness replies, name removal and DOWN. Exit continuation finalization always joins physical work, including logical-cleanup/output failure paths, before releasing continuation ownership or settling host `Done`/`Quiescence`. Host `ProcessHandle.Completion`, Dispose and Halt retain their actual-work join guarantees. Physical failures aggregate with earlier failures rather than being hidden by logical success.

The controlled dirty test uses the actual managed C# dirty scheduler and independent physical markers. It compares the whole assembled ETF literally to the actual original C protocol. The workload bodies differ: this is not execution of that native C fixture inside the managed runtime and does not establish general NIF ABI equivalence.

The allocator control admits a real histogram request and inspects its actual origin, batch and pending physical completion while another real auxiliary lane is held. It checks alive/name/DOWN can finish first, while host completion and an actual Dispose continue waiting until release. This is a source-guided managed ownership regression; controlled original auxiliary execution is not claimed.

The ETS control holds the actual stored-object mutex reached through a genuine read lease, so real owner-table retirement cannot complete. It confirms both liveness requests were actually admitted, remain pending, the name remains owned and DOWN is absent until that real storage barrier releases. A read lease alone is not presented as a logical barrier: existing tests correctly permit it to retain payload roots after logical table cleanup. This managed control protects against returning false immediately for every terminating process; it is not a claimed controlled original ETS comparison.

Part500's fourth result field now comes from the actual internal dirty-status query, matching its original observer, rather than substituting an independently measured physical completion marker. Its original six golden protocol outputs remain unchanged. Rename the canonical test from `runtime/process-liveness500/physical-retirement-before-false` to `runtime/process-liveness500/logical-retirement-before-false`; this replaces one name and adds no test or API by itself.

## Retained results and delivery boundary

The history is deliberate and does not represent one uninterrupted final-source test run:

| Receipt | Result | Meaning |
|---|---:|---|
| [Initial controlled managed case](history/managed-before.json) | 0/1 | Reproduces the old physical-wait behavior against the new original event protocol. |
| [Dirty split focus](history/focus-fixed.json) | 9/9 | Includes inherited liveness and five actual dirty lifecycle/ownership controls. |
| [Allocator split focus](history/focus-aux.json) | 3/3 | Retains corrected500/controlled dirty and adds actual auxiliary ownership. |
| [First ETS fixture attempt](history/focus-storage-failed.json) | 3/4 | A real mutex wait timed out because fixture scheduling allowed lane contention; this attempt is not accepted as compatibility proof. |
| [Repaired ETS fixture](history/focus-storage-fixed.json) | 1/1 | Separate bound lanes for killer/storage and query actors; dedicated bounded mutex holder. Actual admission and wait assertions remain. |
| [Final affected regression](affected-final.json) | 1736/1736 | Complete selected processes/native/ports-distribution blocks and retained BIF/native ownership controls on the final runtime sources. |

The [final serial build](history/build-final.log) reports zero warnings and errors. The source roster remains1729; [three existing runtime bindings advance](source/runtime-source-delta.json), without native ABI or infrastructure promotion. A fresh [native empty-map collector](native-map-fresh-completion.json) also passes two whole ordinary/reviewed original comparisons on the revised runtime, with provider and owned-process cleanup checks. Its Windows x64 constructor scope remains unchanged.

Parent Part501 exact head `0363df7e5e5024707469f8a6518b0d7c2290375e` has independently verified [green CI40356733018](https://github.com/develmax/Erlang.Net/actions/runs/40356733018),9048 tests and retained artifacts. [Copied proof](parent501-artifact-proof.json) and [metadata](parent501-ci-metadata.json) preserve that delivery result while explicitly retaining `InheritedLivenessClassificationBlockedByControlled502Probe:true` and `WholeSemanticCompatibilityClaimed:false`. A green parent run did not validate the missing physical/logical partition. Its historical Parent500 count179 is separate from canonical Part501 count180.

Registered708/full180/pending504/known24 remain unchanged; API/full deltas0/0. New catalog9051 adds three ownership groups and accounts for the renamed legacy group. This is not a percentage of complete project compatibility. Final1736 affected validation passes. Exact new publication-head CI, retained original/native/package/consumer/cleanup artifacts and fresh PR review are still delivery gates. The [acceptance record](acceptance.json) keeps those pending gates explicit.

## Evidence handling and remaining scope

[manifest.json](manifest.json) binds raw bytes of the copied original results, build/collector receipts, source excerpts, parent artifact proof and historical failures. README and authored acceptance status are separate from that immutable copied-evidence manifest. No binary result is seeded from managed output. Only the original controlled C fixture claims Windows x64 native execution; no Linux native execution, general NIF ABI, identical-workload performance or whole native-library compatibility is claimed.

Process-table membership while exiting, concurrent snapshot behavior and registered-name ordering remain separate validation work. In particular, a physically running dirty body alone does not require retaining its PID in `processes/0`. LICENSE.local.txt remains excluded.
