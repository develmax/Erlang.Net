using System.Reflection;

namespace Erlang.Tests;

internal static class ActualOpcodeLoadNifHostNames
{
    internal const string Resource = ".Beam.beam_fixture_load_nif_opcodes.beam";
    internal const string Sha256 = "9842C5D8B849874B7DCA8EABC3C2FC957C6E6195A821D4AFC34194CAC04340F9";
    internal const string NonTail = "non_tail";
    internal const string TailLast = "tail_last";
    internal const string TailOnly = "tail_only";
    internal const string Path = "actual-opcode-load-path";
    internal const string Marker = "retained_opcode_marker";
    internal const string DictionaryKey = "nif_frame_marker";
    internal const string Code = "code";
    internal const string Functions = "Functions";
    internal const string Instructions = "Instructions";
    internal const string FunctionName = "Name";
    internal const string FunctionArity = "Arity";
    internal const string FirstInstruction = "FirstInstructionIndex";
    internal const string EndInstruction = "EndInstructionIndex";
    internal const string Definition = "Definition";
    internal const string DefinitionName = "Name";
    internal const string Operands = "Operands";
    internal const string ModuleInfo = "module_info";
    internal const string Label = "label";
    internal const string Line = "line";
    internal const string Start = "nif_start";
    internal const string MetadataType = "Erlang.BeamGeneratedMetadata";
    internal const string IsGenerated = "IsGenerated";
    internal const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.NonPublic;
    internal const string Execution = "execution";
    internal const string Image = "Image";
    internal const string Failure = "Actual opcode load_nif handler failure";
    internal const int MarkerArity = 3;
    internal const int SuccessCalls = 1;
    internal const int FailureCalls = 2;
    internal const int CancellationCalls = 3;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
