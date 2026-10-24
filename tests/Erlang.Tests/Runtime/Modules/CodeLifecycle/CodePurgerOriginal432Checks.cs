using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class CodePurgerOriginal432Checks
{
    internal static async Task Run()
    {
        string output = Path.GetFullPath(Path.Combine(CodePurgerOriginal432Names.Output,
            System.Guid.NewGuid().ToString(CodePurgerOriginal432Names.Guid)));
        Directory.CreateDirectory(output);
        var captured = new Dictionary<string, (Term Term, byte[] Bytes)>();
        var errors = new List<object>();
        ProcessRuntime? runtime = null;
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            runtime = new ProcessRuntime(1);
            ProcessRuntime owner = runtime;
            await owner.InitializeModulesAsync(registry =>
            {
                var declared = new BeamDependencySelection(new[] { CodePurgerOriginal432Names.Runtime, CodePurgerOriginal432Names.File, CodePurgerOriginal432Names.Internal, CodePurgerOriginal432Names.Purger, CodePurger432HostNames.Subject, CodePurgerOriginal432Names.Module });
                PreparedBeamImage facet = registry.PrepareBeam(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.FacetBeam), declared);
                PreparedBeamImage purger = registry.PrepareBeam(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.PurgerBeam), declared);
                PreparedBeamImage worker = registry.PrepareBeam(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.WorkerBeam), declared);
                CodePurgerOriginal432Access.Require(registry.PublishPreparedBeam(new[] { facet, purger, worker }).Status == BeamPublicationStatus.Published);
                CodePurgerOriginal432Access.Require(facet.NativePrimitiveExports.Count == CodePurgerOriginal432Names.NativeCount
                    && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(CodePurgerOriginal432Names.NativeFunction, 2))
                    && facet.NativePrimitiveExports.Contains(new BeamFunctionSignature(CodePurgerOriginal432Names.Request, 3)));
                CodePurgerOriginal432Access.Require(registry.Exports.Count(export => export.Module == facet.Name) == CodePurgerOriginal432Names.TotalExports);
                CodePurgerOriginal432Access.Require(purger.NativePrimitiveExports.Count == 0
                    && registry.Exports.Count(export => export.Module == purger.Name) == CodePurgerOriginal432Names.PurgerExports);
                foreach (var image in new[] { facet, purger, worker })
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        CodePurgerOriginal432Access.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
            typeof(ProcessRuntime).GetMethod(CodePurgerOriginal432Names.StartBeforeBoot, CodePurger432HostNames.Flags)!.Invoke(owner, null);
            long readiness = Environment.TickCount64;
            while (owner.WhereIs(CodePurgerOriginal432Names.Purger) is not Pid
                && Environment.TickCount64 - readiness < CodePurger432HostNames.Deadline.TotalMilliseconds) await Task.Delay(1);
            CodePurgerOriginal432Access.Require(owner.WhereIs(CodePurgerOriginal432Names.Purger) is Pid role
                && CodePurger432HostAccess.Field(owner, CodePurger432HostNames.Actor) is ProcessContext sourceActor
                && sourceActor.Self.Equals(role)
                && ReferenceEquals(
                    CodePurger432HostAccess.Field(owner, CodePurger432HostNames.Source),
                    owner.Modules.SourceOwner(CodePurgerOriginal432Names.Purger, CodePurger432HostNames.Start, 0)
                ));
            actor = new RuntimeNodeFoundationHostActor(owner);
            RuntimeNodeFoundationHostActor caller = actor;

            async Task Capture(string function, string file)
            {
                try
                {
                    Term value = await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        CodePurgerOriginal432Names.Module,
                        function,
                        new BitString(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.FirstBeam)),
                        new BitString(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.SecondBeam)),
                        new BitString(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.RejectedBeam))
                    ));
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        CodePurgerOriginal432Names.Runtime,
                        CodePurgerOriginal432Names.Encode,
                        value
                    ));
                    captured.Add(file, (value, wire.ToArray()));
                    File.WriteAllBytes(Path.Combine(output, CodePurgerOriginal432Names.Actual + file), wire.ToArray());
                    File.WriteAllText(
                        Path.Combine(output, CodePurgerOriginal432Names.Actual + file + CodePurgerOriginal432Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
            }
            await Capture(CodePurgerOriginal432Names.Run, CodePurgerOriginal432Names.Values);
            await Capture(CodePurgerOriginal432Names.Stacks, CodePurgerOriginal432Names.StackValues);
        }
        catch (Exception error)
        {
            errors.Add(new { Preparation = error.ToString() });
        }
        finally
        {
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { RuntimeCleanup = error.ToString() });
                }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { ActorCleanup = error.ToString() });
                }
        }
        // Decode expectations only after both independent routes and real owner cleanup.
        var differences = new List<object>();
        int examined = 0, equal = 0;
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        foreach (string file in new[] { CodePurgerOriginal432Names.Values, CodePurgerOriginal432Names.StackValues })
        {
            byte[] bytes = CodePurgerOriginal432Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, CodePurgerOriginal432Names.Expected + file), bytes);
            File.WriteAllText(
                Path.Combine(output, CodePurgerOriginal432Names.Expected + file + CodePurgerOriginal432Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Term);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            equal += file == CodePurgerOriginal432Names.Values
                ? IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Term,
                    file,
                    CodePurgerOriginal432Names.Rows,
                    differences,
                    ref examined
                )
                : IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Term,
                    CodePurgerOriginal432Names.Rows,
                    differences,
                    ref examined
                );
        }
        File.WriteAllText(
            Path.Combine(output, CodePurgerOriginal432Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = CodePurgerOriginal432Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                OrderedProcessSignals = true,
                WholeOriginalInternalImageClaim = false,
                PhysicalNumericEqualityClaim = false,
                ProfileArity = 3,
                Projection = false,
                ApiOrFullContractPromotion = false
            })
        );
        CodePurgerOriginal432Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == CodePurgerOriginal432Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
