namespace Erlang.Tests;

internal static class BeamOnLoadMutationParameters
{
    internal const string StructureType = "Erlang.BeamStructureReader";
    internal const string InstructionType = "Erlang.BeamInstructionReader";
    internal const string Read = "Read";
    internal const string Definition = "Definition";
    internal const string Name = "Name";
    internal const string Offset = "Offset";
    internal const string Code = "Code";
    internal const string Instructions = "Instructions";
    internal const string InitY = "init_yregs";
    internal const string Allocate = "allocate";
    internal const int OpcodeBytes = 1;
    internal const int ExtendedListBytes = 1;
    internal const int ListCountBytes = 1;
    internal const int SmallPayloadShift = 4;
    internal const int YTag = 4;
    internal const int SmallUnsignedTag = 0;
    internal const int OutsideTwoSlotFrame = 2;
    internal const int UnknownFrameSlotCount = 3;
}
