# Part 1904 Windows OTP 29.1.1 release comparison

| Equal | Different | Unsupported | SourceRejected | Seconds |
| ---: | ---: | ---: | ---: | ---: |
| 415 | 951 | 0 | 0 | 965.045 |

The four isolated workers compiled all 1,366 modules without caching compilation results. All 412 preceding byte matches are retained. The new complete matches are DSS, PKCS-3 and megaco_user_default. Effective build inputs are unchanged for all 1,366 images. All three complete matches were prepared and exercised in native ERTS through source-built images with the same SHA-256. All 18 native release observations agree, including malformed decoder inputs. The 31 changed images all prepared in the pinned runtime. Strong record validation reports zero uninitialized update registers across 20 modules.

The source-derived binary fast paths, terminal case fold, checked tuple aliases, branch-owned record frames and checked decoder continuations yield 308 normalized matching functions and 258 smaller frames in the changed function cohort. No normalized function or raw chunk match is lost. Raw gains by section are AtU8 3, Attr 3, Code 3, ExpT 3, ImpT 4, LitT 1 and LocT 2. The 58-module pilot took 239.414 seconds and produced 9 Equal and 49 Different. All 108 focused cases passed. The updated opcode rank covers 951 different images across 28 partitions.

The repository-wide Part 1898 runtime report still records the previously reproduced original Jaro worker timeout. This part establishes compiler scope and the release byte count. The last green complete repository readiness report is Part 1875.

[Raw report](full-uncached/otp-corpus.json) · [Status delta](full-uncached/status-changes.json) · [Native observations](native-early-records/receipt.json) · [Focused report](focused.json) · [Opcode rank](compact-function-rank.terms).
