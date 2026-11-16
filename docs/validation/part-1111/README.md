# Part 1111 — forward nested ordinary calls without Y slots

An unconditional function can forward the result of a nested ordinary call into the next call if each outer call has one computed argument and its other arguments are constants. The innermost call reads entry operands directly. Each call passes its result in X0 and the final call is emitted as a tail call with a zero-slot frame. Cases that need another source value across a call keep the existing path.

The pinned OTP 29.1.1 probe matched the full instruction lists for a nested tail call and a chain of three calls. Both images executed with the same argument order, results and inner-call exception.

- [Probe source](beam_nested_entry_calls_probe.erl)
- [Execution and disassembly assertions](run.eval)
- [Matching instruction lists](disassembly.txt)

The next OTP probe records the layouts for entry operands needed by outer calls. OTP saves only those operands in Y registers and trims a slot once its operand has been consumed by an intermediate call.

- [Surviving operands probe](beam_nested_entry_survivor_probe.erl)
- [OTP survivor layouts](survivor-otp.txt)

The release substitution/3 instructions match the generated function apart from global labels. The frame is now zero slots and the allocation live count is two. The full corpus produced 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Of the 165 changed images, 164 have a smaller Code length gap and none have a larger one.

- [Release comparison](substitution.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
