using System.Collections;
using System.Numerics;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDeferredConsHeadChecks
{
    internal const string TestName = "compiler/beam/cons-tuple-head-follows-call-tail";
    private const string Module = "beam_deferred_cons_head";
    private const string Fixture = Module + ".erl";
    private const string ChangedSchedule = "The pure cons head was not constructed after its call tail.";
    private const string ChangedResult = "Cons head deferral changed evaluation order or binding behavior.";

    internal static async Task Run()
    {
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(Fixture));
        byte[] image = CodeGeneration.CompileBeamModule(source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        (string Name, int[] Entry, int PostCallHeap)[] shapes =
        [
            ("drop", [2, 5, 3], 5),
            ("-captured/2-fun-0-", [2, 5, 3], 5),
            ("repeated", [2, 5, 3], 7),
            ("improper", [1, 3], 5)
        ];
        foreach (var (name, entry, words) in shapes)
        {
            object function = Items(code, "Functions").Single(function => (string)Property(function, "Name") == name);
            int first = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            object[] body = instructions[first..end];
            int allocate = Array.FindIndex(body, instruction => IsOpcode(instruction,
                entry.Length == 2 ? BeamEmissionFormat.Allocate : BeamEmissionFormat.AllocateHeap));
            int call = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.ExternalCall));
            int heap = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.TestHeap));
            if (allocate < 0 || allocate >= call || call >= heap
                || !OperandValues(body[allocate]).SequenceEqual(entry.Select(value => new BigInteger(value)))
                || !OperandValues(body[heap]).SequenceEqual([new BigInteger(words), BigInteger.One])
                || heap + 2 >= body.Length
                || !IsOpcode(body[heap + 1], BeamEmissionFormat.PutTuple)
                || !IsOpcode(body[heap + 2], BeamEmissionFormat.PutList))
            {
                throw new InvalidOperationException(ChangedSchedule + " " + name);
            }
        }

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("order_key");
            Term outside = Term.A("outer");
            Term inside = Term.A("inner");
            Term keep = Term.A("keep");
            Term head = Term.A("head");
            Term tail = Term.A("tail");
            Term undefined = Term.A("undefined");
            Term tracked = Term.Tuple(outside, inside);
            Term values = Term.List(tracked, key, tracked);
            Term remaining = Term.List(tracked, key, tracked);
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("drop", [values, outside, inside], remaining),
                ("invoke_captured", [values, outside, inside], remaining),
                ("repeated", [values, outside, inside], Term.List(Term.Tuple(
                    inside,
                    outside,
                    outside,
                    keep
                ), key, tracked)),
                ("ordered", [key], Term.List(Term.Tuple(undefined, keep), head)),
                ("improper", [outside, key, keep], new Cons(Term.Tuple(outside, keep), tail)),
                ("head_match_failure", [key], Term.A("initial")),
                ("invoke_shadow", [outside, inside], new Cons(Term.Tuple(inside, keep), undefined)),
                ("malformed", [outside, inside], Term.A("caught"))
            ];
            foreach (var (function, arguments, expected) in cases)
            {
                if (!expected.Equals(await module.Invoke(context, function, arguments)))
                {
                    throw new InvalidOperationException(ChangedResult + " " + function);
                }
            }

            return Nil.Value;
        });
    }

    private static bool IsOpcode(object instruction, byte opcode) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode") == opcode;

    private static IEnumerable<BigInteger> OperandValues(object instruction) =>
        Items(instruction, "Operands").Select(operand => (BigInteger)Property(operand, "Value"));

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
