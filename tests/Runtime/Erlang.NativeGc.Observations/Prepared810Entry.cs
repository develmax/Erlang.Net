using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;
using Erlang.Tests;

internal static class Prepared810Entry
{
    internal static Task Builder()
    {
        try
        {
            return (Task)typeof(PreparedLoader807Checks).GetMethod("BuilderCloseWhileMRetained", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
        }
    }

    internal static Task Admission()
        => BeamLoaderAdmissionRaceChecks.Run(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.I(807))));
}
