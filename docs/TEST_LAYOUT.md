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

Part463 adds `tests/Erlang.Otp.Boot.Library.Tests`, an ordinary consumer of Stdlib, Kernel, Erts and Sasl packages/projects.104 checks inspect all39 new image identities and independent resource reads, reject invalid requests, prove original image ownership and compare19 complete outcomes/stacks with a retained original fixture and fresh admitted OTP capture. Raw ETF bytes match exactly without identity mapping or frame removal. Both native atomics/counters owners have created resources and settle to zero arrays/bytes/invocations after disposal. The same checks run in a cold published consumer with empty PATH. `validate-otp-boot-original.ps1` binds nine original execution files and fixture source/BEAM/golden hashes; raw original/managed evidence is uploaded by CI.

Part464 adds tests/Erlang.Otp.Syntax.Library.Tests:53 public-consumer checks inspect13 image identities, defensive resource readers and explicit refusals, prove original ownership of six selected images including the identical compiled observer, and compare19 complete outcomes/stacks byte-for-byte against fresh original OTP. The same executable runs from cold NuGet/publish with empty PATH. Its original-capture script also verifies all33 packaged producer files and eight execution bindings. These are separate from the unchanged8842-group runtime catalog.

Part465 adds tests/Erlang.Otp.Compiler.Library.Tests:138 public-consumer checks inspect all59 images, resource isolation, selectors/refusals and original ownership of lists/beam_opcodes/erl_bifs/observer. Twenty complete outcomes (15 returns/five full-stack errors) match fresh OTP byte-for-byte. Original capture binds129 packaged files, six execution files and three compiler bindings. The same138 checks run from cold NuGet/publish with empty PATH. This does not prove compile:file/forms or self-hosting.

Part466 adds tests/Erlang.Otp.Tools.Library.Tests:86 public-consumer checks inspect all29 image identities, resource copies, selectors/refusals and original ownership of seven selected images. Twenty-four complete outcomes (20 returns/four full-stack errors) compare byte-for-byte with a fresh original OTP process. Selected original lists/maps/xref_utils/msacc/runtime_tools/runtime_tools_sup plus the identical observer execute on our runtime; native ERTS import names are explicitly declared. Same86 checks run through cold NuGet/publish with empty PATH and disabled repository build imports. Original capture binds65 packaged files, nine execution files and three compiler bindings. Live VM profiling/tracing and native drivers are outside this finite comparison.

Part467 adds tests/Erlang.Otp.Documentation.Library.Tests:182 public-consumer checks cover69 image identities, defensive reads, explicit selectors, four EDoc support resources and ownership of six executed images. Thirty complete helper outcomes (25 returns/five full-stack errors) match fresh pinned OTP raw ETF. Original capture verifies164 packaged files, eight execution bindings and three compiler bindings. The same executable runs from a cold NuGet/publish directory with empty PATH. One permanent main-catalog group verifies optimized literal-failure admission and five negative mutations; the main catalog becomes8843, independently of consumer check counts. Full EUnit/EDoc/Xmerl applications remain outside this finite execution acceptance.

Part468 adds tests/Erlang.Otp.Mnesia.Library.Tests with82 public-consumer assertions. All31 image identities, independent byte/resource reads and selector refusals are checked; actual source ownership of lists/mnesia_frag_hash/mnesia_app/observer is asserted.24 complete original outcomes (19 returns/five errors with full stacks) compare byte-for-byte against pinned OTP. Fresh capture binds65 packaged files, six original execution files, three compiler bindings and fixture hashes. Cold NuGet/publish executes with empty PATH. This does not certify full database startup, transactions, persistence or replication; the main8843 catalog is unchanged.

Part469 adds tests/Erlang.Otp.Asn1.Library.Tests:65 project/cold assertions cover all22 image identities/copies, two application resources, selection refusals and actual source ownership of original lists/ets/asn1ct_eval_ext/asn1ct_table/observer.26 complete outcomes (20 returns/six errors with full stacks) compare against fresh OTP using whole deterministic ETF. Existing native ETS composition is explicit; the compiler-table lifecycle deletes its table. Capture binds49 provider files, seven execution files, three compiler files and fixture hashes. Keep initial missing-provider/import refusals and ordinary-ETF map-order mismatch separate. Main8843 groups remain unchanged.
