# Nonthrowing external call source locations

The candidate removes an ordinary Line immediately before an external BIF whose MFA is classified as safe by OTP 29.1.1. For non-tail calls it also proves that the straight-line continuation reaches a fresh position or exits before an instruction could report the old position. Linear tuple and cons construction are allowed. A map-association instruction can be crossed if its source is a compiler-emitted map literal. Debug/executable position markers and exception-handler functions keep the existing path. The existing line-table builder updates instruction references, source entries and filenames from the retained code.

The reference commit is ad05823719d77c8faee87348ea39513d4e2f99c5. Pinned erl_bifs:is_safe/3 and beam_ssa_codegen:call_line/3 explain the omitted make_ref/0 line in the Windows release. The implementation uses existing compiler symbol constants and the ETF codec. No dependency was added.

Baseline is the exact Part1853 frozen candidate, source DB3AA7C5BCCB10E7664155CECBBEC7F97C9B0E0CC0BBCA8AB1036DE9015155E4. The raw identity receipt contains the unformatted source and compiler digests. Scoped candidate compiler SHA-256 is 5A91C52380DF3D4F8270F1440E1149C35ED07E11CC8D73FD6FE0116DDAFC4024.

| Scoped comparison, one worker | Part1853 | Candidate |
|---|---|---|
| Equal | 5 | 5 |
| Different | 3 | 3 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Comparison time | 7.47 s | 8.34 s |

No whole-file matches were gained or lost. The generated ssh_channel_sup Line chunk went from 36 to 34 bytes and is now byte-identical to the original chunk. The common SHA-256 is 5BF8BD9CE40CA8AE8507A282F1E3848530710B63862EC56FEA85ED61FBBC44F6. Code moved from 380 to 378 bytes with 369 in OTP. The full module still differs in Code and the derived Attr digest. The other two different images in the selection retained their original hashes.

Native OTP preparation and execution compared both SSH roles and all four public result shapes. They checked the exact child specification fields, including a reference and the selected channel arguments. A separate permanent source fixture validates the compaction of the Line table while preserving the original source line for a subsequent throwing call on the same line. The candidate and baseline produce the same native values and report line 9 in the error frame. The regression fails on the characterized baseline and passes on the candidate with managed preparation.

The isolated build, focused harness, source layout check, and forward/reverse patch checks passed. Logs retain the initial rejected identifier and the initial raw test expectation adjustment. The full acceptance run for this candidate remains with root. The next independent SSH dependencies are keeping the producer and pure construction results in X across its heap checks, then matching case layout and guarded terminal projection.
