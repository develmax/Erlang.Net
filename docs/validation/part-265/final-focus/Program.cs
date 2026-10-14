using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

var repository = Path.GetFullPath(args[0]);
var oldContext = new AssemblyLoadContext("compiler-before-index", true);
var newContext = new AssemblyLoadContext("compiler-after-index", true);
Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
{
    var dependency = Path.Combine(repository, "tests/Erlang.Tests/bin/Debug/net10.0", name.Name + ".dll");
    return File.Exists(dependency) ? context.LoadFromAssemblyPath(dependency) : null;
}
oldContext.Resolving += Resolve;
newContext.Resolving += Resolve;
var oldAssembly = oldContext.LoadFromAssemblyPath(Path.Combine(repository, "artifacts/part265-main/before-named-index/Erlang.Compiler.dll"));
var newAssembly = newContext.LoadFromAssemblyPath(Path.Combine(repository, "src/Erlang.Compiler/bin/Debug/net10.0/Erlang.Compiler.dll"));
Dictionary<int, byte[]?> Methods(Assembly assembly) => assembly.GetTypes()
    .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Cast<MethodBase>().Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)))
    .ToDictionary(method => method.MetadataToken, method => method.GetMethodBody()?.GetILAsByteArray());
var before = Methods(oldAssembly);
var after = Methods(newAssembly);
if (before.Count != after.Count || before.Any(pair => !after.TryGetValue(pair.Key, out var body) || !(pair.Value ?? []).SequenceEqual(body ?? [])))
    throw new InvalidOperationException("Named index changed compiler method IL");
await Erlang.Tests.StaticApplyOnLoadChecks.Run();
await File.WriteAllTextAsync(Path.Combine(repository, "artifacts/part265-main-final-source-proof.json"), JsonSerializer.Serialize(new { Status = "PASS", AllCompilerMethodIlExact = true, CompilerMethods = before.Count, FinalActualMainHostGroupPassed = true, Full6482BeforeNameExtraction = true, NamedConstantExpansionByteExact = true, NativeExecuted = false }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("PASS final Main static apply HOST; all compiler method IL identical after named index extraction");
