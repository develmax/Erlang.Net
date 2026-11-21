using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSeparatedTupleOwnerDefinitionsChecks
{
    internal const string TestName = "compiler/beam/orders-list-helpers-between-separate-tuple-owner-closures";
    private const string Fixture = "mixed_alias_owner.erl";
    private const string WrongOrder = "The tuple owner must retain the pinned Core and SSA helper identities and list definition order.";
    private const string WrongResult = "The tuple owner's guarded list helpers changed the returned values.";
    private const string Source = """
        -module(mixed_alias_owner).
        -export([route/2]).
        -record(state,{value=ok}).
        route({left,#state{value=Value}}, Inputs) ->
            Items = pairs(Inputs),
            First = [T || T={K,_} <- Items, K =/= dropped, K =/= blank],
            Second = [T || T={kept,_} <- Items],
            {Value,First,Second};
        route({single,S}, Inputs) -> {S,[T || T={item,_} <- Inputs]};
        route({callback,_}, _) -> fun(X) -> {old,X} end;
        route({Tag,S}, Inputs) when Tag == pair; Tag == both ->
            {S,{[T || T={left,_} <- Inputs],[T || T={right,_} <- Inputs]}};
        route({alpha,_}, _) -> fun(X) -> {alpha,X} end;
        route({beta,_}, _) -> fun(X) -> {beta,X} end;
        route({gamma,_}, _) -> fun(X) -> {gamma,X} end;
        route({Other,#state{}}, _) -> {other,Other}.
        pairs(Inputs) when is_list(Inputs) ->
            [T || T={K,_} <- Inputs, K =/= invisible].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        string[] definitions = Items(code, "Instructions")
            .Where(instruction => Opcode(instruction) == FunctionInfo)
            .Select(instruction => atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")])
            .ToArray();
        string[] helpers = definitions.Where(name => name.StartsWith("-route/2-lc$", StringComparison.Ordinal)).ToArray();
        if (!helpers.SequenceEqual(new[]
            {
                "-route/2-lc$^4/1-1-", "-route/2-lc$^5/1-0-", "-route/2-lc$^1/1-3-",
                "-route/2-lc$^0/1-2-", "-route/2-lc$^2/1-4-"
            })
            || !new[] { "-route/2-fun-3-", "-route/2-fun-6-", "-route/2-fun-7-", "-route/2-fun-8-" }
                .All(definitions.Contains))
        {
            throw new InvalidOperationException(WrongOrder);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term kept = Term.Tuple(Term.A("kept"), Term.I(7));
            Term dropped = Term.Tuple(Term.A("dropped"), Term.I(8));
            Term blank = Term.Tuple(Term.A("blank"), Term.I(9));
            Term other = Term.Tuple(Term.A("other"), Term.I(10));
            Term payload = Term.A("payload");
            Term returned = await module.Invoke(
                context,
                "route",
                Term.Tuple(Term.A("left"), Term.Tuple(Term.A("state"), payload)),
                Term.List(
                    blank,
                    kept,
                    dropped,
                    other,
                    Term.A("non_tuple")
                )
            );
            if (!Term.Tuple(payload, Term.List(kept, other), Term.List(kept)).Equals(returned))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term left = Term.Tuple(Term.A("left"), Term.I(1));
            Term right = Term.Tuple(Term.A("right"), Term.I(2));
            Term separated = await module.Invoke(
                context,
                "route",
                Term.Tuple(Term.A("pair"), payload),
                Term.List(right, left)
            );
            if (!Term.Tuple(payload, Term.Tuple(Term.List(left), Term.List(right))).Equals(separated))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
