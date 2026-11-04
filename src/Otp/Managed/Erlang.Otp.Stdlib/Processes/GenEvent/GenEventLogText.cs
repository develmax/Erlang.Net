namespace Erlang;

internal static class GenEventLogText
{
    internal const string StatusHeader = "Status for event handler";
    internal const string Status = "Status";
    internal const string LoggedEvents = "Logged Events";
    internal const string Parent = "Parent";
    internal const string InstalledHandlers = "Installed handlers";
    internal const string DebugEvent = "*DBG* ~tp got event ~tp~n";
    internal const string DebugCall = "*DBG* ~tp(~tp) got call ~tp~n";
    internal const string DebugMessage = "*DBG* ~tp got ~tp~n";
    internal const string DebugOther = "*DBG* ~tp : ~tp~n";

    internal static string TermDirective(bool single, bool latin, bool bounded)
        => "~" + (single ? "0" : "") + (latin ? "" : "t") + (bounded ? "P" : "p");

    internal static string Termination(string term, bool single, bool label)
        => single
            ? "Generic event handler " + term + " crashed. Installed: " + term
                + (label ? ". Label: " + term : "") + ". Last event: " + term + ". State: " + term + ". Reason: " + term + "."
            : "** gen_event handler " + term + " crashed.\n** Was installed in " + term + "\n"
                + (label ? "** Process label == " + term + "\n" : "")
                + "** Last event was: " + term + "\n** When handler state == " + term + "\n** Reason == " + term + "\n";

    internal static string MissingInfo(string term, bool single)
        => single ? "Undefined handle_info in " + term + ". Unhandled message: " + term + "."
            : "** Undefined handle_info in ~p\n** Unhandled message: " + term + "\n";
}
