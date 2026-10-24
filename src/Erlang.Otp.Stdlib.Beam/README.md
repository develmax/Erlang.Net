# Original OTP stdlib for .NET

This library owns all98 original stdlib module names from OTP29.1.1 (stdlib8.1), with embedded BEAM images, original Erlang sources, source/include support, stdlib.app/stdlib.appup and producer provenance. The initial18 admitted images and their compilation variants retain their existing bytes. The additional80 images, generated sources and support files come from the SHA256-bound official Windows x64 release inventory. No Erlang installation or checkout is required at runtime.

Add a `ProjectReference` to `src/Erlang.Otp.Stdlib.Beam/Erlang.Otp.Stdlib.Beam.csproj`, or install the `Erlang.Otp.Stdlib.Beam` package. Package production with `UseInstalledPackage=true` depends on `Erlang.Net.CSharp` 0.1.0; repository builds use project references instead.

```csharp
using Erlang;
using Erlang.Otp;

var selection = new RuntimeLibrarySelection([
    OtpStdlib.Select("lists", RuntimeModuleBackend.Managed),
    OtpStdlib.Select("queue", RuntimeModuleBackend.Beam)
]);
await using var runtime = await ProcessRuntime.CreateAsync(selection);
```

`Managed` selects an existing C# alternative; `Beam` selects the original embedded image, checked against the retained SHA256 before loading. Unsupported C# alternatives and unknown modules fail explicitly. There is no fallback. Selection happens before runtime startup and does not migrate live module state. The maintained C# alternatives remain in `Erlang.Otp.Stdlib` and `Erlang.Otp.Kernel`.

The eleven collection/binary images retain their established explicit startup dependencies. Include every dependency in the selection, selecting its backend separately. For example, selecting original `queue` requires selecting `lists` too. The catalog does not automatically select a backend for dependencies.

The other87 images, including `gen`, `sys`, `proc_lib`, `io_lib`, `io_lib_format`, `string` and `unicode_util`, are reusable via `OtpStdlib.ReadImage(name)` and ordinary `ModuleRegistry.PrepareBeam` / atomic publication. `Select` requires explicit dependencies for these modules because their complete startup dependency closure has not been selected by the consumer. Their presence alone does not boot kernel or satisfy every import. Native primitives, full boot closures and application startup retain their existing runtime configuration.

`Otp/` is the primary source/image catalog. `Variants/BackendSelection/` preserves the distinct historical lists/queue compilation artifacts used by the backend example, including their own hashes and provenance. Observer/callback fixtures remain in examples; they are not original OTP libraries. `OtpStdlib.ReadApplication` and `ReadApplicationUpgrade` expose the checked original application resources. The upstream OTP license is included; existing upstream source notices are preserved.

See `examples/HelloOtpLibraryReference` for an ordinary .NET executable with no custom MSBuild imports or external image paths. The older `ErlangLibrary` manifest approach remains available for explicit filesystem images.
