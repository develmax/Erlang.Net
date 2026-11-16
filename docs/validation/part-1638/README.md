# Part 1638

The compiler separates an entry tagged tuple case from overlapping fallback traffic and emits select_val for the disjoint atom cases. It uses the existing argument move planner and shares one fallback for unmatched tuples and non-tuples. The reference is the pinned OTP 29.1.1 beam_ssa_codegen and the release disassembly. No dependency was added.

The deterministic fixture matches OTP byte for byte. The OTP VM verified seven selected atom, tuple and fallback inputs. Three focused checks passed. [Hashes](fixture-sha256.txt) · [Tests](focused.txt) · [VM outcomes](outcomes.txt) · [Disassembly](disassembly.txt).

All 1,366 sources were accepted and compiled. The Windows OTP 29.1.1 comparison recorded 395 Equal and 971 Different. The complete httpd_file release image now matches. ELDAPv3 remains byte-identical. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

The ranked instruction pairs count affected modules and use the last complete corpus before this change. A one-slot OTP frame emitted with two slots appeared in 222 modules. The full runtime suite and readiness regeneration were outside this run. [Instruction ranking](ranked-baseline.txt) · [Frame operands](ranked-frames.txt) · [Previous and OTP dispatch](httpd-file-disassembly.txt).
