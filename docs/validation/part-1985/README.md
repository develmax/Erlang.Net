# Part 1985 — Dead tuple definitions, fixed raw captures and mixed generator ordinal

| Gate | Actual result |
| --- | --- |
| All 1,366 uncached file comparisons | 512 Equal, 854 Different, 0 Unsupported, 0 SourceRejected in 775.93 seconds. New matches 0. Lost matches 0. [Raw report](full-uncached/otp-corpus.json) |
| Managed compiler tests | 597 passed. [Tests](focused-verified.json) |
| Derived affected scope | 172 images, 27 Equal and 145 Different. [Scope](derived-scope/scope.json) |
| Native and loader | 74 pinned Windows OTP observations in 14.59 seconds. All 113 changed images prepared. [Native report](native-scoped/receipt.json) |
| Normalized instruction bodies | Six gains and zero losses across 162 inspected changed functions. [Ranking](ranked-causes.md) |
| Dead checked tuple definitions | The accepted complete release contains zero eligible dead X definitions among 187,195 tuple projections. [Census](earliest-dead-projections.stderr.log) |
| Fixed binary captures | diameter_codec:header_length/1 changes from allocate 0 to no frame, matching OTP. The selected prim_zip widths remain 11 and 16 versus no OTP frames. [Physical widths](targeted-frame-widths.tsv) |
| Mixed generator ordinal | compile:diffable/2 gains the source-proven helper with ordinal 1. Its ordinal 0 helper remains missing. The previous proplists helper is retained. [Function keys](mixed-helper-ordinal.terms) |

The first candidate stopped on C# name binding for the source module accessor and Pattern type. The final package uses the existing accessor and declared type. [Original compiler errors](rejected-fixed-binary-build.log)

The compiler gate includes all prior physical width checks. Full repository validation is closed separately for the integrated Part 1983 source, and remains pending for this isolated package.
