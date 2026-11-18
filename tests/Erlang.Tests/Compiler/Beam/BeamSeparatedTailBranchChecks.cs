using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSeparatedTailBranchChecks
{
    private const string Module = "beam_separated_tail_branches";
    private const string Source = """
        -module(beam_separated_tail_branches).
        -export([route/1, view/1]).

        route(Value) when is_list(Value) ->
            lists:foreach(fun route/1, Value);
        route({Key, Options}) ->
            erlang:put(Key, [first, second | Options]),
            stored.

        view({_, Name, Value, with_child, _}) -> report(Name, Value, nested(Value));
        view({_, Name, Value, worker, _}) -> report(Name, Value).

        report(Name, Value) -> {Name, Value}.
        report(Name, Value, Child) -> {Name, Value, Child}.
        nested(Value) -> {child, Value}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        var instructions = ((IEnumerable)Property(Code(beam), "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        ReadOnlySpan<byte> guardedTail = [
            BeamEmissionFormat.IsList, BeamEmissionFormat.TestHeap, BeamEmissionFormat.Move, BeamEmissionFormat.MakeFun
        ];
        int tupleFrame = Array.IndexOf(opcodes, BeamEmissionFormat.AllocateHeap);
        int selection = Array.IndexOf(opcodes, BeamEmissionFormat.SelectValue);
        int nestedFrame = Array.IndexOf(opcodes, BeamEmissionFormat.Allocate);
        if (opcodes.AsSpan().IndexOf(guardedTail) < 0 || tupleFrame < 0
            || OperandValue(instructions[tupleFrame], 0) != 0
            || selection < 0 || nestedFrame <= selection || OperandValue(instructions[nestedFrame], 0) != 2)
        {
            throw new InvalidOperationException(BeamSeparatedTailBranchDiagnostics.Instructions);
        }

        await using var runtime = new ProcessRuntime();
        var dependencies = new BeamDependencySelection(["lists", ErlangModuleNames.Module]);
        BeamModule image = BeamModule.Prepare(beam, dependencies);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term token = Term.A("token");
            Term key = Term.A("events");
            Term args = Term.Tuple(key, Term.List(token));
            Term routed = await image.Invoke(context, "route", Term.List(args));
            Term observed = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Get,
                key
            );
            Term expected = Term.List(Term.A("first"), Term.A("second"), token);
            Term leaf = await image.Invoke(context, "view", View(Term.A("worker")));
            Term branch = await image.Invoke(context, "view", View(Term.A("with_child")));
            if (!routed.Equals(Term.A("ok")) || !observed.Equals(expected)
                || !leaf.Equals(Term.Tuple(key, token))
                || !branch.Equals(Term.Tuple(key, token, Term.Tuple(Term.A("child"), token))))
            {
                throw new InvalidOperationException(BeamSeparatedTailBranchDiagnostics.Result);
            }

            await Reject("view", View(Term.A("unknown")));
            await Reject("route", Term.Tuple(key));

            return Nil.Value;

            Term View(Term selector) => Term.Tuple(
                Nil.Value,
                key,
                token,
                selector,
                Nil.Value
            );

            async ValueTask Reject(string function, Term value)
            {
                try
                {
                    await image.Invoke(context, function, value);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
                {
                    return;
                }

                throw new InvalidOperationException(BeamSeparatedTailBranchDiagnostics.InvalidInputAccepted);
            }
        });
    }

    private static BigInteger OperandValue(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
