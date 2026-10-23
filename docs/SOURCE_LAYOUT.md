# Source projects and OTP library boundaries

Part456 separates the existing handwritten C# alternatives according to OTP application ownership. Original OTP modules remain Erlang/BEAM and run on the .NET VM; this restructuring does not translate or replace them.

| Project | Purpose | Dependencies |
| --- | --- | --- |
| `Erlang.Terms` | Terms, identifiers, ordering and shared value storage | .NET |
| `Erlang.Serialization` | External Term Format and formatting primitives | Terms |
| `Erlang.Runtime.Core` | Processes, scheduler, BEAM execution, BIF/NIF, native drivers, storage and bootstrap | Terms, Serialization |
| `Erlang.Otp.Stdlib` | C# alternatives for collections, binary wrappers, proc_lib, gen_server, supervisor and sys support | Runtime.Core |
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
