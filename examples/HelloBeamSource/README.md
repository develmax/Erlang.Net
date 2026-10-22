# HelloBeamSource: compile original Erlang, execute on .NET

This optional example builds three `.erl` modules with an explicitly admitted OTP-29.1.1 compiler. The generated BEAM runs on our .NET runtime. The original VM is a build-time dependency only. The two existing fixture sources remain byte-identical; a project driver calls both and an explicitly registered C# host MFA. The C# integer collection is copied into an immutable Erlang list before its source array is changed.

Build the BeamBuild tool, then build this project with `-p:ErlangBeamCompilerProps=<absolute props file>`. The props file supplies `ErlangBeamDotNetHost`, `ErlangBeamInstalledRoot`, `ErlangBeamProvenanceKind`, `ErlangBeamAdmissionReceipt`, `ErlangBeamAdmissionReceiptSha256` and `ErlangBeamCompilerSha256`. All paths are absolute. No installed compiler is inferred from PATH. The example is deliberately outside the default solution because these external compiler properties are mandatory.

The operator first verifies the compiler distribution against its trusted source, then seals the admission receipt and complete installed-file manifest. Providers distinguish an actual source-build receipt from an official-release archive receipt. A release URL in a JSON document alone does not authenticate a compiler; the explicit operator-supplied receipt hash is the admission boundary. The local validation used the official Windows OTP-29.1.1 ZIP, publisher digest and exact annotated source tag. The tool rechecks installed file content before deciding to skip, compiling and accepting outputs. No downloads, installers or PATH changes occur during a project build.

For a package consumer:

```xml
<PropertyGroup>
  <ErlangPreprocessEnabled>false</ErlangPreprocessEnabled>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Erlang.Net.CSharp" Version="0.1.0" />
  <ErlangBeamSource Include="Modules/*.erl" />
</ItemGroup>
```

Supply the same admission properties before the imported targets execute. `ErlangBeamSource` is separate from retained `ErlangBeam` assets and `ErlangSource` generated-C# compilation. The package carries its task and out-of-process build tool in `tools/beam/net10.0`; the compiler distribution is external and explicit. Disabling the existing C# preprocessor is optional when its hybrid syntax is also needed.

Each successful build writes an exact source/output/configuration manifest under `obj/.../beam`, and copies generated BEAM plus the manifest to `Modules` for build/publish. The example validates hashes and actual prepared-image ownership before calls. Content/configuration checks run even on a no-op MSBuild invocation; unchanged modules skip erlc. Clean removes generated outputs/state. A failed compiler batch does not promote prepared modules or copy them to application output. Individual file replacement is atomic; the whole filesystem batch is not a transaction. Consume only a successfully returned manifest, never arbitrary files from the staging directory.

Current admission deliberately accepts only module/export attributes and ordinary expressions; unsupported attributes/macros are rejected rather than silently ignored. Includes, `.hrl`, parse transforms, generated modules, `.app.src`, dependencies, `priv`, config, rebar3, escript, release and boot support remain separate gates. This example proves explicit integer-list/Term and host-function interop, not a universal DTO schema or arbitrary CLR-object lifetime protocol. Runtime deployment contains no original compiler/VM.
