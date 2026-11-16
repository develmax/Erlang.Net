# Part 1092 — test the last tuple selector operand in X0

For a prepared flat tuple case, the second call result can stay in X0 when the patterns only compare or ignore it, the clauses have no guards and the branch expressions do not reference the result binding. The emitter tests literal X0 operands first and reconstructs the original selector on the case_clause path with X0 live. Cases that bind a new variable from X0 or need the result in branch expressions still save it.

The OTP probe exercises literal and bound-variable comparisons, a case_clause error, a branch that needs the result binding, repeated variables, guards, branch exports and side-effect ordering. In the asn1ct closure the second result remains in X0 and the first two comparisons have the same order as OTP. The generated frame dropped from seven slots to six. OTP uses five and reuses an older slot for the result of set_name_of_def/3.

- [Probe source](tuple_order_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)
- [ASN.1 compiler closure](asn1ct-entry.txt)
- [Remaining branch slot mismatch](asn1ct-branch.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1091, two generated images changed and both Code length gaps narrowed. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)
- [Opcode ranking](opcode-ranking.txt)
