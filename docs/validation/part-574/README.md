# Part574: branch consolidation and Linux poll integration

Status: prepared; final-head CI, original Linux replay, review and merge remain gates.

The base is `381868fee71e09c31bf14a21c7dc401e254c466c` (main after PR105).
All 19 selected runtime files come byte-for-byte from `3da2bc7aa096de36b9bbc7c2f3b848333835cb35`.
All existing base files they replace are unchanged since that experiment's base; no string or ordsets changes are overwritten.
The selected runtime moves readiness of borrowed Linux descriptors onto one real per-VM poll thread and joins it before closing owned wake descriptors.
The permanent collectors replace the temporary allocation-workflow override.
The parser accepts the actual OTP poll identity zero while retaining duplicate and other row checks.

## Branch decisions

- Five remote diagnostic branches and their local refs were deleted with exact SHA leases after checking no open PRs or running workflows. Complete raw patches remain under `retired-branches/`; those workflows are historical and must not replace active workflows.
- Seven already merged local refs were deleted. Five clean attached checkouts were detached at the same SHA; files and ignored evidence remain.
- The redundant pre-integration backup ref was deleted after comparing its functional tree with accepted ancestor92ea9036. Source/tests were identical except the accepted completed capture script and added README. Its deleted historical snapshots remain recoverable from the verified local bundle.
- Two already merged refs in the standalone Math clone were deleted. Its dirty current Math branch remains untouched.
- Three dirty common-repository checkouts remain: integration/all-local-docs-20261103, feature/nif-options-543 and feature/gc-record-roots-549. Their uncommitted work is not discarded or silently merged.
- The NIF initialization tool and compiled binary_part/2 fix are already in main. No duplicate source implementation is imported. The useful original-only NIF route is restored as a separate manual workflow, preserving ordinary N1 behavior.
- The final poll branch and NIF diagnostic branch remain until their selected content is accepted in main.

Recovery: local `artifacts/branch-cleanup-20261104/branch-recovery.bundle`, 8286615 bytes, SHA256 `292426E8FB66C4D687A3024A495935EE775F75A2E743235FA6F7C1315BA1D026`; `git bundle verify` succeeded. Its prerequisites are retained main ancestors. The bundle is not a backup of uncommitted or ignored files; all original worktrees remain in place.

## Actual validation

- Release Core and Release test project builds passed, zero warnings/errors.
- Selected regression: original report63/68; four port cases passed after preparing Debug PortProbe and permitted child-process launch. The remaining regex initializer case requires the absent official admission; no passing result is claimed for it. Reports retain original failures and the four-case rerun.
- Changed C# file layout check passed with zero changes. The initial whole-export invocation reached a missing unexported benchmarks directory and is not claimed as a complete repository style run.
- Historical Linux evidence is54/54 total across Debug and Release at3da2, with stable source/provider and joined shutdown. It is not fresh acceptance of this integration.
- API presence delta0, full-contract status delta0. Source-plan rebinding is not compatibility evidence.

## Final acceptance checklist

- [x] Fresh branch/PR/worktree inventory and recovery bundle.
- [x] Select final poll source and preserve current main changes.
- [x] Restore useful NIF workflow without ordinary-loader changes.
- [x] Local build, scoped layout and affected lifecycle checks, retaining missing prerequisites.
- [ ] Exact-head mandatory CI, including the new54-case Linux job.
- [ ] Completed final-head review, comments and inline findings triaged.
- [ ] Merge accepted PR into main and verify main result.
- [ ] Delete remaining redundant final poll/NIF/PR refs with fresh SHA checks.

Fresh integrated parser controls:18/18 passed on CLR10.0.11, built against the current integrated Release runtime DLLs. The tested helper and names are the exact proposed source, not copied implementations.

## Exact CI preflight repair

Run41754147982 on e628 failed before builds: pending-plan file length for Resource529 was stale although its hash was current, and the monitor freeze still named pre-poll Halt source. The raw failure artifact was downloaded with matching SHA256 and ZIP CRC. Fix size propagation when hashes were already substituted; create a Part574 monitor successor with the five changed rows and16 additional closure files, preserving the prior immutable snapshot. Execute the exact CI inline preflight locally:13/13 PASS. API statuses stay unchanged.
