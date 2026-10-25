# Source projects and OTP library boundaries

Part456 separates the existing handwritten C# alternatives according to OTP application ownership. Original OTP modules remain Erlang/BEAM and run on the .NET VM; this restructuring does not translate or replace them.

| Project | Purpose | Dependencies |
| --- | --- | --- |
| `Erlang.Terms` | Terms, identifiers, ordering and shared value storage | .NET |
| `Erlang.Serialization` | External Term Format and formatting primitives | Terms |
| `Erlang.Runtime.Core` | Processes, scheduler, BEAM execution, BIF/NIF, native drivers, storage and bootstrap | Terms, Serialization |
| `Erlang.Otp.Stdlib` | C# alternatives for collections, binary wrappers, proc_lib, gen_server, supervisor and sys support | Runtime.Core |
| `Erlang.Otp.Stdlib.Beam` | Original OTP stdlib sources/provenance and all98 embedded BEAM images; explicit `OtpStdlib.Select` backend selection | Runtime composition (repository) or Erlang.Net.CSharp (package) |
| `Erlang.Otp.Kernel.Beam` | All104 original kernel images, original application/upgrade resources and source/include inventory | Runtime composition (repository) or Erlang.Net.CSharp (package) |
| `Erlang.Otp.Erts.Beam` | All22 original preloaded/implementation images, sources and erts.app; native bindings remain in Runtime.Core | Runtime composition (repository) or Erlang.Net.CSharp (package) |
| `Erlang.Otp.Sasl.Beam` | All17 original SASL images, sources and application/upgrade resources | Runtime composition (repository) or Erlang.Net.CSharp (package) |
| `Erlang.Otp.Kernel` | C# application environment, logging, error_logger and RPC alternatives; public `KernelModules.Register` entry point | Runtime.Core, Stdlib |
| `Erlang.Runtime` | Supported default VM + managed stdlib project composition | Runtime.Core, Stdlib |
| `Erlang.Otp.Behaviours` | Existing public OTP registration/composition entry point | Runtime, Stdlib, Kernel |
| `Erlang.Compiler` | Source compiler, syntax, preprocessing and C# integration | Runtime and OTP composition |
| `Erlang.DriverHost` | Separate native driver process entry point | Runtime composition |
| `Erlang.CSharp.MSBuild` | Package and MSBuild integration | Compiler/tool and library deployment |

`Runtime.Core` retains assembly name **Erlang.Runtime** and the existing namespaces. The `Erlang.Runtime` composition project produces **Erlang.Runtime.Bundle** and supplies the stdlib dependency transitively. C# library types now belong to their actual Stdlib/Kernel assemblies; consumers of previously compiled library-type identities must rebuild. No project cycle, source linking or duplicate compilation is used.

The runtime's existing default managed-library startup loads one explicitly named stdlib registration contract in its own assembly load context. It does not scan directories, discover arbitrary assemblies, register through ambient module initializers or fall back between BEAM and C#. Missing dependencies fail explicitly. Per-module backend selection and original-image ownership checks remain in force. Native BIF implementations and shared term/VM primitives stay in the core, including helpers also used by a C# library.

All C# source files have category directories. Runtime groups include `Bootstrap`, `Distribution`, `Execution/Beam`, `Modules`, `Native/Drivers`, `Native/Nif`, `Native/Regex`, `Primitives`, `Processes`, `Scheduling`, `Services`, `Storage` and `Support`. Libraries group collections and process behaviours by module; the compiler groups syntax, execution, compilation, preprocessing/C# support. Namespace and generated resource names do not depend on the physical directory.

Project references remain the supported source integration route. The CSharp NuGet package deploys the VM and both C# library assemblies; the Tool package carries its transitive dependencies. A native driver package must also bind the actual stdlib DLL hash, alongside the VM, Terms and Serialization assemblies. Historical validation snapshots retain their original paths and hashes; current source manifests and workflow seals are relocated separately.

This is a structural change. It adds no Erlang API or full compatibility claim. Original-library execution, C# backend execution, cold deployment, native ports and exact-commit CI are separate validation gates. Part457 supplies the scoped native port path for authenticated distribution, data exchange and same-peer reconnection through the original `net_kernel`; see `validation/part-457/original-net-kernel-native-distribution`. This finite Windows matrix is separate from full module/transport/platform compatibility.

Native distribution snapshots now carry a `Term` controller identity (`Pid` or actual `PortTerm`); the host snapshot input-handler term is `Nil` for a native controller. C# consumers should rebuild and pattern-match these identities. Native ports own their queue pump and lifetime; no handshaker PID is substituted as controller.

Part458 organizes the executable test harness by category without changing assembly or resource identities. See [test layout](TEST_LAYOUT.md).

