using System.Text.Json;
using System.Diagnostics;
using Erlang.Compiler;

if (args is ["--generate-asn1", var schemaPath, var outputDirectory])
{
    var schema = new Asn1BerSchema(File.ReadAllText(schemaPath));
    Asn1BerErlangWriter.Write(schema, outputDirectory);

    return 0;
}

if (args.Length is < 2 or > 4)
{
    Console.Error.WriteLine("Usage: Erlang.BeamCorpus <OTP-29.1.1 installation> <report.json> [public-key-asn1-headers [otp-source-headers]]");
    Console.Error.WriteLine("       Erlang.BeamCorpus --generate-asn1 <schema.asn1> <output-directory>");

    return 2;
}

const string otpVersion = "29.1.1";
string root = Path.GetFullPath(args[0]);
string versionPath = Path.Combine(root, "releases", "29", "OTP_VERSION");
if (!File.Exists(versionPath) || File.ReadAllText(versionPath).Trim() != otpVersion)
{
    Console.Error.WriteLine("The reference installation must be OTP 29.1.1.");

    return 2;
}

string report = Path.GetFullPath(args[1]);
string reportRelativeToOtp = Path.GetRelativePath(root, report);
if (!Path.IsPathRooted(reportRelativeToOtp) && !reportRelativeToOtp.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Write the corpus report outside the reference installation.");

    return 2;
}
string? publicKeyHeaders = args.Length >= 3 ? Path.GetFullPath(args[2]) : null;
if (publicKeyHeaders is not null && !Directory.Exists(publicKeyHeaders))
{
    Console.Error.WriteLine("The public_key ASN.1 header directory does not exist.");
    return 2;
}

string? sourceHeaders = args.Length == 4 ? Path.GetFullPath(args[3]) : null;
if (sourceHeaders is not null && !Directory.Exists(sourceHeaders))
{
    Console.Error.WriteLine("The OTP source header directory does not exist.");
    return 2;
}

string? generatedDirectory = Environment.GetEnvironmentVariable("ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES") == "1"
    ? report + ".generated"
    : null;
if (generatedDirectory is not null)
{
    if (Directory.Exists(generatedDirectory))
    {
        Console.Error.WriteLine("Use a new corpus report path when saving generated differences.");
        return 2;
    }

    Directory.CreateDirectory(generatedDirectory);
}

var sources = Directory.EnumerateFiles(root, "*.erl", SearchOption.AllDirectories)
    .ToLookup(path => (Application(path), Path.GetFileNameWithoutExtension(path)));
