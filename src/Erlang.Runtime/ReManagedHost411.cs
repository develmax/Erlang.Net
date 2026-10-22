using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Erlang;

internal static class ReManagedHost411
{
    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);

        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static bool IsManagedCli(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new PEReader(stream);
        var cor = reader.PEHeaders.CorHeader;
        if (!reader.HasMetadata || cor is null) return false;
        var metadata = reader.GetMetadataReader();
        if (!metadata.IsAssembly || metadata.GetString(metadata.GetAssemblyDefinition().Name).Length == 0) return false;
        if ((cor.Flags & CorFlags.ILOnly) != 0) return true;
        var directory = cor.ManagedNativeHeaderDirectory;
        if (directory.RelativeVirtualAddress == 0 || directory.Size < 16) return false;
        var header = reader.GetSectionData(directory.RelativeVirtualAddress).GetReader(0, directory.Size);
        if (header.ReadUInt32() != 0x00525452) return false;
        if (header.ReadUInt16() == 0) return false;
        header.ReadUInt16();
        header.ReadUInt32();
        var sections = header.ReadUInt32();

        return sections > 0 && sections <= (directory.Size - 16) / 12;
    }

    internal static Dictionary<string, string> Capture()
    {
        if (AppContext.GetData(ReProtocolNames411.TrustedPlatformAssemblies) is not string trusted)
            throw new InvalidOperationException(ReDiagnosticNames411.ManagedHostTpaUnavailable);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            AddActualPath(result, entry);
        }
        if (result.Count == 0) throw new InvalidOperationException(ReDiagnosticNames411.NoManagedCliTpaBaseline);

        return result;
    }

    private static void AddActualPath(Dictionary<string, string> baseline, string entry)
    {
        var path = Path.GetFullPath(entry);
        var hash = Hash(path);
        if (baseline.TryGetValue(path, out var existing))
        {
            if (hash != existing) throw new InvalidOperationException(ReDiagnosticNames411.TpaDuplicateBytesChanged);

            return;
        }
        baseline.Add(path, hash);
    }

    internal static bool IsBound(IReadOnlyDictionary<string, string> baseline, string path)
    {
        path = Path.GetFullPath(path);

        return baseline.TryGetValue(path, out var expected) && Hash(path) == expected && IsManagedCli(path);
    }

}
