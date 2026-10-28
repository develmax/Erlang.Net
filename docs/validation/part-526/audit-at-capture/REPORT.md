# Read-only open-PR audit: 61, 64, 65, 66, 67, 68

Captured 2026-10-28 00:14 UTC. Repository develmax/Erlang.Net. No active sources, Git state, CI runs, discussions, review states or branches were mutated. GitHub metadata, all returned conversation comments, review submissions, inline threads and current-head workflow jobs are preserved separately. Findings below are triaged against exact head source, not only bot summaries. This is a bounded comment-resolution audit, not a fresh full source review or VM execution.

| PR | Exact head | Base; draft | Exact-head CI | Handling |
|---|---|---|---|---|
|61|0363df7e5e5024707469f8a6518b0d7c2290375e|feature/process-liveness-contract-499; draft|40356733018 success; 2/2 jobs|Close superseded PR; do not merge independently. Branch deletion is eligible after final current reachability/open-dependent/protection checks by root.|
|64|2647b738a1ac385cbda35ba9f595d21a04d09f8d|main; ready|40422559287 success; 3/3 jobs|One genuine unresolved bug is repaired in child65, not in64. Do not call this exact head review-clean standalone; integrate the verified65 repair in the merge sequence and resolve after actual current-main verification.|
|65|4ff4482f1fb6d077d66f18038f209af80580129e|feature/native-maps-known-differences-504; ready|40439065288 success; 3/3 jobs|No inline findings or review submissions. Includes actual duplicate-flat repair; next after64.|
|66|0781df2c3ef2c6d6c5de422a574c78bdf507901b|feature/native-map-materialization-505; ready|40446495646 success; 3/3 jobs|Reviewed shutdown-chain defect fixed at exact head; thread resolved/outdated. Next after65.|
|67|5ccc5c6df5684274acd4687050943011e6093744|feature/guest-term-root-integration-506; ready|40455813441 success; 3/3 jobs|No inline findings or review submissions. Next after66.|
|68|553f530ea16fd363c83cb256664146d600c71ad1|feature/trusted-guest-and-beam-roots-507; ready|40462728659 success; 3/3 jobs|Reviewed quadratic construction-custody defect fixed at exact head; thread resolved/outdated. Next after67.|

All queried commit status APIs returned an empty external-status list; successful Actions jobs are separately recorded, rather than treating that empty list as success. The run lookup is filtered by the full head SHA. Each returned job and step conclusion is success. No fresh CI rerun was initiated. PR merge checks must be re-read after any retarget/new commit.

## PR61: superseded plus retained style issue

PR61 has 3 combined comments, 1 review, 1 unresolved inline thread. The PR body explicitly records the obsolete logical/physical liveness inference and directs integration through corrected PR62. Live compare confirms head0363df7 is the merge-base of main, main is ahead6/behind0. At capture main is30d294e5c9e5ce8bd0c01248adf3e9e4a610b9a4. PR62 is closed/merged, merge b330dd3d884802b8b64faaac78edf813e2fc7fa1. Consequently no missing code requires merging61.

The thread PRRT_kwDOVCU9ks6wx_H2 is still actionable in the retained file on main: tools/Native/Erlang.NifMapCreation501Observations/build-native.py still has `key,value = line.split(...); env[...] = value`, an inline header-copy loop, and inline main call. The comment correctly identifies the AGENTS readability rule. Closing61 does not repair the carried file. Root should repair this small Python layout issue on current code, preserve behavior, and refresh any direct source-plan bindings before delivery. No style source mutation was made by this audit.

## PR64: three findings require different treatment

Discussion inventory: 6 combined comments, 2 review submissions, 3 inline threads (all marked unresolved by GitHub).

1. PRRT_kwDOVCU9ks6wfFna, NifNativeMapTree504.cs collision CMP ties: **do not apply the suggested exact-equality change**. The committed acceptance.json explicitly records rejection based on pinned source and genuine original behavior; collision-completion.json records1151 whole original bytes, two actual loader comparisons, both canonical/default strict, no normalization, provider6427 before/after and cleanup. The source deliberately preserves CMP ties with a source comment. This is a reviewed suggestion contradicted by an actual compatibility protocol, not an unfixed correctness bug. Root can explain that evidence and resolve the thread after verifying it survives integration; no silent dismissal.
2. PRRT_kwDOVCU9ks6wfFne, static Get quadratic reconstruction: **fixed at64**. Exact-head Get branches on StaticTerm MapTerm and directly calls map.TryGet; only pending nodes use graph lookup. Thread is outdated but still unresolved. Committed evidence has red0/2 then finite green allocation controls and actual native comparisons. Root can resolve after checking current code.
3. PRRT_kwDOVCU9ks6wgBcx, duplicate physical flat reentry: **real at64**. Exact-head Graph504 still uses `.First(entry => ReferenceEquals(entry.Key,key))`, so two same-reference physical occurrences both receive the first value. Child65 changes it to NativeMapFlatEntries505.Read, which consumes each key occurrence from a per-identity FIFO (or retains saved PhysicalEntries). Its README binds genuine3898-byte original reentry and ordinary/reviewed strict default/deterministic matches, red0/2 -> green2/2; latest exact65 Actions independently passes the reentry step. Do not resolve this against64 alone. Carry the verified65 correction to main then re-fetch the thread/current code and resolve with that evidence.

## PR65: actual duplicate correction and native materialization

One combined bot-summary comment, zero review submissions, zero threads. Exact65 graph delegates physical flat reopening to NativeMapFlatEntries505. Saved layout uses PhysicalEntries directly; fallback groups values by reference-identity key then dequeues one value for each physical key occurrence. This addresses64's actual concern without changing source-exact CMP collision behavior. Latest CI includes both materialized-map and duplicate-reentry original comparisons. No additional actionable review item was found in this bounded audit.

## PR66: enclosing owned-invocation shutdown

Three combined comments, one review, one resolved/outdated thread PRRT_kwDOVCU9ks6wiM8w. Exact-head RequireOwnedTermShutdownCaller506 calls OwnedTermInvocation506.HasActiveFor506(this). That helper walks previousActive until null and rejects any live invocation from the runtime, addressing A->B->A.Dispose deadlock. It no longer checks only the top Active entry. No unresolved actionable review item remains in this audit.

## PR67

One combined bot-summary comment; zero review submissions/threads. Exact-head CI has all three jobs passing. No actionable discussion item was found; this does not mean the whole source diff was independently proven compatible by this audit.

## PR68: complete input custody instead of repeated descendant scans

Three combined comments, one review, one resolved/outdated thread PRRT_kwDOVCU9ks6wkK5X. Exact-head BeginConstruction514 retains the complete input once and creates a transaction admission set. PublishConstructedNode511 validates direct children against that set, then stores the actual node root with `values.Add` instead of another PublishRoot512 descendant/binary-retain traversal. On publication failure it forgets the provisional admission. ProcessTermConstruction514 builds the complete input set once and extends it by each constructed node. This removes the reviewed repeated suffix retention while preserving real input leases. No unresolved actionable review item remains in this audit.

## Dependency/merge safety

Live compare proofs: main->64 ahead2/behind0;64->65 ahead2/behind0;65->66 ahead2/behind0;66->67 ahead1/behind0;67->68 ahead2/behind0. Each parent is the exact merge-base of its child. This permits oldest-to-newest integration without losing the prepared stack. Before deleting a just-merged parent branch, retarget the next still-open PR to main or otherwise verify GitHub has done so; avoid deleting a base branch still needed by an open PR. Root owns protection/archive checks, current head rechecks, merges, resolution comments and branch cleanup.
