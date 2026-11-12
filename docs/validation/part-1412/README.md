# Part 1412 — Check tagged results between calls

The saved-call planner now accepts calls followed by tagged tuple matches and exact atom matches. A combined tuple and tag test runs while X0 still holds the original value. A used payload can flow into the next argument register or through the existing spill planner. Projection runs only for live payloads. Success falls through to the next call. Out-of-line badmatch blocks retain the original value and the matching source line.

The planner supports tail calls, constant returns and tuple returns after these checks. Before a final checked call, it can trim dead slots and defer an independent X0 constant until after the trim.

The deterministic fixture covers an extracted payload passed to put/2, consecutive tagged matches, a checked sequence returning a constructed tuple, and an exact atom match. Its complete image matches OTP, including the two badmatch cases and the process dictionary update.

An intermediate run changed the register schedule of ttb:write_binary/2. The established plan now takes precedence for that shape and its normalized instructions again match OTP.

The final Windows OTP 29.1.1 run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains equal from C# generated Erlang source. OTP prepared all 986 differing images for loading. The change modifies 22 images. Normalized instructions newly match in 11 functions across eight modules, and all previous matches are preserved. The total Code size gap shrinks by 622 bytes. Peak frame sizes reach the reference in 20 functions. In edoc_layout_chunks:anno/2 the frame now uses two slots while OTP uses three.

The refreshed audit finds 132 functions in 94 modules with an initial tagged tuple match and differing entry frame. Tuple matches immediately followed by case appear in 88 functions across 57 modules, and their tagged equivalents appear in 23 functions across 22 modules. These are candidates for the next instruction comparisons.

All three selected compiler cases passed with the corpus compiler assembly. Full runtime verification, canonical readiness regeneration and publication are pending.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Instructions before](operands-before.txt), [after](operands-after.txt) and [ttb schedule](preserved-ttb.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Frame delta](frame-delta.txt), [frame audit](frame-audit.txt) and [source audit](source-audit.txt)
- [Oracle](check-checked-call-sequence.ps1), [result](probe.txt), [hashes](fixture-hashes.txt) and [compiler tests](focused.json)
