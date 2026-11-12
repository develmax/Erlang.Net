# Part 1266 — Match Unicode conversion frames and tuple arity alternatives

For constant-module wrappers with a NUL-terminated Unicode conversion, allocate the cons cell with the call frame and save only the arguments needed after conversion. Reload the queue operands and trim the saved slots before the external call.

For two exact tuple arity alternatives on the same argument, emit one tuple shape check and two arity checks. Accept both short-circuit disjunctions and separate guard alternatives.

The standalone `gl` image matches 1,058 of 1,068 functions after label normalization, up from 1,024. The Code section falls from 58,751 to 57,097 bytes, with 56,777 in the release. The import table now has the release size but its bytes still differ. `Dbgi` remains byte-identical.

The full run generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Only `gl.beam` changed, and the corpus image matches the standalone rebuild. The C# ASN.1 generated `ELDAPv3.beam` remains Equal. OTP accepted all differing pairs.

- [Probe source](unicode_arity_probe.erl)
- [Probe command](check-unicode-arity.ps1)
- [Execution result](probe.txt)
- [Normalized gl function comparison](gl-functions.txt)
- [Ten remaining gl functions](gl-remaining.txt)
- [Standalone gl section comparison](gl-chunks.txt)
- [Atom and import membership comparison](gl-table-members.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
