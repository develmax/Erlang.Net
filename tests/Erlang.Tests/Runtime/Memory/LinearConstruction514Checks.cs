using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.LinearConstruction514Names;

namespace Erlang.Tests;

internal static class LinearConstruction514Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Scaling()
    {
        var failures = new List<string>();
        foreach (int count in Sizes)
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create();
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.Invoke(context =>
            {
                object heap = Property(context, Heap), registry = Property(runtime, Registry);
                using var frame = (IDisposable)Call(heap, Enter)!;
                int slot = (int)Call(frame, Reserve)!;
                BitString[] binaries = Enumerable.Range(0, count).Select(_ => new BitString(new byte[BackingBytes])).ToArray();
                Term source = Term.List(binaries.Cast<Term>().ToArray());
                object before = Property(registry, Traversal);
                Term adopted = (Term)Call(
                    frame,
                    Adopt,
                    slot,
                    source
                )!;
                object after = Property(registry, Traversal);
                long visits = Number(after, Visits) - Number(before, Visits);
                long retains = Number(after, Retains) - Number(before, Retains);
                long peak = Number(after, Peak);
                object snapshot = Call(registry, Snapshot, (object?)null)!;
                Check((int)Property(snapshot, Distinct) == count);
                Check(visits >= count * RetainedCopiesLimit && retains >= RetainedCopiesLimit);
                if (visits > VisitsPerElementLimit * count + VisitConstantLimit
                    || retains > MaximumRetains || peak > RetainedCopiesLimit * count)
                    failures.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        Diagnostic,
                        Failure,
                        count,
                        visits,
                        retains,
                        peak
                    ));
                Console.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    Observation,
                    count,
                    visits,
                    retains,
                    peak
                ));
                Check(Number(after, References) == count);
                int index = 0;
                foreach (Term binary in Cons.Items(adopted))
                    Check(ReferenceEquals(binary, binaries[index++]));
                Check(index == count);
                Call(frame, Clear, slot);
                Call(heap, Collect);
                Check(Number(Property(registry, Traversal), References) == 0);

                return true;
            });
        }
        if (failures.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
    }

    internal static async Task NestedEmptyTuples()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Property(context, Heap);
            using var frame = (IDisposable)Call(heap, Enter)!;
            int slot = (int)Call(frame, Reserve)!;
            Term source = Term.Tuple(Term.Tuple(), Term.List(Term.Tuple()));
            Term adopted = (Term)Call(
                frame,
                Adopt,
                slot,
                source
            )!;
            Check(adopted.Equals(source));
            Call(frame, Clear, slot);
            Call(heap, Collect);

            return true;
        });
    }

    internal static async Task HiddenPhysicalMap()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Property(context, Heap), registry = Property(runtime, Registry);
            var binary = new BitString(new byte[BackingBytes]);
            object?[] arguments = [runtime, binary, null];
            MapTerm map = (MapTerm)typeof(SourceBinaryComposition511Checks).GetMethod(HiddenMap, Members)!.Invoke(null, arguments)!;
            Term hiddenKey = (Term)arguments[2]!;
            using var frame = (IDisposable)Call(heap, Enter)!;
            int slot = (int)Call(frame, Reserve)!;
            Term value = (Term)Call(
                frame,
                Adopt,
                slot,
                Term.List(map)
            )!;
            Call(heap, Collect);
            Check(Number(Call(registry, Snapshot, (object?)null)!, Bytes) == BackingBytes);
            MapTerm retained = (MapTerm)((Cons)value).Head;
            Check(retained.TryGet(hiddenKey, out Term? hidden) && ReferenceEquals(hidden, binary));
            Call(frame, Clear, slot);
            Call(heap, Collect);
            Check(Number(Property(registry, Traversal), References) == 0);

            return true;
        });
    }

    internal static async Task RejectAndRollback()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Property(context, Heap), registry = Property(runtime, Registry);
            using var existing = (IDisposable)Call(heap, Enter)!;
            int slot = (int)Call(existing, Reserve)!;
            var previous = new BitString(new byte[BackingBytes]);
            Call(
                existing,
                Set,
                slot,
                previous
            );
            Term retainedPrevious = (Term)Call(existing, Read, slot)!;
            long baseline = Number(Property(registry, Traversal), References);
            using (var construction = (IDisposable)Call(heap, Enter)!)
            {
                var input = new BitString(new byte[BackingBytes]);
                object custody = Call(construction, Begin, Term.List(input))!;
                Check(Number(Property(registry, Traversal), References) == baseline + 1);
                bool rejected = false;
                lock (Property(heap, Gate))
                {
                    Term rogue = (Term)Call(
                        heap,
                        CreateCons,
                        new BitString(new byte[BackingBytes]),
                        Nil.Value
                    )!;
                    try
                    {
                        Call(custody, Publish, rogue);
                    }
                    catch (InvalidOperationException)
                    {
                        rejected = true;
                    }
                }
                Check(rejected);
                Check(Number(Property(registry, Traversal), References) == baseline + 1);
                Check(ReferenceEquals(retainedPrevious, Call(existing, Read, slot)));
            }
            Check(Number(Property(registry, Traversal), References) == baseline);
            Check((long)Call(heap, Collect)! > 0);
            Call(existing, Clear, slot);
            Check(Number(Property(registry, Traversal), References) == 0);

            return true;
        });
    }

    private static long Number(object value, string name) => Convert.ToInt64(Property(value, name), CultureInfo.InvariantCulture);

    private static object Property(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

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

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
