using System.Reflection;

namespace Erlang.Tests;

internal static class MailboxCompletion427Names
{
    internal const string Test = "host/mailbox427-published-halt-before-completed-control-return";
    internal const string Wait = "WaitForControlCompletion";
    internal const string Halt = "HaltRequested";
    internal const string Signals = "ProcessSignals";
    internal const string Failure = "A published halt must cancel guest control completion independently of physical completion";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
}
