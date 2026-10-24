using System.Text;
using System.Text.Json;
using Erlang;
namespace Erlang.Tests;

internal static class StandardBootOriginal412Checks
{
    internal static async Task Run(
        string output,
        string admission,
        bool includeCollectors = false,
        bool includeWholePersistent = false
    )
    {
        Directory.CreateDirectory(output);
        var observedFailures = new System.Collections.Concurrent.ConcurrentQueue<string>();
        EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> observeFailure = (_, observation) =>
        {
            if (observation.Exception is not OperationCanceledException
                && observedFailures.Count < StandardBootOriginal412Names.MaximumDiagnosticFailures)
                observedFailures.Enqueue(observation.Exception.ToString()
                    + (observation.Exception is ErlangException guest
                        ? StandardBootOriginal412Names.GuestTracePrefix + guest.StackTraceTerm : string.Empty));
        };
        AppDomain.CurrentDomain.FirstChanceException += observeFailure;
        var errors = new List<object>();
        var captured = new Dictionary<string, (Term Value, byte[] Bytes)>();
        ProcessRuntime? runtime = null;
        StandardBootOriginal412Actor? actor = null;
        bool started = false, initIdentity = false;
        StandardBootCollectors415Proof? collectors = null;
        StandardBootPersistent417Proof? persistent = null;
        object[] sourceOwners = [];
        try
        {
            using var receipt = JsonDocument.Parse(File.ReadAllBytes(admission));
            string installed = receipt.RootElement.GetProperty(StandardBootOriginal412Names.InstalledRoot).GetString()!;
            foreach (string boot in new[] { StandardBootOriginal412Names.BootFile, StandardBootOriginal412Names.StartClean })
                StandardBootOriginal412Access.Require(File.ReadAllBytes(Path.Combine(installed, StandardBootOriginal412Names.Bin, boot))
                    .AsSpan().SequenceEqual(StandardBootOriginal412Access.Resource(boot)));
            string ebin = Path.Combine(output, StandardBootOriginal412Names.Ebin);
            Directory.CreateDirectory(ebin);
            foreach (string worker in new[] { StandardBootOriginal412Names.Subject, StandardBootOriginal412Names.Observer })
                File.WriteAllBytes(
                    Path.Combine(ebin, worker + StandardBootOriginal412Names.Beam),
                    StandardBootOriginal412Access.Resource(worker + StandardBootOriginal412Names.Beam)
                );
            runtime = await StandardBootOriginal412Runtime.Create(admission, includeCollectors, includeWholePersistent);
            actor = new StandardBootOriginal412Actor(runtime);
            ProcessRuntime owner = runtime;
            StandardBootOriginal412Actor caller = actor;
            _ = await caller.InvokeAsync(async context => await owner.Modules.Call(context,
                StandardBootOriginal412Names.PrimFile, StandardBootOriginal412Names.OnLoad));
            string[] arguments = [StandardBootOriginal412Names.RootOption, installed,
                StandardBootOriginal412Names.BinOption, Path.Combine(installed, StandardBootOriginal412Names.ErtsBin),
                StandardBootOriginal412Names.BootOption, Path.Combine(installed, StandardBootOriginal412Names.Bin, StandardBootOriginal412Names.BootBase),
                StandardBootOriginal412Names.ModeOption, StandardBootOriginal412Names.Interactive,
                StandardBootOriginal412Names.NoShell, StandardBootOriginal412Names.NoInput,
                StandardBootOriginal412Names.PathOption, ebin];
            using var deadline = new CancellationTokenSource(StandardBootOriginal412Names.BootDeadline);
            RuntimeOtpBootSession session = await owner.StartOtpBootAsync(
                arguments.Select(argument => new BitString(Encoding.UTF8.GetBytes(argument))), deadline.Token);
            started = !session.Completion.IsCompleted;
            Term registered = await caller.InvokeAsync(async context => await owner.Modules.Call(
                context,
                StandardBootOriginal412Names.Erts,
                StandardBootOriginal412Names.Whereis,
                Term.A(StandardBootOriginal412Names.Init)
            ));
            initIdentity = registered.Equals(session.InitializationProcess);
            StandardBootOriginal412Access.Require(started && initIdentity);
            if (includeCollectors)
                collectors = await StandardBootCollectors415Proof.Capture(owner);
            if (includeWholePersistent)
                persistent = StandardBootPersistent417Proof.Capture(owner);

            async Task Capture(string file, Func<ProcessContext, Task<Term>> route)
            {
                try
                {
                    Term value = await caller.InvokeAsync(route);
                    BitString wire = (BitString)await caller.InvokeAsync(async context => await owner.Modules.Call(
                        context,
                        StandardBootOriginal412Names.Erts,
                        StandardBootOriginal412Names.Encode,
                        value
                    ));
                    byte[] bytes = wire.ToArray();
                    captured.Add(file, (value, bytes));
                    File.WriteAllBytes(Path.Combine(output, StandardBootOriginal412Names.ActualPrefix + file), bytes);
                    File.WriteAllText(
                        Path.Combine(output, StandardBootOriginal412Names.ActualPrefix + file + StandardBootOriginal412Names.Json),
                        JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(value))
                    );
                }
                catch (Exception error)
                {
                    errors.Add(new { Route = file, Failure = error.ToString() });
                }
                finally
                {
                    File.WriteAllText(
                        Path.Combine(output, StandardBootOriginal412Names.DiagnosticFailures),
                        JsonSerializer.Serialize(observedFailures.ToArray())
                    );
                }
            }
            await Capture(
                StandardBootOriginal412Names.Values,
                async context => await owner.Modules.Call(context,
                StandardBootOriginal412Names.Observer, StandardBootOriginal412Names.Run)
            );
            await Capture(
                StandardBootOriginal412Names.StackValues,
                async context => await owner.Modules.Call(context,
                StandardBootOriginal412Names.Observer, StandardBootOriginal412Names.Stacks)
            );
            await Capture(
                StandardBootOriginal412Names.Proof,
                async context =>
            {
                Term status = await owner.Modules.Call(context, StandardBootOriginal412Names.Init, StandardBootOriginal412Names.GetStatus);
                Term script = await owner.Modules.Call(context, StandardBootOriginal412Names.Init, StandardBootOriginal412Names.ScriptId);

                async Task<Term> IsRegisteredPid(string name)
                {
                    Term process = await owner.Modules.Call(
                        context,
                        StandardBootOriginal412Names.Erts,
                        StandardBootOriginal412Names.Whereis,
                        Term.A(name)
                    );

                    return await owner.Modules.Call(
                        context,
                        StandardBootOriginal412Names.Erts,
                        StandardBootOriginal412Names.IsPid,
                        process
                    );
                }

                return Term.Tuple(
                    status,
                    script,
                    await IsRegisteredPid(StandardBootOriginal412Names.Init),
                    await IsRegisteredPid(StandardBootOriginal412Names.ApplicationController),
                    await IsRegisteredPid(StandardBootOriginal412Names.KernelSupervisor)
                );
            }
            );
            sourceOwners = owner.Modules.Exports.Where(export => export.Module == StandardBootOriginal412Names.Init
                    || export.Module == StandardBootOriginal412Names.Observer || export.Module == StandardBootOriginal412Names.Subject)
                .Select(export => (object)new
                {
                    export.Module,
                    export.Function,
                    export.Arity,
                    SourceOwned = owner.Modules.SourceOwner(export.Module, export.Function, export.Arity) is PreparedBeamImage
                }).ToArray();
        }
        catch (Exception error)
        {
            errors.Add(new { PreparationOrBoot = error.ToString() });
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= observeFailure;
            File.WriteAllText(
                Path.Combine(output, StandardBootOriginal412Names.DiagnosticFailures),
                JsonSerializer.Serialize(observedFailures.ToArray())
            );
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { ActorCleanup = error.ToString() });
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(new { RuntimeCleanup = error.ToString() });
                }
        }
        if (includeCollectors)
            try
            {
                if (!StandardBootCollectors415Proof.Complete(output, runtime, collectors))
                    errors.Add(new { CollectorLifecycle = StandardBootCollectors415Names.Failure });
            }
            catch (Exception error)
            {
                errors.Add(new { CollectorLifecycle = error.ToString() });
            }
        if (includeWholePersistent)
            try
            {
                if (!StandardBootPersistent417Proof.Complete(output, persistent))
                    errors.Add(new { PersistentLifecycle = StandardBootPersistent417Names.Failure });
            }
            catch (Exception error)
            {
                errors.Add(new { PersistentLifecycle = error.ToString() });
            }
        // Expected observations remain passive and are decoded only after all execution and cleanup.
        var differences = new List<object>();
        var whole = new Dictionary<string, bool>();
        var raw = new Dictionary<string, bool>();
        int examined = 0, equal = 0;
        foreach (string file in new[] { StandardBootOriginal412Names.Values, StandardBootOriginal412Names.StackValues, StandardBootOriginal412Names.Proof })
        {
            byte[] bytes = StandardBootOriginal412Access.Resource(file);
            Term expected = OwnedFunctionOriginal407Terms.Decode(bytes);
            File.WriteAllBytes(Path.Combine(output, StandardBootOriginal412Names.ExpectedPrefix + file), bytes);
            File.WriteAllText(
                Path.Combine(output, StandardBootOriginal412Names.ExpectedPrefix + file + StandardBootOriginal412Names.Json),
                JsonSerializer.Serialize(OwnedFunctionOriginal407Terms.Typed(expected))
            );
            bool present = captured.TryGetValue(file, out var actual);
            whole[file] = present && expected.Equals(actual.Value);
            raw[file] = present && bytes.AsSpan().SequenceEqual(actual.Bytes);
            if (!present) continue;
            if (file == StandardBootOriginal412Names.Values)
                equal += IoPrintableOriginal404Comparison.Rows(
                    expected,
                    actual.Value,
                    file,
                    StandardBootOriginal412Names.Rows,
                    differences,
                    ref examined
                );
            else if (file == StandardBootOriginal412Names.StackValues)
                equal += IoPrintableOriginal404Comparison.Profiles(
                    expected,
                    actual.Value,
                    StandardBootOriginal412Names.Rows,
                    differences,
                    ref examined
                );
            else
            {
                examined++;
                if (whole[file]) equal++;
                else differences.Add(new
                {
                    Scope = file,
                    Original = OwnedFunctionOriginal407Terms.Typed(expected),
                    Actual = OwnedFunctionOriginal407Terms.Typed(actual.Value)
                });
            }
        }
        File.WriteAllText(
            Path.Combine(output, StandardBootOriginal412Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Expected = StandardBootOriginal412Names.WholeRows,
                Examined = examined,
                Equal = equal,
                WholeTerms = whole,
                WholeRawEtf = raw,
                Errors = errors,
                Differences = differences,
                ActualNotifyWhenStarted = started,
                ActualRegisteredInitMatchesSession = initIdentity,
                SourceOwners = sourceOwners,
                ProfileArity = 3,
                ExpectedDecodedAfterCleanup = true,
                SelfHaltingCaptureHarnessOmitted = true,
                ProofUsesRealQueries = true,
                Projection = false,
                StrictWholeLibraryClosureClaim = false,
                ApiOrFullContractPromotion = false
            })
        );
        StandardBootOriginal412Access.Require(errors.Count == 0 && differences.Count == 0
            && examined == StandardBootOriginal412Names.WholeRows && equal == examined
            && whole.Values.All(value => value) && raw.Values.All(value => value));
    }
}
