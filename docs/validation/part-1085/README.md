# Part 1085 — retain entry call arguments in X registers

For an unconditional function that binds an immediate ordinary call result and returns it in a tuple, the general emitter sizes the Y frame from parameters referenced by the tuple. It passes the call arguments from their incoming X registers with the existing OTP move scheduler. The result remains in X0. Specialized Erlang BIF lowering remains outside this path, with put/2 handled as an ordinary external call.

The probe executes on OTP 29.1.1 and covers argument permutations, fanout, duplicate result fields and a match in a call argument. The four optimized functions match OTP's disassembled instructions after removing source file paths from line operands. The repeated result function now has one Y slot, down from two.

- [Probe source](frame_entry_probe.erl)
- [Execution assertions](run.eval)
- [Instruction comparison](instruction-comparison.txt)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Six generated images changed from part 1084 and all six Code size gaps narrowed. OTP accepted all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused compiler tests](focused.json)
- [Opcode ranking](opcode-ranking.txt)
