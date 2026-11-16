# Part 1326

Run the existing register entry checks and tuple branch planner for lifted functions without a self binding or a separate guard failure entry. The focused test checks the pre-frame tuple test and executes the successful, fallback and function_clause paths with a captured value. OTP routes the unmatched single clause through a lifted failure helper. The generated code still targets function_info directly.

The full Windows corpus generated 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. The Code size gap narrowed in all 463 changed images by a combined 47,091 bytes. Four release functions gained complete instruction matches after label normalization and none lost one. OTP accepted every differing image. ELDAPv3 remained Equal.

In the first-opcode-difference ranking, the tuple test versus allocate group decreased from 661 modules to 627. The tagged tuple group decreased from 526 to 516. The move versus init_yregs group with different allocations increased from 793 to 795.

- [Focused check](check-closure-entry-checks.ps1)
- [Focused output](probe.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
- [Opcode ranking before](opcode-ranking-before.txt)
- [Opcode ranking after](opcode-ranking-after.txt)