var results = new List<BeamCorpusResult>();
foreach (string beam in Directory.EnumerateFiles(root, "*.beam", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
{
    string relative = Path.GetRelativePath(root, beam);
    string sameDirectory = Path.ChangeExtension(beam, ".erl");
    var candidates = File.Exists(sameDirectory)
        ? [sameDirectory]
        : sources[(Application(beam), Path.GetFileNameWithoutExtension(beam))].ToArray();
    if (candidates.Length == 0 && relative == Path.Combine("lib", "eldap-1.3.1", "ebin", "ELDAPv3.beam"))
    {
        try
        {
            candidates = [OtpAsn1Source.Generate(root, report)];
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            results.Add(new(relative, "SourceGenerationFailed", Detail: error.Message));
            continue;
        }
    }
    if (candidates.Length != 1)
    {
        results.Add(new(relative, candidates.Length == 0 ? "MissingSource" : "AmbiguousSource", Detail:
            string.Join(", ", candidates.Order(StringComparer.Ordinal).Select(path => Path.GetRelativePath(root, path)))));
        continue;
    }

    string source = candidates[0];
    string relativeSource = Path.GetRelativePath(root, source);
    byte[] generated;
    try
    {
        string library = Path.Combine(root, "lib");
        string application = Application(beam);
        generated = CodeGeneration.CompileBeamFile(
            source,
            library,
            IncludeDirectories(library, application, publicKeyHeaders, sourceHeaders),
            OtpWindowsReleaseProfiles.FindForSource(application, relativeSource)
        );
    }
    catch (CompileException error)
    {
        results.Add(new(
            relative,
            "SourceRejected",
            relativeSource,
            $"{error.Code} at {error.Offset}: {error.Message}"
        ));
        continue;
    }
    catch (NotSupportedException error)
    {
        results.Add(new(
            relative,
            "Unsupported",
            relativeSource,
            error.Message,
            EmissionSite: EmissionSite(error)
        ));
        continue;
    }
    catch (Exception error)
    {
        results.Add(new(
            relative,
            "CompilerError",
            relativeSource,
            $"{error.GetType().Name}: {error.Message}"
        ));
        continue;
    }

    byte[] original = File.ReadAllBytes(beam);
    if (original.AsSpan().SequenceEqual(generated))
    {
        results.Add(new(relative, "Equal", relativeSource));
        continue;
    }

    if (generatedDirectory is not null)
    {
        string imagePath = Path.Combine(generatedDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);
        File.WriteAllBytes(imagePath, generated);
    }

    int offset = 0;
    int sharedLength = Math.Min(original.Length, generated.Length);
    while (offset < sharedLength && original[offset] == generated[offset])
    {
        offset++;
    }
    results.Add(new(
        relative,
        "Different",
        relativeSource,
        $"Original length {original.Length}, generated length {generated.Length}. "
            + BeamCorpusChunkComparison.Describe(original, generated),
        offset
    ));
}

var counts = results.GroupBy(result => result.Status).OrderBy(group => group.Key, StringComparer.Ordinal)
    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
Directory.CreateDirectory(Path.GetDirectoryName(report)!);
File.WriteAllText(report, JsonSerializer.Serialize(new
{
    OtpVersion = otpVersion,
    Total = results.Count,
    Counts = counts,
    Modules = results
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(counts));

return results.Count != 0 && results.All(result => result.Status == "Equal") ? 0 : 1;

string Application(string path)
{
    string[] parts = Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar);

    return parts.Length > 2 && parts[0] == "lib" ? parts[1] : parts[0];
}

static string? EmissionSite(NotSupportedException error)
{
    Exception cause = error.GetBaseException();
    var frame = new StackTrace(cause, fNeedFileInfo: true).GetFrame(0);
    var method = frame?.GetMethod();
    if (method is null)
    {
        return null;
    }

    string? file = frame!.GetFileName();
    return file is null
        ? $"{method.DeclaringType?.Name}.{method.Name}"
        : $"{Path.GetFileName(file)}:{frame.GetFileLineNumber()} ({method.Name})";
}

static string[] IncludeDirectories(string library, string application, string? publicKeyHeaders, string? sourceHeaders)
{
    string directory = Path.Combine(library, application);
    string include = Path.Combine(directory, "include");
    if (application.StartsWith("wx-", StringComparison.Ordinal))
    {
        // The installed examples are one directory deeper than in the source tree.
        return [include, Path.Combine(directory, "src"), Path.Combine(directory, "examples", "demo")];
    }
    if (application.StartsWith("snmp-", StringComparison.Ordinal))
    {
        return [include, Path.Combine(directory, "src", "misc"), Path.Combine(directory, "src", "app"),
            Path.Combine(directory, "src", "compiler")];
    }

    if (application.StartsWith("stdlib-", StringComparison.Ordinal))
    {
        string kernel = Path.Combine(library, "kernel-11.0.4", "include");
        return sourceHeaders is null
            ? [include, kernel]
            : [include, kernel, Path.Combine(sourceHeaders, "lib", "stdlib", "src")];
    }

    string appName = application.Split('-', 2)[0];
    string[] extra = appName switch
    {
        "common_test" => [Path.Combine(library, "snmp-5.20.5", "include"), Path.Combine(library, "kernel-11.0.4", "include")],
        "public_key" when publicKeyHeaders is not null => [publicKeyHeaders],
        "compiler" or "parsetools" or "sasl" => [Path.Combine(library, "stdlib-8.1", "include")],
        "eldap" => [Path.Combine(directory, "ebin")],
        "erts" => [Path.Combine(library, "kernel-11.0.4", "src"), Path.Combine(library, "kernel-11.0.4", "include")],
        "inets" => [Path.Combine(directory, "src", "inets_app"), Path.Combine(directory, "src", "http_lib")],
        "observer" => [Path.Combine(library, "et-1.8", "include")],
        _ => []
    };
    if (sourceHeaders is null)
    {
        return [include, .. extra];
    }

    // Use the upstream source layout for both local and sibling application headers.
    string sourceDirectory = appName == "erts"
        ? Path.Combine(sourceHeaders, "erts", "preloaded", "src")
        : Path.Combine(sourceHeaders, "lib", appName, "src");

    return [include, .. extra, sourceDirectory];
}
