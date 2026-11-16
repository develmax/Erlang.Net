# Part 1244 — preserve nested calls and list prefix abstract forms

Keep the exact postfix token for each application, including when the parser adds a source line. Retain the original nonempty list-prefix pattern for the abstract writer while compiling the folded pattern. Use the first token inside grouping parentheses for binary segment annotations. Restore the release header paths for filelib and qlc_pt.

The strict release probe reproduces all decoded forms and all three Dbgi chunks for filelib, qlc and qlc_pt. The source fixture matches OTP after file path normalization.

The full corpus generated all 1,366 images with 357 Equal, 1,009 Different and zero rejected. Only Dbgi changed across 56 modules and 36 chunks newly match the release. The audit found no remaining 66-byte placeholders where OTP has a larger Dbgi. OTP accepted all differing image pairs.

- [Strict release comparison](abstract-probe.txt)
- [Source fixture](debug_nested_apply_probe.erl)
- [Abstract form comparison](probe.txt)
- [Full corpus](otp-corpus.json)
- [Chunk delta](debug-delta.txt)
- [Chunk audit](chunk-audit.txt)
- [OTP loader](loader.txt)
- [Nearest image function comparison](httpd-socket-ranking.txt)
