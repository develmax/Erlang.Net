using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedListMembershipChecks
{
    internal const string TestName = "compiler/beam/propagates-pair-and-list-types-through-private-membership-entries";
    private const string Fixture = "beam_closed_list_membership_probe.erl";
    private const string IncorrectLayout = "The entry retained a redundant pair check or prematurely extracted the list cells.";
    private const string IncorrectResult = "The known list proof changed an exact membership check or the failure result.";

    private const string Source = """
        -module(beam_closed_list_membership_probe).
        -export([observe/1, observe_open/2]).
        observe(Value) ->
            try bridge(Value, {[[1, iso], [2, member_body]], [42, 7]}) of
                _ -> true
            catch
                throw:false -> false
            end.
        observe_open(Value, Pair) ->
            try loose(Value, Pair) of
                _ -> true
            catch
                throw:false -> false
            end.

        bridge(Value, {Prefix, Tail}) when is_tuple(Value) ->
            walk(tuple_to_list(Value), Prefix, Tail);
        bridge(_, _) ->
            throw(false).

        loose(Value, {Prefix, Tail}) when is_tuple(Value) ->
            consume(tuple_to_list(Value), Prefix, Tail);
        loose(_, _) ->
            throw(false).

        consume(_, yes, _) -> yes;
        consume(_, _, _) -> throw(false).

        walk([Item | Rest], [Key | Keys], Tail) ->
            case lists:member(Item, Key) of
                false -> throw(false);
                true -> walk(Rest, Keys, Tail)
            end;
        walk(Tail, [], Tail) ->
            true;
        walk(_, _, _) ->
            throw(false).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] bridge = Function("bridge", 2);
        object[] loose = Function("loose", 2);
        object[] loop = Function("walk", 3);
        int[] labels = loop.Where(instruction => Opcode(instruction) == Label)
            .Select(instruction => Value(Items(instruction, "Operands").Single())).ToArray();
        object[] bridgeFrames = bridge.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        object[] loopFrames = loop.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (bridgeFrames is not [var bridgeFrame] || Items(bridgeFrame, "Operands").Select(Value).ToArray() is not [1, 2]
            || bridge.Any(instruction => Opcode(instruction) == TestArity)
            || !loose.Any(instruction => Opcode(instruction) == TestArity)
            || loopFrames is not [var loopFrame] || Items(loopFrame, "Operands").Select(Value).ToArray() is not [3, 5]
            || !labels.SequenceEqual(labels.Order()))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term yes = Term.A(ErlangBooleanAtoms.True);
            Term no = Term.A(ErlangBooleanAtoms.False);
            Term accepted = Term.Tuple(
                Term.A("iso"),
                Term.A("member_body"),
                Term.I(42),
                Term.I(7)
            );
            Term wrongPrefix = Term.Tuple(
                new FloatTerm(1.0),
                Term.I(2),
                Term.I(42),
                Term.I(7)
            );
            Term wrongTail = Term.Tuple(
                Term.I(1),
                Term.I(2),
                Term.I(42),
                Term.I(8)
            );
            if (!yes.Equals(await module.Invoke(context, "observe", accepted))
                || !no.Equals(await module.Invoke(context, "observe", wrongPrefix))
                || !no.Equals(await module.Invoke(context, "observe", wrongTail))
                || !no.Equals(await module.Invoke(context, "observe", Nil.Value))
                || !no.Equals(await module.Invoke(
                    context,
                    "observe_open",
                    accepted,
                    Term.A("not_a_pair")
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            return Nil.Value;
        });

        object[] Function(string name, int arity)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var count]
                    && atoms[Value(atom)] == name && Value(count) == arity
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
