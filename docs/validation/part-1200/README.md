# Part 1200 — reduce frames for entry case sequences

The entry call planner now handles a case with an ordinary selector call at the start of a sequence, including a variable match on the case result. The liveness check includes the branches and the following expressions. The case emitter accepts the selector result in X0 and preserves the normal branch exports.

The first allocation is closer to OTP in all 36 functions where it changed. Of the 89 differing images with changed bytes, 40 moved closer in Code chunk size and 26 moved farther. Full image status did not change. The run generated 1,363 files, with 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 is Equal. OTP accepted all differing files for loading. Three focused tests passed, and an OTP execution probe matched all four expected results for variable matching and branch export.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Frame comparison](frame.txt)
- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](probe.erl)
- [Probe result](probe.txt)
- [Opcode ranking](ranking.txt)
