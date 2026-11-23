using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamLocalConstructorSingletonChecks
{
    internal const string TestName = "compiler/beam/local-constructor-keeps-singleton-arguments";
    private const string ExpectedSingletons = "Expected known arguments to fold only in the private constructor with a single set of inputs.";

    internal static Task Compare()
    {
        const string source = """
            -module(constructor_singleton_probe).
            -export([init/1, single/1, mixed/1, other/1, escaped/1, closure/0, worker/2]).
            init(X) -> local_spec(service, [], X).
            single(X) -> sup_spec(X, []).
            mixed(X) -> mixed_spec(service, [], X).
            other(X) -> mixed_spec(other_service, [arg], X).
            escaped(X) -> escaped_spec(service, [], X).
            closure() -> fun escaped_spec/3.
            worker(Args, Tail) -> worker_spec(service, Args, Tail).
            local_spec(Name, Args, X) -> {Name,{Name,start_link,Args},X}.
            sup_spec(Name, Args) -> {Name,{Name,start_link,Args},[Name,supervisor]}.
            mixed_spec(Name, Args, X) -> {Name,{Name,start_link,Args},X}.
            escaped_spec(Name, Args, X) -> {Name,{Name,start_link,Args},X}.
            worker_spec(Name, Args, Tail) -> {Name,{Name,start_link,Args},[Name] ++ Tail}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "constructor_singleton_probe.erl");
        byte[] reference = BeamModuleEmissionChecks.Read("constructor_singleton_probe.beam");
        ReadOnlySpan<byte> twoTupleAllocation = [BeamEmissionFormat.TestHeap, 0x80, 0x30];
        if (!beam.AsSpan().SequenceEqual(reference) || Count(CompileWithoutSsa(source), twoTupleAllocation) != 3)
        {
            throw new InvalidOperationException(ExpectedSingletons);
        }

        return Task.CompletedTask;
    }

    private static byte[] CompileWithoutSsa(string source)
    {
        var module = new Parser(source.Insert(source.IndexOf('\n') + 1, "-compile(no_ssa_opt).\n")).ParseModule();
        var lines = Enumerable.Range(3, module.DeclaredFunctions.Count).Select(line => new BeamSourceLocation(null, line)).ToArray();
        byte[] beam = new BeamModuleCompiler(module.Name, sourceModule: module).Emit(
            module.DeclaredFunctions,
            lines,
            module.DeclaredExports.ToHashSet(),
            new BeamModuleMetadata(module.Attributes, null)
        );

        return BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
    }

    private static int Count(byte[] code, ReadOnlySpan<byte> marker)
    {
        int uses = 0;
        for (int index = 0; index <= code.Length - marker.Length; index++)
        {
            if (code.AsSpan(index).StartsWith(marker))
            {
                uses++;
            }
        }

        return uses;
    }
}
