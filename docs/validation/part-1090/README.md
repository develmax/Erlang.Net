# Part 1090 — expose calls in entry tuples to frame liveness

The general emitter lowers consecutive eligible call operands of a tuple to temporary bindings in source evaluation order. This applies to direct tuple values, case selectors and a first match whose value has that shape. The change is local to emission and keeps the source forms for metadata. The existing first-call and slot reuse logic can then see the calls. If a returned tuple immediately consumes the second result, the emitter reads it in X0.

The probe executes in OTP 29.1.1. It checks side effects when two calls use the same key, the selected case branch, the term in a case_clause error and a lifted closure. The closure instructions match after normalizing labels and omitting line instructions. The generated closure includes an extra line instruction before the second call on the same line.

The reference asn1ct closure now uses a Y/X swap in the generated image. Its frame still has ten slots compared with five in OTP. OTP tests the two selector operands directly, while the C# emitter constructs a tuple and matches it.

- [Probe source](tuple_order_probe.erl)
- [Execution assertions](run.eval)
- [Instruction comparison](instruction-comparison.txt)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)
- [ASN.1 compiler closure](asn1ct-entry.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1089, 31 images changed. Code length gaps narrowed in 16 files and widened in 14, with an aggregate absolute gap reduction of 126 bytes. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)
- [Opcode ranking](opcode-ranking.txt)
