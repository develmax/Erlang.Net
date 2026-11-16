# Part 1628 — Drop unused tuple and list construction

The general emitter evaluates discarded tuple members and list heads and tail for their effects in source order. Literals and reads of bound variables need no instructions in this context. This follows pinned OTP sys_core_fold effect lowering and keeps other expression forms on the existing path. No dependency was added.

The permanent fixture covers a discarded tuple with an improper list and calls in both the list heads and tail. The frame shrank from two slots to zero and the full BEAM matches OTP. The OTP VM confirmed the call order and result. Three focused checks passed. A separate compact source probe with all calls on one line still differs because the general emitter repeats line instructions. [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM outcome](outcomes.txt) · [Tests](focused.txt).

The Windows release corpus accepted and generated all 1,366 sources. 393 matched byte for byte and 973 differed. ELDAPv3 was Equal. The change affected 101 release images. The Code size gap narrowed in 95 and widened in six, with a signed aggregate reduction of 5,970 bytes. One more function, snmpa_app:convert_config/0, matched after label normalization with no losses. The OTP loader accepted all 973 differing images. [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Code sizes and loader](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

Both builds used SDK 10.0.404 through MSBuild. In the compiler regression block, 3,714 of 3,830 tests passed and the same 116 cases failed as in part 1626. The full runtime suite and readiness regeneration were outside this run. [Compiler regression block](compiler-block.txt).
