# Part 1647

The existing return-block pass now shares identical local tail calls with the same target, arity, and frame size. The bytecode rewrite skips label operand edits inside a removed block. OTP beam_jump provides the reference. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified all seven dispatch paths and three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 9 images. The OTP loader accepted each differing image. See the memsup disassembly for the remaining instruction differences. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [memsup](memsup-disassembly.txt).

The pre-change corpus had no candidates for the exact nil-test shape, 15 functions in nine modules for a broader single-test pattern, and 11 functions in nine modules for duplicate labeled local tails. These counts are pattern hits. [Nil tests](nil-returns.txt) · [Generic tests](test-returns.txt) · [Local tails](tail-returns.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
