using Erlang;
using Erlang.Differential;
using System.Reflection;
using System.Text.Json;
try
{
var directory = Path.GetFullPath(args[0]);
var options = new ProcessRuntimeOptions();
typeof(ProcessRuntimeOptions).GetProperty("OwnedTermsPilot506", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(options, args[1] == "private-owned");
Term actual;
var compiled = new List<IDisposable>();
var runtime = new ProcessRuntime(ProcessNodeDefaults.Local, null, 1 << 20, null, 2, options);
try
{
    await runtime.InitializeModulesAsync(registry =>
    {
        foreach (string module in args.Length > 5 ? args[5].Split(',') : new[] { "native_writable_protocol", "native_writable_observer" })
        {
            if (args[2] == "source")
            {
                var image = GeneratedModuleCompiler.Compile(File.ReadAllText(Path.Combine(directory, module + ".erl")), char.ToLowerInvariant(directory[0]) + Path.Combine(directory, module + ".erl")[1..].Replace('\\', '/'));
                compiled.Add(image);
                image.Definition.Register(registry);
            }
            else
            {
                var image = registry.PrepareBeam(File.ReadAllBytes(Path.Combine(directory, module + ".beam")), new BeamDependencySelection((args.Length > 5 ? args[5].Split(',') : new[] { "native_writable_protocol", "native_writable_observer", "external_identifier_observer" }).Concat(new[] { "erlang", "binary" }).ToArray()));
                if (registry.PublishPreparedBeam(image).Status != BeamPublicationStatus.Published) throw new InvalidOperationException("Actual fixture publication failed");
            }
        }
    });
    var done = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    var process = runtime.Spawn(async context =>
    {
        try { done.TrySetResult(await runtime.Modules.Call(context, args.Length > 4 ? args[4] : "native_writable_observer", "run")); }
        catch (Exception failure) { done.TrySetException(failure); }
        return Nil.Value;
    });
    actual = await done.Task.WaitAsync(TimeSpan.FromSeconds(20));
    Term reason = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
    if (!reason.Equals(Term.A("normal"))) throw new InvalidOperationException(reason.ToString());
}
finally
{
    await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(20));
    foreach (var item in compiled) item.Dispose();
}
File.WriteAllBytes(Path.Combine(directory, (args.Length > 3 ? args[3] : "") + args[2] + "-" + args[1] + ".etf"), ExternalTermFormat.Encode(actual));
Term expected = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory,"original.etf")));
bool equal = actual.Equals(expected);
Console.WriteLine(JsonSerializer.Serialize(new { WholeEqual = equal, RuntimePhysicallyClosed = true, Backend=args[2],Profile=args[1] }));
Console.WriteLine(actual);
return equal ? 0 : 1;

}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    return 2;
}
