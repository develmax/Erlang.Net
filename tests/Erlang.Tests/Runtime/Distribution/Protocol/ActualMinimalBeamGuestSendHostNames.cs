using System.Reflection;

namespace Erlang.Tests;

internal static class ActualMinimalBeamGuestSendHostNames
{
    internal const string Test = "host_actual_original_minimal_beam_connected_bang";
    internal const string BeamResource = ".Beam.guest_connected_bang322.beam";
    internal const string ProvenanceResource = ".Beam.guest_connected_bang322.provenance.json";
    internal const string BeamSha256 = "AAADAC2F0BE0422FC84C4B79F398DD6EDD5B0D35778D925A0A4BC2D0BF2FE7E5";
    internal const string ProvenanceSha256 = "3718A2F53E9BF8B19110BD33AD00FC5D487AB285C9DAEE87EE8494FC21FAB631";
    internal const string Module = "guest_connected_bang322";
    internal const string Bang = "bang";
    internal const string GroupLeader = "group_leader";
    internal const string ProcessFlag = "process_flag";
    internal const string AsyncDistribution = "async_dist";
    internal const string Payload = "actual_beam_guest_send_payload";
    internal const string ChangedPayload = "caller_array_changed";
    internal const string Failure = "Actual original BEAM guest send contract failed.";
    internal const string Code = "code";
    internal const string Execution = "execution";
    internal const string Image = "Image";
    internal const string Functions = "Functions";
    internal const string Instructions = "Instructions";
    internal const string Name = "Name";
    internal const string Arity = "Arity";
    internal const string FirstInstruction = "FirstInstructionIndex";
    internal const string EndInstruction = "EndInstructionIndex";
    internal const string Definition = "Definition";
    internal const string SendInstruction = "send";
    internal const string CodeGate = "codeGate";
    internal const string ActiveCode = "activeCode";
    internal const string TestsSha = "TestsSha256";
    internal const string RuntimeSha = "RuntimeSha256";
    internal const string TestsMvid = "TestsMvid";
    internal const string RuntimeMvid = "RuntimeMvid";
    internal const string CapturedBeamSha = "BeamSha256";
    internal const string CapturedProvenanceSha = "ProvenanceSha256";
    internal const int BangArity = 2;
    internal const int ExpectedSendInstructions = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
