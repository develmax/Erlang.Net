# Part 1203 — release temporary container operands

The general emitter reuses temporary Y slots after building a tuple or list. The allocator preserves slots for bindings created while evaluating operands.

First allocation size moved closer to OTP in 5,629 functions and remained equally distant in three. Code chunk size moved closer in 642 modules. The verified full run generated 1,363 files and recorded 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP accepted all differing images for loading. Three focused tests passed. The execution probe preserved exported bindings and used nine slots, compared with twelve before the change and five in OTP.

The first attempted corpus run used a stale compiler DLL and was discarded. The recorded run checked that the rebuilt DLL contained the new slot release method before starting.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [First allocation comparison](frame.txt)
- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](probe.erl)
- [Probe result](probe.txt)
- [Probe allocations](probe-frames.txt)
- [Opcode ranking](ranking.txt)
