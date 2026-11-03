# Part 557 correction: native child rollback and exact Windows source transport

This correction follows published head `1a60b9eadc16e438d4574009f0d8ff2636260345`. The original proof and failed receipts remain unchanged. No new API or full-contract promotion is claimed (delta 0 / 0).

## Runtime correction

The PR review identified a real close waiter that never completed when a child reservation was aborted before preparation. An actual creator/reservation/Close path reproduced the old timeout; the owned processes still cleaned up. The first fence-only correction exposed a second real defect: a reason borrow survived rollback. The same compiled stronger test failed on that intermediate package, then passed with common `FinishStopped` cleanup. The rollback now releases the real reason borrow and settles both logical/physical fences without fabricating a native-port closure certificate. The added eighth permanent scenario is a component test; it does not claim native-child-create wire parity.

## Final local results

- Debug and Release: 8 / 8 genuine native-original/component cases per profile. Raw observations, original/own ETFs and process cleanup receipts are copied unchanged.
- Debug and Release: 101 / 101 selected affected regressions per profile, with runtime input stability. The full 9,601-case catalog was listed, not run locally.
- Debug and Release final normal builds passed with equal 13,904-input before/after maps. Compact build summaries are explicitly derived and bind to the full original review hash. The incremental proof inherits 13,875 unchanged raw Git blobs from the accepted original proof and directly checks all 29 changed/new inputs (two additions); it is not normalized or semantic source equivalence.
- Actual Git dependency checkout: 16,993 files with `core.autocrlf=true`; all 461 raw snapshot rows and 81 native inputs matched. The exact workflow admission blocks passed all 13 focused checks plus the combined MFA/native and resource checks (4,750 / 4,139 rows). Admission scripts do not execute SDK builds or a product VM.

## Windows transport correction

The old CI head passed Linux/native jobs but Windows rejected a source binding before SDK execution. Finite changed-path checking had missed a raw consumer and an unchanged builder dependency. Four exact root-anchored `-text` rules preserve raw bytes for the observer project, async wrapper, catalog wrapper and native builder. The current 461-row monitor snapshot and every dependent metadata/workflow pin were resealed without relaxing hash modes. Earlier failed checkout/admission records remain historical evidence.

## Acceptance boundary

Source candidate `aece6fb81965f49f494ee920a4360a5cb2fabb4a` is bound by these receipts. A later commit may add only documentation/evidence without changing that source candidate. Corrected exact-head GitHub CI, fresh review and merge are still pending at packet creation. Earlier-head successful jobs are not successor acceptance. Whole `statistics` compatibility, pending authenticated RPC cancellation and default polling remain open. Forced compiler ancillary cleanup remains explicitly reported separately from ordinary no-forced-cleanup native/original execution.

The immutable 1418/1419 receipt `Scope` strings contain stale references to planned stage 1363/1365 and packages 1320/1335. Their actual commands/results are the final 101-case Debug/Release runs bound to builds 1416/1417 and the final runtime hashes. Those raw strings are preserved; this report corrects their interpretation. Build-receipt Scope strings also predate the subsequently completed checkout admissions and affected runs.
