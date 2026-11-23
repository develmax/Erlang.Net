using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCountedTermFoldChecks
{
    internal const string TestName = "compiler/beam/delays-a-counted-fold-frame-until-after-arithmetic";
    private const string Fixture = "beam_counted_term_fold_probe.erl";
    private const string WrongSchedule = "The counted fold lost its delayed frame or the alternate callback was replaced.";
    private const string WrongResult = "The optimized count or the original rejection changed.";
    private const string Source = """
        -module(beam_counted_term_fold_probe).
        -export([measure/1, alternate/1]).
        measure({wrapped, Value}) ->
            measure(Value);
        measure(Value) when is_tuple(Value) ->
            measure(tuple_to_list(Value));
        measure([First, Second | Tail]) ->
            Combined = 40 * First + Second,
            {Head, HeadSize} = packed(Combined),
            {Rest, RestSize} = lists:mapfoldl(fun count/2, 0, Tail),
            {[Head | Rest], HeadSize + RestSize}.

        alternate({wrapped, Value}) ->
            alternate(Value);
        alternate(Value) when is_tuple(Value) ->
            alternate(tuple_to_list(Value));
        alternate([First, Second | Tail]) ->
            Combined = 40 * First + Second,
            {Head, HeadSize} = packed(Combined),
            {Rest, RestSize} = lists:mapfoldl(fun undo/2, 0, Tail),
            {[Head | Rest], HeadSize + RestSize}.

        packed(Value) when Value =< 127 ->
            {[255 band Value], 1};
        packed(Value) ->
            packed(Value bsr 7, [Value band 127], 1).
        packed(0, Acc, Count) ->
            {Acc, Count};
        packed(Value, Acc, Count) ->
            packed(Value bsr 7, [Value band 127 bor 128 | Acc], Count + 1).

        count(Value, Total) ->
            {Encoded, Size} = packed(Value),
            {Encoded, Total + Size}.
        undo(Value, Total) ->
            {Encoded, Size} = packed(Value),
            {Encoded, Total - Size}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] normal = ReadFunction("measure");
        object[] alternate = ReadFunction("alternate");
        var arithmetic = Array.FindIndex(normal, item => Opcode(item) == GcBifTwo);
        var closure = Array.FindIndex(normal, item => Opcode(item) == MakeFun);
        var allocate = Array.FindIndex(normal, arithmetic + 1, item => Opcode(item) == Allocate);
        if (arithmetic < 0 || allocate <= arithmetic || closure <= allocate
            || Items(normal[allocate], "Operands").ToArray() is not [var slots, var live]
            || Value(slots) != 1 || Value(live) != 3
            || alternate.Count(item => Opcode(item) == Allocate) == 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term components = Term.List(Term.List(Term.I(43)), Term.List(Term.I(129), Term.I(1)));
            Term source = Term.List(Term.I(1), Term.I(3), Term.I(129));
            Term tuple = Term.Tuple(Term.A("wrapped"), Term.Tuple(Term.I(1), Term.I(3), Term.I(129)));
            if (!Term.Tuple(components, Term.I(3)).Equals(await module.Invoke(context, "measure", source))
                || !Term.Tuple(components, Term.I(3)).Equals(await module.Invoke(context, "measure", tuple))
                || !Term.Tuple(components, Term.I(-1)).Equals(await module.Invoke(context, "alternate", source)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                _ = await module.Invoke(context, "measure", Term.List(Term.A("bad"), Term.I(3)));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArithmetic)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });

        object[] ReadFunction(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                    && atoms[Value(atom)] == name && Value(arity) == 1
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
