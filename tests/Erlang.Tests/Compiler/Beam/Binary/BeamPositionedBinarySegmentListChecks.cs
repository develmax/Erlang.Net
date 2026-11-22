using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPositionedBinarySegmentListChecks
{
    internal const string TestName = "compiler/beam/reuses-position-for-fixed-binary-segment-list";
    private const string WrongSchedule = "The binary segment scanner lost its shared position or root prefix.";
    private const string WrongResult = "The binary segment scanner changed its decoded order or restored failure.";
    private const string Source = """
        -module(beam_positioned_binary_segment_list).
        -export([read16/1, read24/1, raw/2]).

        read16(Binary) -> decode16(Binary, []).
        read24(Binary) -> decode24(Binary, []).

        decode16(<<>>, Acc) -> lists:reverse(Acc);
        decode16(<<Value:16, Rest/binary>>, Acc) -> decode16(Rest, [<<Value:16>> | Acc]).

        decode24(<<>>, Acc) -> lists:reverse(Acc);
        decode24(<<Value:24, Rest/binary>>, Acc) -> decode24(Rest, [<<Value:16>> | Acc]).

        raw(<<>>, Acc) -> lists:reverse(Acc);
        raw(<<Value:16, Rest/binary>>, Acc) -> raw(Rest, [<<Value:16>> | Acc]).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_positioned_binary_segment_list.erl");
        CheckLayout(image);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([ListModuleNames.Module]));
        var selection = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed(ErlangModuleNames.Module), RuntimeModuleSelection.Managed(ListModuleNames.Module)]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(
            selection,
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var first = new BitString([1, 2]);
            var second = new BitString([3, 4]);
            var expected = Term.List(first, second);
            if (!expected.Equals(await module.Invoke(context, "read16", new BitString([1, 2, 3, 4])))
                || !expected.Equals(await module.Invoke(context, "read24", new BitString([255, 1, 2, 254, 3, 4])))
                || !Nil.Value.Equals(await module.Invoke(context, "read16", new BitString([]))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            var nonbinary = Term.A("nonbinary");
            var unaligned = new BitString([1, 2, 128], 17);
            await Failure(
                new BitString([1, 2, 255]),
                new BitString([255]),
                Term.List(first)
            );
            await Failure(unaligned, unaligned, Nil.Value);
            await Failure(nonbinary, nonbinary, Nil.Value);

            return Nil.Value;

            async Task Failure(Term source, Term suffix, Term accumulator)
            {
                try
                {
                    _ = await module.Invoke(context, "read16", source);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause))
                    && error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom { Name: "decode16" }, var arguments, _] } }
                    && Cons.Items(arguments).SequenceEqual(new[] { suffix, accumulator }))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("decode16");
        Check("decode24");
        if (Body("raw").Any(IsFused))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        void Check(string function)
        {
            object[] body = Body(function);
            byte[] operations = body.Select(Opcode).ToArray();
            if (operations.Count(operation => operation == StartBinaryMatch) != 1
                || operations.Count(operation => operation == GetBinaryPosition) != 1
                || operations.Count(operation => operation == SetBinaryPosition) != 1
                || operations.Count(operation => operation == GetBinaryTail) != 1
                || operations.Any(operation => operation is Allocate or AllocateHeap or Deallocate)
                || !body.Any(IsFused))
            {
                throw new InvalidOperationException(WrongSchedule);
            }
        }

        object[] Body(string function)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Operand(instruction, 1)] == function);
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }

        static bool IsFused(object instruction) => Opcode(instruction) == CreateBinary
            && Operand(instruction, 1) == ConsHeapWords && Operand(instruction, 2) == 3;
    }

    private static int Operand(object instruction, int index) =>
        (int)(BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
