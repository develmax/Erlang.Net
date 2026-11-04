# Unicode backend selection

Run the same API against either implementation on the .NET runtime:

For a source checkout, first build the current MSBuild library task:
`dotnet build tools/Build/Erlang.BeamBuild -m:1`.

```powershell
dotnet run --project examples/HelloOtpUnicode -p:UnicodeBackend=Managed
dotnet run --project examples/HelloOtpUnicode -p:UnicodeBackend=Beam
```

The example converts mixed Unicode text to UTF16 little endian and normalizes
decomposed text to NFC. Output identifies the selected backend, original image
hash (BEAM only) and managed assembly identities. The pinned unicode_util BEAM
supplies Unicode tables in either selection. Erlang/ERTS is not required to run
the deployed example. Startup requires explicit dependencies and never falls back.

The default `Managed` selection now uses C# for both `unicode` and `unicode_util`.
Its generated library manifest contains no BEAM images. `Beam` selects both
unchanged original modules. See [backend selection and pinned compatibility](../../docs/unicode-backends.md).
