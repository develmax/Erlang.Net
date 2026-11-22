using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamStagedListConstructionChecks
{
    internal const string TestName = "compiler/beam/folds-staged-literals-into-independent-cons-arguments";
    private const string Fixture = "beam_staged_list_construction_probe.erl";
    private const string RedundantStaging = "The literal cons still uses a temporary before its independent sibling construction.";
    private const string IncorrectValue = "Folding a staged cons changed a forwarded call argument or result.";

    internal const string Source = """
        -module(beam_staged_list_construction_probe).
        -export([saved/7, discarded/7, blocked/7]).
        -compile({inline, {prepend_nil, 1}}).
        -compile({inline, {prepend_false, 1}}).

        saved(S,word,Ss,Stack,T,Ts,Z) ->
            finish(S,word,Ss,Stack,T,Ts,Z);
        saved(S,Cat,Ss,Stack,T,Ts,Z) ->
            Values = prepend_nil(Stack),
            finish(S,Cat,[85|Ss],Values,T,Ts,Z).

        discarded(_S,Cat,Ss,Stack,T,Ts,Z) ->
            Values = prepend_false(Stack),
            finish(78,Cat,[72|Ss],Values,T,Ts,Z).

        blocked(S,Cat,Ss,Stack,T,Ts,Z) ->
            Values = prepend_nil(Stack),
            finish(S,Cat,[Stack|Ss],Values,T,Ts,Z).

        prepend_nil(Stack) -> [begin [] end | Stack].
        prepend_false(Stack) -> [begin false end | Stack].
        finish(S,Cat,Ss,Stack,_T,_Ts,_Z) -> {S,Cat,Ss,Stack}.
        """;

    private static void CheckSingletonTailScratch()
    {
        if (Rewrite(Build()) is not { } result || !result.AsSpan().SequenceEqual(Build(rewritten: true))
            || Rewrite(Build(keepsScratch: true)) is not null
            || Rewrite(Build(collects: true)) is not null)
        {
            throw new InvalidOperationException(RedundantStaging);
        }

        static byte[] Build(bool rewritten = false, bool keepsScratch = false, bool collects = false)
        {
            byte[] save = rewritten ? [] : [Move, X(0), X(1)];
            byte[] collector = collects ? [TestHeap, U(4), U(2)] : [];

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    1
                ),
                Allocate, U(0), U(1), TestHeap, U(6), U(1),
                PutList, X(0), A(0), X(0),
                .. save, .. collector,
                PutList, rewritten ? X(0) : X(1), A(2), X(0),
                PutList, A(2), X(0), X(0),
                TailExternalCallWithFrame, U(keepsScratch ? 2 : 1), U(0), U(0), EndCode
            ];
        }

        static byte[]? Rewrite(byte[] bytes)
        {
            ReadOnlyMemory<byte> code = bytes;
            object header = New(
                "BeamCodeHeader",
                0,
                (int)PutList,
                3,
                1,
                code
            );
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                3,
                0,
                null,
                null
            );

            return (byte[]?)Call(
                typeof(BeamModuleCompiler),
                "RewriteUnaryTailListScratch",
                code,
                body
            );
        }
    }

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        CheckSingletonTailScratch();
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!DirectConsPair("saved", "nil", 85)
            || !DirectConsPair("discarded", "atom", 72))
        {
            throw new InvalidOperationException(RedundantStaging);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term current = Term.A("state");
            Term category = Term.A("category");
            Term states = Term.List(Term.A("old_state"));
            Term values = Term.List(Term.A("old_value"));
            Term[] args = [current, category, states, values, Nil.Value, Nil.Value, Nil.Value];
            Term saved = Term.Tuple(
                current,
                category,
                new Cons(Term.I(85), states),
                new Cons(Nil.Value, values)
            );
            Term discarded = Term.Tuple(
                Term.I(78),
                category,
                new Cons(Term.I(72), states),
                new Cons(Term.A("false"), values)
            );
            Term blocked = Term.Tuple(
                current,
                category,
                new Cons(values, states),
                new Cons(Nil.Value, values)
            );
            if (!saved.Equals(await module.Invoke(context, "saved", args))
                || !discarded.Equals(await module.Invoke(context, "discarded", args))
                || !blocked.Equals(await module.Invoke(context, "blocked", args)))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });

        bool DirectConsPair(string name, string literalKind, int state)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
            );
            object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int first = Array.FindIndex(body, instruction => Opcode(instruction) == PutList);

            return first >= 0 && first + 1 < body.Length && Opcode(body[first + 1]) == PutList
                && Operands(body[first]) is [var literal, var previousValues, var valueResult]
                && (string)Property(literal, "Kind") == literalKind && Register(previousValues, 3) && Register(valueResult, 3)
                && Operands(body[first + 1]) is [var stateLiteral, var previousStates, var stateResult]
                && (string)Property(stateLiteral, "Kind") == "integer" && Value(stateLiteral) == state
                && Register(previousStates, 2) && Register(stateResult, 2);
        }
    }

    private static bool Register(object operand, int number) =>
        (string)Property(operand, "Kind") == "x" && Value(operand) == number;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
