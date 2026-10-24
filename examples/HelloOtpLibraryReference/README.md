# Select original Erlang or C# in an ordinary .NET project

The project references `Erlang.Otp.Stdlib.Beam`; it has no image files or custom MSBuild imports. `Program.cs` explicitly selects each module's backend and checks both the executed result and actual BEAM/managed ownership.

```powershell
dotnet build examples/HelloOtpLibraryReference -c Release -m:1
dotnet examples/HelloOtpLibraryReference/bin/Release/net10.0/HelloOtpLibraryReference.dll Managed Beam
```

The two arguments select `lists` and `queue` respectively. Each accepts `Managed` or `Beam`. All four combinations return `[1,2,3]`. Omitted arguments select managed lists and original queue. Unknown values and unsupported selections fail explicitly.

For NuGet, reference `Erlang.Otp.Stdlib.Beam` version `0.1.0` instead of the project, or set this example's `UseInstalledPackage=true` property with the local package feed configured. Its dependency is `Erlang.Net.CSharp` version `0.1.0`. Set `ErlangPreprocessEnabled=false` for plain C# consumers; hybrid source preprocessing is unnecessary for this route. The BEAM images are embedded in the library assembly and work in a published directory without the repository or installed OTP. This does not imply complete compatibility or that all OTP libraries have already been packaged.
