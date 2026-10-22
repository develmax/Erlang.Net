using System.Text;

namespace Erlang;

internal sealed class DdllVisibility438Events : IDisposable
{
    internal EventWaitHandle Attempt { get; }
    internal EventWaitHandle Entered { get; }
    internal EventWaitHandle Release { get; }
    internal EventWaitHandle Completed { get; }
    private readonly string attempt;
    private readonly string entered;
    private readonly string release;

    internal DdllVisibility438Events(RuntimeOsEnvironment environment, string driver)
    {
        string stem = DdllVisibility438Names.EventPrefix + Guid.NewGuid().ToString("N");
        attempt = stem + ".attempt";
        entered = stem + ".entered";
        release = stem + ".release";
        Attempt = Create(attempt);
        try { Entered = Create(entered); }
        catch { Attempt.Dispose(); throw; }
        try { Release = Create(release); }
        catch { Entered.Dispose(); Attempt.Dispose(); throw; }
        try { Completed = Create(stem + ".completed"); }
        catch { Release.Dispose(); Entered.Dispose(); Attempt.Dispose(); throw; }
        try
        {
            _ = environment.Unset(Term.String(DdllVisibility438Names.RemoveKey + driver), CancellationToken.None);
            Put(environment, DdllVisibility438Names.PhaseKey + driver, "linked");
            Put(environment, DdllVisibility438Names.AttemptKey + driver, attempt);
            Put(environment, DdllVisibility438Names.EnterKey + driver, entered);
            Put(environment, DdllVisibility438Names.ReleaseKey + driver, release);
            Put(environment, DdllVisibility438Names.CompletedKey + driver, stem + ".completed");
        }
        catch { Dispose(); throw; }
    }

    private static EventWaitHandle Create(string name)
    {
        var result = new EventWaitHandle(false, EventResetMode.ManualReset, name, out bool created);
        if (created) return result;
        result.Dispose();
        throw new InvalidDataException(name);
    }

    internal static void Put(RuntimeOsEnvironment environment, string key, string value)
        => DriverParentHarnessAccess423.Require(environment.WriteDriver422(
            Encoding.ASCII.GetBytes(key), Encoding.ASCII.GetBytes(value)) == 0);

    internal void WaitEntered()
        => DriverParentHarnessAccess423.Require(Entered.WaitOne(DdllVisibility438Names.Deadline));

    public void Dispose()
    {
        Release.Set();
        Release.Dispose();
        Completed.Dispose();
        Entered.Dispose();
        Attempt.Dispose();
    }
}