Part461 moves reusable original OTP library assets out of examples into `src/Erlang.Otp.Stdlib.Beam/Otp`. Distinct historical compilation variants are retained under `Variants/BackendSelection`. Example observers/callbacks stay in examples. Ordinary .NET consumers can reference the project or package and select original BEAM or existing C# implementations through `OtpStdlib.Select`; no external image directory, original OTP installation or custom build import is needed for this embedded route. See the library README and `examples/HelloOtpLibraryReference`. The seven process/formatting images do not by themselves constitute a complete kernel startup closure.

Part462 adds the complete original kernel application asset inventory under `src/Erlang.Otp.Kernel.Beam/Otp`, including104 images,104 Erlang sources, source/header support and kernel.app/kernel.appup. `OtpKernel.ReadImage` and the application-resource readers verify embedded hashes and return independent bytes. `OtpKernel.Beam` requires explicit dependencies. This is application packaging completeness; native setup, on_load and complete boot remain distinct runtime concerns. See [packaging progress](OTP_LIBRARY_PACKAGING.md).

Part462 also completes the original stdlib catalog from18 to98 names without replacing the initial admitted images. The additional80 images, generated Erlang sources, source/include support and stdlib application/upgrade resources retain official release provenance. ReadImage and the resource readers verify their hashes; non-collection startup selections continue to require explicit dependencies.

Part463 completes the ERTS/SASL asset inventories. Original wrappers and their explicit C# native bootstrap remain distinct. A common retained observer executes19 whole outcomes/stacks on original OTP and our runtime with exact raw ETF equality; this finite observation does not certify all BIFs, release management or whole application boot.

Part464 adds independent original syntax_tools/parsetools libraries under src. They retain all13 images,16 unchanged source/header files and four application resources. The public OtpSyntaxTools/OtpParsetools readers and explicit dependency selections follow the existing original-library boundary.

Part465 adds src/Erlang.Otp.Compiler.Beam, retaining all59 original compiler images,68 source/header files and two application resources. Sixty-five source/header files match pinned Git bytes; beam_opcodes.erl, beam_opcodes.hrl and core_parse.erl retain admitted generated release provenance. This is the original compiler application, separate from our Erlang.Compiler C# frontend.

Part466 adds separate original tools/runtime_tools libraries under src:all29 BEAM images,32 source/header files and four application resources. Thirty-one sources/headers match pinned upstream Git; generated xref_parser.erl is bound to official release provenance. OtpTools/OtpRuntimeTools readers verify independent resource bytes and create explicit BEAM selections. Existing C# runtime/native bindings remain unchanged.

Part467 adds separate original EUnit, EDoc and Xmerl projects under src. All69 images,85 source/header/grammar entries, six application resources and four EDoc priv assets remain release-identical. Explicit readers/selectors verify hashes and copies; support readers do not execute the retained source escript or install a filesystem mapping. Shared runtime admission handles bounded literal calls which fail before an optimized try-case continuation; there is no module-name bypass.

Part468 adds src/Erlang.Otp.Mnesia.Beam:31 original BEAM images,32 pinned source/header files and both application resources. OtpMnesia exposes defensive hash-checked readers and explicit BEAM selectors. No database logic is rewritten in C#; native/runtime behavior remains separate from the unchanged original Erlang application.

Part469 adds src/Erlang.Otp.Asn1.Beam:22 original BEAM images,25 source/header files (23 match pinned Git; two are unchanged generated release sources) and asn1.app/appup. OtpAsn1 exposes hash-checked independent readers and explicit BEAM selectors. Preserve the descriptor distinction (one listed native runtime module versus22 actual images). Native ASN.1 support and compiler self-hosting are not supplied by the asset library.

Part470 adds src/Erlang.Otp.Crypto.Beam (all2 images/two source files) and src/Erlang.Otp.PublicKey.Beam (all42 images,44 Erlang/application source files,five include files,33 ASN.1/config inputs). All132 packaged files match the admitted release;52 inputs match Git and32 generated release files are identified separately. Quoted/mixed-case module names are preserved by consulting the actual application descriptor in OTP. Four native producer DLLs are excluded explicitly. No crypto/certificate library logic is translated to C#.

Part471 adds src/Erlang.Otp.Ssl.Beam (78 images/94 source-header files) and src/Erlang.Otp.Ssh.Beam (43 images/50 source-header files), plus four application resources. All269 files match the admitted release; all144 source/header files match pinned Git. General runtime admission now ignores the unused bs_skip_bits2 flags operand as OTP does and admits bs_match BigInteger fields up to the existing int bit-index bound. Original protocol logic remains Erlang; no handshake/native crypto replacement is introduced.

Part472: Four original network application projects retain upstream directory layouts, including nested Inets HTTP client/server/library sources and generated ELDAPv3.hrl in ebin. Native runtime/BIF implementations remain in their existing projects; no handwritten protocol replacement is added.
