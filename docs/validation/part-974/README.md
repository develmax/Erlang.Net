# Part 974 generated wx release profile

Select the pinned wx bootstrap release profile for sources in wx/src/gen. The gl image has a different compiler and source path. The corpus still compiles the original Erlang sources. No dependency was added.

| Differing chunk among the 139 affected images | Before | After |
| --- | --- | --- |
| CInf | 139 | 0 |
| Dbgi | 139 | 1 |
| Docs | 139 | 6 |
| Code | 139 | 139 |
| Line | 139 | 134 |

The complete corpus has 223 Equal, 1140 Different and 3 SourceRejected. The equal set is unchanged and includes ELDAPv3. All three focused checks passed. [Corpus](otp-corpus.json) and [focused checks](focused.json).

Disassemble the 235 images emitted with the common profile and pair them with the installed reference images by module. The diagnostic compares the first different opcode per matching function after ignoring label, line and func_info. It counts modules per opcode pair, so groups can overlap. The leading divergences are is_tagged_tuple against allocate in 128 modules and is_tuple against allocate in 93. [Diagnostic expression](wx-disasm.eval) and [results](wx-opcodes.txt).
