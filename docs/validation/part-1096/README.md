# Part 1096 — test an entry tuple selector before allocating the frame

A function that directly returns a single-clause, unguarded case on a flat tuple of incoming parameters can compare the fields in X before allocating its frame. A successful branch saves only the parameter registers and aliases that the body references. A failed comparison constructs the original selector from its unchanged incoming registers and emits case_end without a frame.

The OTP 29.1.1 probe exercises literal and bound-variable comparisons, repeated pattern bindings, selector field reordering, live aliases and the original case_clause term. Cases after an effectful sequence prefix and intermediate cases continue through their existing lowering. The terminal probe now emits its first two tests before allocate, as OTP does. Its success frame still contains an extra slot because the branch reads a parameter whose literal value is already proven by the pattern.

- [Probe source](saved_case_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

The experimental full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. All 1015 differing images match the previous generated image hashes. OTP prepared them for loading. The experiment was reverted because the tested entry shape does not affect the release corpus.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)

The frame analysis counted 6,605 functions in 813 files where the generated frame is larger at the first move/init_yregs difference. In 27 functions across 16 files the frame sizes match. File counts overlap. This supports prioritizing general frame liveness over swapping the initialization order.

- [Opcode ranking](opcode-ranking.txt)
- [Frame size split](frame-size.txt)
- [Frame size analysis script](frame-size.eval)
