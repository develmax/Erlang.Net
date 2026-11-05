# Independently verified Math CI

Run [41638368916](https://github.com/develmax/Erlang.Net/actions/runs/41638368916) passed all three jobs for PR #102 head `f3c4b8ca2220592121607c5fd64f1db238313758`. All three full job logs independently confirm checkout of this exact head.

Windows x64, Ubuntu 26.04.1 x64, and macOS 26.6.2 ARM64 each passed both explicitly selected backends: 502,772 numeric/type/error comparisons, complete value/error cases, and five full-stack profiles. All six reports have Failed=0, WholeValues=true, WholeStacks=true.

All three downloaded artifact ZIP SHA-256 values match GitHub digests. All 24 oracle files passed independent receipt length/hash verification; each report oracle/receipt/math-image SHA points to its own archive files, and both backend reports use the same runtime on each platform. The summary retains runtime, terms, serialization, CoreLib, validator, original image and oracle identities.

See verified-ci-summary.json. The six production Math files, validator and capture source have no diff from prior head20e02eb9. No common source/ledger/checkpoint was changed for this independent handoff. These receipts belong to the stated head; aggregate project CI/review and main merge remain integration-owner gates.
