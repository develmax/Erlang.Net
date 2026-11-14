# Part 1666

When saving a call result into a reused slot, load the old slot value into the second argument and stage the first argument in X2 before writing the list literal. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified the returned tuple and the value consumed from the reused slot. Three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 397 Equal and 969 Different. ELDAPv3 remains byte-identical. The change altered 3 images and aligned argument preparation in ex_textCtrl:do_init/1. Frame trimming still differs. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
