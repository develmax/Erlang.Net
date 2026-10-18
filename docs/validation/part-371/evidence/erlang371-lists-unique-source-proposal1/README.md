# Part371 lists unique-sort entry and source merge continuation

Artifact-only C# source adaptation of pinned OTP-29.1.1
ad05823719d77c8faee87348ea39513d4e2f99c5 `lib/stdlib/src/lists.erl`.
Root owns Main copying, styling, builds, actual SDK capture, comparisons,
source guard/status changes and publication. No build/network/Main write was
performed by this helper.

The production proposal ports all84 clauses of19 native source helper states:
`umergel/1,3`, `rumergel/3`, forward/reverse two-way unique merge, and all
forward/reverse three-way selector helpers. `generate.py` parses only this
fixed source fragment and emits ordinary C# switch/pattern/guard code. No
Erlang parser, interpreter, BEAM execution, source table interpreter or new
dependency is introduced into production. `parsed-source-clauses.json` retains
every clause and its exact source line for independent review.

The runtime loop performs tail transfers by state/argument replacement. True
non-tail nested merge/reverse expressions use an explicit continuation stack;
on return they prepend the actual result to the original accumulator and
resume the source outer state. Effective residual arguments and failing
helper first-clause lines are retained. Pending caller frames use the actual
source child-call line (including3306), not the public API line. Stable
continuation identities permit the existing source stack capture policy to
coalesce identical native continuation addresses. This requires actual final
full-stack comparison; no fixture-specific error rewrite is included.

Every pinned `lists:reverse/2` expression calls the actual current
ModuleRegistry entry under only the genuine pending continuations. The current
tail helper frame is absent, matching source tail calls. Scope disposal on
success/failure/cancellation restores the calling context. Scheduler charging
occurs once per source state transfer; no identical reduction timing claim is
made. The asc/desc atoms are immutable local cached values.

`ListMergeOperations.MergeMany` routes only unique merge to this source
machine. Ordinary merge, public custom comparator paths and existing two-way
API registrations remain unchanged. The machine's internal two/three-way
algorithms are the pinned source versions rather than generic repeated merge.

`ListSortOperations.Unique` applies exactly the public usort entry shape:
Nil, proper singleton, or at least two Cons cells match the source clauses.
Nonlist and a single improper Cons fail with original argument and line2075.
The existing proper-input sorting implementation remains unchanged. Broader
`usort_1`/`usplit_*` malformed-tail source stacks and mathematical comparer
closure are not declared complete by this narrow public-entry fix.

The ListModuleRegistrations candidate changes only the usort registration
block and preserves Root's existing `zf -> LegacyFilterMap` change byte for
byte. Its source hash is guarded by the copy plan. Do not overwrite a newer
registry version; apply the precise before/after block if Root adds another
independent change.

The permanent HOST compiles a real Erlang fixture: three-way union, multiple
merge passes over64 sorted runs, first-list numeric representative selection,
usort original argument error, umergel/3 residual error, and umerge2_1/5
residual arguments with the3306 pending caller. Append registration before the
catalog snapshot, preserving every prior test name:

    Test(Erlang.Tests.ListUniqueSourceHostNames.TestName, Erlang.Tests.ListUniqueSourceHostChecks.Run);

Add the embedded resource
`Resources/Erlang371/lists_unique_source_contract.erl`. Root must run style,
build, this HOST, existing affected lists/ordsets regressions, and final actual
whole-field original comparisons on the final DLL closure. Existing actual
original witness is the admitted full21 run39113248779; this artifact claims
no new execution and no automatic complete-contract promotion.

Central attribution: retain the existing OTP license/port register and record
this exact source function fragment. No per-file license headers were added.
