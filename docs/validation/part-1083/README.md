# Part 1083 — copy entry arguments before initializing the rest of the frame

The experimental general emitter copies incoming X registers to their reserved Y slots before emitting `init_yregs` for the remaining slots. Initialization still precedes closure allocation and clause emission. The OTP probe needs no init_yregs instruction because both Y slots are initialized by copies. In asn1_db:init/2 OTP emits init_yregs immediately after allocate for the slot that receives the first call result. The probe exercises two successive process dictionary updates on OTP and preserves their arguments and previous values.

All 1,366 files were accounted for. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 was Equal. The generated images changed in 1006 files that still differ from the release. OTP accepted all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Loader results](loader.txt)
- [Focused tests](focused.json)
- [Probe source](frame_entry_probe.erl)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

The original move versus init_yregs cohort fell from 817 to 585 files. After changing the order, the reverse pair is the largest remaining cohort at 738 files. Counts overlap because a file may contain multiple differing functions.

- [Opcode ranking](opcode-ranking.txt)

The global reorder was reverted. The next change must size the frame from values that live through calls, then initialize the remaining slots at allocation.

- [asn1_db:init/2 disassembly in OTP, baseline and experiment order](asn1-db-init.txt)
