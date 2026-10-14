using System.Collections;
using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class BeamOnLoadModeledBytes
{
    internal static byte[] UnownedY(byte[] original) => Mutation(
        original,
        BeamOnLoadMutationParameters.InitY,
        BeamOnLoadMutationParameters.OpcodeBytes + BeamOnLoadMutationParameters.ExtendedListBytes + BeamOnLoadMutationParameters.ListCountBytes,
        (BeamOnLoadMutationParameters.OutsideTwoSlotFrame << BeamOnLoadMutationParameters.SmallPayloadShift) | BeamOnLoadMutationParameters.YTag
    );

    internal static byte[] MismatchedFrame(byte[] original) => Mutation(
        original,
        BeamOnLoadMutationParameters.Allocate,
        BeamOnLoadMutationParameters.OpcodeBytes,
        (BeamOnLoadMutationParameters.UnknownFrameSlotCount << BeamOnLoadMutationParameters.SmallPayloadShift) | BeamOnLoadMutationParameters.SmallUnsignedTag
    );

    private static byte[] Mutation(
        byte[] original,
        string opcode,
        int operandOffset,
        int encoded
    )
    {
        Assembly assembly = typeof(BeamModule).Assembly;
        Type reader = assembly.GetType(BeamOnLoadMutationParameters.StructureType)!;
        object structure = reader.GetMethod(BeamOnLoadMutationParameters.Read, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [new ReadOnlyMemory<byte>(original), null])!;
        object header = structure.GetType().GetProperty(BeamOnLoadMutationParameters.Code)!.GetValue(structure)!;
        ReadOnlyMemory<byte> instructions = (ReadOnlyMemory<byte>)header.GetType().GetProperty(BeamOnLoadMutationParameters.Instructions)!.GetValue(header)!;
        int origin = original.AsSpan().IndexOf(instructions.Span);
        if (origin < 0 || original.AsSpan(origin + 1).IndexOf(instructions.Span) >= 0)
            throw new InvalidOperationException(BeamOnLoadHostNames.ProofFailure);
        Type decoder = assembly.GetType(BeamOnLoadMutationParameters.InstructionType)!;
        IEnumerable decoded = (IEnumerable)decoder.GetMethod(BeamOnLoadMutationParameters.Read, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [structure])!;
        object selected = decoded.Cast<object>().First(instruction =>
        {
            object definition = instruction.GetType().GetProperty(BeamOnLoadMutationParameters.Definition)!.GetValue(instruction)!;

            return (string)definition.GetType().GetProperty(BeamOnLoadMutationParameters.Name)!.GetValue(definition)! == opcode;
        });
        int offset = (int)selected.GetType().GetProperty(BeamOnLoadMutationParameters.Offset)!.GetValue(selected)!;
        byte[] changed = original.ToArray();
        changed[origin + offset + operandOffset] = (byte)encoded;

        return changed;
    }
}
