namespace Erlang.Compiler;

internal sealed class BeamIncludeResolver(
    string? otpLibraryDirectory,
    IReadOnlyList<string> includeDirectories,
    IReadOnlyDictionary<int, string>? recordedIncludeSearchDirectories = null,
    BeamIncludePathMap? pathMap = null,
    string? mainPhysicalPath = null
)
{
    internal string? SourceArgument => pathMap?.SourceArgument;

    internal BeamResolvedInclude? ResolveInclude(
        string currentPath,
        string? currentArgument,
        string requested,
        bool library
    )
    {
        if (pathMap is null || currentArgument is null || mainPhysicalPath is null)
        {
            string? legacyPath = Resolve(
                currentPath,
                requested,
                library,
                currentArgument,
                out string? legacyRecordedPath
            );

            return legacyPath is null ? null : new(legacyPath, legacyRecordedPath);
        }
        if (!IsRelativeInclude(requested))
        {
            return null;
        }

        var current = new BeamIncludeSearchDirectory(
            Path.GetDirectoryName(currentPath)!,
            RecordedDirectory(currentArgument)
        );

        var main = new BeamIncludeSearchDirectory(
            Path.GetDirectoryName(mainPhysicalPath)!,
            RecordedDirectory(pathMap.SourceArgument)
        );

        // EPP replaces only the first search directory when entering an include.
        var candidate = ResolveCandidate(current, requested)
            ?? ResolveCandidate(pathMap.WorkingDirectory, requested)
            ?? ResolveCandidate(main, requested);
        if (candidate is not null)
        {
            return candidate;
        }

        foreach (var directory in pathMap.IncludeDirectories)
        {
            candidate = ResolveCandidate(directory, requested);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        string[] parts = requested.Split('/', 2);
        if (library && parts.Length == 2 && IsRelativeInclude(parts[1])
            && pathMap.Libraries.TryGetValue(parts[0], out var application))
        {
            return ResolveCandidate(application, parts[1]);
        }

        return null;
    }

    private static bool IsRelativeInclude(string requested) =>
        !string.IsNullOrWhiteSpace(requested) && requested.IndexOf('\0') < 0 && !Path.IsPathRooted(requested);

    private static BeamResolvedInclude? ResolveCandidate(BeamIncludeSearchDirectory directory, string requested)
    {
        string? physicalPath = Path.GetFullPath(requested, directory.PhysicalDirectory);
        if (!File.Exists(physicalPath))
        {
            physicalPath = ResolveRenamedChild(directory, requested);
            if (physicalPath is null)
            {
                return null;
            }
        }

        string recordedPath = directory.RecordedDirectory == "."
            ? requested
            : JoinRecordedPath(directory.RecordedDirectory, requested);

        return new(physicalPath, recordedPath);
    }

    private static string? ResolveRenamedChild(BeamIncludeSearchDirectory directory, string requested)
    {
        if (directory.PhysicalChildDirectories is not { } children)
        {
            return null;
        }

        string relative = JoinRecordedPath(".", requested);
        int separator = relative.IndexOf('/');
        if (separator <= 0 || !children.TryGetValue(relative[..separator], out string? child))
        {
            return null;
        }

        string path = Path.GetFullPath(relative[(separator + 1)..], child);

        return File.Exists(path) ? path : null;
    }

    private static string RecordedDirectory(string recordedPath)
    {
        int separator = recordedPath.LastIndexOf('/');

        return separator switch
        {
            -1 => ".",
            0 => "/",
            _ => recordedPath[..separator]
        };
    }

    private static string JoinRecordedPath(string directory, string requested)
    {
        string path = directory.TrimEnd('/') + "/" + requested;
        string joined = string.Join('/', path.Split('/').Where(part => part.Length > 0 && part != "."));

        return path.StartsWith('/') ? "/" + joined : joined;
    }

    internal string? Resolve(
        string currentPath,
        string requested,
        bool library,
        string? recordedParent,
        out string? recordedPath
    )
    {
        recordedPath = null;
        if (!IsRelativeInclude(requested))
        {
            return null;
        }

        if (!library)
        {
            string local = Path.GetFullPath(requested, Path.GetDirectoryName(currentPath)!);
            if (File.Exists(local))
            {
                if (recordedParent?.LastIndexOf('/') is int separator && separator >= 0)
                {
                    recordedPath = recordedParent[..(separator + 1)] + requested;
                }

                return local;
            }

            for (int index = 0; index < includeDirectories.Count; index++)
            {
                string included = Path.GetFullPath(requested, includeDirectories[index]);
                if (File.Exists(included))
                {
                    if (recordedIncludeSearchDirectories?.GetValueOrDefault(index) is { } buildDirectory)
                    {
                        recordedPath = buildDirectory.TrimEnd('/') + "/" + requested;
                    }

                    return included;
                }
            }

            return null;
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
