# Part 1644

The emitter runs alternatives of a list-head equality guard using direct branches and retains the tail in the entry register. It preserves numeric equality, constant fallback and nil behavior. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified numeric equality and proper and improper list behavior and three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 1 images. The OTP loader accepted each differing image. The instructions of xmerl_xpath_pred:start_link/1 match after label normalization. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Xmerl](xmerl-xpath-pred-disassembly.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
