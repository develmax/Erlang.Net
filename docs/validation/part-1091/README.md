# Part 1091 — match prepared tuple operands directly

The general emitter compares saved tuple elements directly against flat case patterns. It supports literals, fresh and previously bound variables, repeated variables, wildcards and guards. The branch binding and export machinery is shared with ordinary case lowering. The failure path still constructs the selector and raises case_clause with the same term. The second call in a planned consecutive pair omits its line instruction when both calls have the same source location.

In the reference asn1ct closure the generated frame dropped from ten slots to seven and the selector is no longer materialized on the successful path. OTP uses five slots, tests the literal before the bound variable and keeps the second result in X0.

- [Probe source](tuple_order_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)
- [ASN.1 compiler closure](asn1ct-entry.txt)
- [Normalized closure comparison](instruction-comparison.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1090, 85 generated images changed and all 85 Code length gaps narrowed. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)
- [Opcode ranking](opcode-ranking.txt)
