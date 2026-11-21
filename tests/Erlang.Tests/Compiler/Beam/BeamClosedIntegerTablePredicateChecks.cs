using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedIntegerTablePredicateChecks
{
    internal const string TestName = "compiler/beam/uses-a-closed-integer-default-with-a-single-exception-slot";
    private const string Fixture = "beam_closed_integer_table_predicate_probe.erl";
    private const string IncorrectInstructions = "The closed default emitted the wrong integer test or exception frame.";
    private const string IncorrectResults = "The closed default changed the result of a named or numeric value.";

    private const string Source = """
        -module(beam_closed_integer_table_predicate_probe).
        -export([matches/1, first/1, variable/2]).

        matches(default) -> true;
        matches(Value) ->
            try matches_integer(Value, 7, [{named,7},{other,8}]) of
                _ -> true
            catch
                throw:false -> false
            end.

        first(default) -> true;
        first(Value) ->
            try matches_first(Value, 1, [{primary,1},{other,2}]) of
                _ -> true
            catch
                throw:false -> false
            end.

        variable(default, _) -> true;
        variable(Value, Default) ->
            try variable_integer(Value, Default, [{named,7},{other,8}]) of
                _ -> true
            catch
                throw:false -> false
            end.

        matches_integer(Value, Value, _) when is_integer(Value) -> true;
        matches_integer(Value, Default, Table) when is_atom(Value) ->
            case lists:keyfind(Value, 1, Table) of
                {_,Default} -> true;
                _ -> throw(false)
            end;
        matches_integer(_, _, _) -> throw(false).

        matches_first(Value, Value, _) when is_integer(Value) -> true;
        matches_first(Value, Default, Table) when is_atom(Value) ->
            case lists:keyfind(Value, 1, Table) of
                {_,Default} -> true;
                _ -> throw(false)
            end;
        matches_first(_, _, _) -> throw(false).

        variable_integer(Value, Value, _) when is_integer(Value) -> true;
        variable_integer(Value, Default, Table) when is_atom(Value) ->
            case lists:keyfind(Value, 1, Table) of
                {_,Default} -> true;
                _ -> throw(false)
            end;
        variable_integer(_, _, _) -> throw(false).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] protectedCall = Read("matches");
        object[] closed = Read("matches_integer");
        object[] variable = Read("variable_integer");
        object[] first = Read("matches_first");
        object[] firstSetup = first.SkipWhile(instruction => Opcode(instruction) != Allocate)
            .TakeWhile(instruction => Opcode(instruction) != ExternalCall).ToArray();
        if (Frames(protectedCall) is not [1]
            || Frames(closed) is not [0]
            || Frames(first) is not [0]
            || firstSetup.Any(instruction => Opcode(instruction) == Move)
            || closed.Any(instruction => Opcode(instruction) == IsInteger)
            || !variable.Any(instruction => Opcode(instruction) == IsInteger))
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            foreach (var (value, expected) in new (Term, Term)[]
            {
                (Term.A("default"), Term.A("true")),
                (Term.A("named"), Term.A("true")),
                (Term.A("other"), Term.A("false")),
                (Term.A("unknown"), Term.A("false")),
                (Term.I(7), Term.A("true")),
                (Term.I(8), Term.A("false")),
                (Term.List(Term.I(7)), Term.A("false"))
            })
            {
                if (!expected.Equals(await module.Invoke(context, "matches", value)))
                {
                    throw new InvalidOperationException(IncorrectResults);
                }
            }

            if (!Term.A("true").Equals(await module.Invoke(context, "first", Term.A("primary")))
                || !Term.A("false").Equals(await module.Invoke(context, "first", Term.I(2)))
                || !Term.A("true").Equals(await module.Invoke(
                    context,
                    "variable",
                    Term.A("other"),
                    Term.I(8)
                ))
                || !Term.A("false").Equals(await module.Invoke(
                    context,
                    "variable",
                    Term.A("other"),
                    Term.I(7)
                )))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            return Nil.Value;
        });

        object[] Read(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static int[] Frames(object[] body) => body.Where(instruction => Opcode(instruction) == Allocate)
        .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
