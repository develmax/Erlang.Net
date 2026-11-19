using System.Collections;
using System.Numerics;
using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedTupleListHelperChecks
{
    internal const string TestName = "compiler/beam/tagged-list-generator-keeps-matched-tuple";
    private const string Module = "beam_tagged_tuple_list_helper";
    private const string Fixture = Module + ".erl";
    private const string ChangedSchedule = "The tagged tuple list helper did not retain the checked item across its recursive call.";
    private const string ChangedBehavior = "The tagged list helper changed tuple matching or traversal behavior.";

    internal static async Task Run()
    {
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(Fixture));
        byte[] image = CodeGeneration.CompileBeamModule(source, Fixture);
        object code = Code(image);
        AssertSchedule(code, "-aliases/1-lc$^0/1-0-", projectsField: false);
        AssertSchedule(code, "-records/1-lc$^0/1-0-", projectsField: false);
        AssertSchedule(code, "-fields/1-lc$^0/1-0-", projectsField: true);
        AssertSchedule(code, "-record_fields/1-lc$^0/1-0-", projectsField: true);

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("entry");
            Term other = Term.A("other");
            Term tail = Term.A("tail");
            Term one = new Integer(1);
            Term first = Term.Tuple(tag, one, one);
            Term second = Term.Tuple(tag, other, tag);
            Term wrongTag = Term.Tuple(other, other, other);
            Term wrongArity = Term.Tuple(tag, other);
            Term input = Term.List(
                first,
                wrongTag,
                wrongArity,
                other,
                second
            );
            Term matches = Term.List(first, second);
            Term projected = Term.List(one, other);
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("aliases", [input], matches),
                ("records", [input], matches),
                ("fields", [input], projected),
                ("record_fields", [input], projected),
                ("aliases", [Nil.Value], Nil.Value),
                ("repeated", [input], Term.List(one)),
                ("guarded", [input], Term.List(first)),
                ("caught", [Term.A("aliases"), new Cons(first, tail)], Term.Tuple(Term.A("bad_generator"), tail)),
                ("caught", [Term.A("strict"), input], Term.Tuple(Term.A("badmatch"), wrongTag))
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

    private static void AssertSchedule(object code, string name, bool projectsField)
    {
        object? function = Items(code, "Functions").SingleOrDefault(function => (string)Property(function, "Name") == name);
        if (function is null)
        {
            throw new InvalidOperationException(ChangedSchedule);
        }

        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions")[start..end];
        int check = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.IsTaggedTuple));
        int call = Array.FindIndex(body, instruction => IsOpcode(instruction, BeamEmissionFormat.LocalCall));
        int cons = call + (projectsField ? 3 : 2);
        if (check < 0 || check >= call || cons >= body.Length
            || !IsOpcode(body[call + 1], BeamEmissionFormat.TestHeap)
            || !IsOpcode(body[cons], BeamEmissionFormat.PutList)
            || projectsField && (!IsOpcode(body[call + 2], BeamEmissionFormat.GetTupleElement)
                || Value(body[call + 2], 1) != BigInteger.One))
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
