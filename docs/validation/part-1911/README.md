# Part 1911 full Windows OTP 29.1.1 comparison

| Equal | Different | Unsupported | SourceRejected | Seconds |
| ---: | ---: | ---: | ---: | ---: |
| 431 | 935 | 0 | 0 | 723.704 |

Four isolated workers compiled all 1,366 inputs without compilation-result caching. The complete comparison retains all 428 prior byte matches. The three new matches are diameter_session, ftp_progress and megaco_trans_sup. All 1,366 effective input profiles agree. Pinned ERTS prepares all 32 changed images, and the three exact native witnesses verify arithmetic/results, selective mailbox behavior and supervisor child restarts.

Normalized equality increases for 131 functions across 132 compared functions, with 0 losses. Frame-width distance improves for 58 functions and increases for 0. There are 21 new exact chunks and 0 chunk losses.

The 76-module comparison produced 36 Equal and 40 Different in 129.492 seconds. The 146 selected checks include 145 fast cases and one exclusive whole-module fixture. The initial linked build recorded three compiler errors because standalone Diameter fixtures returned void and the shared test catalog expects Task. The fixture return signatures were adapted without changing any assertions or production code. The production DLL was reused by its verified SHA. The original errors and retake are retained separately.

The complete managed repository gate remains open. Readiness uses the last passing managed report from Part 1875 and the explicit previous snapshot from Part 1839, at 733 registered MFAs and delta zero.

[Raw release result](full-uncached/otp-corpus.json) · [Native witnesses](native-early-records/receipt.json) · [Focused test evidence](focused-verified.json).
