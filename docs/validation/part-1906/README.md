# Part 1906 full Windows OTP 29.1.1 release comparison

| Equal | Different | Unsupported | SourceRejected | Seconds |
| ---: | ---: | ---: | ---: | ---: |
| 418 | 948 | 0 | 0 | 894.391 |

All 1,366 modules compiled using four isolated partitions without compiler-result caching. The linked candidate retains all 416 previous byte matches and adds debugger and mnesia_frag_hash. All 1,366 effective inputs retain their profiles. Pinned ERTS prepares all 45 changed images. Independent source-generated images for the two new matches bind to the exact release hashes, with nine Debugger and 29 Mnesia observations. The isolated closure controls are retained independently.

The first focused group passes all 113 selected cases. The whole-module case passes in its exclusive phase in 24.584 seconds, establishing coverage of all 114 distinct names. The 87-module uncached pilot records 14 Equal and 73 Different in 299.137 seconds. Its first source-freeze orchestration collided with an open stderr file. The immutable dependency copy was verified file by file before the separately logged source seal.

The full diagnostic observes 273 newly equal normalized functions, zero losses, one smaller frame and zero increases. Seven new raw chunks match and no matching chunk is lost. The 948 remaining images have a separate raw corpus and opcode rank. The earlier repository-wide managed timeout and canonical readiness reports retain their original scope.

[Raw results](full-uncached/otp-corpus.json) · [Byte delta](full-uncached/status-changes.json) · [Native evidence](native-early-records/receipt.json) · [Focused coverage](focused-verified.json) · [Opcode rank](compact-function-rank.terms).
