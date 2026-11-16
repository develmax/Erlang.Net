# Part 1671

After an exact list match, a unary GC BIF followed by a tuple return can use the extracted X registers directly. Its result goes to X0. The BIF and tuple heap checks use the registers live at each point.

The full deterministic fixture is byte-identical to OTP 29.1.1. Both images returned the expected values and preserved the arguments in function_clause failures. Three focused checks passed, including the permanent byte-comparison regression. [Fixture](beam_exact_list_projection.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. Every release image was unchanged. The baseline ranking found 881 modules with at least one function whose first normalized difference was in the operands of allocate. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
