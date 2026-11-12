# Part 1367 — Keep the last computed call operand in X0

The general call emitter keeps the last computed argument in X0 through argument setup. Earlier computed arguments still have to survive evaluation of later arguments. The emitter moves the final value to its argument register before loading other operands and retains the guard and receive scratch register paths.

The focused probe compares the prior compiler, this compiler and Windows OTP 29.1.1. Three functions use one fewer stack slot. The probe also verifies returned values, the order of the two nested calls and which calls execute when an argument raises. Seven earlier probes passed.

The full corpus generated 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal and the release loader accepted all 997 differing images. The Code size gap narrowed in 723 images and widened in one, for a net reduction of 32,772 bytes. Peak stack use fell in 2,919 functions across 609 modules and reached the OTP count in 99 functions. The normalized comparison covers all 724 changed images and found three new matches with no lost matches. They are inet:udp_sockets/0, inet:tcp_sockets/0 and ssh_dbg:is_off/0.

The Code size gap for ex_sizers grew from 114 to 124 bytes because its generated Code section was already smaller than the reference. Its handle_info/2 frame moved from four to three slots. OTP uses one. The peak frame of diameter_codegen:c_enumerated_avp/2 moved from eight to seven slots. OTP uses twelve.

The regular test runner passed the documentation and installed release image checks. Its whole-module check reached the default 15-second timeout. The same test method completed successfully when invoked directly outside that runner deadline. The full test runner has not produced its final report. Canonical readiness was not regenerated.

- [Complete corpus](otp-corpus.json)
- [Code size gap changes](code-delta.json)
- [Peak frame comparison](peak-slots.txt)
- [Normalized function comparison](normalized-functions.txt)
- [Opcode ranking](opcode-ranking.txt)
- [Release loader check](loader.txt)
- [ex_sizers disassembly](ex-sizers.txt)
- [Focused fixture](call_operand_probe.erl)
- [Focused check](check-call-operands.ps1)
- [Focused output](probe.txt)
- [Standard runner output](focused.json)
- [Direct whole-module check](whole-module.txt)
- [Entry case field regression](entry-case-field.txt)
- [Private constants regression](private-case-constants.txt)
- [wx command regression](wx-text-command.txt)
- [Tagged match regression](tagged-call-match.txt)
- [Tagged sequence regression](tagged-sequence.txt)
- [Tagged lifetime regression](tagged-lifetime.txt)
- [Tagged case regression](tagged-case.txt)
