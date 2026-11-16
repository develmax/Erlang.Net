# Part 1087 — forward the first result directly into the second call

The general emitter keeps a first call result in X0 when the following ordinary call consumes it and the remaining expressions do not refer to it. The parallel move scheduler reads both X and Y operands and retains X dependencies when preparing argument registers. Terminal local and external calls use the matching framed tail opcodes. Results that survive the second call retain their stack slot.

The OTP 29.1.1 probe checks both tail calls, duplicate result operands in a nonterminal call, a result live after the second call and a preexisting match. The local and external tail functions match the reference instructions after normalizing entry label numbers and source file paths.

- [Probe source](first_call_probe.erl)
- [Execution assertions](run.eval)
- [Instruction comparison](instruction-comparison.txt)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)
- [Focused tests](focused.json)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1086, 254 images changed. The Code length gap narrowed in 253 files and widened in ex_sizers.beam, where generated Code moved from 5855 to 5848 bytes against 5906 in OTP. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Opcode ranking](opcode-ranking.txt)
