# Part 1242 — preserve match specification abstract forms

Retain the original call for each transformed ETS match specification. Read its source forms and serialize the transformed lists, bindings, guards and actions with OTP coordinates. Keep unaffected expression nodes so the abstract writer retains parser token identity. Restore the release header paths in the five affected profiles.

The probe matches OTP for a match specification with multiple clauses, a captured variable, a guard and a head alias. The strict release probe generates full abstract code for all five modules. Three Dbgi chunks match byte for byte. Dialyzer still has two missing source-text annotations for the quoted maybe atom. Yecc differs at one coordinate.

The full corpus generated all 1,366 images with 357 Equal, 1,009 Different and zero rejected. Only Dbgi changed across eight modules. In three modules that already had abstract code, removing the applied parse transform makes their compile attributes match OTP. The complete chunks in those three still differ. OTP accepted all differing image pairs.

- [Synthetic abstract form comparison](probe.txt)
- [Probe source](debug_match_spec_probe.erl)
- [Strict release comparison](abstract-probe.txt)
- [Remaining abstract form differences](remaining-abstract-differences.txt)
- [Compile attribute comparison](compile-attribute-probe.txt)
- [Full corpus](otp-corpus.json)
- [Chunk delta](debug-delta.txt)
- [OTP loader](loader.txt)
