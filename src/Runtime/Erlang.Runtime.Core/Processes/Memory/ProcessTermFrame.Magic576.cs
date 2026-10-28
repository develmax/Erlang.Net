using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    private void ReleaseRootsAndMagic576()
    {
        Exception? failure = null;
        try
        {
            binaryRoots512?.Dispose();
        }
        catch (Exception error)
        {
            failure = error;
        }
        try
        {
            constructionCustody514?.RollbackResourceBits577();
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        try
        {
            constructionCustody514?.RollbackMagic576();
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        try
        {
            owner.Release(this);
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
