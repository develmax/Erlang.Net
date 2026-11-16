# Part 1233 — keep guarded list-head matches in X registers

Disassembly of the 1,009 differing release pairs found 15,632 paired functions with an allocation only in the generated code across 892 modules. The largest reference prefix group starts with a nonempty-list test and list extraction. It covers 1,401 functions in 405 modules and includes several distinct control-flow shapes.

The compiler now handles a three-clause shape with a guarded tagged tuple chain, a tail call on the remaining list and a literal result for an empty list. It reuses X0 for the tail and X1 for the tag checks and guard. The generated instructions for asn1ct_check:any_manual_tag/1 match OTP after label normalization.

Only asn1ct_check changed in this corpus. Its Code size gap fell by 66 bytes. The full corpus generated 1,366 images with 357 Equal, 1,009 Different and zero rejected sources. OTP accepted all differing image pairs. The execution probe verified a matching head, a failing guard, an empty list and the function_clause error for an improper tail.

- [Frameless-function ranking](frameless-ranking.txt)
- [Ranking script](rank-frameless.eval)
- [Previous release function disassembly](any-manual-tag.txt)
- [Normalized release function](any-manual-tag-normalized.txt)
- [Execution probe](probe.txt)
- [Probe source](guarded_list_tail_probe.erl)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
