namespace Erlang;

internal static class BeamLinkNames
{
    internal const string Label = "label";
    internal const string Line = "line";
    internal const string FunctionInfo = "func_info";
    internal const string CodeEnd = "int_code_end";
    internal const string Move = "move";
    internal const string Return = "return";
    internal const string LocalCall = "call_only";
    internal const string ExternalCall = "call_ext";
    internal const string ExternalTailCall = "call_ext_only";
    internal const string GuardBif = "gc_bif2";
    internal const string Bif = "bif0";
    internal const string IntegerTest = "is_integer";
    internal const string EqualityTest = "is_eq_exact";
    internal const string HeapTest = "test_heap";
    internal const string PutList = "put_list";
    internal const string PutTuple = "put_tuple2";
    internal const string Allocate = "allocate";
    internal const string Deallocate = "deallocate";
    internal const string Send = "send";
    internal const string LoopReceive = "loop_rec";
    internal const string LoopReceiveEnd = "loop_rec_end";
    internal const string RemoveMessage = "remove_message";
    internal const string WaitTimeout = "wait_timeout";
    internal const string Timeout = "timeout";
    internal const string OnLoad = "on_load";
}
