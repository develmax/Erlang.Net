using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang.Tests;

internal static class RuntimeBuildMetadataChecks
{
    internal static async Task Run()
    {
        var failures = new List<Exception>();
        string? previousVersion = null, previousArchitecture = null;
        foreach (bool owned in new[] { false, true })
        {
            try
            {
                var options = new ProcessRuntimeOptions();
                PropertyInfo pilot = typeof(ProcessRuntimeOptions).GetProperty(RuntimeBuildMetadataTestNames.OwnedOption, BindingFlags.Instance | BindingFlags.NonPublic)!;
                pilot.SetValue(options, owned);
                await using var runtime = new ProcessRuntime(options);
                await runtime.InitializeModulesAsync(_ => { });
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                await actor.InvokeAsync(async context =>
                {
                    async Task<Term> Read(Term key) => await runtime.Modules.Call(
                        context,
                        RuntimeBuildMetadataTestNames.Erlang,
                        RuntimeBuildMetadataTestNames.SystemInfo,
                        key
                    );
                    string version = Text(await Read(Term.A(RuntimeBuildMetadataTestNames.Version)));
                    string architecture = Text(await Read(Term.A(RuntimeBuildMetadataTestNames.Architecture)));
                    Assembly engine = typeof(ProcessRuntime).Assembly;
                    Require(version.StartsWith(RuntimeBuildMetadataTestNames.EnginePrefix, StringComparison.Ordinal)
                        && version.Contains(engine.ManifestModule.ModuleVersionId.ToString(RuntimeBuildMetadataTestNames.ModuleFormat), StringComparison.Ordinal));
                    string? information = engine.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    if (!string.IsNullOrEmpty(information))
                        Require(version.Contains(information, StringComparison.Ordinal));
                    Require(architecture.Contains(RuntimeInformation.RuntimeIdentifier, StringComparison.Ordinal)
                        && architecture.EndsWith(RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(), StringComparison.Ordinal));
                    Require(Text(await Read(Term.A(RuntimeBuildMetadataTestNames.Version))) == version
                        && Text(await Read(Term.A(RuntimeBuildMetadataTestNames.Architecture))) == architecture);
                    if (previousVersion is not null)
                        Require(version == previousVersion && architecture == previousArchitecture);
                    previousVersion = version;
                    previousArchitecture = architecture;
                    Require((bool)pilot.GetValue(options)! == owned);
                    Term wrong = Term.String(RuntimeBuildMetadataTestNames.Version);
                    try
                    {
                        _ = await Read(wrong);
                        throw new InvalidOperationException(RuntimeBuildMetadataTestNames.Failure);
                    }
                    catch (ErlangException error)
                    {
                        Require(error.ExceptionClass == RuntimeBuildMetadataTestNames.Error
                            && error.Reason.Equals(Term.A(RuntimeBuildMetadataTestNames.Badarg)));
                        Require(error.StackTraceTerm is Cons { Head: TupleTerm frame }
                            && frame.Items[0].Equals(Term.A(RuntimeBuildMetadataTestNames.Erlang))
                            && frame.Items[1].Equals(Term.A(RuntimeBuildMetadataTestNames.SystemInfo))
                            && frame.Items[2].Equals(Term.List(wrong)));
                    }

                    return true;
                });
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(failures);
    }

    private static string Text(Term value)
    {
        Term[] cells = Cons.Items(value).ToArray();
        Require(cells.Length > 0 && cells.All(cell => cell is Integer { Value.Sign: >= 0 }));

        return string.Concat(cells.Select(cell => char.ConvertFromUtf32(checked((int)((Integer)cell).Value))));
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(RuntimeBuildMetadataTestNames.Failure);
    }
}
