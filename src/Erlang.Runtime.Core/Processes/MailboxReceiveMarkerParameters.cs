namespace Erlang;

internal static class MailboxReceiveMarkerParameters
{
    internal const int Capacity = 8;
    internal const int MaximumPasses = 4;
    internal const string PriorityUnsupported = "Priority messages with a selected receive marker are not supported";
    internal const string DuplicateReferenceUnsupported = "Duplicate live receive-marker reference bindings are not supported";
}
