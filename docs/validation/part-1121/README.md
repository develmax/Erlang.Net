# Part 1121 — share complementary entry comparisons

Two adjacent frameless arithmetic clauses with complementary first comparisons can share that test. The compiler compares operand positions and literal values, then routes a failed first comparison directly to the second clause's remaining comparison. If the first comparison passes and its conjunction fails, neither clause can match. A successful comparison of a variable against zero supplies a conservative numeric upper bound for division and remainder operands.

The OTP 29.1.1 probe matched both arithmetic branches through the fallback label, including the generic operand type and the negative range annotations. Execution verified both matched cases, both mixed-sign fallbacks, the zero fallback and badarith.

- [Probe source](beam_guarded_bif_tail_probe.erl)
- [Execution and disassembly assertions](run.eval)
- [Matching disassembly](disassembly.txt)

The complete release fQuotient/2 instruction list matches after local label renumbering, including line directives and typed operands. Its module, xmerl_xsd_type, is the only changed image. The Code section grew by three bytes (32,243 to 32,246 versus 21,130 in OTP) while its Type section grew from 14 to 24 bytes, toward 372 in OTP. The module still differs.

The full corpus generated 1,363 images, with 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP accepted all differing images during loader preparation.

- [Complete normalized fQuotient/2 comparison](fquotient.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
