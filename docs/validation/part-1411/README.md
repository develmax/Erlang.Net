# Part 1411 — Project call result tuples into registers

The entry tuple match emitter now schedules the calls that consume its fields. It projects only the fields the continuation uses, reuses X0 for the final field when possible, and can construct a list or tuple argument before overwriting the original tuple. Calls nested in the tail arguments can consume a field and pass their result to the final call in X0. Only inputs used after the producing call are saved on the stack.

For local calls with a proven exact tuple arity, the emitter skips shape tests when the pattern introduces only fresh bindings. Unknown results retain tuple and arity tests with a badmatch branch. The emitter also handles the resolved date/0 BIF and keeps the match line separate from the non-failing BIF call.

The deterministic fixture matches OTP byte for byte. It covers a local tuple producer, an argument built from an extracted field, a nested call, the date BIF, and badmatch errors for a non-tuple and for a tuple of the wrong arity.

The final Windows OTP 29.1.1 run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains equal. OTP prepared all 986 differing images for loading. The change modifies 55 images. Normalized instructions newly match in 180 functions across 45 modules with no previous match lost. The total Code size gap shrinks by 2,876 bytes. All eight changed peak frame sizes reach the OTP size.

The release functions parse_FieldName/1, parse_Enumeration/1, ct_logs:year/0 and remove_xmlnsattr_for_tag/2 now match the normalized OTP instructions. The refreshed source audit finds 42 functions across 34 modules with an initial tuple match, immediate tail call and differing entry frame. Tagged tuple matches at entry occur in 148 functions across 101 modules with differing entry frames. These counts identify candidates for comparison.

The first focused run reached the whole-module timeout while the corpus diagnostics were running. All three selected cases passed in the isolated retry with the corpus compiler assembly. Full runtime verification, canonical readiness regeneration and publication are pending.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Frame delta](frame-delta.txt), [frame audit](frame-audit.txt) and [source audit](source-audit.txt)
- [Oracle](check-call-tuple-match.ps1), [result](probe.txt), [hashes](fixture-hashes.txt) and [compiler tests](focused.json) and [first focused run](focused-concurrent.json)
