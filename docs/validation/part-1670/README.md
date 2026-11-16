# Part 1670

The compiler emits get_tl for unused elements of fixed-length list arguments and retains the original argument in X0 through the length check. First-call forwarding uses the actual extraction registers.

The fixture matches OTP list tests, extraction instructions and register operands. Frame allocation and the body instructions still differ. Both images returned the expected values and preserved the arguments in function_clause failures. Three focused checks passed. [Fixture](beam_exact_list_projection.erl) · [Instructions](disassembly.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. The change altered 2 release images. In observer_app_wx:init/1 the list extraction sequence matches OTP. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
