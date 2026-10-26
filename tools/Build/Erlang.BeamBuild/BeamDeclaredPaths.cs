namespace Erlang.BeamBuild;

internal static class BeamDeclaredPaths
{
    internal static string Relative(string root, string path)
    {
        string full = Path.GetFullPath(path);
        RequireNoReparse(full);
        string logical = Path.GetRelativePath(root, full).Replace('\\', '/');
        RequireLogical(logical, false);

        return logical;
    }

    internal static string DirectoryName(string logical)
    {
        int last = logical.LastIndexOf('/');

        return last < 0 ? "." : logical[..last];
    }

    internal static void RequireLogical(string logical, bool directory)
    {
        if (directory && logical == ".")
            return;
        if (string.IsNullOrWhiteSpace(logical) || Path.IsPathRooted(logical) || logical.Contains('\\'))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        foreach (string part in logical.Split('/'))
            if (part is "" or "." or ".." || part.EndsWith('.') || part.EndsWith(' ')
                || part.Any(character => char.IsControl(character) || "<>:\"|?*".Contains(character)))
                throw new InvalidDataException(BeamBuildNames.Invalid);
    }

    internal static string Resolve(string directory, string include)
    {
        if (string.IsNullOrWhiteSpace(include) || Path.IsPathRooted(include)
            || include.Contains('\\') || include.Contains('$') || include.Contains(':'))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        var parts = new List<string>();
        foreach (string part in (directory == "." ? include : directory + "/" + include).Split('/'))
        {
            if (part == ".")
                continue;
            if (part == "..")
            {
                if (parts.Count == 0)
                    throw new InvalidDataException(BeamBuildNames.Invalid);
                parts.RemoveAt(parts.Count - 1);
                continue;
            }
            if (part.Length == 0)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            parts.Add(part);
        }
        string logical = string.Join('/', parts);
        RequireLogical(logical, false);

        return logical;
    }

    internal static void RequireNoReparse(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(BeamBuildNames.Invalid);
    }
}
