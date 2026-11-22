using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedNestedTupleReturnChecks
{
    internal const string TestName = "compiler/beam/retains-checked-nested-tuple-in-result-register";
    private const string Fixture = "beam_checked_nested_tuple_return_probe.erl";
    private const string WrongLayout = "The checked nested tuple must remain the sole heap root before its final projection.";
    private const string WrongValue = "The checked nested tuple changed its fallback, result, or case failure value.";

    private const string Source = """
        -module(beam_checked_nested_tuple_return_probe).
        -export([probe/2, failing/2]).
        probe(Key, Cell) ->
            case fetch(Cell) of
                {value,{Key,Payload}} -> {value,Payload};
                _ -> false
            end.
        failing(Key, Cell) ->
            case fetch(Cell) of
                {value,{Key,Payload}} -> {value,Payload}
            end.
        fetch(Cell) -> erlang:get(Cell).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.I(1);
            Term cell = Term.A("selected");
            Term tag = Term.A("value");
            Term payload = Term.A("payload");
            Term source = Term.Tuple(tag, Term.Tuple(key, payload));
            context.Dictionary[cell] = source;
            if (!Term.Tuple(tag, payload).Equals(await module.Invoke(
                context,
                "probe",
                key,
                cell
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term[] rejected =
            [
                Term.Tuple(tag, Term.Tuple(new FloatTerm(1.0), payload)),
                Term.Tuple(tag, Term.Tuple(key)),
                Term.Tuple(Term.A("unexpected"), Term.Tuple(key, payload))
            ];
            foreach (Term wrong in rejected)
            {
                context.Dictionary[cell] = wrong;
                if (!Term.A(ErlangBooleanAtoms.False).Equals(await module.Invoke(
                    context,
                    "probe",
                    key,
                    cell
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "failing",
                    key,
                    cell
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(
                Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), rejected[^1])))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var name, var arity] && atoms[Value(name)] == "probe" && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int heap = Array.FindIndex(body, instruction => Opcode(instruction) == TestHeap);
        if (heap < 2 || heap + 1 >= body.Length || Opcode(body[heap - 1]) != ExactEqualTest
            || Operands(body[heap]) is not [var words, var live] || Value(words) != 3 || Value(live) != 1
            || Opcode(body[heap + 1]) != GetTupleElement
            || Operands(body[heap + 1]) is not [var owner, var member, var output]
            || !IsX0(owner) || Value(member) != 1 || !IsX0(output))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static bool IsX0(object operand) => Property(operand, "Kind") is "x" && Value(operand) == 0;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
