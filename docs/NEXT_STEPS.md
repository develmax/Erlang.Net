# Next steps

Part592: real multi-scheduling closes the selected erts_internal helper gate. Both backends pass262 complete observations and45 scenarios, plus10 physical and36 connected groups. The original Erlang/BEAM is retained. Native debug node/dist inventory and broader certification remain open; no universal100% claim or full regression/readiness refresh. [Backend contract](erts-internal-backends.md), [receipt](validation/part-592/README.md).

## Executable queue

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
