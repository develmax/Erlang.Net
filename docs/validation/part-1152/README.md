# Part 1152 — keep a call result live through list construction

The forwarded-call planner recognizes a terminal argument built by prepending bound variables to the preceding call result. It keeps the result in X0 and writes the list directly to its argument register.

In asn1ct add_generated_function/1, this removes a stack slot, init_yregs, a spill and an argument move. Its instructions match OTP 29.1.1 after local label normalization. The permanent probe verifies an improper list that uses the result as both a head and a tail. OTP reuses X0 for the intermediate cons in that probe. The managed compiler uses the destination register for both cons instructions.

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Two images have smaller Code gaps. The normalized comparison found one gained instruction match and none lost. OTP prepared all differing images for loading. All three focused compiler tests passed.

- [Corpus](otp-corpus.json)
- [Delta](delta.json)
- [Affected images](changed-images.txt)
- [Instruction comparisons](function-comparison.txt)
- [asn1ct example](asn1ct-add-generated-function.txt)
- [Probe disassembly and execution](disassembly.txt)
- [Probe assertions](run.eval)
- [Focused tests](focused.json)
- [Loader](loader.txt)
