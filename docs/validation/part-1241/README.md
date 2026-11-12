# Part 1241 — reproduce Diameter accessor abstract forms

Emit the export attribute and generated record accessors before the first original function, preserving the line annotations from OTP's diameter_exprecs transform. Reuse the record definitions in the compiler's generated accessor AST. Drop the applied transform option from abstract code and record the Windows release path to diameter_gen.hrl.

The strict probe rebuilt all six generated dictionary modules. All decoded forms and all six Dbgi chunks match the release. The intermediate comparison identified exactly one wrong header path in each abstract form list before the profile change.

The full Release run generated all 1,366 images with 357 Equal, 1,009 Different and zero rejected. The six Diameter images changed in Dbgi and Docs. Their Docs chunks still differ from the release. OTP accepted all differing image pairs.

- [Strict release probe](abstract-probe.txt)
- [Intermediate header differences](header-path-differences.txt)
- [Full corpus](otp-corpus.json)
- [Chunk delta](debug-delta.txt)
- [Docs comparison](docs-probe.txt)
- [OTP loader](loader.txt)
