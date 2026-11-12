# Part 1394 — Dispatch tuple arities before allocating the frame

The grouped tuple entry can select any argument and retain a final unconditional clause as the fallback. The arity table uses the original argument, keeps source clause order within an arity and branches to the fallback with the original inputs. Simple returns and tail calls can stay in registers. Grouped patterns preserve register scratch space for literal tests and can return the whole matched tuple through an alias. A tail call can extract its final field into a dead tuple register after all tests pass.

The focused OTP oracle exercises tag and arity mismatches, repeated variables, guard failure, aliases and local and external tail calls. Three probe functions match the normalized OTP instructions. The permanent deterministic fixture matches the complete OTP 29.1.1 image. The selected compiler cases, whole-module check and previous list-entry oracle pass with the same compiler assembly used by the corpus.

The final Windows OTP 29.1.1 corpus generated all 1,366 images, with 372 Equal, 994 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepts all 994 differing images for loading.

The change affects 310 release images. The first tuple test matches OTP in 352 functions across 178 modules that previously allocated first. Ten functions gain normalized instruction matches and none lose one. Peak frames move closer to OTP in 112 functions, including 17 that reach the OTP size. Another 72 move farther, including two that lose an exact frame size. The Code gap narrows in 100 images and widens in 138. The net gap shrinks by 2,351 bytes.

The tuple-before-allocation group falls from 2,187 functions in 558 modules to 1,831 in 526. The largest remaining frame group is allocation operands in variable-head sequences, with 1,802 functions across 558 modules. The largest source subgroup starts with a local call result bound to a variable, covering 612 functions in 237 modules. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Tuple entry delta](tuple-entry-delta.txt)
- [Tuple source groups before](tuple-entry-groups.txt)
- [Remaining tuple source groups](remaining-tuple-entry-groups.txt)
- [Release disassembly before](tuple-entry-before.txt)
- [Release disassembly after](tuple-entry-after.txt)
- [Frame audit](frame-shapes.txt)
- [Sequence prefixes](sequence-prefixes.txt)
- [Normalized instructions](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size gaps](code-delta.json)
- [OTP loader check](loader.txt)
- [Focused oracle](check-tuple-arity-fallback.ps1)
- [Oracle result](probe.txt)
- [Complete fixture hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Whole-module check](whole-module.txt)
