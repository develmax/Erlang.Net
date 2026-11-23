using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTupleArityFailureChecks
{
    internal const string TestName = "compiler/beam/shortens-isolated-tuple-arity-failures";
    private const string Fixture = "beam_tuple_arity_relay_probe.erl";
    private const string IncorrectEdge = "A tuple arity failure did not bypass its isolated non-tuple comparison.";
    private const string IncorrectSharedEdge = "A shared tuple fallback was redirected.";
    private const string IncorrectValue = "The tuple cases returned an unexpected value or failure.";

    internal const string Source = """
        -module(beam_tuple_arity_relay_probe).
        -export([list_head/1, shared/2]).

        list_head(Data) ->
            case hd(Data) of
                {_,_,_} -> tuple;
                undefined -> absent
            end.

        shared({A,B},{C,D}) -> {A,B,C,D};
        shared(undefined, _) -> left;
        shared(_, undefined) -> right.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        CheckSharedConsReturns();
        CheckConsShapeRejections();
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] positive = Body("list_head");
        int tuplePair = FindPair(positive);
        int relay = Value(Operands(positive[tuplePair])[0]);
        int badArity = Value(Operands(positive[tuplePair + 1])[0]);
        int relayIndex = Array.FindIndex(positive, instruction =>
            Opcode(instruction) == Label && Value(Operands(instruction)[0]) == relay);
        if (relayIndex < 0 || relayIndex + 1 >= positive.Length
            || Opcode(positive[relayIndex + 1]) != ExactEqualTest
            || relay == badArity || Value(Operands(positive[relayIndex + 1])[0]) != badArity)
        {
            throw new InvalidOperationException(IncorrectEdge);
        }

        object[] shared = Body("shared");
        int sharedPair = FindPair(shared);
        if (shared.Skip(sharedPair).Take(4).Select(Opcode).SequenceEqual(new byte[] { IsTuple, TestArity, IsTuple, TestArity }) is false
            || shared.Skip(sharedPair).Take(4).Select(instruction => Value(Operands(instruction)[0])).Distinct().Count() != 1)
        {
            throw new InvalidOperationException(IncorrectSharedEdge);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term pair = Term.Tuple(Term.I(1), Term.I(2));
            Term undefined = Term.A("undefined");
            Term triple = Term.Tuple(
                Term.I(1),
                Term.I(2),
                Term.I(3)
            );
            if (!Term.A("tuple").Equals(await module.Invoke(context, "list_head", Term.List(triple)))
                || !Term.A("absent").Equals(await module.Invoke(context, "list_head", Term.List(undefined)))
                || !Term.A("left").Equals(await module.Invoke(
                    context,
                    "shared",
                    undefined,
                    pair
                ))
                || !Term.A("right").Equals(await module.Invoke(
                    context,
                    "shared",
                    pair,
                    undefined
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                _ = await module.Invoke(context, "list_head", Term.List(pair));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), pair)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);
        });

        object[] Body(string name)
        {
            int info = Array.FindIndex(instructions, instruction =>
                Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name);

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static void CheckConsShapeRejections()
    {
        Check(
            overwriteArgument: false,
            differentInput: false,
            lineOnFailure: false,
            expectRedirect: true
        );
        Check(
            overwriteArgument: true,
            differentInput: false,
            lineOnFailure: false,
            expectRedirect: false
        );
        Check(
            overwriteArgument: false,
            differentInput: true,
            lineOnFailure: false,
            expectRedirect: false
        );
        Check(
            overwriteArgument: false,
            differentInput: false,
            lineOnFailure: true,
            expectRedirect: false
        );

        static void Check(
            bool overwriteArgument,
            bool differentInput,
            bool lineOnFailure,
            bool expectRedirect
        )
        {
            byte head = X(overwriteArgument ? 0 : 2);
            byte[] line = lineOnFailure ? [Line, U(2)] : [];
            byte[] source =
            [
                .. Start(
                    1,
                    2,
                    2,
                    2
                ),
                IsNonemptyList, F(3), X(0),
                GetList, X(0), head, X(3),
                IsTuple, F(3), head,
                TestArity, F(3), head, U(2),
                Move, head, X(0), Return,
                Label, U(3), .. line,
                IsNil, F(1), X(differentInput ? 1 : 0),
                Move, A(2), X(0), Return, EndCode
            ];
            object header = New(
                "BeamCodeHeader",
                0,
                (int)DebugLine,
                4,
                1,
                new ReadOnlyMemory<byte>(source)
            );
            var references = new List<(int Start, int End, int Label)>();
            Action<int, int, int> recordLabel = (start, end, label) => references.Add((start, end, label));
            object instructions = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                4,
                0,
                recordLabel,
                null
            );
            var initial = references.ToArray();
            System.Type edit = typeof(BeamModuleCompiler).GetNestedType(
                "CodeEdit",
                System.Reflection.BindingFlags.NonPublic
            )!;
            var edits = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(edit))!;
            _ = Call(
                typeof(BeamModuleCompiler),
                "RedirectIsolatedTupleArityFailures",
                instructions,
                references,
                new[] { 0, 1, 2, 3 },
                edits
            );
            if (!expectRedirect && (edits.Count != 0 || !references.SequenceEqual(initial))
                || expectRedirect && (edits.Count != 2
                    || !edits.Cast<object>().Select(change => (int)Property(change, "Start"))
                        .SequenceEqual(initial.Skip(1).Take(2).Select(reference => reference.Start))
                    || edits.Cast<object>().Any(change => (int)Property(change, "Value") != 1)
                    || !references.SequenceEqual(new[] { initial[0], initial[3] })))
            {
                throw new InvalidOperationException(IncorrectEdge);
            }
        }
    }

    private static void CheckSharedConsReturns()
    {
        byte[] start =
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            IsNonemptyList, F(4), X(1), GetList, X(1), X(2), X(3)
        ];
        byte[] tuple = [IsTuple, F(1), X(2), TestArity, F(1), X(2), U(2), GetTupleElement, X(2), U(0), X(4)];
        byte[] hotReturn = [Move, A(2), X(0), Return, Label, U(3)];
        byte[] remaining = [Move, A(3), X(0), Return, Label, U(4), IsNil, F(1), X(1)];
        byte[] coldReturn = [Move, A(2), X(0), Return, EndCode];
        byte[] original = [.. start, .. tuple, IsLess, F(3), X(0), X(4), .. hotReturn, .. remaining, .. coldReturn];
        byte[] expected =
        [
            .. start, .. tuple, IsGreaterOrEqual, F(3), X(0), X(4), .. remaining, Label, U(3), .. coldReturn
        ];
        byte[] shared = tuple.ToArray();
        shared[1] = F(3);
        shared[4] = F(3);
        if (Rewrite(original) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite([.. start, .. shared, IsLess, F(3), X(0), X(4), .. hotReturn, .. remaining, .. coldReturn]) is not null
            || Rewrite([.. start, .. tuple, IsLess, F(3), X(0), X(4), .. hotReturn, .. remaining,
                    Move, A(3), X(0), Return, EndCode]) is not null
            || Rewrite([.. start, .. tuple, IsLess, F(3), X(0), X(4), .. hotReturn, .. remaining,
                    Line, U(2), .. coldReturn]) is not null)
        {
            throw new InvalidOperationException(IncorrectSharedEdge);
        }

        static byte[]? Rewrite(byte[] source)
        {
            ReadOnlyMemory<byte> instructions = source;
            object header = New(
                "BeamCodeHeader",
                0,
                (int)DebugLine,
                5,
                1,
                instructions
            );
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                4,
                0,
                null,
                null
            );

            return (byte[]?)Call(
                typeof(BeamModuleCompiler),
                "RewriteConsGuardNilReturns",
                instructions,
                body
            );
        }
    }

    private static int FindPair(object[] body)
    {
        int index = Array.FindIndex(body, instruction => Opcode(instruction) == IsTuple);
        if (index < 0 || index + 1 >= body.Length || Opcode(body[index + 1]) != TestArity)
        {
            throw new InvalidOperationException(IncorrectEdge);
        }

        return index;
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
