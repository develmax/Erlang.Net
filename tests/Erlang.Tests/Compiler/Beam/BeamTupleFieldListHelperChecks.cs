using System.Collections;
using System.Numerics;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleFieldListHelperChecks
{
    internal const string TestName = "compiler/beam/tuple-list-generator-checks-before-recursing";
    private const string Module = "beam_tuple_field_list_helper";
    private const string Fixture = Module + ".erl";
    private const string ChangedSchedule = "The tuple list helper lost its checked deferred field projection.";
    private const string ChangedBehavior = "Tuple list projection changed source traversal or failure behavior.";

    internal static async Task Run()
    {
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(Fixture));
        byte[] image = CodeGeneration.CompileBeamModule(source, Fixture);
        object code = Code(image);
        AssertSchedule(code, "-project/1-lc$^0/1-0-");

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        await actor.InvokeAsync(async context =>
        {
            Term bucket = Term.A("bucket");
            Term key = Term.A("key");
            Term one = Term.A("one");
            Term two = Term.A("two");
            Term stop = Term.A("stop");
            Term other = Term.A("wrong_record");
            Term tail = Term.A("tail");
            Term bad = Term.A("bad");
            Term first = Term.Tuple(key, one);
            Term second = Term.Tuple(key, two);
            Term valid = Term.List(
                first,
                bad,
                Term.Tuple(key, one, two),
                second
            );
            Term improper = new Cons(first, new Cons(bad, new Cons(second, tail)));
            Term projected = Term.List(one, two);
            Term badGenerator = Term.Tuple(Term.A("bad_generator"), tail);
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("project", [Term.Tuple(bucket, valid, key)], projected),
                ("project", [Term.Tuple(bucket, Nil.Value, key)], Nil.Value),
                ("raw", [valid], projected),
                ("repeated", [Term.List(Term.Tuple(one, two), Term.Tuple(one, one), bad)], Term.List(one)),
                ("caught", [Term.A("project"), other], Term.Tuple(Term.A("badrecord"), other)),
                ("caught", [Term.A("project"), Term.Tuple(bucket, improper, key)], badGenerator),
                ("caught", [Term.A("strict"), valid], Term.Tuple(Term.A("badmatch"), bad)),
                ("caught", [Term.A("effect"), Term.List(first, Term.Tuple(key, stop), second)],
                    Term.Tuple(Term.A("selected"), Term.List(stop, one))),
                ("caught", [Term.A("effect"), improper], Term.Tuple(badGenerator, Term.List(two, one)))
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

    private static void AssertSchedule(object code, string name)
    {
        object? function = Items(code, "Functions").SingleOrDefault(function => (string)Property(function, "Name") == name);
        if (function is null)
        {
            throw new InvalidOperationException(ChangedSchedule);
        }

        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions")[first..end];
        int call = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.LocalCall));
        int check = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.TestArity));
        int allocation = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.Allocate));
        if (check < 0 || check >= allocation || allocation >= call || call + 3 >= body.Length
            || Value(body[allocation], 0) != BigInteger.One || Value(body[allocation], 1) != 2
            || !IsOpcode(body[call + 1], BeamEmissionFormat.TestHeap)
            || !IsOpcode(body[call + 2], BeamEmissionFormat.GetTupleElement)
            || !IsOpcode(body[call + 3], BeamEmissionFormat.PutList)
            || Value(body[call + 2], 1) != BigInteger.One)
        {
            throw new InvalidOperationException(ChangedSchedule);
        }
    }

    private static bool IsOpcode(object instruction, byte opcode) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode") == opcode;

    private static BigInteger Value(object instruction, int index) =>
        (BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();
}
