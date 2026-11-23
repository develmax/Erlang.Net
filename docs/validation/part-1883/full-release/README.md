# Part 1883 full Windows release comparison

The sealed 242-path candidate builds all 1,366 Windows OTP 29.1.1
release inputs without cached compilation. The four disjoint shards
finish in 597.11 seconds. The report has **410 Equal, 956 Different,
0 Unsupported and 0 SourceRejected**. The same 410 images are byte
equal as in the accepted Part 1875 package.

Of the 649 changed outputs, 646 pass the pinned ERTS preparation
step. Crypto, zstd and pubkey_os_cacerts load through their normal
on-load functions. Native transcripts agree over SHA-256, a binary
compression round trip and the sorted 59-certificate trust set.
The stage retains exact image digests and all three variant transcripts.

Fifty-three more Dbgi chunks, nine Type chunks and one LitT chunk
match the reference byte for byte. No section loses equality.
An existing normalized body gains equality in 142 cases and none
lose it. There are 550 nearer frame distance observations and five
farther observations. Four are attributable to unread projection
removal running before established scheduling. The previously
introduced systools_make capture remains a separate repair.
Part 1883 is a complete release observation, with local whole
repository validation and repair of frame ordering still open.

| Remaining group | Current reach | Phase dependency |
| --- | --- | --- |
| Excess ordinary frame with the same X root count | 7,647 functions in 825 modules | Y liveness, frame placement and cleanup after call preparation |
| Excess combined frame with equal heap and X root counts | 761 functions in 327 modules | Heap/frame combination after source and Y ownership |
| Missing list-comprehension helper key | 2,659 functions in 391 modules | Core helper ownership, constant capture and arity before code scheduling |
| Existing symbol with a different arity | 286 functions in 87 modules | Determine source/capture equivalence before changing helper bodies |
| Type | 859 images | Work from exact use-site and source facts before table assignment |
| Dbgi | 48 images | File provenance and raw source annotation are independently measured |
| Line | 923 images | Different instruction paths and generated span decisions precede table order |

The counts overlap and use full function/section membership reports.
All 956 remaining different images differ in Code. Table and
normalized instructions are diagnostic metrics.

[Complete release report](otp-corpus.json) · [Timing](timing.json) ·
[Exact section delta](section-delta.json) · [Rank](function-rank.terms) ·
[Normalized comparison](normalized-compact.terms) ·
[Raw frame witnesses](frame-increases.terms) ·
[Original inputs](input-verification.json) ·
[Native on-load transcripts](native-on-load/).

