# Part 1201 — emit direct list construction operands

The general emitter passes constant list heads and constant or bound list tails directly to put_list. Computed expressions retain evaluation order. The heap check keeps X0 live when a computed tail is held there and retains the receive guard roots.

The first stack allocation moved closer to OTP in all 3,476 functions where it changed. Code size moved closer in 917 modules and farther in ex_sizers.beam, where generated Code shrank from 5,821 to 5,806 bytes against the 5,906-byte reference. The full corpus generated 1,363 files and recorded 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP accepted the differing images for loading. Three focused tests passed and the execution probe matched all three expected results.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [First allocation comparison](frame.txt)
- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](probe.erl)
- [Probe result](probe.txt)
- [Opcode ranking](ranking.txt)
