using System.Globalization;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.DictionaryProjection635Names;

namespace Erlang.Tests;

internal static class DictionaryProjection635Checks
{
    internal static async Task Scaling()
    {
        var failures = new List<string>();
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        using var compiled = GeneratedModuleCompiler.Compile(Source, DictionaryProjection635Names.File);
        compiled.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (int count in Sizes)
        {
            await actor.Invoke(context =>
            {
                context.Dictionary.Clear();
                for (int index = 0; index < count; index++)
                    context.Dictionary[Term.I(index)] = Term.A(SelectedValue);

                return true;
            });
            foreach (string entry in new[] { All, Matching, Missing })
            {
                object registry = Property(runtime, LinearConstruction514Names.Registry);
                long before = Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.Visits);
                Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    DictionaryProjection635Names.Module,
                    entry
                ));
                long visits = Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.Visits) - before;
                int expected = entry == Missing ? 0 : count;
                long limit = (long)VisitsPerEntry * count + FixedVisits;
                string row = string.Format(
                    CultureInfo.InvariantCulture,
                    Observation,
                    count,
                    entry,
                    visits,
                    limit
                );
                Console.WriteLine(row);
                if (!value.Equals(Term.I(expected)) || visits > limit)
                    failures.Add(row);
            }
        }
        await actor.Invoke(context =>
 {
     context.Dictionary.Clear();

     return true;
 });
        if (failures.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
    }

    private static long Number(object value, string property)
        => Convert.ToInt64(Property(value, property), CultureInfo.InvariantCulture);
}
