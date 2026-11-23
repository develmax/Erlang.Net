# Part 1910 full Windows OTP 29.1.1 comparison

| Equal | Different | Unsupported | SourceRejected | Seconds |
| ---: | ---: | ---: | ---: | ---: |
| 428 | 938 | 0 | 0 | 772.626 |

Four isolated workers compiled all 1,366 inputs without compilation-result caching. The complete comparison retains all 426 prior byte matches. The two new matches are wxAcceleratorTable and megaco_tcp_accept. All 1,366 effective input profiles agree. Pinned ERTS prepares all 71 changed images, and the two exact native witnesses verify local wx command behavior and the controlled Megaco receive/cleanup path.

Normalized equality increases for 147 functions across 236 compared functions, with 0 losses. Frame-width distance improves for 12 functions and increases for 0. There are 11 new exact chunks and 0 chunk losses.

The 138-module comparison produced 28 Equal and 110 Different in 485.233 seconds. The final 135 selected tests include 134 fast cases and one exclusive whole-module fixture. The first detached fixture ran from the staging directory and could not find its repository-relative example. The retake fixed its working directory and reused the compiler and test binaries. The original failure and first interrupted short comparison remain in separate files. The linked patch set drops unrelated formatting hunks in two BER input patches. Their algorithmic hunks and tests are preserved.

The complete managed repository gate remains open. Readiness uses the last passing managed report from Part 1875 and the explicit previous snapshot from Part 1839, at 733 registered MFAs and delta zero.

[Raw release result](full-uncached/otp-corpus.json) · [Native witnesses](native-early-records/receipt.json) · [Focused test evidence](focused-verified.json).
