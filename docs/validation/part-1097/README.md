# Part 1097 — reuse Y slots across ordinary call bindings

For complete function-body sequences, the general emitter uses the remaining expressions to identify Y slots whose aliases are all dead. A fresh call result can reuse one of those slots after evaluation of the call and its arguments. Guarded, intermediate and specialized decoder sequences retain their existing behavior. Selector-specific reuse shares the same alias check.

The OTP probe checks ordinary and tail continuations, reuse after an intervening call, live aliases and a value used only by a nested case branch. Both reuse examples shrink from three slots to two, matching OTP frame sizes. The probe checks return values and process dictionary effects.

- [Probe source](frame_liveness_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. Compared with part 1096, 434 images changed. The Code length gap narrowed in 427 files and widened in eunit_autoexport.beam and ex_sizers.beam. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)

Within the move/init_yregs first-difference cohort, the number of functions with oversized frames dropped from 6605 to 6591. The file count remains 813. Another 27 functions in 16 files have the same frame size as OTP.

- [Opcode ranking](opcode-ranking.txt)
- [Frame size split](frame-size.txt)
