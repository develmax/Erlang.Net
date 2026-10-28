namespace Erlang;

// Implemented by the compiler's private sealed receive matcher, never by a
// public/opaque Func supplied through ReceiveSelectingAsync.
internal interface ISourceMailboxSelector593<T> where T : class
{
    ProcessContext Context { get; }

    ValueTask<T?> Select(Term candidate);
}
