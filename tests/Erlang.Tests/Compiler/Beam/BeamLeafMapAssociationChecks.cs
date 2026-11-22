using System.Collections;
using System.Numerics;
using System.Reflection;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamLeafMapAssociationChecks
{
    internal const string TestName = "compiler/beam/recomputes-collector-roots-for-frameless-map-construction";
    private const string Fixture = "beam_leaf_map_assoc_probe.erl";
    private const string WrongRoots = "Map construction must preserve an initialized X root prefix.";
    private const string WrongValue = "Map construction changed a key or its value.";
    private const string Source = """
        -module(beam_leaf_map_assoc_probe).
        -export([build/3, keyed/3, update/2]).

        build(Value, Option, Metadata) ->
            Actual = case Option of
                absent -> [];
                _ -> Option
            end,
            #{value => Value, option => Actual, metadata => Metadata}.

        keyed(Key, Value, Mode) ->
            Marker = case Mode of
                first -> one;
                _ -> two
            end,
            #{Key => Value, mode => Marker}.

        update(Input, Value) ->
            Input#{entry => {payload, Value}}.
        """;

    internal static async Task Run()
    {
        if (PlanRoots(registerBase: false, missingPrefix: false) != 1
            || PlanRoots(registerBase: true, missingPrefix: false) is not null
            || PlanRoots(registerBase: false, missingPrefix: true) is not null
            || PlanRoots(
                registerBase: true,
                missingPrefix: false,
                allowRegisterBase: true
            ) != 1
            || PlanRoots(
                registerBase: true,
                missingPrefix: true,
                allowRegisterBase: true
            ) is not null)
        {
            throw new InvalidOperationException(WrongRoots);
        }

        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object[] update = FunctionBody(image, "update");
        if (update.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            || update.SingleOrDefault(instruction => Opcode(instruction) == PutMapAssoc) is not { } registerUpdate
            || (string)Property(Items(registerUpdate, "Operands").ElementAt(1), "Kind") != "x"
            || Value(Items(registerUpdate, "Operands").ElementAt(3)).Sign <= 0)
        {
            throw new InvalidOperationException(WrongRoots);
        }

        object[] body = FunctionBody(image, "build");
        if (body.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            || body.SingleOrDefault(instruction => Opcode(instruction) == PutMapAssoc) is not { } association
            || Value(Items(association, "Operands").ElementAt(3)).Sign <= 0)
        {
            throw new InvalidOperationException(WrongRoots);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.I(20);
            Term metadata = Term.A("metadata_value");
            Term option = Term.A("option_value");
            Term absent = Term.A("absent");
            Term dynamicKey = Term.A("dynamic");
            Term mode = Term.A("mode");
            Term first = Term.A("first");
            Term one = Term.A("one");
            Term two = Term.A("two");
            Term expectedDynamic = new MapTerm([new(dynamicKey, value), new(mode, two)]);
            Term expectedOverwrite = new MapTerm([new(mode, one)]);
            if (!Result(Nil.Value).Equals(await module.Invoke(
                context,
                "build",
                value,
                absent,
                metadata
            ))
                || !Result(option).Equals(await module.Invoke(
                    context,
                    "build",
                    value,
                    option,
                    metadata
                ))
                || !expectedDynamic.Equals(await module.Invoke(
                    context,
                    "keyed",
                    dynamicKey,
                    value,
                    absent
                ))
                || !expectedOverwrite.Equals(await module.Invoke(
                    context,
                    "keyed",
                    mode,
                    value,
                    first
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            var updated = new MapTerm([new(dynamicKey, value), new(Term.A("entry"), Term.Tuple(Term.A("payload"), option))]);
            if (!updated.Equals(await module.Invoke(
                context,
                "update",
                new MapTerm([new(dynamicKey, value)]),
                option
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "update",
                    option,
                    value
                );
            }
            catch (ErlangException ex) when (ex.ExceptionClass == ErlangExceptionClasses.Error
                && ex.Reason.Equals(Term.Tuple(Term.A("badmap"), option)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);

            Term Result(Term field) => new MapTerm([
                new(Term.A("value"), value),
                new(Term.A("option"), field),
                new(Term.A("metadata"), metadata)
            ]);
        });
    }

    private static int? PlanRoots(bool registerBase, bool missingPrefix, bool allowRegisterBase = false)
    {
        const int join = 3;
        byte[] entry = Start(
            1,
            2,
            2,
            1
        );
        byte[] mapBase = registerBase ? [X(0)] : [ExtendedLiteralOperand, U(0)];
        byte[] ordinary =
        [
            Move, X(0), Y(0),
            PutMapAssoc, F(0), .. mapBase, X(0), U(registerBase ? 1 : 0),
            ExtendedListOperand, U(2), A(3), Y(0)
        ];
        byte[] withGap =
        [
            Move, A(2), Y(0), IsAtom, F(join), X(0), Move, A(3), X(1), Label, U(join),
            PutMapAssoc, F(0), .. mapBase, X(3), U(1), ExtendedListOperand,
            U(4), A(3), X(0), A(4), Y(0), Move, X(3), X(0)
        ];
        byte[] bytes =
        [
            .. entry, Allocate, U(1), U(1), .. (missingPrefix ? withGap : ordinary),
            Deallocate, U(1), Return, EndCode
        ];
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            4,
            1,
            new ReadOnlyMemory<byte>(bytes)
        );
        object decoded = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            1,
            null,
            null
        );
        object[] instructions = ((IEnumerable)decoded).Cast<object>().ToArray();
        var planner = typeof(BeamModuleCompiler).GetMethod(
            "PlanLeafFrameRegisters",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        Func<object, bool>? isColdMapErrorTail = allowRegisterBase ? _ => false : null;
        object?[] arguments = [decoded, 2, instructions.Length - 1, 1, false, null, null, false,
            null, null, 0, false, null, null, null, null, isColdMapErrorTail];

        return planner!.Invoke(null, arguments) is int?[] && arguments[6] is Dictionary<int, int> roots
            ? roots[Array.FindIndex(instructions, instruction => Opcode(instruction) == PutMapAssoc)]
            : null;
    }

    private static object[] FunctionBody(byte[] image, string function)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Items(instruction, "Operands").ElementAt(1))] == function
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongRoots);
        }

        return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
