# Part 1238 — recover release abstract code for parentheses and chained postfixes

Rank byte differences per chunk across the 1,009 nonmatching images. All differ in Code, 1,008 differ in ExpT, and every image differs in at least four chunks. The audit found 116 modules with a 66-byte Dbgi placeholder and a larger release chunk.

Treat an opening parenthesis as grouping only when the token is a symbol. Associate map and record postfix expressions with their exact source tokens, so a postfix outside parentheses does not consume the inner expression's postfix. Compare the decoded function forms and source coordinates with OTP.

With incomplete abstract code disabled, the baseline fails all 116 candidates. The parenthesis fix alone restores 43, and both fixes restore 83. The remaining 33 fail on other constructs.

The final Release build generated all 1,366 images, with 357 Equal, 1,009 Different and zero rejected. Only Dbgi changed across the 83 affected images. Thirty-three restored chunks are byte-identical to OTP. OTP accepted all differing image pairs.

- [Chunk audit](chunk-audit.txt)
- [Strict baseline diagnostics](abstract-diagnostics.json)
- [Strict final diagnostics](abstract-diagnostics-final.json)
- [OTP abstract forms probe](probe.txt)
- [Probe source](debug_paren_probe.erl)
- [Full corpus](otp-corpus.json)
- [Chunk delta](debug-delta.txt)
- [OTP loader](loader.txt)
