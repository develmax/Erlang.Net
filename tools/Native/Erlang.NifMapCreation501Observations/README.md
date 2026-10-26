# Windows native empty-map observations

C# implements `enif_make_new_map` at pinned callback slot110. The C fixture exercises actual ABI calls only. The original OTP provider, MSVC build, ordinary loader and reviewed loader remain separate phases with actual resource cleanup. Failed preparation never starts dependent execution; independent original/ordinary/reviewed routes collect their own failures before the combined result.

Run after building the native backend:

```powershell
python -B tools/Native/Erlang.NifMapCreation501Observations/collect.py --repository . --provider-admission artifacts/library376-official-compiler/official-admission.json --plan-sha256 3E2BAFD2AB62E8588434C07C3CD341D9E429333C6AF4F7916E4D2279D9954579 --output artifacts/native-map501/local --runtime-version 10.0.11 --check-golden
```

Use a fresh output directory. Requires the admitted unchanged6427-file official OTP-29.1.1 Windows provider and installed MSVC x64 tools. Only public pinned headers are consumed, never redistributed here. Source plan binds header bytes, build/collector code, fixture sources and original golden. Full-output comparisons use zero normalization. Read receipts and both managed route counts; a source/build hash is not a whole NIF compatibility claim. Linux/native map mutation/iteration are outside this finite Windows constructor scope.
