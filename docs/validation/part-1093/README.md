# Part 1093 — reuse dead selector slots inside a terminal branch

The general emitter can bind the result of a call at the start of a terminal tuple case branch in a slot already used by a selector element. It checks whether the remaining branch reads any alias of that slot. A slot with a live alias or a deferred tuple projection is preserved. The call is evaluated before overwriting the slot, and other branches and the case_clause failure path retain the original bindings.

The OTP probe covers reuse, a live alias and the original case_clause tuple. In the asn1ct closure the result of set_name_of_def/3 now occupies Y3, as in OTP. The frame drops from six slots to five and the initial init_yregs instruction disappears. Register order, the reload before get_name_of_def/1 and later branch instructions still differ.

- [Probe source](tuple_order_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)
- [ASN.1 compiler closure](asn1ct-entry.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1092, only asn1ct.beam changed and its Code length gap narrowed. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)
- [Opcode ranking](opcode-ranking.txt)
