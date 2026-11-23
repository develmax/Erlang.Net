using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamSeparatedClauseListDefinitionsChecks
{
    internal const string TestName = "compiler/beam/orders-clause-separated-list-helpers-and-closures";
    private const string Failure = "The grouped source clauses lost their helper order or an unsupported head was admitted.";

    internal static Task Run()
    {
        const string source = """
            -module(separated_clause_list_definitions).
            -export([walk/3, blocked_head/4, blocked_scope/3]).
            walk(Item, Limit, Mode) when is_atom(Item) ->
                walk([Item], Limit, Mode);
            walk(Items, Limit, false) when is_list(Items), is_integer(Limit) ->
                Seen = [probe:mark(Item, Limit) || Item <- Items],
                probe:consume(Seen);
            walk(Items, Limit, true) when is_list(Items), is_integer(Limit) ->
                Seen = [probe:mark(Item, Limit) || Item <- Items],
                probe:consume(Seen, fun(Item) -> probe:lookup(Item, Limit) end);
            walk(Other, Limit, Mode) -> {Other, Limit, Mode}.

            blocked_head(Items, Key, Key, false) when is_list(Items), is_atom(Key) ->
                Seen = [probe:mark(Item, Key) || Item <- Items],
                probe:consume(Seen);
            blocked_head(Items, Key, Key, true) when is_list(Items), is_atom(Key) ->
                Seen = [probe:mark(Item, Key) || Item <- Items],
                probe:consume(Seen, fun(Item) -> probe:lookup(Item, Key) end);
            blocked_head(Other, Key, Value, Mode) -> {Other, Key, Value, Mode}.

            blocked_scope(Item, Limit, Mode) when is_atom(Item) ->
                blocked_scope([Item], Limit, Mode);
            blocked_scope(Items, Limit, false) when is_list(Items), is_integer(Limit) ->
                Seen = [probe:mark(Item, Limit) || Item <- Items],
                probe:consume(Seen);
            blocked_scope(Items, Limit, true) when is_list(Items), is_integer(Limit) ->
                Seen = [probe:mark(Item, Limit) || Item <- Items],
                probe:consume(Seen, fun() -> receive ready -> ok end end);
            blocked_scope(Other, Limit, Mode) -> {Other, Limit, Mode}.
            """;
        object code = Code(CodeGeneration.CompileBeamModule(source, "separated_clause_list_definitions.erl"));
        var atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        var entries = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Where(instruction => Opcode(instruction) == FunctionInfo)
            .Select(instruction => (Name: atoms[Value(Operand(instruction, 1))], Arity: Value(Operand(instruction, 2))))
            .ToArray();
        var walk = entries.Where(entry => entry.Name.StartsWith("-walk/3-", StringComparison.Ordinal)).ToArray();
        if (!walk.SequenceEqual(new[]
            {
                ("-walk/3-lc$^0/1-0-", 2),
                ("-walk/3-fun-2-", 2),
                ("-walk/3-lc$^1/1-1-", 2)
            })
            || entries.Any(entry => entry.Name.StartsWith("-blocked_head/4-lc$^", StringComparison.Ordinal)
                || entry.Name.StartsWith("-blocked_scope/3-lc$^", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }
}
