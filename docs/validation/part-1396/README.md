# Part 1396 — Prepare computed arguments for tail calls

Saved-call sequences now flatten nested ordinary call arguments before a tail call as well as before a terminal case. The frame planner can reuse a dying argument slot when a previous result must survive the nested call. A separate closure argument path builds the closure after the prefix call, keeps its result live in X0 and emits a tail call. Captures from incoming arguments use stack slots or literal operands when all known callers pass the same constant.

The OTP oracle exercises tuple conversion, its badarg result, captured arguments and a literal capture. All three probe functions reach the OTP frame size and match normalized instructions, with closure identifiers normalized for the two closure functions. The permanent deterministic conversion fixture matches the complete OTP image. The selected compiler cases, whole-module check and earlier call-sequence oracle pass with the compiler assembly used by the corpus.

All 1,366 Windows OTP 29.1.1 release images generate. There are 372 Equal, 994 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepts all 994 differing images for loading.

The change affects 47 release images. Twenty-two functions gain normalized instruction matches and none lose one. Of 39 changed peak frames, 38 reach the OTP size and one moves closer. The Code gap narrows in 35 images and widens in nine. The net gap shrinks by 364 bytes.

The pre_process_oid/1 instructions match after normalizing labels. The do_match_value/5 frame drops from three slots to two and the tail call keeps its prior result in X0 while building the closure. OTP uses one slot because Def is a literal capture. The C# caller proof does not yet follow the parameter forwarded by match_value/5. The largest frame group now covers 1,772 functions in 555 modules. The immediate local tail subgroup has 93 functions in 34 modules. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Release instructions before](tail-before.txt)
- [Release instructions after](tail-after.txt)
- [Normalized functions](normalized-functions.txt)
- [Peak frames](peak-slots.txt)
- [Code size gaps](code-delta.json)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Sequence prefixes](sequence-prefixes.txt)
- [Continuation ranking](case-continuations.txt)
- [Focused oracle](check-computed-tail.ps1)
- [Oracle result](probe.txt)
- [Complete fixture hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Whole-module check](whole-module.txt)
