# Part 1990 local compiler checkpoint

The frozen baseline is Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. These runs used SDK 10.0.400 and runtime SHA-256 7445A7EA8A055C952841AD41A2F4F0138B7FE39EAB6762EB1030E59308A1C8AA. The [source transfer](source-transfer.json) binds the changed compiler inputs and permanent regressions to the last built source. The official checkout and release images were read only.

The [observed ledger](observed-status.json) accumulates 539 Equal, 827 Different, 0 Unsupported and 0 SourceRejected. An earlier compiler measured 513 Equal and 853 Different in the last full uncached run. These narrower uncached runs each produced zero new exact BEAMs, zero Unsupported and zero SourceRejected.

| Scoped run | Different files | Changed images prepared by OTP | Normalized bodies gained / lost | Corpus elapsed |
| --- | ---: | ---: | ---: | ---: |
| [p128-core-cons-construction-order](p128-core-cons-construction-order/status.json) | 24 | 5 | 2 / 0 | 74.249 s |
| [p129-shared-core-construction-entry](p129-shared-core-construction-entry/status.json) | 32 | 7 | 7 / 0 | 105.171 s |
| [p130-structural-success-atoms](p130-structural-success-atoms/status.json) | 44 | 3 | 1 / 0 | 185.407 s |
| [p131-shared-local-tail-successors](p131-shared-local-tail-successors/status.json) | 44 | 2 | 2 / 0 | 190.453 s |
| [p132-per-call-construction-regions](p132-per-call-construction-regions/status.json) | 28 | 11 | 7 / 1 | 76.999 s |
| [p133-outgoing-registers-and-dead-y-prefix](p133-outgoing-registers-and-dead-y-prefix/status.json) | 28 | 9 | 7 / 0 | 72.976 s |
| [p134-live-call-construction-roots](p134-live-call-construction-roots/status.json) | 28 | 3 | 1 / 0 | 78.201 s |

Normalized bodies and native execution are separate from byte equality. Part 2129 recompiles the eight changed paths on the immediate parent. Two apparent SSH regressions predated the constructor change. Part 2131 restores both with shared local tail transfers. Part 2132 lost ASN.1 open_hrl/2 because its last outgoing Y slot was initialized instead of removed. Part 2133 restores the dead-slot prefix trim. Part 2134 makes all normalized instructions and source locations in ct_snmp:manager_register/2 match and leaves one physical copy-order difference in lcnt:toggle_category/2.

Three focused managed tests and 79 paired pinned-native fixture observations passed on the final checkpoint. The paired output has SHA-256 7A913F276F9C044B2A2FD0E587F4B804CB7562F9B3F378871C0DDB2B4631FE99. Released BEAM execution checks covered two EDoc encodings, real Diameter forms, ASN.1 enumeration, SSH algorithm methods and debug setup, syntax-tool parsing, and both HTTP client manager startup paths.

Complete repository regression, fresh 1,366-image acceptance, readiness regeneration and the package commit remain pending.
