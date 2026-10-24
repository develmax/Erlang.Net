# Explicit OTP backends in an ordinary .NET console

Build and run with `dotnet run --project examples/HelloOtpBackends`. The default selects the retained original OTP-29.1.1 `lists` and `queue` images. They execute in our .NET runtime; the application does not start `erl`, use an installed OTP directory or compile Erlang at runtime.

Select the maintained alternatives explicitly:

```powershell
dotnet run --project examples/HelloOtpBackends -p:ListsBackend=Managed -p:QueueBackend=Managed
dotnet run --project examples/HelloOtpBackends -p:ListsBackend=Managed -p:QueueBackend=Beam
```

`ErlangLibrary` items generate `erlang-libraries.json` and hash-named image files in the build/publish output. The application loads that exact manifest with `RuntimeLibraryManifest.Load` and creates its runtime with `ProcessRuntime.CreateAsync`. Startup prepares the complete selection before publication, validates pure dependencies and native primitive bindings, and refuses invalid images without choosing a managed replacement. Missing files and hash mismatches are errors. Omitted modules retain ordinary defaults.

The program reports the actual selected backend, BEAM SHA256, module MD5, native primitive exports, managed assembly identities and explicit dependencies. It checks current image ownership before invoking the compiled observer. This is a startup snapshot, not automatic live backend switching. The observer's own source worker provides the same process root for original/.NET comparisons; no native `erl_eval` frames are removed from results.

The `src/Erlang.Otp.Stdlib.Beam/Variants/BackendSelection` directory owns the unchanged pinned sources, distinct retained compiled images and provenance. SHA256 binds the selected image; a user-written manifest alone does not establish upstream provenance or whole-module compatibility. The source and prior producer evidence remain separately recorded. Upstream notices are retained and the project copies the OTP license. The compiled observer exercises successful library/callback calls and caught errors with full backtraces; optional `--observations <new-directory>` saves raw ETF packets and the backend report for validation.

For a standalone NuGet consumer without repository-relative image paths, use `examples/HelloOtpLibraryReference` with the `Erlang.Otp.Stdlib.Beam` package. This older example retains its filesystem manifest route and exact historical image variants. Local package creation/installation is not external package publication.

Only modules/images admitted by the startup library API can be selected. This example establishes lists/queue integration, not complete OTP compatibility, arbitrary module support or a performance advantage. Original Erlang/BEAM, maintained C# and generated C#/IL are distinct execution paths.
