# Remaining branch usefulness audit

Read-only snapshot; no refs, worktrees, source or indexes modified. No fetch, deletion, checkout, reset or cleanup performed.

## Result

The initial ancestry snapshot covers **all 40 local/remote refs** by the union of open PR77 `f7632980` and current PR76 fix `710a6848`: **zero feature commits outside that union**. No separately committed feature needs cherry-picking before the ordered merges. This is a coverage result, not a claim that all branches were already merged to main.

The final remote advertisement contains **19 heads including main**. During the audit main advanced from `30d294e5` to `873fc466`; remote native-maps-known-differences-504 advanced from `2647b738` to `4ff4482f` (the latter is already in the reviewed union). The final main object is absent locally. Fresh final-main reachability is intentionally pending the root's completed merges/fetch; this audit did not fetch or infer ancestry from branch names.

## Recommendations after ordered merges

| Branch/group | Committed usefulness | Cleanup gate |
|---|---|---|
| All 18 remote feature branches | No commits outside the current PR union | Candidate only after fresh exact-SHA final-main ancestry, closed PR and protection checks |
| local coordinated-contract-workers-520 at f1265fcc | No unique commits; 3 behind remote12893d5e | Obsolete local tip after related PR merge; fresh branch checks required |
| local native-dirty-and-boot-518 at f1265fcc | Same committed tip, no unique commits | PRIMARY is checked out and dirty: preserve active WIP/recovery before switching; never reset/delete its worktree as cleanup |
| local resource-local-references-525 at f7632980 | No extra commit beyond old PR77 head | w517fix holds uncommitted reference work; preserve WIP. Rebased PR77 can make old f763 fail ancestry later: require fresh patch/range/source review, never force-delete from today's union proof |
| local resource-bignums-524 | Rebase in progress onto710a6848 | Reserved by w518repair rebase-merge/head-name despite detached display; no cleanup until root finishes it |
| coordinated-resource-custody-514 / w504 | Covered committed history, clean worktree | Final-main branch cleanup checks still required; worktree removal is separate scope |

## Worktree custody

Counts below are porcelain entries; an untracked directory entry may contain many files.

| Worktree | Tracked changes | Untracked entries | Note |
|---|---:|---:|---|
| PRIMARY | 205 | 536 | Checked out native-dirty-and-boot-518; active source/docs/script/binary WIP |
| w517fix | 42 | 2 | Checked out resource-local-references-525; preserve active reference WIP |
| w518repair | 45 | 0 | Active rebase524, detached710a6848 |
| w504 | 0 | 0 | Checked out coordinated-resource-custody-514 |
| lc-validation | 37 | 8 | Detached historical validation; no uncovered committed branch feature |
| artifacts/ordsets372-original-validation | 23 | 0 | Detached; 1 unique validation commit; filename-too-long warnings limit status completeness |
| artifacts/part361-original-validation2 | 1 | 1 | Detached; 17 unique validation-history commits |
| artifacts/proplists-family-ci-validation1 | 2 | 0 | Detached; 1 unique validation commit |
| artifacts/proplists-stack-ci-validation1 | 2 | 0 | Detached; 22 unique validation-history commits |

The four historical validation histories contain commits outside the feature PR union. They are **not uncovered feature branches** and are not cleanup candidates. Retain them in their original directories; no copying historical docs/validation into Recovery is proposed. Raw status, HEADs, representative unique subjects and rebase metadata are retained in worktree-status-readonly.json.

## Final safeguards

1. Finish oldest-to-newest reviewed PR merges and the524 rebase; re-read exact remote SHA/CI/reviews after mutations.
2. Recompute branch reachability against actual final main. Initial union coverage cannot authorize forced deletion after history rewriting.
3. Preserve/verify active WIP recovery separately from committed branch coverage; leave historical worktrees untouched.
4. Delete only branches proven obsolete at that fresh snapshot, respecting checked-out and rebase-reserved refs. No blanket worktree removal.

Evidence: branch-coverage-readonly.json, branch-remote-final-readonly.json and worktree-status-readonly.json. All findings are snapshot-bound; parent is concurrently merging.
