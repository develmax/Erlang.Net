# Part 1561 — Reuse a stack slot after its last argument read

The saved-call planner can load a dying Y slot into its argument registers and then save the previous call result in that same slot. This applies when the argument setup only needs X0 from the preceding call, and no saved load overwrites X0. The change reuses the existing argument emitter and follows the OTP 29.1.1 live-range approach in beam_ssa_pre_codegen.erl. No dependency was added.

The fixture drops from two Y slots to one and matches four outcomes on the OTP VM, including repeated reads from the dying slot. The instructions in global:delete_global_lock/2 match OTP after label normalization. Three focused tests passed. [Before](run-before.txt) · [After](run-after.txt) · [Fixture](beam_reuse_dying_call_slot.erl) · [Outcomes](fixture.txt) · [Release disassembly](delete_global_lock.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. Two images changed, and one function gained an instruction match after label normalization. No functions lost a match. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader](loader-and-code.txt).
