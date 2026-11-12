# Part 1102 — retain ordinary call results in X0 for strict comparisons

The general emitter handles a single unconditional clause that returns an exact comparison of a direct call with an entry parameter or literal. It saves only the parameter needed after the call and emits the comparison BIF directly using X0. The same path covers nonrecursive lifted closures without a guard-failure entry.

The OTP 29.1.1 probe matched the instructions excluding source lines in all three comparison functions. The reference and generated files both ran on OTP with the same strict equality, inequality and exception outcomes. Their complete module bytes still differ.

- [Probe source](beam_call_comparison_probe.erl)
- [Matching comparison instructions](disassembly.txt)

The complete corpus yielded 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Eighteen generated images changed and all eighteen have a smaller Code length gap.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Focused compiler tests](focused.json)
- [OTP loader preparation](loader.txt)
