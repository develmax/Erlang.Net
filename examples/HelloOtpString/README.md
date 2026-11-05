# Explicit string backends

Run from the repository root:

```powershell
dotnet run --project examples/HelloOtpString -p:StringBackend=Managed
dotnet run --project examples/HelloOtpString -p:StringBackend=Beam
```

Both configurations use the same MFA calls and execute on .NET. The output
records the selected backend, original image digest and delegate assembly
identities. Original Erlang `string.erl`/`string.beam` remain in the original
stdlib package. Unicode tables/normalization/graphemes use the explicitly
selected pinned `unicode` and `unicode_util` images in either configuration.
The `InternalScalars413` native provider supplies the original number parser's
runtime primitive. No Erlang executable is needed to run the built example.

MSBuild writes the explicit library manifest and copies the hash-bound images
to the output. A missing dependency or invalid image fails startup without
selecting another backend. See [string validation](../../docs/string-backends.md).
