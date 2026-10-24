using System.Reflection;

namespace Erlang.Tests;

internal static class QueueBeamAdmissionHostNames
{
    internal const string DirectTest = "host_original_queue_beam_direct_roundtrip";
    internal const string PublishedTest = "host_original_queue_beam_selected_backend_roundtrip";
    internal const string Resource = ".Beam.otp_queue373.beam";
    internal const string Sha256 = "759B1FE208F8A0DE292147EB00FFCE98F929263ADCFCD4AABB485E3B3EB4538D";
    internal const string Module = "queue";
    internal const string Lists = "lists";
    internal const string New = "new";
    internal const string In = "in";
    internal const string Out = "out";
    internal const string Value = "value";
    internal const string Empty = "empty";
    internal const string Failure = "Original queue BEAM admission contract failed.";
    internal const string Substitution = "Maintained C# queue backend was invoked unexpectedly.";
    internal const string First = "queue_beam_first";
    internal const string Second = "queue_beam_second";
    internal const string Code = "code";
    internal const string Instructions = "Instructions";
    internal const int NewArity = 0;
    internal const int InArity = 2;
    internal const int OutArity = 1;
    internal const int InputBytes = 27544;
    internal const int InstructionCount = 2068;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}
