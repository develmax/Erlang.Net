# Part 975 entry tuple tests

The general emitter checks top-level tuple arity and atom tags in X registers before allocating the frame for a single clause. The entry failure jumps directly to function_info with the original arguments. Nested patterns, alias binding and guard failures use the existing framed path. This follows the instruction shape observed in the release and the is_tagged_tuple lowering in OTP 29.1.1 beam_ssa_codegen.erl. No dependency was added.

A runtime check covers tagged and plain tuple bindings, early shape and tag failures, and later nested match and guard failures. All three focused checks passed. [Focused checks](focused.json).

Across the 235 generated wx images, the old first difference is_tagged_tuple against allocate disappears in 365 functions in 117 modules. For is_tuple against allocate it disappears in three functions in three modules. Other function paths still expose these opcode pairs. The comparison pairs functions by name and arity and ignores labels, line instructions and func_info, as in [Part 974](../part-974/wx-disasm.eval). [Function delta](wx-tuple-delta.txt) and [remaining differences](wx-opcodes.txt).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1140 Different and 3 SourceRejected. The set of exact matches is unchanged and includes ELDAPv3. [Corpus](otp-corpus.json).
