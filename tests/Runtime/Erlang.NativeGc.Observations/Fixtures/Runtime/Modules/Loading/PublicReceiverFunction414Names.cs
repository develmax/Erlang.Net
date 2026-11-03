using System.Reflection;
namespace Erlang.Tests;

internal static class PublicReceiverFunction414Names
{
    internal const string Module = "public_receiver414";
    internal const string Source = "-module(public_receiver414). -export([make/1,echo/1]). make(Value)->fun()->Value end. echo(Value)->Value.";
    internal const string MissingModule = "missing_receiver414";
    internal const string MissingSource = "-module(missing_receiver414). -export([echo/1]). echo(Value)->Value.";
    internal const string Make = "make";
    internal const string Echo = "echo";
    internal const string ErlangModule = "erlang";
    internal const string FunInfo = "fun_info";
    internal const string PidInfo = "pid";
    internal const string MapKey = "public_receiver414_map";
    internal const string RecordName = "public_receiver414_record";
    internal const string RecordField = "value";
    internal const string BeamModule = "beam_fixture_closure";
    internal const string BeamResource = "Erlang.Tests.PublicReceiver414.beam_fixture_closure.beam";
    internal const string BeamHash = "99A54BA32C5BD185F23550303E7036F451D52609376D8A738CF19EA3BBDD76F9";
    internal const int BeamArgument = 7;
    internal const int BeamResult = Value + BeamArgument;
    internal const string StoredIdentity = "StoredLocalIdentity";
    internal const string OwnedFunction = "OwnedFunctionStorage";
    internal const string RuntimeIdentity = "RuntimeLocalTransferIdentity414";
    internal const string Heap = "TermHeap506";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Export = "Export";
    internal const string RefusedClosure = "A local function has no admitted source or BEAM reconstruction descriptor.";
    internal const string ForeignRoot = "An owned guest value crossed heaps without the explicit copy boundary.";
    internal const string Failure = "Public receiver-only function reconstruction414 failed.";
    internal const int Value = 42;
    internal const int SentinelValue = 913;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
