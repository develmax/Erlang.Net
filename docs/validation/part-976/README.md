# Part 976 entry type guards

The single-clause general emitter now checks the leading conjunction of unary type guards on input arguments in X registers before allocation. It verifies the BIF binding and stops at guards that need values from nested patterns or other evaluation. A failure before allocation uses the original argument registers. The existing framed path handles the remaining guard expression. No dependency was added.

All three focused compiler checks passed, including the runtime fixture for entry and framed guard failures. [Focused checks](focused.json).

In the 235 generated wx images, modules with a first opcode difference of is_integer against allocate fell from 64 to 9. The corresponding is_list group fell from 78 to 58. In 73 modules a function now exposes move against init_yregs as its first opcode difference. The groups overlap. Glu samples show the argument move order and an additional frame slot after guards whose opcode sequences now agree. [Opcode comparison](wx-opcodes.txt) and [frame samples](frame-samples.txt).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1140 Different and 3 SourceRejected. The equal set is unchanged and includes ELDAPv3. [Corpus](otp-corpus.json).
