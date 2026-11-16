# Part 1413 — Project call results into case branches

The compiler projects a field from a matched call result directly into the case selector register. In branches with simple returns, other live fields can stay in X registers. Fields needed across calls and input arguments needed by the branches use stack slots. The checked producer preserves the original value for badmatch.

A case selector can call another function with a projected field and can wrap that call in catch. The compiler reuses the existing register case emitter and preserves an already matching tagged result when the branch would rebuild exactly the same tuple.

The deterministic fixture matches the complete OTP image. It exercises a caught parser call, direct case selection, an additional field kept in a register, and badmatch on a wrong tag and on a non-tuple.

The final Windows OTP 29.1.1 run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains equal from C# generated Erlang source. OTP prepared all 986 differing images for loading. Three images change and all three changed peak frame sizes reach the reference. ct_config_xml:list_to_term/1 gains a normalized instruction match. All previous matches are preserved, and the Code size gap shrinks by 148 bytes.

The refreshed source audit distinguishes 51 tuple-match cases with call selectors across 41 modules, 25 with tagged tuple branches across 20 modules, and 23 with tagged list-head branches across two modules. The consecutive tuple-match group spans 105 functions across 50 modules. These are candidates with differing entry frames.

All three focused compiler cases passed using the corpus compiler assembly. Full runtime verification, canonical readiness regeneration and publication are pending.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Frame delta](frame-delta.txt), [frame audit](frame-audit.txt) and [source audit](source-audit.txt)
- [Oracle](check-tuple-case.ps1), [result](probe.txt), [hashes](fixture-hashes.txt) and [compiler tests](focused.json)
