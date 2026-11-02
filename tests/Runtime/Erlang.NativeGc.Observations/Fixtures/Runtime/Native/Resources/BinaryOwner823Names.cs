namespace Erlang.Tests;

internal static class BinaryOwner823Names
{
    internal const string Mode = "binaryOwner823";
    internal const string BinaryName = "binary";
    internal const string Query = "QueryProcessInfo";
    internal const string Capture = "CaptureBinaryInfo823";
    internal const string ItemType = "Erlang.ProcessInfoItem";
    internal const string Pending = "PendingInfoRequests";
    internal const string Count = "Count";
    internal const string Admit = "AdmitSignals";
    internal const string Heap = "TermHeap506";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Clear = "Clear";
    internal const string Collect = "Collect";
    internal const string Gate = "gate";
    internal const string Stop = "Stop";
    internal const string Terminate = "Terminate";
    internal const string SendPending = "SendPending";
    internal const int TerminateParameterCount = 4;
    internal const string NormalReason = "normal";
    internal const string UndefinedResult = "undefined";
    internal static readonly Atom NormalAtom = Term.A(NormalReason);
    internal static readonly Atom UndefinedAtom = Term.A(UndefinedResult);
    internal const int BinaryBytes = 131;
    internal static readonly Atom BinaryAtom = Term.A(BinaryName);
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);
}
