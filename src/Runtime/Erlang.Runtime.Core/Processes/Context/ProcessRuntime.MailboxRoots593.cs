namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly RuntimeMailboxRootOwner593 mailboxRoots593 = new();
    internal RuntimeMailboxRootOwner593 MailboxRoots593 => mailboxRoots593;

    private void StopMailboxRootAdmission593() => mailboxRoots593.StopAdmission();

    private Task DrainMailboxRoots593() => mailboxRoots593.DrainAsync();
}
