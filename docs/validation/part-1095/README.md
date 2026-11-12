# Part 1095 — omit adjacent reloads from Y to an unchanged X register

The shared register move emitter drops an immediate reverse move from a Y slot to the X register that just supplied the slot value. The preceding store remains. The optimization requires adjacent encoded instructions so any label, call, register clobber or other emitted instruction ends eligibility.

The OTP probe verifies the adjacent case, an intervening call and reads across case branches. In the ASN.1 compiler closure the extra reload of Y3 before get_name_of_def/1 disappears. The store remains, and the later reload after a call remains.

- [Probe source](adjacent_move_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)
- [ASN.1 compiler branch](asn1ct-branch.txt)

The full corpus generated 1,363 BEAM files. There were 348 Equal, 1015 Different and 3 SourceRejected. ELDAPv3 is Equal. The change affected 839 generated images. Code length gaps narrowed in 838 files and widened in ex_sizers.beam, from 5832 to 5829 bytes against the 5906-byte reference. OTP prepared all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)
- [Opcode ranking](opcode-ranking.txt)

In the changed images, 45 function instruction prefixes became longer and none became shorter. The comparison ignores line instructions and label declarations while retaining operands. Twenty-one function instruction sequences became equal by this metric and none lost equality.

- [Function instruction delta](function-prefix.txt)
- [Comparison script](../part-1088/function-prefix.eval)
