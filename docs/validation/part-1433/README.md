# Part 1433 — Keep accepted case fields in X registers

A tagged case branch can now extract its fields to X registers after the tag and field checks pass. The construction and call planners consume those registers alongside existing Y values. A whole-tuple alias can refer directly to x0. Branches with unsupported bodies or repeated new field bindings keep the existing emission path.

The OTP oracle covers construction, passing a field to a local call, returning the whole selector and preserving the case_clause payload. The two C# probe frames shrink from four to three slots. The reference uses two slots. In compile_proc/3, the single-file path passes the suffix without a field spill. The multiple-files path still determines the six-slot frame size, and calls still join the shared return path.

- [OTP oracle](check-register-case-fields.ps1), [before](previous-probe.txt), [after](probe.txt), and [compile_proc instructions](compile-proc.txt)

All 1,366 release images generate with zero rejected sources and zero compiler errors. There are 382 Equal and 984 Different. ELDAPv3 remains Equal from C#-generated Erlang. The OTP loader prepares all 984 differing images.

The change reaches 371 images and 934 functions. Frame size moves closer to OTP in 289 functions, including 75 that reach the reference size. The aggregate Code size gap decreases by 2,558 bytes. Whole-image and normalized-function equality stay unchanged. All three targeted .NET tests pass.

- [Corpus](otp-corpus.json), [frame statistics](summary.txt), [instructions](normalized-functions.txt), and [loader and Code comparison](loader-and-code.txt)
- [Focused tests](focused.json), [compiler identity](compiler-hash.txt), and [remaining allocation groups](sequence-endings.txt)
