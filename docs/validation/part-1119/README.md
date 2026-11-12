# Part 1119 — emit guarded arithmetic returns without a frame

An initial clause with only variable patterns, direct comparison guards and a binary arithmetic return can keep its operands in the entry X registers. The emitter validates the guard conjunction before writing instructions, shares opcode selection with the general guard path, and branches to the next clause without changing its inputs.

The OTP 29.1.1 probe verified division, remainder, fallback and badarith from a non-integer division operand. Both arithmetic branches are frameless. OTP still uses typed arithmetic operands and carries facts across failed guards. The generated fallback in the probe still allocates a frame.

- [Probe source](beam_guarded_bif_tail_probe.erl)
- [Execution assertions](run.eval)
- [OTP and generated disassembly](disassembly.txt)

The division branches of the release fQuotient/2 now keep the entry arguments in X registers. The remainder-based fallback still allocates four slots, and guard sharing and operand types still differ. The full corpus generated 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. All 23 changed images have smaller Code length gaps.

- [Release comparison](fquotient.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
