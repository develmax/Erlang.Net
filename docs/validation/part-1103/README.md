# Part 1103 — lower boolean cases on an ordinary call result

The general emitter recognizes a two-clause case that compares a direct call result to an entry-bound variable and falls back through a wildcard. It keeps the result in X0. When a match returns true, OTP emits a direct exact comparison BIF. When a match returns false, OTP keeps a conditional test and two returns. A fresh pattern variable stays on the general path.

The OTP 29.1.1 probe matched the instruction sequences excluding source lines for both boolean orders and verified strict numeric-type distinctions, a fresh binding, and exception propagation. In the release asn1ct module, the frame of the closure in exit_if_nameduplicate2/2 decreased from three slots to one. Its instructions match the reference except for the global labels.

- [Probe source](beam_call_case_comparison_probe.erl)
- [Execution and disassembly assertions](run.eval)
- [Probe instructions](disassembly.txt)
- [Release closure, previous emission and current emission](asn1ct-closure.txt)
- [Focused compiler tests](focused.json)

The full corpus generated 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. The Code length gaps narrowed in asn1ct and systools_make, the only two changed images. OTP accepted all 1,015 differing images during loader preparation.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
