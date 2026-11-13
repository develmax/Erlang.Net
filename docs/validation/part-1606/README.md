# Part 1606 — Plan saved calls after tuple entry checks

Tuple clauses returning a tuple can use the saved-call planner after entry tests. The entry planner recognizes an empty list in a tuple field and tests it before allocating the body frame. In xmerl_xlate:hook/2 the fallback now returns without a frame and the matching branch uses six slots instead of seven. OTP uses two. The disassembly still differs in the nil test and call preparation. [Previous instructions](hook-before.txt) · [Candidate instructions](hook-after.txt).

The OTP VM agreed on the matched branch, two fallback inputs and the badmatch reason. Three focused checks passed. The builds used a larger .NET stack with shared compilation and analyzers disabled. [Fixture](beam_nil_record_clause.erl) · [VM outcomes](csharp.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and generated. There were 392 Equal and 974 Different, with ELDAPv3 Equal. Of 44 changed images, the Code size gap shrank in 30, grew in 13 and held steady in one. The net reduction was 669 bytes. The normalized function match count stayed the same. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt) · [Function comparison](normalized-functions.txt).
