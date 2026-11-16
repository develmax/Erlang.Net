# Part 1660

The emitter orders the saved result spill and argument preparation in the same order as OTP and places the literal append suffix after the swap. OTP 29.1.1 release disassembly is the reference. No dependency was added.

Both deterministic fixtures are byte-identical. The OTP VM verified saved results, call effects and the badarg for an invalid append prefix. Three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered two images and aligned observer_traceoptions_wx:optionpage_top_right/4 after label normalization. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
