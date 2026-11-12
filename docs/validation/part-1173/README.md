# Part 1173 — share integer guards across wx overloads

For a wx command overload that dispatches on an integer or a pair of integers, the emitter now checks the shared integer prefix once. It validates the tuple fields in one scratch register and preserves the tuple for the queue call.

The Windows OTP 29.1.1 corpus contains 1,366 release images. wxGridSizer and wxFlexGridSizer changed from Different to Equal. The run reported 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP prepared all 1,007 differing images for loading. All three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](part1173-delta.json)
- [Focused tests](part1173-focused.json)
- [Loader](part1173-loader.txt)
- [Opcode ranking](part1173-ranking.txt)
