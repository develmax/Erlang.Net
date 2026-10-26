# Original Linux fixture failure: dirty IO observation self-block

This packet does not edit active tools, runtime, 505 files, branches, or CI.
No compiler, VM, native fixture, git mutation, or CI dispatch was executed.

## Verified failed evidence

Run `40414718764`, job `121894025787`; preserved archive SHA256:
`D875A00F11B070BB4EF2501B9CE406F5D34A20C4E42BE4637CC096F2113F4E42`.
`failed-original-evidence.json` decodes selected entries directly from that
archive without extracting or executing binaries. The original remains failed.

All three native cases captured the intended initial held state correctly:
dirty CPU/IO queued counts zero; dirty CPU/IO active counts one each. Every case
then raised `state_not_reached` at the queued barrier (original Erlang line64).
VM elapsed times are 24.1379, 24.1389, and 24.1158 seconds. Exit code is1;
processes/streams were reaped/closed without forced termination or cleanup error.
Source build, debug flag, and ordinary normal-queue observations succeeded.

## Confirmed source dependency and resulting diagnosis

The old `sample(held)` writes `held.etf` synchronously before spawning the queued
children. However the experiment has deliberately occupied its sole dirty IO
worker. Pinned `erts/emulator/nifs/common/prim_file_nif.c` lines179,180,182 register
`open_nif`, `close_nif`, and `write_nif` with `ERL_NIF_DIRTY_JOB_IO_BOUND`.
The observation's file operation therefore waits behind its held IO body.
The body exits only at the twenty-second deadline; the saved held counters were
sampled before that wait. After the write finally completes, the test creates
the queued children, but the original stable held state has already expired.
The following 2000 one-millisecond timer retries add roughly four seconds.
This accounts for the measured duration and missing queued stage in all cases.

The actual queued poll values were not recorded in the failed experiment, so no
post-timeout queue shape is invented here. `erts_run_queues_len` itself reads
real queue lengths and adds actual active dirty workers only for active-task
selectors. Nothing in this failure disproves that source accounting.

## Prepared correction and one measurable experiment

`resource-io-defer.patch` changes only the resource collector, fixture C/Erlang,
and README. Stage observations remain in guest memory until native release;
the enclosing runner flushes actual terms afterward, preserving the original
error stack if collection failed. The 32 latest actual barrier polls contain
timestamps, real queue pairs, entered/completed counts, timeout counts, release
state and release-call count. Children send pre-NIF admission/current-function
messages to distinguish ordinary scheduling from actual native entry without
blocking cross-process introspection. Readiness waits are separately bounded.

The collector decodes diagnostics even on a raised outcome and reads only the
declared stage files as ten-key samples. It does not mistake diagnostics for a
stage or reduce any source assertions. Source-control Python parsing passed;
C and Erlang compilation/execution still belong to the parent.

The strengthened native guard requires genuine held and queued polls with both
timeout counters zero. Every retained poll must have zero deadline expirations.
Final cleanup must show released=1, release_calls>=1, empty queues, and CPU/IO
completed counts individually equal their actual entered counts. Queued
cancellation is not forced to enter two bodies. The fixture records a bounded
physical cleanup wait; a failure is retained without replacing an earlier
original exception. All cases still require normal owned VM/process cleanup.
Pure controls passed: one valid unequal CPU/IO entry-total example and18
rejected mutations for timeout, release, missing poll, unfinished/over-counted
body and final counter disagreement. These are source-control tests only,
not executed OTP observations.

Parent: apply the patch on the dedicated 504 fix branch; run the existing
resource `seal-plan.py` and confirm the proposed `source-plan.json` hash; update
only that branch's CI plan seal. Run the same five independent original cases
with the same one CPU/IO profile. Expected measurable change: all native cases
reach the queued stage while timeout counters remain zero, rather than waiting
for the twenty-second native bound. Require original five-case typed controls
and cleanup to pass; compare actual elapsed durations with the preserved failed
run. A new failure must retain its polls and native counters, not be relabelled.
