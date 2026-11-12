namespace Erlang.Compiler;

internal sealed class BeamIncludeResolver(string? otpLibraryDirectory, IReadOnlyList<string> includeDirectories)
{
    internal string? Resolve(string currentPath, string requested, bool library)
    {
        if (string.IsNullOrWhiteSpace(requested) || requested.IndexOf('\0') >= 0 || Path.IsPathRooted(requested))
        {
            return null;
        }

        if (!library)
        {
            string local = Path.GetFullPath(requested, Path.GetDirectoryName(currentPath)!);
            if (File.Exists(local))
            {
                return local;
            }

            return includeDirectories.Select(directory => Path.GetFullPath(requested, directory)).FirstOrDefault(File.Exists);
        }
        if (otpLibraryDirectory is null || !Directory.Exists(otpLibraryDirectory))
        {
            return null;
        }

        string[] parts = requested.Split('/', 2);
        if (parts.Length != 2 || parts[0].Length == 0 || !parts[0].All(character => char.IsAsciiLetterOrDigit(character) || character == '_'))
        {
            return null;
        }
        string application = Path.Combine(otpLibraryDirectory, parts[0]);
        if (!Directory.Exists(application))
        {
            string[] candidates = Directory.GetDirectories(otpLibraryDirectory, parts[0] + "-*");
            if (candidates.Length != 1)
            {
                return null;
            }
            application = candidates[0];
        }
        string header = Path.GetFullPath(parts[1], application);
        string relative = Path.GetRelativePath(application, header);

        return relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(header)
            ? null
            : header;
    }
}
