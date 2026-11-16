# Part 1254 — Prepare computed arguments and share simple local tails

Use the existing argument preparation path for local tail calls with constructed values, computed calls, or arithmetic in terminal atom cases. Merge branches only when their local calls have the same target and identical variable or constant operands.

The instructions of `mnesia_lib:verbose/2` match Windows OTP 29.1.1 after label normalization. Three atom keys share one call block and the other arm constructs its tuple before the local tail call. The probe verifies argument evaluation order, error precedence, tuple and list construction, and shared dispatch labels.

The full run generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. All ten changed images moved closer to the release Code section length, by 265 bytes in aggregate. The tail branch ranking is down from 875 candidates across 217 modules to 860 across 215. OTP accepted all 1,007 differing pairs.

The remaining ranked examples include `asn1ct_check:check_enum_number_root/4` and `check_enum_ext/5`. These require tail lowering inside guarded entry clauses and after branch-local bindings.

- [Probe source](computed_atom_tail_probe.erl)
- [Probe command](check-computed-atom-tails.ps1)
- [Probe result](probe.txt)
- [Normalized verbose/2 comparison](verbose.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Updated tail branch ranking summary](atom-tail-ranking-summary.txt)
- [OTP loader](loader.txt)
