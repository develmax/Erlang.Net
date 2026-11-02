namespace Erlang;

internal static class PidReference775Names
{
    internal const int HeaderWords = 1;
    internal const int DataWords = 3;
    internal const int AllocationWords = HeaderWords + DataWords;
    internal static nuint Header => ((nuint)DataWords << OrdinaryReference562Names.HeaderArityShift) | OrdinaryReference562Names.ReferenceSubtag;
    internal const string Provenance = "Owned PID reference requires this VM's genuine local alias identity.";
}
