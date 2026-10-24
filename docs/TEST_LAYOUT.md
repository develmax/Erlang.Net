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

Historical validation snapshots retain their original source paths. Current source-only NIF plans and workflow seals bind the relocated files; this updates provenance without adding native compatibility credit.
