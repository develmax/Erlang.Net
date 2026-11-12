# Part 977 argument spills before frame initialization

The general emitter copies arguments into their reserved Y registers immediately after allocation. It initializes only the remaining slots, following the treatment of stack slots initialized by copies in OTP 29.1.1 beam_ssa_codegen.erl. The existing frame sizes and register numbers are retained. No dependency was added.

All three focused compiler checks passed. The full Windows OTP 29.1.1 corpus has 223 Equal, 1140 Different and 3 SourceRejected. The equal set is unchanged and includes ELDAPv3. [Focused checks](focused.json) and [corpus](otp-corpus.json).

The first opcode difference move against init_yregs previously appeared in 73 generated wx modules. It no longer appears in the comparison. Another 67 modules now expose get_tuple_element against init_yregs as a first opcode difference. [Opcode comparison](wx-opcodes.txt).

Across 235 generated wx images, the first allocate or allocate_heap is present only in our code for 333 functions in 102 modules. Match functions by name and arity for this count. The largest frameless OTP opcode pattern is test, test, move, call_only in 93 functions across 55 modules. One example is wxAuiManager:addPane/2, a guarded default-argument forwarder. [Frame counts](frame-counts.txt) and [frameless patterns](frameless.txt).
