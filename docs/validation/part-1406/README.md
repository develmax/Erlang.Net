# Part 1406 — Keep process dictionary reads in registers

The call emitter plans sequences of process dictionary reads that feed an ordinary tail call. It keeps values in X registers and frees a register after its last use. For a computed tuple key, it reserves heap space before the reads and attaches the tuple type to the BIF operand. It prepares the tail arguments using the existing register move scheduler.

The deterministic regression fixture matches OTP byte for byte. The oracle verifies absent and populated keys, an explicit erlang:get/1 call and tail argument moves where an input register is already occupied. The release disassembly of asn1ct_parser2:resolve_module/1 matches after normalizing labels.

The final run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains equal. One image changed, and its Code gap narrowed by 21 bytes. OTP prepared all 986 differing images for loading. All three selected compiler cases passed with the corpus compiler assembly.

The refreshed audit distinguishes default BIF signatures from other unqualified calls and groups structured matches by pattern and value. Its largest group is a bound remote call followed by a sequence ending in case, with 131 functions in 96 modules. The signatures are a syntax classification and may include overridden auto-imports.

- [Full corpus](otp-corpus.json)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-dictionary-sequence.ps1), [result](probe.txt) and [hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Frame audit](frame-audit.txt) and [audit script](frame-audit.escript)
