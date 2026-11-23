using System.Collections;
using System.Numerics;
using System.Reflection;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSingleMapLeafFrameChecks
{
    internal const string TestName = "compiler/beam/keeps-single-key-map-leaf-projections-frameless";
    private const string Fixture = "beam_single_map_leaf_probe.erl";
    private const string WrongFrame = "The checked single-key map path must release its unnecessary frame.";
    private const string WrongResult = "Map matching changed the present, absent, or invalid value.";
    private const string Source = """
        -module(beam_single_map_leaf_probe).
        -export([lookup/3, with_heap/3]).

        lookup(Key, {Map, _}, Default) ->
            case Map of
                #{Key := Value} -> Value;
                #{} -> Default
            end.

        with_heap(Key, {Map, _}, Default) ->
            case Map of
                #{Key := Value} -> {found,Value};
                #{} -> {missing,Default}
            end.
        """;

    internal static async Task Run()
    {
        if (!PlansPhysicalLeaf(multipleKeys: false) || PlansPhysicalLeaf(multipleKeys: true))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        if (!IsFrameless(image, "lookup") || !IsFrameless(image, "with_heap"))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("target");
            Term missing = Term.A("absent");
            Term ignored = Term.A("ignored");
            Term invalid = Term.A("invalid");
            Term present = Term.I(42);
            Term fallback = Term.I(91);
            Term map = new MapTerm([new(key, present)]);
            Term input = Term.Tuple(map, ignored);
            if (!present.Equals(await module.Invoke(
                context,
                "lookup",
                key,
                input,
                fallback
            ))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "lookup",
                    missing,
                    input,
                    fallback
                ))
                || !Term.Tuple(Term.A("found"), present).Equals(await module.Invoke(
                    context,
                    "with_heap",
                    key,
                    input,
                    fallback
                ))
                || !Term.Tuple(Term.A("missing"), fallback).Equals(await module.Invoke(
                    context,
                    "with_heap",
                    missing,
                    input,
                    fallback
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "lookup",
                    key,
                    Term.Tuple(invalid, ignored),
                    fallback
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), invalid)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static bool PlansPhysicalLeaf(bool multipleKeys)
    {
        const int previousValue = 1;
        const int fallback = 3;
        byte[] entry = Start(
            1,
            2,
            2,
            2
        );
        byte[] keys = multipleKeys ? [U(4), A(3), Y(0), A(4), X(0)] : [U(2), A(3), Y(0)];
        byte[] code =
        [
            .. entry,
            Allocate, U(1), U(2), Move, X(previousValue), Y(0),
            IsMap, F(fallback), X(0),
            GetMapElements, F(fallback), X(0), ExtendedListOperand, .. keys,
            Move, Y(0), X(0), Deallocate, U(1), Return,
            Label, U(fallback), Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
        ];
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            4,
            1,
            new ReadOnlyMemory<byte>(code)
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            5,
            0,
            null,
            null
        );
        int end = ((IEnumerable)body).Cast<object>().Count() - 1;
        var plan = typeof(BeamModuleCompiler).GetMethod(
            "PlanLeafFrameRegisters",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        object?[] arguments = [body, 2, end, 2, false, null, null, false, null, null, 0, false, null, null, null, null, null, false];

        return plan!.Invoke(null, arguments) is int?[] registers && registers.Length == 1 && registers[0].HasValue;
    }

    private static bool IsFrameless(byte[] image, string name)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
        );
        if (start < 0)
        {
            return false;
        }

        byte[] body = instructions.Skip(start + 1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Select(Opcode).ToArray();

        return body.Contains(GetMapElements) && !body.Contains(Allocate) && !body.Contains(AllocateHeap);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
