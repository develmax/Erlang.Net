namespace Erlang;

// Internal compiler/BEAM selection contract, never inferred from a public
// callback name/type. On success receiver takes this exact actual heap frame.
internal interface IGuestMailboxSelection593 : IDisposable
{
    void AttachMailboxCandidate593(ProcessTermFrame506 candidate);
}
