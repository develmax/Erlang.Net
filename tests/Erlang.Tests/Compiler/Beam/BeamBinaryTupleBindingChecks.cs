using Erlang;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamBinaryTupleBindingChecks
{
    internal const string TestName = "compiler/beam/discarded-binary-tuple-binds-fields-directly";
    private const string ExpectedBindingScope = "Expected temporary tuple elimination only for distinct fresh bindings with no observable inner aliases.";

    internal static async Task Compare()
    {
        const string wrapped = """
            -module(binary_tuple_binding).
            -export([decode/1, dependent/2, effect/1]).
            decode(Bin) -> {Value,Tail} = begin <<X:6,Rest/bits>> = Bin, {X,Rest} end, {Value+1,Tail}.
            dependent(Width,Bin) -> {Value,Tail} = begin <<X:Width,Rest/bits>> = Bin, {X,Rest} end, {value,Value,Tail}.
            effect(Bin) -> {Value,Tail} = begin <<X:6,Rest/bits>> = probe:read(Bin), {X,Rest} end, {value,Value,Tail}.
            """;
        const string direct = """
            -module(binary_tuple_binding).
            -export([decode/1, dependent/2, effect/1]).
            decode(Bin) -> <<Value:6,Tail/bits>> = Bin, {Value+1,Tail}.
            dependent(Width,Bin) -> <<Value:Width,Tail/bits>> = Bin, {value,Value,Tail}.
            effect(Bin) -> <<Value:6,Tail/bits>> = probe:read(Bin), {value,Value,Tail}.
            """;
        const string controls = """
            -module(binary_tuple_binding).
            -export([reuse/1, prebound/2, terminal/1]).
            reuse(Bin) -> {Value,Tail} = begin <<X:6,Rest/bits>> = Bin, {X,Rest} end, {Value,Tail,X}.
            prebound(Value,Bin) -> {Value,Tail} = begin <<X:6,Rest/bits>> = Bin, {X,Rest} end, Tail.
            terminal(Bin) -> {Value,Tail} = begin <<X:6,Rest/bits>> = Bin, {X,Rest} end.
            """;

        byte[] foldedCode = CompileCode(wrapped);
        byte[] directCode = CompileCode(direct);
        byte[] disabledCode = CompileCode(wrapped, disableSsa: true);
        if (!foldedCode.AsSpan().SequenceEqual(directCode) || foldedCode.AsSpan().SequenceEqual(disabledCode))
        {
            throw new InvalidOperationException(ExpectedBindingScope);
        }

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (bool disableSsa in new[] { false, true })
        {
            var module = BeamModule.Prepare(Compile(controls, disableSsa));
            await actor.InvokeAsync(async context =>
            {
                Term input = new BitString([0xA8]);
                Term value = Term.I(42);
                Term tail = new BitString([0], 2);
                if (!(await module.Invoke(context, "reuse", input)).Equals(Term.Tuple(value, tail, value))
                    || !(await module.Invoke(
                        context,
                        "prebound",
                        value,
                        input
                    )).Equals(tail)
                    || !(await module.Invoke(context, "terminal", input)).Equals(Term.Tuple(value, tail)))
                {
                    throw new InvalidOperationException(ExpectedBindingScope);
                }

                Term reason = Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Term.Tuple(value, tail));
                try
                {
                    _ = await module.Invoke(
                        context,
                        "prebound",
                        Term.I(41),
                        input
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return Nil.Value;
                }

                throw new InvalidOperationException(ExpectedBindingScope);
            });
        }
    }

    private static byte[] CompileCode(string source, bool disableSsa = false)
    {
        byte[] beam = Compile(source, disableSsa);
        _ = BeamModule.Prepare(beam);

        return BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
    }

    private static byte[] Compile(string source, bool disableSsa)
    {
        if (disableSsa)
        {
            source = source.Insert(source.IndexOf('\n'), " -compile(no_ssa_opt).");
        }

        var module = new Parser(source).ParseModule();
        var lines = Enumerable.Range(3, module.DeclaredFunctions.Count).Select(line => new BeamSourceLocation(null, line)).ToArray();

        return new BeamModuleCompiler(module.Name, sourceModule: module).Emit(
            module.DeclaredFunctions,
            lines,
            module.DeclaredExports.ToHashSet(),
            new BeamModuleMetadata(module.Attributes, null)
        );
    }
}
