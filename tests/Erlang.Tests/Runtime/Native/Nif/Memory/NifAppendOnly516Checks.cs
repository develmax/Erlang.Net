using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NifAppendOnly516Names;

namespace Erlang.Tests;

internal static class NifAppendOnly516Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task StaticTailScaling()
    {
        var failures = new List<string>();
        var observations = new List<string>();
        foreach (int count in Sizes)
        {
            try
            {
                await using var runtime = OwnedGuest506RuntimeFactory.Create();
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                var measured = await actor.Invoke(context =>
                {
                    object registry = Property(runtime, Registry);
                    object before = Property(registry, Traversal);
                    BitString[] binaries = Enumerable.Range(0, count).Select(_ => new BitString(new byte[BackingBytes])).ToArray();
                    using var environment = Environment(context);
                    nuint current = (nuint)Call(environment, Add, Term.List(binaries.Cast<Term>().ToArray()))!;
                    for (int index = 0; index < count; index++)
                    {
                        object nativeValue = Call(environment, GetValue, current)!;
                        object?[] children = [null, null];
                        Check((bool)nativeValue.GetType().GetMethod(TryCons, Members)!.Invoke(nativeValue, children)!);
                        // Exact managed part of get_list_cell: both extracted
                        // values are re-added to the owning actual environment.
                        nuint head = (nuint)Call(environment, AddValue, children[0]!)!;
                        current = (nuint)Call(environment, AddValue, children[1]!)!;
                        Check(ReferenceEquals(binaries[index], Call(environment, Get, head)));
                    }
                    Check(Call(environment, Get, current) is Nil);
                    object after = Property(registry, Traversal);
                    var result = (Visits: Number(after, Visits) - Number(before, Visits),
                        Retains: Number(after, Retains) - Number(before, Retains),
                        Peak: Number(after, Peak), Live: Number(after, References));
                    environment.Dispose();
                    Check(Number(Property(registry, Traversal), References) == 0);

                    return result;
                });
                string observation = string.Format(
                    CultureInfo.InvariantCulture,
                    Diagnostic,
                    count,
                    measured.Visits,
                    measured.Retains,
                    measured.Peak,
                    measured.Live
                );
                observations.Add(observation);
                Console.WriteLine(observation);
                // Always finish all sizes before reporting the aggregate red.
                if (measured.Visits < count || measured.Retains < 1
                    || measured.Visits > VisitsPerElementLimit * count + VisitConstantLimit
                    || measured.Retains > MaximumRetains || measured.Peak > MaximumCopies * count
                    || measured.Live != count)
                    failures.Add(observation);
            }
            catch (Exception error)
            {
                failures.Add(count.ToString(CultureInfo.InvariantCulture) + ": " + error);
            }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException(Failure + System.Environment.NewLine + string.Join(System.Environment.NewLine, observations)
                + System.Environment.NewLine + string.Join(System.Environment.NewLine, failures));
    }

    internal static async Task HiddenAndNewComposite()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object registry = Property(runtime, Registry);
            var hidden = new BitString(new byte[BackingBytes]);
            object?[] arguments = [runtime, hidden, null];
            MapTerm map = (MapTerm)typeof(SourceBinaryComposition511Checks).GetMethod(HiddenMap, Members)!.Invoke(null, arguments)!;
            using var environment = Environment(context);
            nuint mapHandle = (nuint)Call(environment, Add, map)!;
            long visits = Number(Property(registry, Traversal), Visits);
            long references = Number(Property(registry, Traversal), References);
            _ = Call(environment, Add, hidden);
            Check(Number(Property(registry, Traversal), Visits) == visits);
            Check(Number(Property(registry, Traversal), References) == references);
            var extra = new BitString(new byte[BackingBytes]);
            _ = Call(environment, Add, Term.Tuple(map, extra));
            Check(Number(Property(registry, Traversal), Visits) > visits);
            Check(Number(Property(registry, Traversal), References) > references);
            Check(ReferenceEquals(map, Call(environment, Get, mapHandle)));
            environment.Dispose();
            Check(Number(Property(registry, Traversal), References) == 0);

            return true;
        });
    }

    internal static async Task UnknownRemainsConservative()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object registry = Property(runtime, Registry);
            using var environment = Environment(context);
            var unknown = new UnknownNifGuest516Term();
            _ = Call(environment, Add, unknown);
            long retains = Number(Property(registry, Traversal), Retains);
            _ = Call(environment, Add, unknown);
            Check(Number(Property(registry, Traversal), Retains) == retains + 1);

            return true;
        });
    }

    private static IDisposable Environment(ProcessContext context)
    {
        object image = typeof(NifEnvironmentRoots516Checks).GetMethod(Image, Members)!.Invoke(null, null)!;

        return (IDisposable)typeof(NifEnvironmentRoots516Checks).GetMethod(EnvironmentFor, Members)!.Invoke(null, [context, image])!;
    }

    private static object Property(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static long Number(object value, string name) => Convert.ToInt64(Property(value, name), CultureInfo.InvariantCulture);

    private static object? Call(object value, string name, params object?[] arguments)
    {
        MethodInfo method = value.GetType().GetMethods(Members).Single(candidate => candidate.Name == name
            && candidate.GetParameters().Length == arguments.Length
            && candidate.GetParameters().Select((parameter, index) => arguments[index] is null
                || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(match => match));
        try
        {
            return method.Invoke(value, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
