namespace Erlang.Tests;

internal static class MailboxCustody593CaseNames
{
    internal const string RecipientFallback = "runtime/mailbox593/recipient-ready-fallback-and-errors";
    internal const string RecipientIsolation = "runtime/mailbox593/drain-does-not-join-other-recipient";
    internal const string LastRecipient = "runtime/mailbox593/final-recipient-settles-actual-roots";
    internal const string SpecificRootFallback = "runtime/mailbox593/specific-ready-root-fallback-retains-failure";
}
