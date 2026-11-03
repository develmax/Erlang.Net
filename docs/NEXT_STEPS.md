# Next steps

## Executable queue

- Complete the frozen Part569 CI repair acceptance and merge after current mandatory checks/review. [Actual evidence](validation/part-569/ci-integrated-pins-and-reuse/hosted-results/README.md) separates passed9687 units/native cohort/async23 from the still-failed original main pipeline; do not retry unchanged or promote whole-module status.

- Validate the combined PR102/main0386ff27 source package, including the accepted [native async/cancellation contracts](validation/part-557/REPORT.md). Keep the [incoming next-step history](history/main-103-next-steps/index.md), existing library scope and unresolved public statistics contracts. The separate persistent-poll owner retains shared runtime/checklist ownership; do not start a competing full run on the PC.

- Follow [integration evidence](validation/integration-20261103/README.md) and [PR #102](https://github.com/develmax/Erlang.Net/pull/102) for the exact source checkpoint and delivery state. Continue the module acceptance tasks below after its recorded merge gates pass; preserve later isolated proposals until their own acceptance.

- Close the [ordsets acceptance gates](ORDSETS_COMPATIBILITY.md): shared term ordering/equality and `lists` helper closure, followed by complete regression and readiness publication. Preserve the exact OTP corpus and repaired native reverse source frames.

- Complete the [math backend acceptance gates](math-backends.md): platform numerical audit and complete regression/readiness publication. The 26 exports must not be presented as 100% semantic compatibility.

1. Read the [remaining-work index](remaining-work/INDEX.md), its linked checkpoint and the latest [changelog volume](../CHANGELOG.md). Resolve their recorded scope against the actual source and current task before implementation.
2. Select the next bounded task from that queue; retain its acceptance criteria, unresolved limitations and original-OTP comparison requirements.
3. Run checks appropriate to that task and record actual results and unrun gates in the changelog and owning plan.

The documentation split does not authorize publication, merge or a new runtime work part. Recorded historical CI and local results are evidence for their original revisions.

## Historical plans

All previous plans, including unfinished scope and handoffs, remain in [ordered historical volumes](history/next-steps/index.md). They preserve the source order and must be interpreted in their recorded checkpoint context. The old entry page started at Part485; consult the maintained queue for later work.

## Maintenance

Keep only current priorities and links here. Maintain detailed plans in remaining-work/, and record completed steps in the active changelog volume.
