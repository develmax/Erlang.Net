# Part 1094 — reuse selector slots in terminal cases backed by Y

The general emitter marks flat tuple cases that return the function result, including the final expression of a sequence. The existing alias-aware reuse can now use a saved selector slot in these branches. Intermediate cases retain their slots for the outer continuation. The call reads its original arguments before the selected slot receives the result.

The OTP probe checks a direct terminal case, a case at the end of a sequence, an intermediate case whose selector is read later, a live alias and the original case_clause term. The direct terminal function still has one more Y slot than OTP, which tests incoming X operands before allocating its frame.

- [Probe source](saved_case_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

Three terminal probe functions use three slots instead of four. The intermediate case still uses five. The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. The changed images are beam_types.beam and erl_types.beam. One Code length gap narrowed and neither widened. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)
- [Opcode ranking](opcode-ranking.txt)
