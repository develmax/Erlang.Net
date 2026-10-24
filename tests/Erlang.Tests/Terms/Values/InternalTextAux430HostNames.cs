using System.Reflection;
namespace Erlang.Tests;

internal static class InternalTextAux430HostNames
{
    internal const string NativeType = "Erlang.InternalTextAuxiliaryNative430", Capture = "TryCapture", Module = "erts_internal";
    internal const string Text = "term_to_string", Auxiliary = "no_aux_work_threads", Undefined = "undefined", BadArgument = "badarg";
    internal const string WriterType = "Erlang.TermDisplayWriter", Write = "Write", Dispatcher = "actorDispatcher", Prepare = "PrepareAuxiliary420", Publish = "Publish", Completion = "Completion";
    internal const string Runtime = "erlang", SystemFlag = "system_flag", Online = "schedulers_online";
    internal const string TextTest = "HOST/internal-text430-shared-budget-and-unchanged-display";
    internal const string OwnerTest = "HOST/internal-text430-real-native-error-owner-closed";
    internal const string AuxiliaryTest = "HOST/internal-aux430-real-registered-participants-and-jobs";
    internal const string Failure = "Internal text/auxiliary430 source protocol HOST failed.";
    internal const int Participants = 3;
    internal const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
