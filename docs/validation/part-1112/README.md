# Part 1112 — save only parameters used after the first nested call

Nested call chains preserve entry operands only if an outer call reads them. The first call still reads its operands directly from X registers. The initial frame orders saved inputs by last use so dead slots can be trimmed after the arguments for an intermediate call have been prepared. The final tail call deallocates the remaining slots.

The pinned OTP 29.1.1 probe matched both instruction lists. The first function preserves one parameter. The second preserves two and trims one after preparing the intermediate call. Both images produced the same nested results.

- [Probe source](beam_nested_entry_survivor_probe.erl)
- [Execution and instruction assertions](run.eval)
- [Matching disassembly](disassembly.txt)

A second probe exercised trimming to a zero-slot frame and retaining an operand needed by both intermediate and final calls. The instruction lists and results match OTP.

- [Trim edge cases](beam_nested_trim_probe.erl)
- [OTP and generated instructions](trim-outcomes.txt)

For the next shape, OTP evaluates sibling arguments left to right. It swaps the first result in X0 with the second call's last-use input in Y0, retaining the first result without allocating another slot.

- [Sibling call probe](beam_nested_siblings_probe.erl)
- [OTP sibling layout](siblings-otp.txt)

The full corpus produced 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Of the 186 changed images, 160 have a smaller Code length gap and none have a larger one.

- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
