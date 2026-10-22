namespace Erlang.BeamBuild;

internal static class BeamApplicationTaskPaths
{
    internal static string Portable(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value) || value.Contains('\\'))
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        foreach (string segment in value.Split('/'))
        {
            string stem = segment.Split('.')[0];
            if (segment.Length == 0 || segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ')
                || segment.Any(character => character < ' ' || BeamApplicationBuildTaskNames.InvalidPortableCharacters.Contains(character))
                || BeamApplicationBuildTaskNames.ReservedDeviceStems.Contains(stem)
                || stem.Length == 4 && (stem.StartsWith(BeamApplicationBuildTaskNames.ComDevicePrefix, StringComparison.OrdinalIgnoreCase)
                    || stem.StartsWith(BeamApplicationBuildTaskNames.LptDevicePrefix, StringComparison.OrdinalIgnoreCase)) && stem[3] is >= '1' and <= '9')
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
        }

        return value;
    }

    internal static string Relative(string root, string file)
        => Portable(Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(file)).Replace(Path.DirectorySeparatorChar, '/'));

    internal static string Absolute(string value)
    {
        if (!Path.IsPathFullyQualified(value))
            throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);

        return Path.GetFullPath(value);
    }

    internal static void RejectReparse(string root, string file)
    {
        string absoluteRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string? current = Path.GetFullPath(file);
        while (current is not null)
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
            if (current.Equals(absoluteRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return;
            current = Path.GetDirectoryName(current);
        }
        throw new InvalidDataException(BeamApplicationBuildTaskNames.Invalid);
    }

    internal static string Hash(string file) => BeamBuildStorage.HashFile(file);
}
