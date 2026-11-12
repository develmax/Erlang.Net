# Part 1156 — reserve heap before extracting tail argument fields

The constructor emitter can reserve heap space before extracting a list or tuple field. At that point only the original argument registers are live. The entry emitter uses the source register for the final extraction after tests that can branch elsewhere.

Both probes match OTP 29.1.1 after local label normalization and return the expected values. The full run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Thirty-one images changed and seven Code size gaps narrowed. The instruction comparison found 236 changed functions, with four matches gained and none lost. OTP prepared all differing images for loading. All three focused tests passed.

- [Oracle probes](disassembly.txt)
- [Probe assertions](run.eval)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
