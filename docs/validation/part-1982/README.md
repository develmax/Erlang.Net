# Part 1982 — Joint checked entry, frame reuse and closure forwarding

| Validation | Result |
| --- | --- |
| Selected compiler checks | [586 passed](focused-verified.json), including the two corrected fixture paths. |
| Scoped release | [253 files](pilot-receipt.json) in 538.82 seconds. The [scope result](pilot-delta.json) records 27 Equal and 226 Different. |
| Uncached Windows OTP 29.1.1 release | [Result](full-uncached/otp-corpus.json) for 1,366 files in 679.48 seconds. 512 Equal, 854 Different, 0 Unsupported and 0 SourceRejected. |
| New raw matches | None |
| Lost raw matches | None |
| Normalized bodies | [13 gains and 0 losses](rank-counts.json) |
| Native behavior | [62 observations](native-scoped/receipt.json), 187 changed images prepared and 0 shared on-load restrictions. |
| Physical frame widths | [212 earlier controls](full-widths/receipt.json) and [five recent controls](full-recent-widths/receipt.json). |
| Repository gate | [Previous integrated evidence](../part-1978/README.md). Integration of the new checked source and the repository-wide run are independent gates. |

The preliminary build was retired before starting any test. The following frozen build passed 584 of 586 checks. Its exact raw failures are preserved. The retake corrects the two synthetic inputs and includes the separately verified declared closure forwarding difference before one new selected run.

Direct allocation vectors expose a new first-frame difference in both ct_rpc functions. `call/6` moves from [5,6] to [7,5] against pinned [5,5]. `cast/5` moves from [4,5] to [6,4] against pinned [4,4]. The terminal frame moves closer and the first frame widens. Both differences are tracked for correction. [Exact register-width audit](ct-rpc-physical-widths-lf.tsv).

The current [ranked causes](ranked-causes.md) retain unique path and function identities. Only complete raw files determine byte equality. The source pin and release inputs are recorded in the [input verification](input-verification.json).
