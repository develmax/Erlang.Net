# Part 1635

A straight-line append operand now supplies its emitted source location to the following append or call. The compiler reuses that location when the argument emission produced a line at the consumer's location. Eligibility is restricted to append expressions with prepared leaves. The rule follows pinned OTP beam_z and the compiler's existing nested-call location propagation. No dependency was added.

The probe emits one body line, matching OTP. Its full image still differs due to frame allocation and trimming. The OTP VM verified values and the line reported for badarg from either append operation. All three selected regression checks passed. [Before](before-disassembly.txt) · [After](beam_append_lines-disassembly.txt) · [VM outcome](outcomes.txt) · [Tests](focused.txt).

The compiler accepted and generated all 1,366 Windows OTP 29.1.1 images. 394 matched, including ELDAPv3 and Observer, and 972 differed. The OTP loader accepted all differing images. The Code size gap narrowed in all 273 changed images by an aggregate 3,019 bytes. No whole function changed match status after label normalization. The httpd_file Code gap fell from 30 to 14 bytes. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [httpd_file](httpd-file-disassembly.txt).

The full test suite and readiness regeneration were outside this run.
