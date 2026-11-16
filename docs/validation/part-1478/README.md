# Part 1478 — Fold literal atom conversions and align documentation signatures

The BEAM lowering pass folds atom_to_list/1 with a literal atom when the call resolves to Erlang. It uses the same Unicode code point conversion as the runtime. It preserves local definitions and writes original source forms to debug metadata. The entry emitter preserves x0 when a one-arm case returns the atom it matches. The fallback raises case_clause with the actual selector.

The documentation emitter builds the spec and callback signature table in source order. It retains reachable private type entries when a visible declaration refers to them. Exported types remain eligible regardless of references. These rules follow the pinned OTP beam_doc module's signature and type passes.

The released erl_tracer on_load/0 matches OTP instructions after folding. With the documentation fix the complete file matches the Windows OTP 29.1.1 release. The prim_buffer and zlib on_load/0 instructions also match. Those images have differences in other functions and sections. The regression check embeds the original erl_tracer source and image with their upstream provenance. The control tests the Unicode BIF and local override, and checks the successful and rejected paths of the one-arm case.

- [On-load instructions](erl_tracer.txt), [preliminary documentation differences](erl-tracer-remaining.txt), and [release hashes](erl-tracer-hashes.txt)
- [prim_buffer](prim_buffer.txt), [zlib](zlib.txt), [runtime probe](probe.txt), and [local override](atom-shadow.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1478-checked-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1478-checked-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1478-checked-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1478-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1478-checked-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1478-checked-focused.json
```

The complete Windows OTP 29.1.1 corpus generates all 1,366 images with 385 Equal and 981 Different. There are zero rejected sources, unsupported modules and compiler errors. erl_tracer is the only whole-file transition. ELDAPv3 remains Equal after C# ASN.1 generation. The OTP loader accepts both images in every differing pair.

The changes affect 63 release images. Four function bodies gain normalized instruction equality and no body loses it. The Docs chunk reaches equality in 29 files and none lose it. The preliminary compiler-only run reached 384 Equal and 982 Different before the documentation changes.

- [Full corpus](otp-corpus.json), [status transitions](status-changes.txt), and [OTP loader](loader-and-code.txt)
- [Normalized instructions](normalized-functions.txt), [Docs delta](docs-delta.txt), and [remaining preloaded functions](remaining-preloaded-functions.txt)
- [Focused checks](focused.json) and [compiler hashes](compiler-hash.txt)

Five focused checks passed using the compiler binary used for the final corpus. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.
