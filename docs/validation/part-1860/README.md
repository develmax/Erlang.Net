# Outgoing tuple projections, arity-selected frame placement and list field lifetimes

The source package starts from the frozen Part 1859 compiler
4fab0d567e6403a4ebb7bfd0e58dd6b7071d239310ad630a1f852813765a9f40.
The baseline stays pinned to Windows OTP 29.1.1 at
ad05823719d77c8faee87348ea39513d4e2f99c5. The last whole-file
checkpoint is Part 1858 with 409 Equal, 957 Different, 0
Unsupported and 0 SourceRejected.

- Restore the outgoing X-register projections of the retained tuple
  producer. The release witnesses are ct_doctest:clean_stacktrace_item/1
  and test_server:rewrite_loc_item/1. Require precisely one outgoing
  use of the projected value and no read of the destination's old
  value. Regression controls cover direct X1 and X2, repeated uses and
  argument exchange.
- Prove the selected arity and the uniquely owned allocation before
  moving a literal first-field tag check into the unframed arm.
  Keep cleanup and frame semantics for other predecessors. Sixteen
  of twenty-one native paired windows across twelve modules have the
  observed raw cleanup pattern. The declared selection covers all
  twelve modules, including custom failure negatives.
- Retain the tested tuple through the recursive list helper and
  defer extracting its selected field. Permit the covered checked
  record-source getter without relaxing nested or anonymous helper
  ordering. Pinned no_ssa_opt_sink confirms the late field projection.
  Eight modules contain projected or existing helper, fallback and
  byte-exact controls.

The 55 selected tests cover a 9,803-case candidate catalog. Ninety-eight
release paths cover the previous linked cohort, independently matched
census entries and both normalized regressions. The linked candidate and native fixture results below are complete. No runtime MFA or external dependency is new.

[Paths](pilot-modules.txt) · [Cases](focused-test-names.txt) ·
[Patch identities](patch-identities.json) ·
[Outgoing projection source proof](outgoing-projection-source-proof.md) ·
[Arity-frame source proof](arity-tag-frame-source-proof.md) ·
[List rebase proof](tuple-list-rebase-receipt.json).


## Linked candidate results

All 55 focused cases pass. The uncached 98-image selection takes
456.62 seconds and reports 12 Equal, 86 Different, 0 Unsupported and
0 SourceRejected. It retains the status of every selected Part 1858
path and all 82 overlapping Part 1859 paths. No raw chunk equality
changes. The scope includes the large erl_parse negative witness.

All 25 changed files prepare in pinned ERTS. Twenty-seven of 54
comparable functions gain normalized equality, including both
Part 1858 projection losses. Nine frame distances improve and none
increase. A separate stricter helper comparison finds eight exact
label-normalized instruction and source-line bodies among the twelve
prepared witnesses. That includes a previously absent code_server
helper. The Mnesia and cover owner-scope negatives remain absent.

All twelve release modules retain the 21 paired arity windows and
sixteen clean failure shapes. The new entry movement passes the exact
binary regression and native fixture without a demonstrated release
gain in this group. Further source eligibility diagnosis is pending.

Three actual generated fixture images execute successfully in
pinned ERTS. Their controls preserve returned terms, original
function_clause argument, badrecord, malformed generator and effect
ordering. An initial diagnostic script had an invalid stack
pattern in its Erlang eval. The raw error and corrected run remain
retained.

The new full uncached comparison is active in four isolated groups
with the same partition manifest used for Part 1858. Build, Tooling
and whole managed regression remain pending on this compiler.

[Focused cases](focused.json) · [Corpus](pilot.json) ·
[Normalized delta](pilot-counts.terms) · [Restored functions](regression-normalized.terms)
· [Exact helper diagnostics](projected-helpers.terms) ·
[Generated fixture native output](native-fixtures/) ·
[Full launch](full-launch.json).

## Full uncached release comparison

All 1,366 installed paths retain their Part 1858 whole-file status:
409 Equal, 957 Different, 0 Unsupported and 0 SourceRejected.
There are 82 byte-changing generated files and all 82 prepare in
pinned ERTS. Seventy-nine of 140 changed comparable function bodies
gain normalized equality, including the two restored outgoing
projections. None lose it. Eleven frame distances improve and none
move farther. No complete raw chunk gains or losses occur.

The four equal-coverage partitions and effective build profiles
are identical. The run takes 604.36 seconds against 556.72 seconds
on the previous frozen compiler, an observed 8.56 percent increase.
The median per-image duration ratio is 1.0985 for the 1,284
unchanged images and 1.0294 for the 82 changed images. One pass
cannot separate source cost from host timing. Keep the pairwise
measurements for an interleaved isolated comparison.

The standard solution build and Tooling finish successfully in
277.24 and 323.46 seconds respectively. The complete managed
regression passes 9,802 of 9,803 cases in 3,131.88 seconds. Its
single failure is the original Jaro fixture's established host
command timeout, with 10,793 ms total case time. The corrected
alternating lifecycle test passes in 12,388 ms in this full run.
The Jaro command and cleanup deadlines remain as specified by
the earlier watchdog contract. The five selected runtime
assemblies have identical SHA-256 on both Part 1855 and 1860.
Canonical readiness retains the previous completely passed report.

[Complete report](repository-full.json.gz) · [Raw stdout](repository-full.json.stdout.log.gz)
· [Raw stderr](repository-full.json.stderr.log.gz) · [Receipt and failure](repository-full.receipt.json)
· [Runtime identities](jaro-runtime-hashes.json).

[Classifications](full-otp-corpus.json) · [Timing comparison](full-timing-comparison.json)
· [Instruction gains](full-normalized-counts.terms) · [Native preparation](full-loader-summary.terms)
· [Effective profiles](profile-equivalence.json).

The isolated follow-up alternates the exact Part 1855 and
Part 1860 test assemblies on the same workspace root with the
same runtime libraries and original timer. It runs after the
complete managed process ends. All four partial invocations pass
in the order previous, candidate, candidate, previous. Their
total case times are 10,228, 9,930, 9,554 and 10,581 ms. Case
time includes setup and cleanup outside the command's watchdog.
This shows that the original command can succeed with the
unchanged thresholds. The complete regression's failure remains
its acceptance result. [Isolated reports](isolated-jaro/summary.json).
