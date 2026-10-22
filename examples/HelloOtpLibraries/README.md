# Original OTP libraries in an ordinary .NET application

This application explicitly selects eleven retained original OTP29.1.1 BEAM images through `ErlangLibrary` project items. It executes original maps, proplists, binary, lists, queue, ordsets, sets, dict, orddict, gb_sets and gb_trees on the Erlang.Net runtime. There is no original Erlang VM subprocess, runtime compiler, PATH lookup or managed fallback.

```powershell
dotnet build examples/HelloOtpLibraries/HelloOtpLibraries.csproj
dotnet examples/HelloOtpLibraries/bin/Debug/net10.0/HelloOtpLibraries.dll
```

`Program.cs` explicitly loads the generated manifest and calls `ProcessRuntime.CreateAsync`. Merely deploying a manifest does not change the default constructor. The output reports actual selected image hashes, public exports, native boundaries and an executed result for each library. `Otp/provenance.json` binds upstream source/image provenance. Original Erlang sources are retained unchanged alongside the images; their presence does not trigger compilation.

The five newly selectable collections (sets, dict, orddict, gb_sets, gb_trees) currently require Beam. The six earlier libraries retain their explicit Managed alternatives. Native BIF/NIF stub bodies and runtime compiler helpers remain C# integration points; pure selected library functions execute original BEAM. Finite comparison evidence and remaining compatibility scopes are recorded in the English changelog and `docs/OTP_BACKEND_PROGRESS.md`.

For a separate cold PackageReference consumer, use the local package workflow from HelloOtpBackends with `UseInstalledPackage=true`. Retained images require no installed original compiler. Building `.erl` input is a separate admitted `ErlangBeamSource` route, documented in `docs/RUNTIME_LIBRARY_SELECTION.md`.
