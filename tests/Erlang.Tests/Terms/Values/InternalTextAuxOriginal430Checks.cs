using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class InternalTextAuxOriginal430Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(InternalTextAuxOriginal430Names.Output, System.Guid.NewGuid().ToString(InternalTextAuxOriginal430Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var failures = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime(InternalTextAuxOriginal430Names.Participants);
            ProcessRuntime owner = runtime;
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                owner,
                (registry, scope) =>
            {
                var dependencies = new BeamDependencySelection(new[] { InternalTextAuxOriginal430Names.Runtime, InternalTextAuxOriginal430Names.File, InternalTextAuxOriginal430Names.Internal, InternalTextAuxOriginal430Names.Module });
                PreparedBeamImage facet = scope.Prepare(() => registry.PrepareBeam(InternalTextAuxOriginal430Access.Resource(InternalTextAuxOriginal430Names.FacetBeam), dependencies));
                PreparedBeamImage worker = scope.Prepare(() => registry.PrepareBeam(InternalTextAuxOriginal430Access.Resource(InternalTextAuxOriginal430Names.WorkerBeam), dependencies));
                InternalTextAuxOriginal430Access.Require(facet.NativePrimitiveExports.Count == InternalTextAuxOriginal430Names.NativeCount
                    && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(InternalTextAuxOriginal430Names.Text, 2))
                    && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(InternalTextAuxOriginal430Names.Auxiliary, 0)));

                return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
                {
                    InternalTextAuxOriginal430Access.Require(registry.PublishPreparedBeam([facet, worker]).Status == BeamPublicationStatus.Published);
                    InternalTextAuxOriginal430Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == InternalTextAuxOriginal430Names.TotalExports);
                    foreach (var image in new[] { facet, worker }) foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        InternalTextAuxOriginal430Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
                });
            }
            );
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;
            foreach (var route in new[] { (InternalTextAuxOriginal430Names.Run, InternalTextAuxOriginal430Names.Values), (InternalTextAuxOriginal430Names.RunStacks, InternalTextAuxOriginal430Names.Stacks) })
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(context, InternalTextAuxOriginal430Names.Module, route.Item1));
                    var wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        InternalTextAuxOriginal430Names.Runtime,
                        InternalTextAuxOriginal430Names.Encode,
                        value
                    ));
                    captured.Add(route.Item2, (value, wire.ToArray()));
                    System.IO.File.WriteAllBytes(Path.Combine(output, InternalTextAuxOriginal430Names.Actual + route.Item2), wire.ToArray());
                    System.IO.File.WriteAllText(
                        Path.Combine(output, InternalTextAuxOriginal430Names.Actual + route.Item2 + InternalTextAuxOriginal430Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    failures.Add(new { Route = route.Item2, Failure = error.ToString() });
                }
        }
        catch (Exception error)
        {
            failures.Add(new { Preparation = error.ToString() });
        }
        finally
        {
            try
            {
                if (actor is not null) await actor.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(new { ActorCleanup = error.ToString() });
            }
            try
            {
                if (runtime is not null) await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(new { RuntimeCleanup = error.ToString() });
            }
        }
        var differences = new List<object>();
        var own = new List<object>();
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        int examined = 0, strict = 0, strictEqual = 0, ownCount = 0, ownAccepted = 0;

        void Rows(Term expected, Term actual, string route)
        {
            Term[] left = Cons.Items(expected).ToArray(), right = Cons.Items(actual).ToArray();
            InternalTextAuxOriginal430Access.Require(left.Length == InternalTextAuxOriginal430Names.Rows && right.Length == left.Length);
            for (int index = 0; index < left.Length; index++)
            {
                examined++;
                if (left[index] is TupleTerm { Items: [Atom { Name: InternalTextAuxOriginal430Names.OwnCase }, TupleTerm { Items: [Atom { Name: InternalTextAuxOriginal430Names.Value }, Integer native] }] })
                {
                    ownCount++;
                    Term wanted = Term.Tuple(
                        Term.A(InternalTextAuxOriginal430Names.OwnCase),
                        Term.Tuple(Term.A(InternalTextAuxOriginal430Names.Value), Term.I(InternalTextAuxOriginal430Names.Participants))
                    );
                    bool accepted = native.Value > 0 && right[index].Equals(wanted);
                    if (accepted) ownAccepted++;
                    own.Add(new { Route = route, Index = index, Accepted = accepted, NativeComplete = OwnedFunctionOriginal407Terms.Typed(left[index]), ManagedComplete = OwnedFunctionOriginal407Terms.Typed(right[index]), NumericEqualityClaim = false });
                }
                else
                {
                    strict++;
                    bool equal = left[index].Equals(right[index]) && ExternalTermFormat.Encode(left[index], new EtfEncodingOptions { MinorVersion = 2 }).AsSpan().SequenceEqual(ExternalTermFormat.Encode(right[index], new EtfEncodingOptions { MinorVersion = 2 }));
                    if (equal) strictEqual++; else differences.Add(new { Route = route, Index = index, Expected = OwnedFunctionOriginal407Terms.Typed(left[index]), Actual = OwnedFunctionOriginal407Terms.Typed(right[index]) });
                }
            }
        }
        foreach (string file in new[] { InternalTextAuxOriginal430Names.Values, InternalTextAuxOriginal430Names.Stacks })
        {
            byte[] expectedBytes = InternalTextAuxOriginal430Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(expectedBytes);
            System.IO.File.WriteAllBytes(Path.Combine(output, InternalTextAuxOriginal430Names.Expected + file), expectedBytes);
            System.IO.File.WriteAllText(
                Path.Combine(output, InternalTextAuxOriginal430Names.Expected + file + InternalTextAuxOriginal430Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && expectedBytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            if (file == InternalTextAuxOriginal430Names.Values) Rows(expected, actual.Term, file);
            else
            {
                Term[] left = Cons.Items(expected).ToArray(), right = Cons.Items(actual.Term).ToArray();
                InternalTextAuxOriginal430Access.Require(left.Length == 5 && right.Length == 5);
                for (int index = 0; index < left.Length; index++)
                {
                    InternalTextAuxOriginal430Access.Require(left[index] is TupleTerm { Items.Count: 3 } && right[index] is TupleTerm { Items.Count: 3 });
                    var l = (TupleTerm)left[index];
                    var a = (TupleTerm)right[index];
                    if (!l.Items[0].Equals(a.Items[0]) || !l.Items[1].Equals(a.Items[1])) differences.Add(new { Route = file, Profile = index, Expected = OwnedFunctionOriginal407Terms.Typed(left[index]), Actual = OwnedFunctionOriginal407Terms.Typed(right[index]) });
                    Rows(l.Items[2], a.Items[2], file + index);
                }
            }
        }
        System.IO.File.WriteAllText(
            Path.Combine(output, InternalTextAuxOriginal430Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Examined = examined,
                StrictRows = strict,
                StrictEqual = strictEqual,
                OwnStateRows = ownCount,
                OwnStateAccepted = ownAccepted,
                OwnState = own,
                WholeRawEtf = raw,
                WholeTerms = whole,
                Failures = failures,
                Differences = differences,
                Projection = false,
                OwnStateCase = InternalTextAuxOriginal430Names.OwnCase,
                PhysicalThreadCountParity = false,
                WholeInternal71Claim = false,
                FullNumericCompatibilityClaim = false
            })
        );
        InternalTextAuxOriginal430Access.Require(failures.Count == 0 && differences.Count == 0 && examined == InternalTextAuxOriginal430Names.WholeRows
            && strict == InternalTextAuxOriginal430Names.StrictRows && strictEqual == strict && ownCount == InternalTextAuxOriginal430Names.OwnRows && ownAccepted == ownCount);
    }
}
