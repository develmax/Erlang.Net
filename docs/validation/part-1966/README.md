# Part 1966 Windows OTP compiler validation

Part 1966 is held at the normalized-instruction acceptance gate. Part 1965 remains the last accepted source and readiness checkpoint.

| Check | Actual result |
| --- | --- |
| Scope | [Pinned source and compiler identities](source-freeze.json), [exact launch](full-launch.json) |
| Focused check | [502 passed](focused-verified.json) after preserving the existing two-argument ordering entry |
| Full uncached release | [508 Equal and 858 Different](full-uncached/otp-corpus.json) in 1599.88 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw delta | [93 changed candidate images](full-uncached/changed-images.json), zero byte-equality or section-equality transitions |
| Normalized | [41 function gains, 3 losses, 40 frames closer and 8 farther](rank-counts.json) |
| ERTS | [93 of 93 images prepared](loader-rollup.json). [155 release and 13 synthetic scenarios](linked-runtime-witnesses/receipt.json) passed. |
| Readiness | [Staged CLI result](readiness-receipt.json) uses the previous immutable managed report. The accepted readiness checkpoint remains Part 1965. |

The normalized losses affect three send payload paths. A generic entry tuple store retained an unnecessary Y slot until the outgoing send. The same mechanism widened six frames. Two more cases retain a tuple across a call to construct a list. Pinned Core and SSA schedule those tuple heads after the call.

The [paired listings](regression/disassembly.terms) show the three lost bodies with exact source lines. The [source correction](regression/narrow-entry-tuple-production.patch) requires a same-frame read after an ordinary call and preserves supported direct saves. The correction and its updated negative fixtures are staged for the next complete compiler gate.

The [first focused failure](initial-focused-failure/lane-0.stdout.log) and [final transfer rejection](source-transfer.stderr.log) are retained verbatim. The rejected compiler was held in local validation staging. Full managed validation remains blocked by the inherited original Jaro deadlines.
