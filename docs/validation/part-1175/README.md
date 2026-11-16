# Part 1175 — reuse an X register for tuple literal comparisons

The general emitter uses X0 for tuple fields that are read solely to compare them against a literal during entry pattern matching. Nested tuple tests reuse the same register. Bound variables and parent tuples keep their existing storage.

The probe passed on both OTP and the C# output. It covers nested tuple matching, failing comparisons, a non-tuple input and bindings surviving a call. The generated frame decreased from nine to four slots. OTP uses two and tests the tuple before allocating.

The full Windows OTP 29.1.1 run generated 1,363 of 1,366 images. It recorded 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. Of the 674 changed differing images, 612 moved closer in Code length. The gap widened by two bytes in ex_sizers, whose Code was already shorter than the reference. The remaining 61 changed images had no Code length gap change. All differing images were accepted for loading by OTP and all three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Loader](loader.txt)
- [Focused tests](focused.json)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
- [Opcode ranking](ranking.txt)
