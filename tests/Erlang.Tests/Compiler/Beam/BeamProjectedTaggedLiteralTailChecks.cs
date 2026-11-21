using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedTaggedLiteralTailChecks
{
    internal const string TestName = "compiler/beam/keeps-tagged-tuples-live-until-the-selected-field-is-needed";
    private const string Fixture = "beam_projected_tagged_literal_tail_probe.erl";
    private const string IncorrectProjection = "The closed tuple field was not compared before the surviving input was projected.";
    private const string IncorrectOutcome = "The selected field, exact constant, or mismatch result changed.";
    private const string Source = """
        -module(beam_projected_tagged_literal_tail_probe).
        -export([observe/1]).
        observe(initial) ->
            true;
        observe(Value) ->
            try check(Value) of
                _ -> true
            catch
                throw:false -> false
            end.
        check(Value) ->
            case Value of
                {record, Field, {fixed, 3}} ->
                    verify(Field, 7);
                _ ->
                    throw(false)
            end.
        verify(Expected, Expected) -> yes;
        verify(_, _) -> throw(false).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)] == "check" && Value(arity) == 1
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectProjection);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[] fields = body.Where(instruction => Opcode(instruction) == GetTupleElement).ToArray();
        if (fields is not [var filter, var projection]
            || Items(filter, "Operands").Select(Value).ToArray() is not [0, 2, 1]
            || Items(projection, "Operands").Select(Value).ToArray() is not [0, 1, 0]
            || body.Count(instruction => Opcode(instruction) == ExactEqualTest) != 1)
        {
            throw new InvalidOperationException(IncorrectProjection);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term prefix = Term.A("record");
            Term value = Term.I(7);
            Term exact = Term.Tuple(Term.A("fixed"), Term.I(3));
            Term near = Term.Tuple(Term.A("fixed"), new FloatTerm(3.0));
            Term yes = Term.A(ErlangBooleanAtoms.True);
            Term no = Term.A(ErlangBooleanAtoms.False);
            if (!yes.Equals(await module.Invoke(context, "observe", Term.Tuple(prefix, value, exact)))
                || !no.Equals(await module.Invoke(context, "observe", Term.Tuple(prefix, value, near)))
                || !no.Equals(await module.Invoke(context, "observe", Term.Tuple(prefix, Term.I(8), exact)))
                || !no.Equals(await module.Invoke(context, "observe", Term.Tuple(Term.A("other"), value, exact)))
                || !no.Equals(await module.Invoke(context, "observe", Term.Tuple(prefix, value))))
            {
                throw new InvalidOperationException(IncorrectOutcome);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
