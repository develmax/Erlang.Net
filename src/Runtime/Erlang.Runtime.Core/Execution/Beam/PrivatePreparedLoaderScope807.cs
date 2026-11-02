using System.Runtime.ExceptionServices;

namespace Erlang;

internal static class PrivatePreparedLoaderScope807
{
    internal static async ValueTask CloseAsync(
        PreparedBeamImage? image,
        ResourceReferenceTerm? handle,
        PreparedLoaderOwner807? owner,
        Exception? primary
    )
    {
        Exception? imageError = null, handleError = null, retirementError = null;
        try
        {
            image?.Dispose();
        }
        catch (Exception error)
        {
            imageError = error;
        }
        try
        {
            handle?.PreparedHost807?.Dispose();
        }
        catch (Exception error)
        {
            handleError = error;
        }
        try
        {
            if (owner is not null) await owner.Join807().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            retirementError = error;
        }
        if (imageError is null && handleError is null && retirementError is null) return;
        Exception[] errors = new[] { primary, imageError, handleError, retirementError }.OfType<Exception>().ToArray();
        if (errors.Length == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        throw new AggregateException(errors);
    }
}
