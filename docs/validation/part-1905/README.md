# Part 1905 uncached Windows release comparison

| Equal | Different | Unsupported | SourceRejected | Seconds |
| ---: | ---: | ---: | ---: | ---: |
| 416 | 950 | 0 | 0 | 1254.516 |

The complete release comparison retains all 415 predecessor matches. Only et_viewer newly matches. Four isolated workers compiled all 1,366 pinned Windows OTP 29.1.1 sources without reusing compilation results. Effective inputs agree for all 1,366 modules. The 87-module focused release lane records 12 Equal and 75 Different in 329.985 seconds.

All 93 ordinary changed images prepare. Pinned ERTS loads crypto and zstd independently in all three versions and returns the same digest or round-trip results. The ten Et runtime observations agree on the source-built 5,264-byte image whose SHA-256 is identical to the release and linked equality result. Normalized function comparisons yield 15 gains and zero losses. Eight frame widths are closer. The mnesia_schema expand_storage_type/1 frame is one slot farther after combining the final two atom checks in a three-atom predicate. The raw witness is retained for the next change.

The initial 110-case run passed 109 tests and preserved a 43.012-second whole-module timeout. The same exact test case then passed in 26.349 seconds on the predecessor and 36.493 seconds on this candidate after the competing corpus work ended, with the original timeout. The aggregate scoped coverage file links the unchanged first report and both single-case reports. The first early corpus process copied dependencies before the producer completed and recorded the original missing Erlang.Terms errors. The separate corrected run retains all seven expected controls and changes only the time of the dependency read. Repository-wide acceptance and canonical readiness retain their earlier records.

[Raw results](full-uncached/otp-corpus.json) · [Raw section delta](full-uncached/section-delta.json) · [Native proof](native-early-records/receipt.json) · [Original focused report](focused.json) · [Covered cases](focused-verified.json) · [Frame outlier](frame-outlier-raw.terms).
