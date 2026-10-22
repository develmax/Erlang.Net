# Original OTP bootstrap on the .NET runtime

The runtime can execute the pinned OTP29.1.1 `init:boot/1`, `erl_prim_loader`
and `code_server` directly from their original BEAM images. These services are
not replaced by C# request handlers. The original compiler is used to prepare
the retained test workers; the .NET execution does not launch ERTS.

The host configures a fresh `ProcessRuntime` before starting actors:

1. Register the explicit `EtsStorageCore | PersistentTermCore` native providers
   with `RuntimeNativePrimitiveProviders.Register` inside module initialization.
2. Register `RuntimeBootstrapFileSystem.Register(runtime, registry)`. This
   Windows-only opt-in enables actual system-process creation and the static
   `prim_file` NIF subset. Other modules keep their ordinary NIF loading route.
3. Prepare and publish the original library images with explicit
   `BeamDependencySelection`. The exercised set is `init`, `erl_prim_loader`,
   `code_server`, `prim_file`, `filename`, `lists`, `os`, `unicode`, `string`,
   `unicode_util`, `gb_sets`, `gb_trees` and `file`. Declaring an import does not
   load that dependency; every reached pure function must have its real owner.
4. Invoke the original `prim_file:on_load/0` in a real runtime process. Its actual
   janitor and exact-image static NIF attachment are required before filesystem
   requests. Only four filesystem NIFs are currently implemented.
5. Supply UTF-8 binary command-line arguments to
   `await runtime.StartOtpBootAsync(arguments, cancellation)`. The boot file is
   an actual external-term encoded OTP boot script on the configured filesystem.

`StartOtpBootAsync` is a one-time operation. It transfers the registered `init`
name from the host control initializer to the new original `init:boot/1`
process. It returns a `RuntimeOtpBootSession` after the real
`notify_when_started` exchange; `InitializationProcess` identifies that process
and `Completion` follows its lifetime. The host control initializer remains a
separate process. Cancellation or failure after the transfer disposes the owned
runtime and joins its cleanup. A missing original init or a pre-cancelled call
is refused before changing the registration.

The Part410 fixture uses a legitimate custom boot script and a source-owned
kernel launcher which calls the original `code_server:start_link/1`. The
original primitive loader reads and dynamically publishes the two worker images
from the real filesystem. Embedded mode has an empty code path. Interactive
fixtures exercise real `Root/lib` directories. No expected observation is
installed as a reply or imported into executing guest state.

This is an explicitly configured ordinary `PrepareBeam` bootstrap route. It is
not yet the complete 258-image standard `ErlangLibrary` dependency closure, the
standard kernel/application boot script, or the `code:start_link` native suite.
Full system-process fatal/restart semantics, the remaining `prim_file` NIFs,
Linux filesystem support, large-file representation and physical scheduling or
memory parity remain open. `system_info(debug_compiled)` describes our own
build, not the original oracle's build.

The executable host configuration is in
`tests/Erlang.Tests/CodeServerRuntime410Checks.cs`; the source-owned boot fixture
and whole-term/full-stack comparisons are in `CodeServerOriginal410*`. Test
helpers used there are not public runtime APIs. Validation counts and retained
failures are recorded in [the active changelog](changelog/0071.md).

Part411 adds a separate strict startup route:

```csharp
var options = new RuntimeLibraryStartupOptions(
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.PersistentTermCore,
    bootstrapFileSystem: true,
    onLoadOrder: selectedOriginalCallbackOrder
);
await using ProcessRuntime runtime = await ProcessRuntime.CreateWithOptionsAsync(
    selection, options, cancellation
);
```

The selected order must contain every selected on_load module exactly once.
All imports and native bodies are checked before ordinary publication;real
callbacks execute in the supplied order and failed initialization disposes the
owned runtime. Module-import cycles are not a callback ordering algorithm.
This explicit eager policy is separate from original init's deferred policy.
Four HOST groups verify ordering,refusals and joined cancellation. The whole
standard closure is not yet admitted.

The unchanged official no_dot_erlang.boot is now the standard-boot probe:it
preserves real kernel/stdlib startup and suppresses the user rc through script
selection. An ignored command-line argument is not equivalent. Current service
progress and explicit native/TTY/regex frontiers are tracked in
[standard boot progress](STANDARD_BOOT_PROGRESS.md).

The stock-script diagnostic additionally publishes genuine original `binary`,
`maps`, `proplists`, `queue`, `ordsets`, `io`, `io_lib`, `math`, `records`,
`gen_server`, `supervisor`, `logger`, `application`, `error_logger`, `proc_lib`,
`sys`, `erpc` and `rpc` images before starting init:31 source images in total.
This explicit source selection matters: a previously registered managed module
is already loaded, so original init can skip loading its BEAM. A managed library
without its generated metadata must not stand in for the original image.
The diagnostic configuration is retained under
`artifacts/library411-standard-boot11-diagnostic/probe-source.cs`; it is not a
completed stock startup recipe. The real `prim_tty` service and `udp_inet`
driver are still missing.

Configured native ETS/persistent-term providers expose four non-native
`module_info` helpers. They describe their actual implemented exports and
managed assembly identity, not a copied original OTP export list or digest.
Their17 native bindings remain separately counted. Static strict import
validation continues to distinguish these helpers from native primitives;
they do not make either complete original storage image admissible.
