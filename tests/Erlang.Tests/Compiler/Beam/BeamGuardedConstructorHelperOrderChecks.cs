using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedConstructorHelperOrderChecks
{
    internal const string TestName = "compiler/beam/orders-guarded-constructor-and-closure-helpers";
    private const string Fixture = "guarded_constructor_closure_probe.erl";
    private const string WrongOrder = "Definition order or Core and SSA ordinals are incorrect.";

    private const string Source = """
        -module(guarded_constructor_closure_probe).
        -export([dispatch/2, guarded/2, ambiguous/2, partial/2]).
        dispatch(remove, #{key := _}) -> removed;
        dispatch(keep, Map = #{key := _}) ->
            maps:map(fun(_Key, Value) -> {kept, Value} end, Map);
        dispatch(Mode, List) when is_list(List) ->
            [mark(Mode, Item) || Item <- List];
        dispatch(_, Other) -> Other.

        guarded(update, List) when is_list(List) ->
            [mark(update, Item) || Item <- List];
        guarded(Mode, List) when is_list(List) ->
            [mark(Mode, Item) || Item <- List];
        guarded(_, Other) -> Other.

        ambiguous(#{high := _}, List) when is_list(List) ->
            [mark(high, Item) || Item <- List];
        ambiguous(#{low := _}, List) when is_list(List) ->
            [mark(low, Item) || Item <- List];
        ambiguous(_, Other) -> Other.

        mark(Mode, Item) -> {Mode, Item}.

        partial(List, Suffix) ->
            F = fun({keep, Value}) -> {Value, Suffix} end,
            [F(Item) || Item <- List].
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        var names = new List<(string Name, int Arity)>();
        foreach (var instruction in ((IEnumerable)Property(code, "Instructions")).Cast<object>())
        {
            if ((byte)Property(Property(instruction, "Definition"), "Opcode") != FunctionInfo)
            {
                continue;
            }

            var operands = ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();
            int nameIndex = (int)(BigInteger)Property(operands[1], "Value");
            int arity = (int)(BigInteger)Property(operands[2], "Value");
            names.Add((atoms[nameIndex], arity));
        }

        var generated = names.Where(entry => entry.Name.StartsWith("-guarded/", StringComparison.Ordinal)
                || entry.Name.StartsWith("-dispatch/", StringComparison.Ordinal)).ToArray();
        if (generated is not
            [
                ("-guarded/2-lc$^1/1-0-", 2),
                ("-guarded/2-lc$^0/1-1-", 1),
                ("-dispatch/2-lc$^1/1-0-", 2),
                ("-dispatch/2-fun-0-", 2)
            ])
        {
            throw new InvalidOperationException(WrongOrder);
        }

        var partial = names.Where(entry => entry.Name.StartsWith("-partial/", StringComparison.Ordinal)).ToArray();
        if (partial is not
            [
                ("-partial/2-lc$^1/1-1-", 2),
                ("-partial/2-fun-0-", 2),
                ("-partial/2-inlined-0-", 1)
            ])
        {
            throw new InvalidOperationException(WrongOrder);
        }

        return Task.CompletedTask;
    }
}
