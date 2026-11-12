# Part 1046 move-chain scan order

OTP beam_ssa_codegen:collect_chain returns the reverse scan order of the remaining assignments when no predecessor is found. The general direct tail-call emitter now follows that rule. The mixed probe's move sequence matches OTP and its generated image executes successfully.

The decoded lifted-tail comparison gained four matches and lost none. Both prior losses in cover and qlc are restored. All 1021 different generated files still prepare successfully in OTP. The full corpus admitted and generated 1363 of 1366 modules. All individual byte comparison statuses stayed the same, including the exact ELDAPv3 image.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Lifted tail comparison](tail-bodies.txt)
- [Loader results](loader.txt)
- [Generated mixed probe](mixed-after.txt)
- [OTP mixed probe](mixed-otp.txt)
