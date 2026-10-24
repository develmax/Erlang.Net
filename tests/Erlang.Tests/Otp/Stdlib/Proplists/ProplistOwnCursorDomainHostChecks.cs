using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistOwnCursorDomainHostChecks
{
    internal static async Task Run()
    {
        using var compiled = GeneratedModuleCompiler.Compile(ProplistOwnCursorNames.Source, ProplistOwnCursorNames.SourceFile);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            var alias = (ReferenceTerm)await runtime.Modules.Call(context, ProplistOwnCursorNames.Erlang, ProplistOwnCursorNames.Alias);
            try
            {
                IReadOnlyList<ProplistOwnCursorDomain> domains = await ProplistOwnCursorDomains.Create(context, compiled, alias);
                var failures = new List<Exception>();
                foreach (var domain in domains)
                {
                    try
                    {
                        using var scope = new ProplistOwnHashScope(runtime);
                        await ProplistOwnCursorAssertions.RootSuffix(context, ProplistOwnCursorAssertions.WithKey(domain.Create()), scope);
                    }
                    catch (Exception error)
                    {
                        failures.Add(new InvalidOperationException(domain.Name, error));
                    }
                }
                try
                {
                    await ProplistOwnFlatCursorHostChecks.Run(context, compiled);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                if (failures.Count != 0)
                    throw new AggregateException(ProplistOwnCursorDiagnostics.Failure, failures);
            }
            finally
            {
                await runtime.Modules.Call(
                    context,
                    ProplistOwnCursorNames.Erlang,
                    ProplistOwnCursorNames.Unalias,
                    alias
                );
            }

            return Nil.Value;
        });
    }
}
