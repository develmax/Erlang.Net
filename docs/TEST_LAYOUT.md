# Test layout

Part458 moves the 1,597 C# files previously at the Erlang.Tests project root into purpose-based directories. Namespaces and assembly identity remain unchanged. The move manifest records every old/new path and content hash. Harness/Program.cs subsequently adds two distribution monitor regression registrations.

| Directory | Purpose |
| --- | --- |
| Harness | Ordered executable catalog, selection, reporting and shared runner support |
| Compiler/Source, Compiler/Integration | Source compilation and deployed consumer checks |
| Compiler/Beam | Binary decoding, receive, exception, call and frame execution |
| Runtime/Distribution | Connections, channels, signals and wire protocol |
| Runtime/Native | NIF loading/memory/callbacks, drivers and regular expressions |
| Runtime/Storage | ETS queries, traversal, ownership, mutations, persistent terms and atomics |
| Runtime/Processes | Spawn, identity, signals, lifecycle and mailboxes |
| Runtime/Diagnostics | Debugging, tracing, logging and monitoring |
| Runtime/Modules, Runtime/Bootstrap | Loading, code lifecycle and original OTP boot |
| Runtime/Scheduling, Runtime/Memory, Runtime/Execution | Shared execution and resource semantics |
| Otp/Stdlib | Module-specific collection, binary, process and IO checks |
| Terms, Serialization | Values, identifiers, ordering/hashing and external formats |

The existing executable test project remains one csproj: its ordered catalog, shared internal helpers and child-process dispatch form a single harness. SDK recursive compilation includes the new directories. Splitting projects solely for folder organization would introduce extra assembly/internal-access boundaries without independent test ownership. Resource and fixture files, embedded resource names, working-directory assumptions and output paths are preserved.

Part461 adds `tests/Erlang.Otp.Library.Tests` as a separate public-consumer packaging contract executable. It needs no access to the main harness internals: 78 checks cover the 18-image embedded catalog, independent returned bytes, invalid/unsupported selections, explicit dependency requirements and original ownership for all eleven collection/binary modules. Build with `dotnet build tests/Erlang.Otp.Library.Tests -c Release -m:1`, then execute its output DLL. The four backend combinations are additionally exercised by `examples/HelloOtpLibraryReference`, both via project references and a cold PackageReference/publish consumer. These checks are separate from the main 8842-group runtime catalog.

Historical validation snapshots retain their original source paths. Current source-only NIF plans and workflow seals bind the relocated files; this updates provenance without adding native compatibility credit.

Part462 adds `tests/Erlang.Otp.Kernel.Library.Tests`, another independent public-consumer executable. Its218 checks inspect all104 original kernel module identities and independent image/resource reads, exercise explicit refusals, and execute eight original inet_parse observations with original image ownership. `validate-otp-kernel-original.ps1` compares the entire printed result with a fresh admitted original OTP process, retaining stdout/stderr and binding metadata. The same executable runs from a cold NuGet/publish directory with empty PATH and repository build imports disabled. Main runtime catalog counts remain separate.

The completed Part462 stdlib inventory extends its separate public-consumer executable from78 to241 checks, including all98 image identities, defensive reads and original application resources. Historical Part461 results remain78 on that exact commit; the main8842-group runtime catalog is unchanged.
