# Part 1637

The shared emitter lays out a frame per branch when two function clauses return a tuple containing the result of a nested append and call. For checked record access it tests the tag before the append, then reads the retained field when building the result. The implementation reuses entry call and tuple emission and follows pinned OTP 29.1.1 disassembly. No dependency was added.

The permanent fixture matches OTP byte for byte. Three focused checks passed. OTP VM evaluation verified both successful branches and the priority of badrecord over an invalid append. [Hashes](fixture-sha256.txt) · [Tests](focused.txt) · [VM outcome](outcomes.txt) · [Disassembly](disassembly.txt).

The Windows OTP 29.1.1 corpus accepted and compiled all 1,366 sources. It recorded 394 Equal, including ELDAPv3, and 972 Different. Only httpd_file changed. Its Line section now matches and its Code section is 456 bytes, down from 459 against 445 in OTP. The instructions of handle_error/5 match after label normalization. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Function comparison](normalized-functions.txt) · [Disassembly](httpd-file-disassembly.txt).

Compare handle_error/4 mixed tagged-tuple and atom dispatch against OTP next. The full runtime suite and readiness regeneration were outside this run.
