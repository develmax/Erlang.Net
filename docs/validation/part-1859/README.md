# Checked projection heaps, marker sends and flat tuple list helpers

The next local source candidate starts from the frozen Part 1858
compiler and pinned Windows OTP 29.1.1
ad05823719d77c8faee87348ea39513d4e2f99c5. No runtime MFA or
dependency changes. Four source-derived compiler paths and one
test-harness case mapping are pending compilation.

- Keep the independent nil guard before the heap check. Move the
  projection reads only when already checked and failure-dead. Prove
  the relocated initialized X root prefix from actual liveness.
- Preserve the fresh reference in X0 across outgoing message
  construction. Bind the opaque marker after construction, retain
  the reference across the send and reuse a dead argument Y slot.
  The materialized-message route retains message and reference through
  the one ordinary recipient call, then transfers the message's former
  Y slot to the recipient. Source and control witnesses track old-argument
  liveness, GC survival and the unchanged old-message receive queue.
- Build pure flat comprehension tuples after recursive tail
  evaluation, reusing the OTP recursive helper identity, liveness
  and intrinsic bad_generator behavior.
- Run the alternating lifecycle fixture using the same outer
  budget as the deep fixture, which calls the same checked runner.
  Both already have 120-second internal phase deadlines. The
  generic 15-second wrapper preempted this contract on the full
  Part 1855 candidate and in earlier exact-source controls.
  The inner phase deadlines, generated fixtures and assertions
  remain intact.

The focused manifest includes 53 unique cases and 82 release paths.
It extends the previous actual corpus selection with module-specific
witnesses and previously exact controls. Report ERTS preparation,
normalized function bodies and raw file statuses independently.
The full Part 1858 uncached gate runs in its existing isolated
stage. The next build and managed execution follow that gate.

[Paths](pilot-modules.txt) · [Cases](focused-test-names.txt) ·
[Patch identities](patch-identities.json).


## Linked results

All 53 selected tests pass out of 9,801 registered cases. The
alternating original lifecycle test now reaches its checked
outcome in 18,817 ms. Its earlier generic 15-second timeout remains
in the original full report.

The 82-image run takes 197.07 seconds and reports 12 Equal,
70 Different, 0 Unsupported and 0 SourceRejected. No file status
changes and no raw chunk gains or losses occur. Pinned ERTS
prepares all 11 changed files. Three of 22 comparable changed
functions gain normalized equality and none lose it. Nine frame
distances improve. Six newly restored, previously absent helper
keys independently have exact normalized instruction bodies and
source lines. The generated list fixture passes all eight native
values and two malformed generator stacks.

Pinned ERTS executes the actual changed erlang code purger and
socket registry images. The return values and untouched queued
message match both preceding and reference images. All four marked
functions now use the pinned frame widths and heap root counts.
Their reserve slot indices still differ. The SCTP image keeps
the four-slot frame while moving the success heap ahead of the
checked field reads, with one root for the candidate register
layout and two for the reference layout.

[Focused results](focused.json) · [Corpus](pilot.json) ·
[Instruction results](pilot-summary.terms) ·
[Register profiles](register-profiles-retake.terms) ·
[Native list output](flat-tuple-native/fixture-candidate.terms) ·
[Actual helper bodies](flat-tuple-native/release-helpers.terms) ·
[Native marker operations](marker-execution/execution.log).
