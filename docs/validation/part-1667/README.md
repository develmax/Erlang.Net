# Part 1667

After a frame trim, relocate the highest occupied slot that survives the next trim. Its current index reflects the preceding relocations. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified the result and the preserved value after consecutive trims. Three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. The change altered 2 images. The wx example ex_textCtrl is now byte-identical. The Code section of sudoku_gui grew by one byte. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
