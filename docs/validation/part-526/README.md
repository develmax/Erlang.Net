# Part 526: ordered PR review and branch consolidation

This checkpoint records the user-requested review of all 15 initially open PRs
(61, 64–77), oldest first. Conversation comments, review submissions and inline
threads were fetched separately and findings checked against current code.
The archived audit observations are time-bound; the table below supersedes their
initial open/closed state, without rewriting their original findings.

| PR | Disposition at this checkpoint |
| --- | --- |
| 61 | Closed as superseded: its entire committed head already entered main through corrected PR62. One retained Python layout finding is repaired in Part526. |
| 64/65 | Before merging64, fast-forward its head to verified65, incorporating the real duplicate-flat-map reentry repair. GitHub marked65 merged; merge64 then brought the corrected aggregate into main. |
| 66–75 | Retargeted to main and merged in ascending order with exact expected head checks and successful exact-head CI. |
| 76 | Repaired legacy native source bindings and canonical fixture checkout, then merged only after CI557 completed successfully on710a6848. |
| 77 | Rebased onto the corrected parent; publishedb2632bf9 preserves all original C#/project bytes. Exact-head CI571 is the remaining merge gate when this checkpoint was authored. |

The CMP-to-EQ suggestion in64 was rejected using the pinned genuine OTP
collision observation; changing the behavior would have introduced a discrepancy.
The indexed lookup, duplicate-key materialization, nested shutdown, quadratic
construction-custody and Python layout comments were checked against their actual
fixes. Stale or incomplete automatic review summaries were not represented as
fresh approvals: current-source bounded reviews were recorded separately for
70–72 and metadata-only follow-ups76/77.

## Final retained review correction

Expand three compressed statement/control-flow bodies in
`tools/Native/Erlang.NifMapCreation501Observations/build-native.py`.
Python AST before/after is identical. Refresh its direct BuildScriptSha256,
the collector workflow plan pin and the two affected downstream source bindings.
No C#, C fixture, golden bytes, API registration or contract classification changes.
All eight passive source checks and all10341 declared rows pass. Fresh genuine OTP map501 matches both ordinary/reviewed loaders in2/2 whole raw comparisons; all6427 provider files and physical cleanup checks pass.

The first local passive attempt exposed four stale CRLF working-tree copies in
the reused audit checkout. Writing their exact already-approved Git blobs under
the existing -text attributes restored the canonical checkout; no fixture hash
or committed fixture content was changed. The initial failure is retained.

## Branch and work-in-progress preservation

Before deleting a remote branch, fetch its actual SHA and require ancestry in
current origin/main; use an expected-SHA lease and atomic deletion. Already
merged remote branches removed at this checkpoint:17 of18 initially present.
The remaining Part524 branch is retained until its final merge. Historical
detached validation worktrees are preserved.

Before local cleanup, the primary dirty checkout was detached at the same HEAD;
its diff remained unchanged. Its committed head and17 other old local feature
heads were already ancestors of main and their branch names were removed.
The dirty Reference525 checkout and its unmerged historical Part524 head are
retained until Part524 integration. No uncommitted feature work is folded into
these merges.

A compact source recovery copy remains outside Git at
`../recovery/pr-consolidation-source-20261028`: primary387files,
Reference52540files and Part524repair45files, with raw files, SHA manifests,
working-tree and index binary patches. Original working directories and ignored
artifacts remain in place. LICENSE.local.txt remains excluded.

## Scope

API/full-contract deltas0/0:708 registered,180 recorded full contracts,
504 pending and24 known differences. This is review/integration work and does
not close the unfinished reference/map producer work or establish whole OTP
compatibility. Final merged-head CI and final branch inventory must be checked
after the remaining merges; historical successful runs do not substitute for it.

## Final informational metadata correction

Independent final77 review verifies all11506 C#/project/build entries are identical and identifies18 stale informational dependency sizes across the three legacy plans. Correct only Bytes according to each declared TextLF sequence, keeping the already-verified SHA/source unchanged. Refresh workflow pins, the current infrastructure evidence binding and downstream plan hashes. All8 passive checks and10341 downstream rows pass on final files; before/after proof is retained in local-checks/informational-sizes.json. These are informational metadata corrections, not new runtime behavior.
