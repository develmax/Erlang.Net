# Part 1640

The compiler saves the original tagged tuple across the producer call and reads its field straight into X0. The call result remains in X registers for the terminal call. Both an inline producer and a producer bound before the final call use the same plan. The tuple test still branches directly to the function_clause entry. The compiler reuses its existing entry call and tail move emitters. No dependency was added.

The sampled module matches the OTP 29.1.1 compiler byte for byte. The OTP VM verified returned values and function_clause results in both forms. Three focused checks passed. [Inline](inline-disassembly.txt) · [Bound](bound-disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 Windows OTP 29.1.1 sources compiled with 395 Equal and 971 Different, including byte-identical ELDAPv3. The change altered 3 release images. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Functions](normalized-functions.txt) · [Code and loader](loader-and-code.txt) · [xmerl_xsd](xmerl-xsd-disassembly.txt).

The pre-change instruction comparison found 220 {allocate,1,1} -> {allocate,2,1}. Check heap and slot use across constructed call arguments next. The full runtime suite and readiness regeneration were outside this run. [Frame ranking](ranked-frames.txt).
