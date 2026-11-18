# Part 1836 — Reuse dead registers in sibling leaf constructors

The existing construction planner can now emit pure sibling tuples
and lists directly when the same X register can hold the last-read
operand and the nested result. Shared caller inputs remain live.

The 24-module sample retained 7 Equal and 17 Different, with zero
Unsupported, zero SourceRejected and no new or lost whole-file
matches. Two changed images passed ERTS preparation. The complete
`snmp_app_sup:sup_spec/3` instruction sequence now matches OTP.
Only `Code` and its checksum-dependent `Attr` changed in the two
sampled files. All 15 named tests passed. The sample took 21.39 seconds.

The first full launch stopped in preflight before starting any
workers because the isolated-test wrapper emitted non-record values
alongside its three true receipts. The linked candidate reads the
validated structured receipts. The next candidate also corrects the
separate SSL tail-move ordering loss identified in the full Part 1833
normalized pass. [Launch error](full-queue.stderr.log).

[Timing](cohort.json.timings.json) · [Changes](changed-images.json) ·
[Tests](focused.json) · [Readiness scope](../../module-readiness-scope.json).
