namespace Erlang.Tests;

internal static class BeamReceiveHostNames
{
    public const int RunArity = 0;
    public const int RetainedMessageCount = 1;
    public const string Test = "BEAM receive actual mailbox, native value and controlled lifecycle";
    public const string Resource = "beam_fixture_receive.beam";
    public const string NativeExpected = "beam_fixture_receive.expected.etf";
    public const string Run = "run";
    public const string Result = "ok";
    public const string Match = "ping";
    public const string Unrelated = "beam_receive_unrelated";
    public const string Waiting = "waiting";
    public const string Status = "status";
    public const string Info = "process_info";
    public const string Erlang = "erlang";
    public const string Suspend = "suspend_process";
    public const string Resume = "resume_process";
    public const string Kill = "kill";
    public const string Killed = "killed";
    public const string Failed = "Actual BEAM receive mailbox contract failed";
    public const string MissingCancellation = "Stopped BEAM receive owner completed successfully";
    public const string CodeChunk = "Code";
    public const string CodeField = "code";
    public const string InstructionProperty = "Instructions";
    public const string DefinitionProperty = "Definition";
    public const string NameProperty = "Name";
    public const string OffsetProperty = "Offset";
    public const string SendInstruction = "send";
    public const string ResourceSeparator = ".";
    public const int FormHeaderBytes = 8;
    public const int FormTypeBytes = 4;
    public const int ChunkIdentifierBytes = 4;
    public const int FormSizeOffset = 4;
    public const int ChunkSizeOffset = 4;
    public const int ChunkHeaderBytes = 8;
    public const int SignedFieldBytes = 4;
    public const int ChunkPaddingMask = 3;
    public const byte MoveOpcode = 64;
    public const byte EncodedX0 = 3;
}
