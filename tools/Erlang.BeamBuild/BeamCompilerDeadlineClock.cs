namespace Erlang.BeamBuild;

internal static class BeamCompilerDeadlineClock
{
    private static readonly AsyncLocal<ClockScope?> selected = new();

    internal static TimeProvider Current => selected.Value?.Provider ?? TimeProvider.System;

    internal static IDisposable Enter(TimeProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var scope = new ClockScope(provider, selected.Value);
        selected.Value = scope;

        return scope;
    }

    private sealed class ClockScope(TimeProvider provider, ClockScope? previous) : IDisposable
    {
        internal TimeProvider Provider { get; } = provider;
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;
            if (!ReferenceEquals(selected.Value, this))
                throw new InvalidOperationException(BeamCompilerDeadlineClockNames.InvalidScope);
            selected.Value = previous;
            disposed = true;
        }
    }
}
