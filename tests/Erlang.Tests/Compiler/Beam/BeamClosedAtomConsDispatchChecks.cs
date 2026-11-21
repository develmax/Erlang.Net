using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedAtomConsDispatchChecks
{
    internal const string TestName = "compiler/beam/selects-distinct-atom-entries-before-unpacking-a-cons";
    private const string Fixture = "beam_closed_atom_cons_dispatch_probe.erl";
    private const string IncorrectSwitch = "The atom table or directly unpacked cons changed.";
    private const string IncorrectOutcome = "The atom entry changed a result or function-clause failure.";
    private const string Source = """
        -module(beam_closed_atom_cons_dispatch_probe).
        -export([encode/3, decode/3]).
        encode(value, Value, _Rest) ->
            {Value, 0};
        encode(params, Value, _Rest) ->
            tags(Value, [<<5>>]);
        encode(nested, Value, [Head | Tail]) ->
            child(Head, Value, Tail).

        decode(value, Value, _) ->
            Value;
        decode(params, Value, _) ->
            Converted = prepare(Value),
            tags(Converted, [flag]);
        decode(nested, Value, [Head | Tail]) ->
            child(Head, Value, Tail).

        prepare(failure) -> erlang:error(prepare_failure);
        prepare(Value) -> {prepared, Value}.
        tags(Value, Tag) -> {Tag, Value}.
        child(Head, Value, Tail) -> {Head, Value, Tail}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        foreach (string function in new[] { "encode", "decode" })
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                    && atoms[Value(atom)] == function && Value(arity) == 3
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectSwitch);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            object[] list = body.Where(instruction => Opcode(instruction) == GetList).ToArray();
            if (body.Count(instruction => Opcode(instruction) == SelectValue) != 1
                || list is not [var read] || Items(read, "Operands").ToArray() is not [var input, var head, var tail]
                || Value(input) != 2 || Value(head) != 0 || Value(tail) != 2)
            {
                throw new InvalidOperationException(IncorrectSwitch);
            }
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term data = Term.A("value");
            Term rest = Term.A("rest");
            Term head = Term.A("head");
            Term inputs = Cons.From([head], rest);
            Term nested = Term.Tuple(head, data, rest);
            Term tags = Term.List(Term.A("flag"));
            if (!nested.Equals(await module.Invoke(
                context,
                "encode",
                Term.A("nested"),
                data,
                inputs
            ))
                || !nested.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.A("nested"),
                    data,
                    inputs
                ))
                || !Term.Tuple(data, Term.I(0)).Equals(await module.Invoke(
                    context,
                    "encode",
                    Term.A("value"),
                    data,
                    Nil.Value
                ))
                || !Term.Tuple(tags, Term.Tuple(Term.A("prepared"), data)).Equals(
                    await module.Invoke(
                        context,
                        "decode",
                        Term.A("params"),
                        data,
                        Nil.Value
                    )))
            {
                throw new InvalidOperationException(IncorrectOutcome);
            }

            await ExpectError("nested", Nil.Value, ErlangErrorReasons.FunctionClause);
            await ExpectError("other", Nil.Value, ErlangErrorReasons.FunctionClause);
            await ExpectError("params", Term.A("failure"), "prepare_failure");

            return Nil.Value;

            async Task ExpectError(string selection, Term input, string reason)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        "decode",
                        Term.A(selection),
                        input,
                        Nil.Value
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(reason)))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectOutcome);
            }
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
