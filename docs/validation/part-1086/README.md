# Part 1086 — retain only the entry arguments live across the first call

For an unconditional clause starting with an ordinary call whose arguments are variables or constants, the general emitter computes which inputs the continuation still references. It forwards the first call from the original X registers and saves only the live inputs in Y. A new result binding receives a Y slot only when the continuation reads it. Matches against an existing binding retain their comparison and failure behavior.

The probe executes on OTP 29.1.1 and covers a live result, two discarded results and a match against a preexisting binding. The live-result example uses two Y slots instead of four. The discarded-result examples use one Y slot and omit init_yregs, as OTP does. Argument move order at the tail call still differs. OTP forwards the live first result from X0 to the next call, which removes another Y slot.

- [Probe source](first_call_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)
- [Focused tests](focused.json)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1085, 564 images changed. The Code length gap narrowed in 486 files and widened in ex_sizers.beam, where the generated Code size moved from 5867 to 5855 bytes against 5906 in OTP. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Opcode ranking](opcode-ranking.txt)
