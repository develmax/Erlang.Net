namespace Erlang;

internal static class SupervisorLogFormats
{
    internal const string ProgressTitle = "PROGRESS REPORT";
    internal const string SupervisorTitle = "SUPERVISOR REPORT";
    internal const string UnexpectedMessage = "Supervisor received unexpected message: ~tp~n";
    internal const string Directive = "~";
    internal const string SingleLineWidth = "0";
    internal const string Empty = "";
    internal const string Indent = "    ";
    internal const string LabelSeparator = ": ";
    internal const string Newline = "~n";
    internal const string Supervisor = "Supervisor: ";
    internal const string Period = ".";
    internal const string Context = ". Context: ";
    internal const string Reason = ". Reason: ";
    internal const string ChildPid = " ~s id=~w,pid=~w.";
    internal const string ChildCount = " ~s id=~w,nb_children=~w.";
    internal const string Started = "Started:";
    internal const string Offender = "Offender:";
}
