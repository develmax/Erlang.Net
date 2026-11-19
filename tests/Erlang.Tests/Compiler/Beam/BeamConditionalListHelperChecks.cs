using System.Collections;
using System.Numerics;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConditionalListHelperChecks
{
    internal const string TestName = "compiler/beam/conditional-list-generator-uses-own-recursion";
    private const string Module = "beam_conditional_list_helper";
    private const string Fixture = Module + ".erl";
    private const string Helper = "-transform/1-lc$^0/1-0-";
    private const string HelperMissing = "The conditional list traversal helper was not emitted.";
    private const string ChangedBehavior = "Moving the conditional into a recursive helper changed traversal behavior.";

    internal static async Task Run()
    {
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(Fixture));
        byte[] image = CodeGeneration.CompileBeamModule(source, Fixture);
        object code = Code(image);
        object[] functions = Items(code, "Functions");
        object? helper = functions.SingleOrDefault(function => (string)Property(function, "Name") == Helper);
        if (helper is null)
        {
            throw new InvalidOperationException(HelperMissing);
        }

        int first = (int)Property(helper, "FirstInstructionIndex");
        int end = (int)Property(helper, "EndInstructionIndex");
        object[] body = Items(code, "Instructions")[first..end];
        int call = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.LocalCall));
        int arithmetic = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.GcBifTwo));
        object allocation = body.Single(instruction => IsOpcode(instruction, BeamEmissionFormat.Allocate));
        if (arithmetic < 0 || arithmetic >= call || call + 2 >= body.Length
            || body.Count(instruction => IsOpcode(instruction, BeamEmissionFormat.IsGreaterOrEqual)) != 2
            || !IsOpcode(body[call + 1], BeamEmissionFormat.TestHeap)
            || !IsOpcode(body[call + 2], BeamEmissionFormat.PutList)
            || OperandValue(allocation, 0) != BigInteger.One || OperandValue(allocation, 1) != BigInteger.One)
        {
            throw new InvalidOperationException(HelperMissing);
        }

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term atom = Term.A("atom");
            Term input = Term.List(
                new Integer(9),
                new Integer(10),
                new FloatTerm(10.5),
                new Integer(20),
                new Integer(21),
                atom
            );
            Term result = Term.List(
                new Integer(9),
                new Integer(7),
                new FloatTerm(7.5),
                new Integer(17),
                new Integer(21),
                atom
            );
            Term effects = Term.Tuple(
                Term.List(
                    new Integer(9),
                    new Integer(17),
                    new Integer(7),
                    atom,
                    new Integer(21)
                ),
                Term.List(new Integer(10), new Integer(20))
            );
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("transform", [input], result),
                ("transform", [Nil.Value], Nil.Value),
                ("bad_input", [new Cons(new Integer(12), atom)], atom),
                ("bad_input", [atom], atom),
                ("effects", [], effects),
                ("selected_failure", [], Term.List(new Integer(15), new Integer(11)))
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

    private static bool IsOpcode(object instruction, byte opcode) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode") == opcode;

    private static BigInteger OperandValue(object instruction, int index) =>
        (BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
