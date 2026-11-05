# code backends

The OTP 29.1.1 `code` interface has 69 exports: 62 library exports implemented
in C# and seven existing runtime-native coverage/debug exports. The original
`code.erl` and `code.beam` are retained unchanged. Both alternatives use the
original `code_server` process, native loader, prepared-code resources and purger.
This port does not implement a second code-server database or use the ERTS VM in
production.

`CodeModule.Exports` is the exact pinned signature inventory.
`CodeModule.RequiredDependencies` lists the C# interface's explicit dependencies.
Choose the backend before creating the runtime:

```csharp
RuntimeModuleSelection managed = RuntimeModuleSelection.Managed(
    CodeModule.Name, CodeModule.RequiredDependencies);
RuntimeModuleSelection original = RuntimeModuleSelection.Beam(
    CodeModule.Name, originalCodeBeam, originalCodeDependencies);
```

Include the chosen entry and actual dependency selections in the same
`RuntimeLibrarySelection`. Startup validates dependency closure and original
imports; dependencies are not implicitly loaded. Filesystem, native providers,
on_load order and OTP boot remain explicit runtime configuration. A complete
consumer startup closure must be validated for that consumer; the focused
checks here configure real original init/loader/server/purger directly.
For explicit host composition, call `CodeModule.Register(registry)` within
`runtime.InitializeModulesAsync`; the original alternative uses the ordinary
prepare/publish route with declared dependencies. There is no automatic fallback
between these two implementations and no live state migration.

API names, server messages, dependency operations, source positions, monitor/AST
layouts and diagnostic text live in their semantic owner catalogs. Small literal
export arities remain declaration data; source line numbers and wire fields are
named independently of unrelated equal numeric values.

The C# implementation ports path normalization, load and ensure protocols,
parallel monitored preparation workers, duplicate checks, opaque prepared-code
validation, on_load partitioning, all_available merging, module status, BEAM
chunk/MD5 wrappers, EEP48/debug-AST documentation, clash reporting and server
startup. OTP helper libraries retain their own independently selected backends.

The BEAM interpreter previously intercepted direct `code:is_loaded/1` calls and
returned `host_beam`. When `code:is_loaded/1` is registered, imports now invoke
the selected implementation and its code-server filename. The pre-existing
interpreter-only bootstrap behavior remains available before a code interface
has been installed.

The [validation receipt](validation/code-port-20261104/README.md) records the exact
corpus, builds and connected tests. Full exception terms and stacks are compared;
the lifecycle scenario loads real BEAM bytes, executes the exported function,
checks names/status/MD5, reloads, atomically publishes and purges code.

Compatibility acceptance remains narrower than the entire OTP platform.
Coverage/debug use the existing non-BEAMASM profile: `coverage_support/0` is
`false`; the other six native signatures raise `badarg`. This is different from
an OTP JIT build's capabilities. Full startup/release closure, all documentation
and filesystem/platform failures, races, resource limits and every upstream
`code_SUITE` case are not certified by the finite corpus. Global readiness is
not regenerated from a partial test report. Full regression was excluded by the
user's focused-check instruction.
