namespace Erlang.BeamBuild;

internal static class BeamApplicationPaths
{
    internal static void RequireLogical(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        BeamDeclaredPaths.RequireLogical(path, false);
        foreach (string part in path.Split(BeamApplicationNames.LogicalSeparatorCharacter))
            if (BeamApplicationNames.ReservedDeviceNames.Contains(part.Split('.')[0]))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
    }

    internal static string ContainedFile(string root, string logical)
    {
        RequireLogical(logical);
        string full = Path.GetFullPath(Path.Combine(root, logical));
        if (BeamDeclaredPaths.Relative(root, full) != logical || !File.Exists(full))
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        RequireActualCase(full);

        return full;
    }

    internal static bool Within(string directory, string candidate)
    {
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        string path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));

        return path.Equals(root, comparison) || path.StartsWith(Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar, comparison);
    }

    internal static void RequireActualCase(string path)
    {
        string full = Path.GetFullPath(path);
        BeamDeclaredPaths.RequireNoReparse(full);
        string current = Path.GetPathRoot(full)!;
        foreach (string part in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] matches = Directory.EnumerateFileSystemEntries(current)
                .Where(entry => Path.GetFileName(entry).Equals(part, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1 || Path.GetFileName(matches[0]) != part)
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            current = matches[0];
            BeamDeclaredPaths.RequireNoReparse(current);
        }
    }

    internal static void RequireCreationPath(string path)
    {
        string full = Path.GetFullPath(path);
        BeamDeclaredPaths.RequireNoReparse(full);
        string current = Path.GetPathRoot(full)!;
        foreach (string part in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] matches = Directory.EnumerateFileSystemEntries(current)
                .Where(entry => Path.GetFileName(entry).Equals(part, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0)
                return;
            if (matches.Length != 1 || Path.GetFileName(matches[0]) != part || !Directory.Exists(matches[0]))
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            current = matches[0];
            BeamDeclaredPaths.RequireNoReparse(current);
        }
    }
}
