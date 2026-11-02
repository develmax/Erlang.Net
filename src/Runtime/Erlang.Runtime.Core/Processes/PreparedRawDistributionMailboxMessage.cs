namespace Erlang;

// Portable wire metadata plus the actual Value/raw token physical fragment.
// This wrapper is storage metadata, never an invocation or extra guest tuple.
internal sealed record PreparedRawDistributionMailboxMessage(
    PreparedMailboxMessage Message,
    Term PortableRaw
);
