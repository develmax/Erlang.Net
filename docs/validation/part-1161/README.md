# Part 1161 — forward a call result into a tail-call tuple

The shared forwarded-call planner now uses the preceding call result in X0 directly when building a tuple argument. It retains saved variables in their existing stack slots and removes the slot for the result itself. The probe matches normalized OTP instructions and returns the expected tuple.

The start_link/3 instructions in httpd_misc_sup match the release with the original labels. Child specification construction remains different in httpd_misc_sup and megaco_misc_sup. In the latter, OTP constructs the nested tuple and list after timer:seconds/1 in a four-slot frame. The baseline generated image uses eight slots.

The full run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Nine Code size gaps narrowed. Eleven functions gained instruction matches and none lost one. OTP prepared all differing images for loading and all three focused tests passed.

- [httpd start_link](httpd-start-link.txt)
- [Megaco child specification baseline](megaco-child-baseline.txt)
- [Probe disassembly](disassembly.txt)
- [Probe assertions](run.eval)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
