# Original OTP library packaging

Packaging progress counts module names in the admitted OTP29.1.1 application inventories, not compatible APIs. Original BEAM executes on our .NET runtime; existing C# alternatives remain separate libraries. No fallback or automatic selection of dependency backends is introduced.

| Application | Original modules packaged in src | Admitted application modules | Packaging progress | Validation scope |
| --- | ---: | ---: | ---: | --- |
| stdlib | 18 | 98 | 18.4% | 78 public-consumer checks; actual11 collection/binary modules; retained72 process observations; four lists/queue backend combinations via projects and cold NuGet/publish |
| kernel | 104 | 104 | 100% | 218 public-consumer checks; all104 image identities; eight inet_parse outcomes compared with fresh original OTP; cold NuGet/publish |

These two applications total122/202 packaged module names (60.4%). This is not a project-wide completion percentage or full compatibility claim. The official kernel producer is Windows x64; platform identity remains explicit. No newly registered runtime APIs or full contracts are added by packaging.

Projects:

- `src/Erlang.Otp.Stdlib.Beam`: original stdlib images, source and producer provenance; OtpStdlib.Select selects Beam or an established supported Managed alternative per module.
- `src/Erlang.Otp.Kernel.Beam`: complete original kernel source/image/application-resource inventory; OtpKernel.ReadImage/ReadApplication/ReadApplicationUpgrade and explicit OtpKernel.Beam startup choices.
- `src/Erlang.Otp.Stdlib` and `src/Erlang.Otp.Kernel`: existing C# alternatives and their current registration/composition APIs. The strict startup selector's Managed support is narrower than every C# registration; unsupported choices fail explicitly.

Next: package the remaining80 stdlib module names, preserving existing admitted compilation variants and dependency boundaries, then extend other applications according to the boot dependency inventory. Shipping an application descriptor does not automatically start it; native bindings, selected dependency images and the existing boot protocol remain required. Continue actual image ownership/execution checks and original OTP comparisons separately from file inventory checks.
