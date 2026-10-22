using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Erlang.BeamBuild;

internal static class BeamDeclaredRequest
{
    internal static BeamBuildRequest Validate(BeamBuildRequest request)
    {
        if (request.SourcePolicy is null)
        {
            if (request.SourceRoot is not null || request.Headers is not null
                || request.IncludeDirectories is not null || request.Defines is not null || request.RuntimeSha256 is not null)
                throw new InvalidDataException(BeamBuildNames.Invalid);

            return request;
        }
        if (request.SourcePolicy != BeamDeclaredInputNames.Policy
            || string.IsNullOrWhiteSpace(request.SourceRoot)
            || !Path.IsPathFullyQualified(request.SourceRoot) || !Directory.Exists(request.SourceRoot)
            || request.Headers is null || request.IncludeDirectories is null || request.Defines is null
            || !Regex.IsMatch(request.RuntimeSha256 ?? string.Empty, BeamBuildValidationNames.Sha256))
            throw new InvalidDataException(BeamBuildNames.Invalid);
        string root = Path.GetFullPath(request.SourceRoot);
        BeamDeclaredPaths.RequireNoReparse(root);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (BeamBuildSource source in request.Sources)
        {
            RequireFile(
                root,
                source.Path,
                BeamBuildNames.SourceExtension,
                names
            );
        }
        foreach (BeamBuildHeader header in request.Headers)
        {
            if (header is null)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            RequireFile(
                root,
                header.Path,
                BeamDeclaredInputNames.HeaderExtension,
                names
            );
        }
        var prefixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string logical in names)
            RequirePrefixSpelling(logical, prefixes);
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string directory in request.IncludeDirectories)
        {
            BeamDeclaredPaths.RequireLogical(directory, true);
            if (!directories.Add(directory))
                throw new InvalidDataException(BeamBuildNames.Invalid);
            if (directory != ".")
                RequirePrefixSpelling(directory, prefixes);
            string full = Path.Combine(root, directory);
            BeamDeclaredPaths.RequireNoReparse(full);
            if (!Directory.Exists(full))
                throw new InvalidDataException(BeamBuildNames.Invalid);
        }
        var macros = new HashSet<string>(StringComparer.Ordinal);
        foreach (BeamBuildDefine define in request.Defines)
        {
            if (define is null || define.Name is null || define.Name.Length > BeamDeclaredInputNames.MaximumAtomCodepoints
                || !Regex.IsMatch(define.Name, BeamDeclaredInputNames.MacroNamePattern)
                || !macros.Add(define.Name))
                throw new InvalidDataException(BeamBuildNames.Invalid);
            if (define.Value is not { } value)
                continue;
            if (value.Text is null)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            _ = new UTF8Encoding(false, true).GetBytes(value.Text);
            switch (value.Kind)
            {
                case BeamDeclaredInputNames.AtomValue:
                    if (value.Text.EnumerateRunes().Count() > BeamDeclaredInputNames.MaximumAtomCodepoints)
                        throw new InvalidDataException(BeamBuildNames.Invalid);
                    break;
                case BeamDeclaredInputNames.IntegerValue:
                    if (!BigInteger.TryParse(
                        value.Text,
                        NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture,
                        out var integer
                    )
                        || integer.ToString(CultureInfo.InvariantCulture) != value.Text)
                        throw new InvalidDataException(BeamBuildNames.Invalid);
                    break;
                case BeamDeclaredInputNames.StringValue:
                    break;
                default:
                    throw new InvalidDataException(BeamBuildNames.Invalid);
            }
        }

        return request with
        {
            SourceRoot = root,
            Headers = request.Headers.ToArray(),
            IncludeDirectories = request.IncludeDirectories.ToArray(),
            Defines = request.Defines.ToArray()
        };
    }

    private static void RequireFile(
        string root,
        string path,
        string extension,
        HashSet<string> names
    )
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || !File.Exists(path)
            || Path.GetExtension(path) != extension || !names.Add(BeamDeclaredPaths.Relative(root, path)))
            throw new InvalidDataException(BeamBuildNames.Invalid);
    }

    private static void RequirePrefixSpelling(string logical, Dictionary<string, string> prefixes)
    {
        string prefix = string.Empty;
        foreach (string part in logical.Split('/'))
        {
            prefix = prefix.Length == 0 ? part : prefix + "/" + part;
            if (prefixes.TryGetValue(prefix, out string? previous) && previous != prefix)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            prefixes[prefix] = prefix;
        }
    }
}
