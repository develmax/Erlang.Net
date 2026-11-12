# Part 1057 preserve registers before supervisor calls

The compiler builds tagged tuples and forwards constant arguments in X registers. It also groups atom branches after an element lookup and forwards the chosen supervisor with the input tuple in a singleton list.

The pinned Windows OTP 29.1.1 images for diameter_sctp_sup and diameter_tcp_sup both match byte for byte. The executed probe checks normal dispatch, badarg and case_clause. In its identity branch, OTP elides a move that the new emitter still produces.

The full corpus generated 1363 of 1366 files. The two Diameter modules are the only status changes. ELDAPv3 remains Equal. OTP prepared all 1015 differing images.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Permanent release fixtures](fixture.json)
- [Loader results](loader.txt)
- [Executed probe](after.txt)
- [OTP probe](otp.txt)
- [Instruction differences by affected image](opcode-ranking.txt)
