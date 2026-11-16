# Part 1604 — Emit direct body BIFs for nodes, tuples and maps

The general emitter uses the direct BEAM instructions for resolved node/0, node/1, tuple_size/1, min/2, max/2, is_map_key/2 and map_get/2 calls with variable or literal operands. The function-body instructions use a zero failure label where the instruction takes one.

The OTP VM agreed on both successful returns and on badarg, badkey and badmap outcomes. The fixture still differs in stack allocation and operand types. All three selected managed tests passed. The builds used a larger .NET stack with shared compilation and analyzers disabled. [Fixture](beam_body_bifs.erl) · [Disassembly of run/4](run.txt) · [Disassembly of lookup/2](lookup.txt) · [VM outcomes](csharp.txt) · [Tests](focused.txt).

All 1,366 pinned sources were accepted and generated. There were 392 Equal and 974 Different, including ELDAPv3 as Equal. 201 images changed. The Code size gap shrank in 194, grew in four and held steady in three, for a net reduction of 2,823 bytes. The count of matching functions after label normalization stayed the same. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt) · [Function comparison](normalized-functions.txt).
