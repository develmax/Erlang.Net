# Trusted native generated-source entry route proposal

Source-only tool overlay; no build, native execution, Main/Git mutation. Apply after root's candidate2+cleanup/veto/layout/options revisions. Existing managed checks and direct-table native tests remain intact. New checks run only when a native profile is explicitly supplied, not for no-argument managed checks.

## Actual route under test

The real Erlang.Compiler CodeGeneration pipeline generates C#, the existing GeneratedModuleCompiler emits a Roslyn Release assembly and retrieves its ModuleDefinition, and the actual ModuleDefinition.Register installs the real source image with -nifs([increment/1,greeting/0]). Existing compilation helpers are linked from their actual Erlang.Differential source files, not forked/reimplemented. The tool references Compiler and the SDK Roslyn assemblies following the existing Differential project convention. Runtime itself gains no Compiler dependency and no new source file. There is one actual Runtime assembly, not a parallel/fake interpreter in the harness.

The module name remains exactly memory_allocator_nif_fixture and the two native functions match the independently trusted old binary/source profile. Before loading, increment/1 executes the actual source fallback erlang:nif_error(not_loaded), and plain/0 returns seventeen. Source install/0 calls a development-only host intrinsic; the intrinsic preserves the true source execution flow and checks the actual captured image AND ModuleDefinition owner before reserving. No manual EnterSourceCode scope or table.Publish injects a fake image. The host bridge invokes the genuine reservation coordinator and native initializer; it is not erlang:load_nif and cannot claim public loader coverage.

After installation, the generated-source direct increment entry, tail call, local named fun, external named fun, anonymous closure, remote source call and actual registry export call must all produce native 41->42. The local non-tail route returns {42,"native hello"}; greeting verifies the native Latin1 result. Native badarg must replace the source not_loaded fallback, while plain/0 remains unchanged. Those values cannot be satisfied by the authored fallback clauses.

Image retirement after source calls must free the native allocation before containment fallback and increment the actual exported unload counter once while a separate OS observer reference keeps the DLL/SO mapped. The generated C# hash is printed as evidence. The collectible generated assembly remains alive through all native/image cleanup; it does not own or replace the process-global Linux shim provider.

## Integration and limits

The two replaced tool files are bound to root-nif-native299-isolated/candidate in source-bindings.json; all other files are new. Program adds one Run(profile) after the old native fixture checks and before optional ABI probe checks. The project adds Compiler and links exactly five existing development compiler helpers. No central runtime registration, BEAM route, public BIF, compiler production source or Main file is changed by the artifact author.

This proposes actual generated-source-to-native evidence, not a faster IL evaluator claim: generated modules still use the existing ModuleDefinition evaluator architecture. No route has been executed here. Root must build the actual isolated tool target and run the existing verified Windows binary with its trusted hash; Linux requires the separately reviewed shim and original configured headers. Preserve raw failures and outputs. Existing original13 semantics remain separate from this two-function trusted source route; no thirteen-to-SDK parity follows.

BEAM native entry, old/current/pending native transitions, runtime shutdown association, resources, dirty scheduling/native threads and general ABI remain unfinished or separate work. No ordinary test output should label the entire loader or erlang complete based on this harness.
