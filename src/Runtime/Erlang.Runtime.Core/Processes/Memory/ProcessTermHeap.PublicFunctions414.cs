namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal PreparedTermTransfer506 PreparePublicFunctions414(ProcessTermFrame506 caller, Term value, PublicReceiverFunction414 receiver)
    {
        ProcessTermBorrow506 borrow = Borrow(caller, value);
        try
        {
            Term portable = OwnedTermDetachedCopy506.CopyPublicFunctions414(runtime, borrow.Value, receiver);

            return new(borrow, portable);
        }
        catch (Exception original)
        {
            try
            {
                borrow.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }
}
