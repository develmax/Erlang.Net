using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCapturingTupleClosureDecisionChecks
{
    internal const string TestName = "compiler/beam/preserves-the-subject-for-a-captured-closure-fallback";
    private const string Fixture = "beam_capturing_tuple_closure_probe.erl";
    private const string IncorrectInstructions = "The closure factory lost the tuple arity decision or retained a redundant frame.";
    private const string IncorrectValue = "The fallback closure lost the original subject.";

    private const string Source = """
        -module(beam_capturing_tuple_closure_probe).
        -export([run/1]).

        run(Input) -> (factory(Input))().

        factory({1,2,7}) -> fun() -> first end;
        factory({1,2,9}) -> fun() -> second end;
        factory({1,4,5}) -> fun() -> fourth end;
        factory({1,3}) -> fun() -> third end;
        factory(Input) -> fun() -> {absent,Input} end.
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
                && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                && atoms[(int)(BigInteger)Property(atom, "Value")] == "factory"
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        byte[] factory = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Select(Opcode).ToArray();
        if (factory.Count(opcode => opcode == SelectTupleArity) != 1 || factory.Count(opcode => opcode == SelectValue) != 2 || factory.Contains(Allocate))
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term missed = Term.Tuple(Term.I(1), Term.I(2), Term.I(5));
            Term wrongHead = Term.Tuple(Term.I(5), Term.I(2), Term.I(7));
            Term atom = Term.A("unmatched");
            foreach (var (input, expected) in new (Term, Term)[]
            {
                (Term.Tuple(Term.I(1), Term.I(2), Term.I(7)), Term.A("first")),
                (Term.Tuple(Term.I(1), Term.I(2), Term.I(9)), Term.A("second")),
                (Term.Tuple(Term.I(1), Term.I(3)), Term.A("third")),
                (Term.Tuple(Term.I(1), Term.I(4), Term.I(5)), Term.A("fourth")),
                (missed, Term.Tuple(Term.A("absent"), missed)),
                (wrongHead, Term.Tuple(Term.A("absent"), wrongHead)),
                (atom, Term.Tuple(Term.A("absent"), atom)),
            })
            {
                if (!expected.Equals(await module.Invoke(context, "run", input)))
                {
                    throw new InvalidOperationException(IncorrectValue);
                }
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
