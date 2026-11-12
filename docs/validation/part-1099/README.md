# Part 1099 — carry continuation liveness through intermediate cases

When the emitter knows the complete enclosing continuation, an intermediate case branch can reuse slots after its pattern and guard. The liveness check includes exported bindings, the match against the case result and expressions after the join. Other case paths retain their existing behavior.

The OTP 29.1.1 probe covers a guarded fallback, exported bindings, assignment from a case and a match of the result against an existing binding. The assigned and checked functions shrink from five slots to four. OTP uses three. The exported case retains its frame size, and the probe verifies both the exported values and the badmatch term.

- [Probe source](branch_liveness_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1098, 128 generated images changed. The Code length gap narrowed in 108 files and widened in the three megaco_ber_media_gateway_control_v1/v2/v3 images. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)

The first move/init_yregs difference still occurs with oversized frames in 6579 functions across 812 files. The same-size group remains 27 functions across 16 files.

- [Opcode ranking](opcode-ranking.txt)
- [Frame size split](frame-size.txt)
