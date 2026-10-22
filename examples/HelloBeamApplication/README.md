# Explicit BEAM application-resource example

Pass an absolute `AdmissionPropertiesFile` containing the admitted original OTP compiler/runtime properties used by HelloBeamIncludes. The source and resource roots are separate. Build:

```powershell
dotnet build examples/HelloBeamApplication -p:AdmissionPropertiesFile=ABSOLUTE_PROPERTIES_FILE
dotnet examples/HelloBeamApplication/bin/Debug/net10.0/HelloBeamApplication.dll
```

The original compiler and file:consult parser are build-time dependencies. The executable explicitly loads `lib/beam_resource378-0.1.0/ebin/resource378.beam` in our .NET runtime, calls `resource378:hello/0`, and checks copied descriptor/priv/config bytes. It only checks manifest existence; the build acceptance packet separately verifies manifest/file hashes. It does not load/start an OTP application, apply config, or use a code:priv_dir facade. No original VM or build tool is needed by the published executable.

The `.app.src` is already concrete and copied byte-for-byte as `.app`. Configuration order is recorded without merging or applying files. These are SDK V1 resource policies, not application_controller search/error/default semantics. No general template/rebar/release tooling is implied.
