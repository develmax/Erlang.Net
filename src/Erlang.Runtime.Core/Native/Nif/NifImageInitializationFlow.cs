namespace Erlang;

// Diagnostic reentry marker only. This never grants code-modification authority.
internal static class NifImageInitializationFlow
{
    private static readonly AsyncLocal<Frame?> current = new();

    internal static bool IsInside(NifImageLoadReservation reservation)
    {
        for (var frame = current.Value; frame is not null; frame = frame.Parent)
            if (ReferenceEquals(frame.Reservation, reservation) && reservation.DriverIsRunning)
                return true;

        return false;
    }

    internal static bool HoldsPermission(ModuleCodeModificationPermission permission)
    {
        for (var frame = current.Value; frame is not null; frame = frame.Parent)
            if (frame.Reservation.HoldsPermission(permission))
                return true;

        return false;
    }

    internal static IDisposable Enter(NifImageLoadReservation reservation)
    {
        var previous = current.Value;
        current.Value = new(reservation, previous);

        return new Scope(previous);
    }

    internal static Func<Task> IsolateActor(Func<Task> body) => current.Value is null ? body : WrapActor(body);

    private static Func<Task> WrapActor(Func<Task> body) => () => RunActor(body);

    private static async Task RunActor(Func<Task> body)
    {
        var previous = current.Value;
        current.Value = null;
        try
        {
            await body().ConfigureAwait(false);
        }
        finally
        {
            current.Value = previous;
        }
    }

    private sealed record Frame(NifImageLoadReservation Reservation, Frame? Parent);

    private sealed class Scope(Frame? previous) : IDisposable
    {
        public void Dispose() => current.Value = previous;
    }
}
