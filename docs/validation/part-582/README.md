# Part582 — Reconcile file PR110 with main

Merge main `91c05943d` (code PR109) into file PR110, whose previous head is
`dfe2836cc`. Keep the Code startup stage at its published ordinal and append
File; keep both conditional registration paths and their separate library
assemblies. Neither backend becomes the default because of this merge.

Main already owns changelog parts578/579. File's original evidence and entries
are retained under [Part580](../part-580/README.md) and
[Part581](../part-581/README.md), with Markdown links redirected. Raw reports
retain their original contents and recorded source heads. Main's code evidence,
entries and source freezes are preserved.

Resolve metadata conflicts from current main, admit file/Unicode/code source
files within each broad plan's existing scope, then refresh current source
hashes, dependent producer identities and workflow pins. The exact additions
are in `membership-audit.json`; the refresh helper and audit are under
`source-plan-refresh/`. No native execution/approval state, evidence
classification, required workflow or historical frozen receipt is promoted.

Validation is limited to the connected file/code package: test project and file
validator builds, the five file host cases and seventeen code/loader cases,
both file 408-observation replays, both literal audits, passive source-plan and
MFA/native bindings, reviewed source membership,22 CI helper tests and document
layout. Reports under `checks/` record the actual results. Full local regression
and manual workflow dispatch remain unrun. Hosted checks on the final PR head
remain required; prior-head successes do not prove the merged source.

The five complete file filesystem cycles remain open provider/native gates as
recorded in Part580. Resolving these conflicts does not establish100% OTP
compatibility and does not authorize merging the draft PR into main.

Actual local results: both builds completed with zero warnings/errors;22/22 selected harness cases passed; both file backends matched408/408 fresh OTP observations with no skips; all13 passive plans,5400 raw bindings,4279 Resource529 dependencies and exact4950-row reviewed membership passed. Stale-source control rejected the expected stage hash. Both literal audits and22 CI helper unit tests passed.
