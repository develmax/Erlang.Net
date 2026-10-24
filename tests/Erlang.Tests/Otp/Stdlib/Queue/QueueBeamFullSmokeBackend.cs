using Erlang;
using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class QueueBeamFullSmokeBackend
{
    internal static PreparedBeamImage? Select(ProcessRuntime runtime, bool beamBackend, bool originalLists = false)
    {
        if (originalLists)
            ListBeamHostChecks.Select(runtime);
        if (!beamBackend)
        {
            foreach (var export in QueueBeamFullSmokeCases.Exports)
                Require(runtime.Modules.SourceOwner(QueueModuleNames.Module, export.Function, export.Arity) is not PreparedBeamImage);

            return null;
        }
        Assembly assembly = typeof(QueueBeamFullSmokeBackend).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(QueueBeamAdmissionHostNames.Resource, StringComparison.Ordinal));
        using Stream source = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        source.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == QueueBeamAdmissionHostNames.Sha256);
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection([QueueBeamAdmissionHostNames.Lists]));
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        Require(prepared.Name == QueueModuleNames.Module && prepared.IsConsumed);
        foreach (var export in QueueBeamFullSmokeCases.Exports)
            Require(ReferenceEquals(runtime.Modules.SourceOwner(QueueModuleNames.Module, export.Function, export.Arity), prepared));

        return prepared;
    }

    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(QueueBeamFullSmokeNames.Failure);
    }
}
