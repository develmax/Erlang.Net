# Part 1590 — Project checked call payloads directly into live stack slots

For a single field of a checked call result that remains live across the next ordinary call, the planner reuses its existing saved tuple projection. It extracts the payload directly from X0 into the allocated Y slot and prepares arguments from that slot. The failed check still sees the original tuple or other result in X0. No dependency was added.

Both OTP VM observations agree, including badmatch on a non-tuple result. The fixture now uses the same direct projection as OTP 29.1.1. Its full image still differs in surrounding checks and other instructions. Three focused tests passed. [Fixture](beam_saved_checked_payload.erl) · [Disassembly](fixture-disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and generated. There were 390 Equal and 976 Different. ELDAPv3 was Equal from the C# ASN.1 generator. Sixteen release images changed. Two functions newly match after label normalization and no matches were lost. The Code size gap fell in eight modules, increased in five and stayed the same in three. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).

The disassembled wx_misc:mSWSetEmulationLevel/2 now matches through the checked projection. The remaining differences in this function are the relocation of the one live Y slot and the two frame trims. [Remaining instructions](wx-two-arguments.txt).
