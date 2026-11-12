namespace Erlang;

internal static class GenServerLogFormats
{
    internal const string DBGTpGotCallTpFromTwN = "*DBG* ~tp got call ~tp from ~tw~n";
    internal const string DBGTpGotCastTpN = "*DBG* ~tp got cast ~tp~n";
    internal const string DBGTpGotTpN = "*DBG* ~tp got ~tp~n";
    internal const string DBGTpSentTpToTwNewStateTpN = "*DBG* ~tp sent ~tp to ~tw, new state ~tp~n";
    internal const string DBGTpNewStateTpN = "*DBG* ~tp new state ~tp~n";
    internal const string DBGTpDbgTpN = "*DBG* ~tp dbg  ~tp~n";
    internal const string Directive = "~";
    internal const string SingleLineWidth = "0";
    internal const string Empty = "";
    internal const string UnicodeModifier = "t";
    internal const string UnlimitedTerm = "p";
    internal const string LimitedTerm = "P";
    internal const string UndefinedHandleInfoIn = "Undefined handle_info in ";
    internal const string UnhandledMessage = ". Unhandled message: ";
    internal const string UndefinedHandleInfoInPNUnhandledMessage = "** Undefined handle_info in ~p~n** Unhandled message: ";
    internal const string N = "~n";
    internal const string GenericServer = "Generic server ";
    internal const string Label = ". Label: ";
    internal const string Reason = ". Reason: ";
    internal const string LastMessage = ". Last message: ";
    internal const string State = ". State: ";
    internal const string Log = " Log: ";
    internal const string GenericServerText = "** Generic server ";
    internal const string ProcessLabel = "** Process label == ";
    internal const string LastMessageInWas = "** Last message in was ";
    internal const string LogN = "** Log ==~n** [";
    internal const string EntrySeparator = ",~n    ";
    internal const string LogEnd = "]~n";
    internal const string Client0pIsDead = " Client ~0p is dead.";
    internal const string ClientPIsDeadN = "** Client ~p is dead~n";
    internal const string Client0pIsRemoteOnNode0p = " Client ~0p is remote on node ~0p.";
    internal const string ClientPIsRemoteOnNodePN = "** Client ~p is remote on node ~p~n";
    internal const string Client = " Client ";
    internal const string ClientText = "** Client ";
    internal const string Stacktrace = " stacktrace: ";
    internal const string StacktraceN = " stacktrace~n** ";
    internal const string Text = ".";
    internal const string Terminating = " terminating";
    internal const string TerminatingText = " terminating \n";
    internal const string NWhenServerState = "~n** When Server state == ";
    internal const string NReasonForTerminationN = "~n** Reason for termination ==~n** ";
}
