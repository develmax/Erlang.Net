using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProvenAtomEntryChecks
{
    internal const string TestName = "compiler/beam/types-proven-atom-entry-operands";
    private const string Fixture = "beam_private_atom_operand_probe.erl";
    private const string WrongType = "The atom entry operand disagrees with the source proof.";
    private const string WrongValue = "The atom selector changed the accepted value or its fallback.";
    private const string Source = """
        -module(beam_private_atom_operand_probe).
        -export([exact/2, choose/2, two_way/1, open/1, open_known/0, scope/2, open_scope/2, head/2, open_head/2]).

        exact(first, Input) -> pick(active, Input);
        exact(second, Input) -> pick(passive, Input).
        pick(active, Input) -> {one,Input};
        pick(_, Input) -> {two,Input}.

        choose(generated, Flag) -> default(generated, Flag);
        choose(record, Flag) -> default(record, Flag);
        choose(other, Flag) -> default(other, Flag).
        default(generated, false) -> true;
        default(record, false) -> true;
        default(_, _) -> false.

        two_way(first) -> pair(first);
        two_way(second) -> pair(second).
        pair(first) -> one;
        pair(second) -> two.

        open(Choice) -> open_pick(Choice).
        open_known() -> open_pick(active).
        open_pick(active) -> yes;
        open_pick(_) -> no.

        scope(Kind, Input) when is_atom(Kind) -> dispatch(Kind, Input);
        scope({Kind, Key}, Input) when is_atom(Kind) -> dispatch({Kind, Key}, Input).
        dispatch(Kind, Input) -> "prefix:" ++ scope_pick(Kind, Input).
        scope_pick(Kind, Input) ->
            case Kind of
                information ->
                    case Input of
                        [1,2] -> "first";
                        [1,3] -> "second"
                    end
            end.

        open_scope(Kind, Input) -> "prefix:" ++ open_scope_pick(Kind, Input).
        open_scope_pick(Kind, Input) ->
            case Kind of
                information ->
                    case Input of
                        [1,2] -> "first";
                        [1,3] -> "second"
                    end
            end.

        head(first, Rows) -> head_key(active, Rows);
        head(second, Rows) -> head_key(passive, Rows).
        head_key(Key, [{Key,Value}|_]) -> {ok,Value};
        head_key(_, _) -> none.

        open_head(Key, Rows) -> open_head_key(Key, Rows).
        open_head_key(Key, [{Key,Value}|_]) -> {ok,Value};
        open_head_key(_, _) -> none.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!TypedInput("pick", ExactEqualTest, 1) || !TypedInput("default", SelectValue, 0)
            || TypedInput("pair", ExactEqualTest, 1) || TypedInput("open_pick", ExactEqualTest, 1)
            || !TypedInput("scope_pick", ExactEqualTest, 1) || TypedInput("open_scope_pick", ExactEqualTest, 1)
            || !(TypedInput("head_key", ExactEqualTest, 1) || TypedInput("head_key", ExactEqualTest, 2))
            || TypedInput("open_head_key", ExactEqualTest, 1) || TypedInput("open_head_key", ExactEqualTest, 2))
        {
            throw new InvalidOperationException(WrongType);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term one = Term.A("one");
            Term two = Term.A("two");
            Term payload = Term.I(9);
            Term yes = Term.A("true");
            Term no = Term.A("false");
            Term kind = Term.A("information");
            Term input = Term.List([Term.I(1), Term.I(2)]);
            Term prefixed = Term.List("prefix:first".Select(character => Term.I(character)).ToArray());
            if (!prefixed.Equals(await module.Invoke(
                context,
                "scope",
                kind,
                input
            ))
                || !prefixed.Equals(await module.Invoke(
                    context,
                    "open_scope",
                    kind,
                    input
                ))
                || !Term.Tuple(one, payload).Equals(await module.Invoke(
                    context,
                    "exact",
                    first,
                    payload
                ))
                || !Term.Tuple(two, payload).Equals(await module.Invoke(
                    context,
                    "exact",
                    second,
                    payload
                ))
                || !yes.Equals(await module.Invoke(
                    context,
                    "choose",
                    Term.A("generated"),
                    no
                ))
                || !no.Equals(await module.Invoke(
                    context,
                    "choose",
                    Term.A("record"),
                    yes
                ))
                || !no.Equals(await module.Invoke(
                    context,
                    "choose",
                    Term.A("other"),
                    no
                ))
                || !one.Equals(await module.Invoke(context, "two_way", first))
                || !two.Equals(await module.Invoke(context, "two_way", second))
                || !Term.A("yes").Equals(await module.Invoke(context, "open_known"))
                || !Term.A("no").Equals(await module.Invoke(context, "open", payload)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term rows = Term.List(Term.Tuple(Term.A("active"), payload));
            Term matched = Term.Tuple(Term.A("ok"), payload);
            if (!matched.Equals(await module.Invoke(
                context,
                "head",
                first,
                rows
            ))
                || !Term.A("none").Equals(await module.Invoke(
                    context,
                    "head",
                    second,
                    rows
                ))
                || !matched.Equals(await module.Invoke(
                    context,
                    "open_head",
                    payload,
                    Term.List(Term.Tuple(payload, payload))
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });

        bool TypedInput(string function, byte opcode, int operand)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == function
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongType);
            }

            object instruction = instructions.Skip(entry + 1)
                .TakeWhile(operation => Opcode(operation) != FunctionInfo)
                .First(operation => Opcode(operation) == opcode);
            object tested = Items(instruction, "Operands").ElementAt(operand);

            return tested.GetType().GetProperty("TypeIndex")!.GetValue(tested) is int;
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
