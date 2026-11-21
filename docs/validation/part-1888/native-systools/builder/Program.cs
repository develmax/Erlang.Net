using System.Reflection;
using Erlang.Compiler;

var release = args[0];
var input = args[1];
var toolDirectory = args[2];
var output = args[3];
var application = "sasl-4.4";
var basePath = Path.Combine(release, "lib");
var relative = Path.Combine("lib", application, "src", "systools.erl");
var source = Path.Combine(release, relative);
var tool = Assembly.LoadFrom(Path.Combine(toolDirectory, "Erlang.BeamCorpus.dll"));
const BindingFlags access = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
var type = tool.GetType("OtpWindowsReleaseProfiles", throwOnError: true)!;
var rule = type.GetMethod("FindForSource", access)!;
var profile = (BeamReleaseBuildProfile)rule.Invoke(null, [application, relative])!;
var bootstrap = tool.GetType("OtpWindowsBootstrapLibraryIncludePaths", throwOnError: true)!;
var includePathMap = (BeamIncludePathMap)bootstrap.GetMethod("Find", access)!.Invoke(
    null, [basePath, application, source, profile, input]
)!;
var include = tool.EntryPoint!.DeclaringType!.GetMethods(access).Single(m => m.Name.Contains("g__IncludeDirectories|", StringComparison.Ordinal));
var includeDirectories = (string[])include.Invoke(null, [basePath, application, Path.Combine(Directory.GetParent(input)!.FullName, "public-key-generated"), input])!;
var emitted = CodeGeneration.CompileBeamFile(source, basePath, includeDirectories, profile with { IncludePathMap = includePathMap });
File.WriteAllBytes(output, emitted);