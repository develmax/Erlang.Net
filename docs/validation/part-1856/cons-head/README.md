# Deferred cons head scoped verification

Part 1855 baseline compiler SHA-256 B6C017C1AFEAFC788C0BFA407A366C8CD1E4082D558EE02B0218C0106A499609. Candidate SHA-256 1F24B1E505042A7F2C2E5F712C17E97840B4DE8001CA7EB393D7BBF240A22D25. Patch SHA-256 B3EE4FA1CD19873114238AE6A23C7315DCF27C38262673174AED1CCE61B19515. OTP-29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5.

| Metric | Before | After |
|---|---:|---:|
| Equal modules | 2 | 2 |
| Different modules | 6 | 6 |
| Unsupported modules | 0 | 0 |
| SourceRejected modules | 0 | 0 |
| Corpus comparison time | 43.33 s | 40.17 s |

One-worker uncached runs. Start-to-finish managed duration 177.93 s, including restore, two builds, two fixture checks and both release runs. Host activity varied; these timings establish scope cost. Raw timing and SHA receipts are retained beside this report.

Only tprof changed release bytes, in Attr, Code, and FunT. The normalized body for '-enable_pattern/6-fun-5-'/3 now matches the pinned release body. The operand, live root and line evidence is in the three tprof terms. Other normalized bodies stayed unchanged. The FunT identity shape (entry/name/arity/free count) stayed unchanged. Two whole-module matches stayed equal.

The focused compiler and test builds produced zero warnings and errors. Baseline failed the intended drop/3 schedule assertion; candidate passed all four instruction schedule assertions and eight interpreted outcomes. The original failure output is in mini-before.log. Independent ERTS runs loaded and executed both fixture images. Each matched eight pinned outcomes, and subtraction failure retained badarg and its source line 8. ERTS prepared the sole changed release image. Both tprof variants loaded and passed the traced drop/3, repeated/3, drop/3 update using a real trace session; the resulting trace map contains one entry per traced function in the pinned order.

Broader corpus and repository integration gates are pending the root package owner. No remote operations or reference checkout writes.
