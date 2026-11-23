using System.Collections;
using System.Numerics;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedTupleConstructionChecks
{
    internal const string TestName = "compiler/beam/retains-entry-tuple-across-producer";
    private const string Module = "beam_retained_tuple_construction";
    private const string Fixture = Module + ".erl";
    private const string ChangedSchedule = "The checked entry tuple was not retained across the producer.";
    private const string ChangedBehavior = "Retaining a checked entry tuple changed source behavior.";

    internal static async Task Run()
    {
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(Fixture));
        byte[] image = CodeGeneration.CompileBeamModule(source, Fixture);
        object code = Code(image);
        AssertDeferredProjection(Body(code, "pack"), 8, [0, 1, 3]);
        AssertDeferredProjection(Body(code, "packed_change"), 5, [0, 2]);
        AssertDeferredProjection(Body(code, "explicit_failure"), 5, [0, 1, 3]);
        AssertEarlyProjection(Body(code, "direct_second"), receiver: 0, destination: 1);
        AssertEarlyProjection(Body(code, "direct_third"), receiver: 1, destination: 2);
        AssertEarlyProjection(Body(code, "pack"), receiver: 0, destination: 0);
        AssertEarlyProjection(Body(code, "repeated_producer_input"), receiver: 0, destination: 0);

        string disabledSource = source.Replace("-export(", "-compile(no_ssa_opt).\n-export(", StringComparison.Ordinal);
        var disabledModule = new Parser(disabledSource).ParseModule();
        var locations = Enumerable.Range(3, disabledModule.DeclaredFunctions.Count)
            .Select(line => new BeamSourceLocation(null, line)).ToArray();
        object disabled = Code(new BeamModuleCompiler(disabledModule.Name, sourceModule: disabledModule).Emit(
            disabledModule.DeclaredFunctions,
            locations,
            disabledModule.DeclaredExports.ToHashSet(),
            new BeamModuleMetadata(disabledModule.Attributes, null)
        ));
        object allocation = Body(disabled, "pack").First(instruction => IsOpcode(instruction, BeamEmissionFormat.Allocate));
        if (OperandValue(allocation, 0) <= BigInteger.One)
        {
            throw new InvalidOperationException("Disabling SSA optimization did not keep the original entry plan.");
        }

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term c = Term.A("c");
            Term p = Term.A("p");
            Term key = Term.A("key");
            Term undefined = Term.A("undefined");
            Term next = Term.A("next");
            Term shared = Term.A("shared");
            Term saved = Term.A("saved");
            Term okay = Term.A("ok");
            Term changed = Term.A("changed");
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("pack", [Term.Tuple(
                    a,
                    b,
                    p,
                    c
                ), key], Term.Tuple(okay, Term.Tuple(
                    a,
                    b,
                    undefined,
                    c
                ))),
                ("packed_change", [Term.Tuple(a, next, c), key], Term.Tuple(
                    c,
                    p,
                    a,
                    changed
                )),
                ("shared_before_call", [Term.Tuple(
                    a,
                    b,
                    shared,
                    c
                ), key], Term.Tuple(okay, Term.Tuple(c, b, a))),
                ("direct_second", [Term.Tuple(a, b, p)], Term.Tuple(a, b, undefined)),
                ("direct_third", [key, Term.Tuple(a, b, p)], Term.Tuple(a, b, Term.Tuple(key, Term.A("default"), p))),
                ("repeated_producer_input", [Term.Tuple(a, b, p)], Term.Tuple(a, b, Term.Tuple(p, p))),
                ("rejected_head", [Term.A("head_key")], undefined),
                ("rejected_guard", [Term.A("guard_key")], undefined),
                ("observed_failure", [Term.A("effect_key")], saved),
                ("invoke_shadow", [a, b, Term.A("shadow_key")], Term.Tuple(b, Term.A("kept")))
            ];
            foreach (var (function, arguments, expected) in cases)
            {
                if (!expected.Equals(await module.Invoke(context, function, arguments)))
                {
                    throw new InvalidOperationException(ChangedBehavior + " " + function);
                }
            }

            return Nil.Value;
        });
    }

    private static void AssertDeferredProjection(object[] body, int heapWords, int[] fields)
    {
        int allocation = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.Allocate));
        int call = Array.FindIndex(
            body,
            instruction =>
            IsOpcode(instruction, BeamEmissionFormat.ExternalCall) || IsOpcode(instruction, BeamEmissionFormat.LocalCall)
        );
        int heap = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.TestHeap));
        if (allocation < 0 || call <= allocation || heap <= call
            || OperandValue(body[allocation], 0) != BigInteger.One || OperandValue(body[allocation], 1) != 2
            || OperandValue(body[heap], 0) != heapWords || OperandValue(body[heap], 1) != BigInteger.One
            || !body.Skip(heap + 1).TakeWhile(instruction => IsOpcode(instruction, BeamEmissionFormat.GetTupleElement))
                .Select(instruction => (int)OperandValue(instruction, 1)).SequenceEqual(fields))
        {
            throw new InvalidOperationException(ChangedSchedule);
        }
    }

    private static void AssertEarlyProjection(object[] body, int receiver, int destination)
    {
        object[] projections = body.TakeWhile(instruction =>
            !IsOpcode(instruction, BeamEmissionFormat.LocalCall) && !IsOpcode(instruction, BeamEmissionFormat.ExternalCall))
            .Where(instruction => IsOpcode(instruction, BeamEmissionFormat.GetTupleElement)).ToArray();
        if (projections is not [var field] || OperandValue(field, 0) != receiver || OperandValue(field, 2) != destination)
        {
            throw new InvalidOperationException(ChangedSchedule);
        }
    }

    private static object[] Body(object code, string name)
    {
        object function = Items(code, "Functions").Single(function => (string)Property(function, "Name") == name);
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions")[first..end];
    }

    private static bool IsOpcode(object instruction, byte opcode) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode") == opcode;

    private static BigInteger OperandValue(object instruction, int index) =>
        (BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
