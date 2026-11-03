using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal sealed class ManagedUnsupportedDiagnostics520 : IDisposable
{
    private readonly object gate = new();
    private readonly string path;
    private int count;
    private bool disposed;

    internal ManagedUnsupportedDiagnostics520(string directory)
    {
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, ManagedContractBatch513Names.UnsupportedDiagnosticFile);
        AppDomain.CurrentDomain.FirstChanceException += Observe;
    }

    private void Observe(object? sender, FirstChanceExceptionEventArgs observation)
    {
        if (observation.Exception is not NotSupportedException)
            return;
        lock (gate)
        {
            if (disposed || count >= ManagedContractBatch513Names.MaximumUnsupportedDiagnostics)
                return;
            count++;
            try
            {
                File.AppendAllLines(path, [observation.Exception.ToString()]);
            }
            catch (Exception diagnostic)
            {
                Console.Error.WriteLine(diagnostic);
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            AppDomain.CurrentDomain.FirstChanceException -= Observe;
        }
    }
}
