namespace Erlang;

internal interface IMailboxRootInterest593 : IDisposable
{
    // Null means another interest remains; do not wait for that recipient.
    // Non-null is the actual last-interest typed-root release work, not a
    // whole VM/native-domain/destructor-completion token.
    MailboxPayloadLease593? ReleaseLast();
}
