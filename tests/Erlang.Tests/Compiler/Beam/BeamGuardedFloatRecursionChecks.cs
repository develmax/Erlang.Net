using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedFloatRecursionChecks
{
    internal const string TestName = "compiler/beam/keeps-guarded-float-base-arm-frameless";
    private const string Module = "beam_guarded_float_recursion_probe";
    private const string IncorrectShape = "The exact integer guard and float/list roots must precede the recursive arm frame.";
    private const string IncorrectValue = "A guarded float recursion must preserve both branches and its function failure.";
    private const string Source = """
        -module(beam_guarded_float_recursion_probe).
        -export([format/1, different/1, finish/2]).

        format(T) when is_integer(T) ->
            if
                T < 100 -> ?MODULE:finish(short, [T / 10]);
                true -> ?MODULE:finish(long, [T div 100, format(T rem 100)])
            end.

        different(T) when is_integer(T) ->
            if
                T < 100 -> ?MODULE:finish(short, [T / 10]);
                true -> ?MODULE:finish(long, [T div 100, different(T rem 99)])
            end.

        finish(short, [Value]) -> {short, Value};
        finish(long, [Quotient, Tail]) -> {long, Quotient, Tail}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object[] instructions = Function(Code(image), "format");
        int floatTail = Array.FindIndex(instructions, instruction => Opcode(instruction) == TailExternalCall);
        int allocation = Array.FindIndex(instructions, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        if (floatTail < 0 || allocation <= floatTail
            || !Operands(instructions[allocation]).Select(Value).SequenceEqual(new[] { 1, 1 })
            || instructions.Take(floatTail).Count(item => Opcode(item) == FloatConvert) != 1
            || instructions.Take(floatTail).Count(item => Opcode(item) == TestHeap) != 1
            || instructions.Skip(allocation).Count(item => Opcode(item) == GcBifTwo && Value(Operands(item)[1]) == 1) != 2
            || Function(Code(image), "different").Count(item => Opcode(item) == FloatConvert) != 2)
        {
            throw new InvalidOperationException(IncorrectShape);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term shortTag = Term.A("short");
            Term longTag = Term.A("long");
            if (!(await module.Invoke(context, "format", Term.I(90))).Equals(Term.Tuple(shortTag, new FloatTerm(9)))
                || !(await module.Invoke(context, "format", Term.I(-20))).Equals(Term.Tuple(shortTag, new FloatTerm(-2)))
                || !(await module.Invoke(context, "format", Term.I(100)))
                    .Equals(Term.Tuple(longTag, Term.I(1), Term.Tuple(shortTag, new FloatTerm(0))))
                || !(await module.Invoke(context, "format", Term.I(205)))
                    .Equals(Term.Tuple(longTag, Term.I(2), Term.Tuple(shortTag, new FloatTerm(0.5)))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await module.Invoke(context, "format", new FloatTerm(2));
            }
            catch (ErlangException ex) when (ex.ExceptionClass == ErlangExceptionClasses.Error
                && ex.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);
        });
    }

    private static object[] Function(object code, string name)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
