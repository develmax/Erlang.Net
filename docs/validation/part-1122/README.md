# Part 1122 — inline nested list checks and rank entry prefixes

The corpus scan inspected 1,015 differing images and classified oversized functions with one unguarded variable clause, preceding expressions and a simple nested tail call. The largest prefix group binds the result of one call before a local tail call, with 1,421 functions in 35 files. Of those, 1,386 have Yecc parser function names across 14 modules. The source shape is an upper bound on impact and does not prove that every function uses a list reduction.

The existing targeted inliner recognized explicit list reduction helpers but skipped their calls inside a larger expression. It now substitutes the original list match and its input in that position. The OTP 29.1.1 probe confirmed helper removal, preservation of proper and improper lists, and badmatch for an empty list. The generated caller still allocates a frame where OTP checks the input directly in X0.

- [Prefix shape scan](prefix-shapes.eval)
- [Prefix ranking](prefix-shapes.txt)
- [Leading group scan](leading-group-modules.eval)
- [Module breakdown](leading-group-modules.txt)
- [Yecc comparison before this change](yeccpars2-3.txt)
- [Inline probe](beam_nested_inline_list_probe.erl)
- [Execution assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full corpus produced 1,363 images, with 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Code length gaps narrowed in all 13 changed images, all parser modules. In yeccpars2_3/7 the helper call is gone, but the generated frame grew from six to seven slots because the general path materializes the checked input again. OTP has no frame for this function.

- [Yecc comparison after inlining](yeccpars2-3-after.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
