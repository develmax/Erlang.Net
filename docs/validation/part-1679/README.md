# Part 1679

The call planner handles apply/2 and apply/3. Tagged tuple fields can be extracted straight into the argument registers when the tuple remains available until the last extraction.

The entire deterministic fixture is byte-identical to OTP. The OTP VM verified function dispatch, module/function/arguments dispatch and the original arguments on a failed record match. Three focused checks passed, including the permanent byte comparison. [Fixture](beam_apply_tail.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. The change affected 30 release images. Eleven modules gained a matching normalized function, with no lost function matches. Code size moved farther from OTP in ct_release_test and diameter_dict_util. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
