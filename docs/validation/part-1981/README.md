# Part 1981 — Joint frame recovery and generated helper validation

| Validation | Result |
| --- | --- |
| Frozen compiler | [Content identities](source-freeze.json) |
| Selected managed tests | [584 passed](focused-verified.json). The initial fixture rejection is preserved. |
| Representative and affected release cases | [94 in 312.85 seconds](pilot-receipt.json). 27 Equal, 67 Different. |
| Complete uncached Windows release | [1,366 files](full-uncached/otp-corpus.json) in 675.39 seconds. 512 Equal, 854 Different, 0 Unsupported, 0 SourceRejected. |
| Newly byte-exact modules | lib/diameter-2.7.2/ebin/diameter.beam |
| Lost byte-exact modules | None |
| Normalized function bodies | [21 gains and 0 losses](rank-counts.json) |
| Native behavior and loader | [53 native observations](native-scoped/receipt.json). 64 prepared changed images and 0 shared on-load restrictions. |
| Frame-width controls | [212 earlier](full-widths/receipt.json) and [5 newer](full-recent-widths/receipt.json). Actual allocation widths are available per function. |
| Complete local repository | [Part 1978](../part-1978/README.md) has the current integrated managed report. The repository gate for this source is pending. |

The package applies source-derived stack scheduling, selective control-flow sharing, tail dispatch, and writable and positioned binary lowering. The first build attempts and fixture failure remain in their original scopes. The [ranked cause table](ranked-causes.md) describes the remaining functions and sections. The OTP checkout and release artifacts are comparison inputs with recorded identities.
