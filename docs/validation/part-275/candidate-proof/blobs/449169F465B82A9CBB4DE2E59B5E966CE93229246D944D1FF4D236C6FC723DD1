using System.Numerics;
using System.Buffers.Binary;
using System.Reflection;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class RuntimeSetNodeHostChecks
{
    internal static async Task Run()
    {
        await CompiledContracts();
        await AdmissionAndNotifications();
        await ReservedEpochAdmission(false);
        await ReservedEpochAdmission(true);
        await CapturedLocalBinding();
        await CreatorAndFailureReservations();
        await MetadataAndRegistryLockOrder();
        await ForeignDomain();
        await ForeignFactoryEnvironments();
        await NoNetworkReason();
        Console.WriteLine(RuntimeSetNodeHostNames.Status);
    }

    private static async Task CompiledContracts()
    {
        await using var runtime = new ProcessRuntime();
        new Parser(RuntimeSetNodeHostNames.CompiledSource).ParseModule().Register(runtime.Modules);
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await caller.InvokeAsync(async context =>
        {
            Check((await runtime.Modules.Call(
                context,
                RuntimeSetNodeHostNames.CompiledModule,
                RuntimeSetNodeHostNames.Bad,
                Term.A(RuntimeSetNodeHostNames.First),
                Term.I(0)
            )).Equals(Term.A(ErlangErrorReasons.BadArgument)));
            Check((await runtime.Modules.Call(context, RuntimeSetNodeHostNames.CompiledModule, RuntimeSetNodeHostNames.State)).Equals(Term.Tuple(Term.A(ErlangBooleanAtoms.False), Term.A(ProcessNodeDefaults.Local), Term.I(0))));
            Check((await runtime.Modules.Call(
                context,
                RuntimeSetNodeHostNames.CompiledModule,
                RuntimeSetNodeHostNames.Watch,
                Term.A(ErlangBooleanAtoms.True),
                new MapTerm([])
            )).Equals(Term.I(0)));

            return true;
        });
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        await caller.InvokeAsync(async context =>
        {
            Check((await runtime.Modules.Call(
                context,
                RuntimeSetNodeHostNames.CompiledModule,
                RuntimeSetNodeHostNames.Start,
                Term.A(RuntimeSetNodeHostNames.First),
                Term.I(RuntimeSetNodeHostParameters.Requested)
            )).Equals(Term.A(ErlangBooleanAtoms.True)));
            Check((await runtime.Modules.Call(context, RuntimeSetNodeHostNames.CompiledModule, RuntimeSetNodeHostNames.State)).Equals(Term.Tuple(
                Term.A(ErlangBooleanAtoms.True),
                Term.A(RuntimeSetNodeHostNames.First),
                Term.I(RuntimeSetNodeHostParameters.MinimumCreation)
            )));
            Check((await runtime.Modules.Call(
                context,
                RuntimeSetNodeHostNames.CompiledModule,
                RuntimeSetNodeHostNames.Bad,
                Term.A(RuntimeSetNodeHostNames.Next),
                Term.I(0)
            )).Equals(Term.A(ErlangErrorReasons.BadArgument)));
            Check(await Event(context, RuntimeSetNodeHostNames.NodeUp) is TupleTerm { Items.Count: 3 } up && up.Items[2].Equals(new MapTerm([])));

            return true;
        });
    }

    private static async Task AdmissionAndNotifications()
    {
        await using var runtime = new ProcessRuntime(RuntimeSetNodeHostNames.Configured);
        Check(runtime.ConfiguredNode == RuntimeSetNodeHostNames.Configured && runtime.Node == ProcessNodeDefaults.Local && !runtime.IsDistributionAlive);
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await caller.InvokeAsync(async context => await Reject(() => SetNode(context, RuntimeSetNodeHostNames.First, Term.I(RuntimeSetNodeHostParameters.Requested))));
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        await caller.InvokeAsync(async context =>
        {
            foreach (Term creation in new Term[] { Term.I(-1), new Integer(BigInteger.One << RuntimeNodeCreationHostParameters.FirstNonSmallShift), Term.A(ErlangBooleanAtoms.True), new FloatTerm(1) })
                await Reject(() => SetNode(context, RuntimeSetNodeHostNames.First, creation));
            await Reject(() => SetNode(context, ProcessNodeDefaults.Local, Term.I(0)));

            return true;
        });
        Pid retained = (await caller.Ready).Self;
        var stable = new Dictionary<Pid, Term> { [retained] = Nil.Value };
        Term mapOptions = new MapTerm([
            new(Term.A(RuntimeSetNodeHostNames.ConnectionId), Term.A(ErlangBooleanAtoms.True)),
            new(Term.A(RuntimeSetNodeHostNames.DownReason), Term.A(ErlangBooleanAtoms.True)),
            new(Term.A(RuntimeSetNodeHostNames.NodeType), Term.A(RuntimeSetNodeHostNames.All))
        ]);
        await observer.InvokeAsync(async context =>
        {
            Check((await Flag(context, Term.A(RuntimeSetNodeHostNames.Flag), true)).Equals(Term.I(0)));
            Check((await Flag(context, Term.A(RuntimeSetNodeHostNames.Flag), true)).Equals(Term.I(1)));
            Check((await Flag(context, Term.A(RuntimeSetNodeHostNames.Flag), false)).Equals(Term.I(2)));
            Check((await Flag(context, Term.A(RuntimeSetNodeHostNames.Flag), false)).Equals(Term.I(0)));
            Check((await Flag(context, Term.Tuple(Term.A(RuntimeSetNodeHostNames.Flag), mapOptions), true)).Equals(Term.I(0)));
            Check((await Flag(context, Term.Tuple(Term.A(RuntimeSetNodeHostNames.Flag), mapOptions), true)).Equals(Term.I(1)));
            Check((await Flag(
                context,
                Term.Tuple(
                    Term.A(RuntimeSetNodeHostNames.Flag),
                    new MapTerm([new(Term.A(RuntimeSetNodeHostNames.NodeType), Term.A(RuntimeSetNodeHostNames.Hidden))])
                ),
                true
            )).Equals(Term.I(0)));
            foreach (Term invalid in new Term[] { Term.List(Term.A(RuntimeSetNodeHostNames.All)), new MapTerm([new(Term.A(RuntimeSetNodeHostNames.List), Term.A(ErlangBooleanAtoms.False))]), new MapTerm([new(Term.A(RuntimeSetNodeHostNames.UnknownOption), Term.A(ErlangBooleanAtoms.True))]), new MapTerm([new(Term.A(RuntimeSetNodeHostNames.NodeType), Term.I(0))]) })
                await Reject(() => Flag(context, Term.Tuple(Term.A(RuntimeSetNodeHostNames.Flag), invalid), true));

            return true;
        });
        Check((await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.First, Term.I(RuntimeSetNodeHostParameters.Requested)))).Equals(Term.A(ErlangBooleanAtoms.True)));
        Check(runtime.IsDistributionAlive && retained.Node == RuntimeSetNodeHostNames.First && retained.Creation == RuntimeSetNodeHostParameters.MinimumCreation && stable.ContainsKey(retained));
        await caller.InvokeAsync(async context => await Reject(() => SetNode(context, RuntimeSetNodeHostNames.Next, Term.I(0))));
        await observer.InvokeAsync(async context =>
        {
            for (int index = 0; index < 2; index++)
                Check(await Event(context, RuntimeSetNodeHostNames.NodeUp) is TupleTerm { Items.Count: 3 } up && up.Items[2].Equals(new MapTerm([
                    new(Term.A(RuntimeSetNodeHostNames.ConnectionId), Term.A(RuntimeSetNodeHostNames.Undefined)),
                    new(Term.A(RuntimeSetNodeHostNames.NodeType), Term.A(RuntimeSetNodeHostNames.Visible))
                ])));
            Check(context.Mailbox.Count == 0);

            return true;
        });
        await kernel.DisposeAsync();
        Check(!runtime.IsDistributionAlive && retained.Node == ProcessNodeDefaults.Local && stable.ContainsKey(retained));
        await observer.InvokeAsync(async context =>
        {
            for (int index = 0; index < 2; index++)
                Check(await Event(context, RuntimeSetNodeHostNames.NodeDown) is TupleTerm { Items.Count: 3 } down && down.Items[2].Equals(new MapTerm([
                    new(Term.A(RuntimeSetNodeHostNames.ConnectionId), Term.A(RuntimeSetNodeHostNames.Undefined)),
                    new(Term.A(RuntimeSetNodeHostNames.DownReason), Term.A(RuntimeSetNodeHostNames.KernelTerminated)),
                    new(Term.A(RuntimeSetNodeHostNames.NodeType), Term.A(RuntimeSetNodeHostNames.Visible))
                ])));
            Check(context.Mailbox.Count == 0);

            return true;
        });
    }

    private static async Task ReservedEpochAdmission(bool compressed)
    {
        await using var runtime = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        FunctionTerm external = runtime.Functions.External(runtime, new(Term.A(ErlangModuleNames.Module), Term.A(ErlangModuleNames.Self), 0));
        Term input = Term.Tuple(
            external,
            new Pid(RuntimeSetNodeHostNames.First, RuntimeSetNodeHostParameters.ExternalPid, RuntimeSetNodeHostParameters.MinimumCreation),
            new BitString(new byte[RuntimeSetNodeHostParameters.CompressionPaddingBytes])
        );
        byte[] bytes = ExternalTermFormat.Encode(input, new EtfEncodingOptions { CompressionLevel = compressed ? RuntimeSetNodeHostParameters.CompressionLevel : 0 });
        Check(!compressed || bytes[1] == RuntimeSetNodeHostParameters.CompressedTag);
        Task<EtfDecodedTerm> decoding = Task.Run(() => runtime.ImportExternalTerm(
            bytes,
            new EtfDecodeOptions
            {
                ExternalFunctionFactory = (identity, safe) =>
                {
                    entered.TrySetResult();
                    Check(release.Wait(RuntimeSetNodeHostParameters.Deadline));

                    return runtime.Functions.External(runtime, identity, safe);
                }
            }
        ));
        try
        {
            await entered.Task.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
            GC.Collect();
            Check((await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.First, Term.I(RuntimeSetNodeHostParameters.Requested)))).Equals(Term.A(ErlangBooleanAtoms.True)));
            Check((await caller.InvokeAsync(async context => await context.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeSetNodeHostNames.SystemInfo,
                Term.A(RuntimeSetNodeHostNames.Creation)
            ))).Equals(Term.I(RuntimeSetNodeHostParameters.SkippedCreation)));
        }
        finally
        {
            release.Set();
        }
        EtfDecodedTerm result = await decoding.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
        Check(result.Term is TupleTerm tuple && tuple.Items[1] is Pid pid && pid.Node == RuntimeSetNodeHostNames.First && pid.Creation == RuntimeSetNodeHostParameters.MinimumCreation);
        await kernel.DisposeAsync();
        await using var successor = new RuntimeNodeFoundationHostActor(runtime);
        await successor.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.First, Term.I(RuntimeSetNodeHostParameters.MinimumCreation)));
        Check(runtime.Node == RuntimeSetNodeHostNames.First);
        Check((await caller.InvokeAsync(async context => await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            RuntimeSetNodeHostNames.SystemInfo,
            Term.A(RuntimeSetNodeHostNames.Creation)
        ))).Equals(Term.I(RuntimeSetNodeHostParameters.SkippedCreation)));
        GC.KeepAlive(result);
    }

    private static async Task CapturedLocalBinding()
    {
        await using var runtime = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.First, Term.I(RuntimeSetNodeHostParameters.MinimumCreation)));
        FunctionTerm external = runtime.Functions.External(runtime, new(Term.A(ErlangModuleNames.Module), Term.A(ErlangModuleNames.Self), 0));
        Term input = Term.Tuple(
            external,
            new Pid(RuntimeSetNodeHostNames.First, RuntimeSetNodeHostParameters.ExternalPid, RuntimeSetNodeHostParameters.MinimumCreation),
            new ReferenceTerm(RuntimeSetNodeHostNames.First, [1, 2, 3], RuntimeSetNodeHostParameters.MinimumCreation),
            new PortTerm(RuntimeSetNodeHostNames.First, RuntimeSetNodeHostParameters.ExternalPid, RuntimeSetNodeHostParameters.MinimumCreation)
        );
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        Task<EtfDecodedTerm> decoding = Task.Run(() => runtime.ImportExternalTerm(
            ExternalTermFormat.Encode(input),
            new EtfDecodeOptions
            {
                ExternalFunctionFactory = (identity, safe) =>
                {
                    entered.TrySetResult();
                    Check(release.Wait(RuntimeSetNodeHostParameters.Deadline));

                    return runtime.Functions.External(runtime, identity, safe);
                }
            }
        ));
        await entered.Task.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
        await kernel.DisposeAsync();
        await using var successor = new RuntimeNodeFoundationHostActor(runtime);
        await successor.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        try
        {
            await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.Next, Term.I(RuntimeSetNodeHostParameters.MinimumCreation)));
        }
        finally
        {
            release.Set();
        }
        TupleTerm result = (TupleTerm)(await decoding.WaitAsync(RuntimeSetNodeHostParameters.Deadline)).Term;
        Check(result.Items[1] is Pid pid && pid.Node == RuntimeSetNodeHostNames.Next);
        Check(result.Items[2] is ReferenceTerm reference && reference.Node == RuntimeSetNodeHostNames.Next);
        Check(result.Items[3] is PortTerm port && port.Node == RuntimeSetNodeHostNames.Next);
    }

    private static async Task CreatorAndFailureReservations()
    {
        await using var runtime = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        var identity = new LocalFunctionIdentity(
            Term.A(RuntimeSetNodeHostNames.FactoryModule),
            null,
            0,
            1,
            new BitString(new byte[RuntimeSetNodeHostParameters.UniqueBytes])
        );
        var function = new FunctionTerm(
            identity,
            0,
            [new Pid(
                RuntimeSetNodeHostNames.First,
                RuntimeSetNodeHostParameters.ExternalPid,
                RuntimeSetNodeHostParameters.SecondReservedCreation
            )],
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        byte[] bytes = ExternalTermFormat.Encode(
            function,
            new EtfEncodingOptions { FunctionOwner = new Pid(RuntimeSetNodeHostNames.First, RuntimeSetNodeHostParameters.ExternalPid, RuntimeSetNodeHostParameters.MinimumCreation) }
        );
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        Task<EtfDecodedTerm> pending = Task.Run(() => runtime.ImportExternalTerm(
            bytes,
            new EtfDecodeOptions
            {
                LocalFunctionFactory = (code, arity, environment) =>
                {
                    entered.TrySetResult();
                    Check(release.Wait(RuntimeSetNodeHostParameters.Deadline));

                    return runtime.Functions.Restore(
                        runtime,
                        new(
                            code.Module,
                            null,
                            code.Index,
                            code.OldUnique,
                            code.Unique,
                            code.OldIndex
                        ),
                        arity,
                        environment
                    );
                }
            }
        ));
        try
        {
            await entered.Task.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
            GC.Collect();
            await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.First, Term.I(RuntimeSetNodeHostParameters.Requested)));
            Check((await caller.InvokeAsync(async context => await context.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeSetNodeHostNames.SystemInfo,
                Term.A(RuntimeSetNodeHostNames.Creation)
            ))).Equals(Term.I(RuntimeSetNodeHostParameters.BothSkippedCreation)));
        }
        finally
        {
            release.Set();
        }
        EtfDecodedTerm decoded = await pending.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
        Check(decoded.Term is FunctionTerm restored && restored.Environment.Count == 1 && restored.Environment[0] is Pid retained && retained.Creation == RuntimeSetNodeHostParameters.SecondReservedCreation);
        await kernel.DisposeAsync();
        await using var successor = new RuntimeNodeFoundationHostActor(runtime);
        await successor.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        FunctionTerm external = runtime.Functions.External(runtime, new(Term.A(ErlangModuleNames.Module), Term.A(ErlangModuleNames.Self), 0));
        byte[] failing = ExternalTermFormat.Encode(Term.Tuple(
            external,
            new Pid(RuntimeSetNodeHostNames.Next, RuntimeSetNodeHostParameters.ExternalPid, RuntimeSetNodeHostParameters.MinimumCreation)
        ));
        bool failed = false;
        try
        {
            runtime.ImportExternalTerm(
                failing,
                new EtfDecodeOptions { ExternalFunctionFactory = (_, _) => throw new InvalidOperationException(RuntimeSetNodeHostNames.FactoryFailure) }
            );
        }
        catch (InvalidOperationException exception) when (exception.Message == RuntimeSetNodeHostNames.FactoryFailure)
        {
            failed = true;
        }
        Check(failed);
        await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.Next, Term.I(RuntimeSetNodeHostParameters.Requested)));
        Check((await caller.InvokeAsync(async context => await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            RuntimeSetNodeHostNames.SystemInfo,
            Term.A(RuntimeSetNodeHostNames.Creation)
        ))).Equals(Term.I(RuntimeSetNodeHostParameters.MinimumCreation)));
        GC.KeepAlive(decoded);
    }

    private static async Task MetadataAndRegistryLockOrder()
    {
        await using var runtime = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        var raw = new Pid(RuntimeSetNodeHostNames.First, RuntimeSetNodeHostParameters.ExternalPid, RuntimeSetNodeHostParameters.MinimumCreation);
        FunctionTerm external = new(
            new ExternalFunctionIdentity(Term.A(ErlangModuleNames.Module), Term.A(ErlangModuleNames.Self), 0),
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        using var gateHeld = new ManualResetEventSlim();
        using var factoryEntered = new ManualResetEventSlim();
        Task registryTask = Task.Run(() => runtime.Modules.Apply(registry =>
        {
            gateHeld.Set();
            Check(factoryEntered.Wait(RuntimeSetNodeHostParameters.Deadline));
            using RuntimeExternalTermReservation lease = runtime.ReserveExternalTerm(raw);
            registry.PublishModuleCode(
                RuntimeSetNodeHostNames.HostModule,
                new(
                    [],
                    [],
                    new BitString(new byte[RuntimeSetNodeHostParameters.UniqueBytes]),
                    Term.Tuple(raw)
                )
            );
        }));
        Check(gateHeld.Wait(RuntimeSetNodeHostParameters.Deadline));
        Task<EtfDecodedTerm> importing = Task.Run(() => runtime.ImportExternalTerm(
            ExternalTermFormat.Encode(Term.Tuple(external, raw)),
            new EtfDecodeOptions
            {
                ExternalFunctionFactory = (identity, safe) =>
                {
                    factoryEntered.Set();
                    Check(runtime.Modules.HasExport(identity.Module.Name, identity.Function.Name, identity.Arity));

                    return runtime.Functions.External(runtime, identity, safe);
                }
            }
        ));
        await registryTask.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
        EtfDecodedTerm imported = await importing.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
        Assembly assembly = typeof(RuntimeSetNodeHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(
            BeamPreparedHostNames.ResourceSeparator + BeamPreparedHostNames.Constant + BeamPreparedHostNames.BeamExtension,
            StringComparison.Ordinal
        ));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var input = new MemoryStream();
        stream.CopyTo(input);
        var second = new Pid(
            RuntimeSetNodeHostNames.First,
            RuntimeSetNodeHostParameters.ExternalPid,
            RuntimeSetNodeHostParameters.SecondReservedCreation
        );
        byte[] metadata = BeamMetadataModeledBytes.WithAttributes(input.ToArray(), Term.Tuple(second));
        byte[] literal = ExternalTermFormat.Encode(second);
        byte[] table = new byte[RuntimeSetNodeHostParameters.TableHeaderBytes + literal.Length];
        BinaryPrimitives.WriteInt32BigEndian(table.AsSpan(RuntimeSetNodeHostParameters.WordBytes), 1);
        BinaryPrimitives.WriteInt32BigEndian(table.AsSpan(RuntimeSetNodeHostParameters.WordBytes * 2), literal.Length);
        literal.CopyTo(table, RuntimeSetNodeHostParameters.TableHeaderBytes);
        byte[] combined = BeamMetadataModeledBytes.WithChunk(metadata, RuntimeSetNodeHostNames.LiteralChunk, table);
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(combined);
        Check(!prepared.IsConsumed);
        await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.First, Term.I(RuntimeSetNodeHostParameters.Requested)));
        Check((await caller.InvokeAsync(async context => await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            RuntimeSetNodeHostNames.SystemInfo,
            Term.A(RuntimeSetNodeHostNames.Creation)
        ))).Equals(Term.I(RuntimeSetNodeHostParameters.BothSkippedCreation)));
        Check(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        GC.KeepAlive(imported);
        GC.KeepAlive(prepared);
    }

    private static async Task NoNetworkReason()
    {
        await using var runtime = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
 {
     runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

     return true;
 });
        await caller.InvokeAsync(async context =>
        {
            Check((await Flag(
                context,
                Term.Tuple(
                    Term.A(RuntimeSetNodeHostNames.Flag),
                    new MapTerm([
                new(Term.A(RuntimeSetNodeHostNames.DownReason), Term.A(ErlangBooleanAtoms.True))
            ])
                ),
                true
            )).Equals(Term.I(0)));
            await SetNode(context, RuntimeSetNodeHostNames.First, Term.I(RuntimeSetNodeHostParameters.Requested));
            Check(await Event(context, RuntimeSetNodeHostNames.NodeUp) is TupleTerm { Items.Count: 3 });

            return true;
        });
        await caller.Invoke(context =>
        {
            runtime.Exit(context, kernel.Process.Pid, Term.A(RuntimeSetNodeHostNames.NoNetwork));

            return true;
        });
        await kernel.Process.Completion.WaitAsync(RuntimeSetNodeHostParameters.Deadline);
        Check(!runtime.IsDistributionAlive && runtime.Node == ProcessNodeDefaults.Local);
        await caller.InvokeAsync(async context =>
        {
            Check(await Event(context, RuntimeSetNodeHostNames.NodeDown) is TupleTerm { Items.Count: 3 } down && down.Items[2].Equals(new MapTerm([
                new(Term.A(RuntimeSetNodeHostNames.DownReason), Term.A(RuntimeSetNodeHostNames.NoNetwork))
            ])));

            return true;
        });
    }

    private static async Task ForeignDomain()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(foreign);
        Pid foreignPid = (await actor.Ready).Self;
        bool rejected = false;
        try
        {
            runtime.ReserveExternalTerm(Term.Tuple(new MapTerm([new(Term.A(RuntimeSetNodeHostNames.First), foreignPid)])));
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Check(rejected);
    }

    private static async Task ForeignFactoryEnvironments()
    {
        await using var foreign = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(foreign);
        PortTerm port = (PortTerm)await actor.InvokeAsync(async context => await foreign.Modules.Call(
            context,
            ErlangModuleNames.Module,
            PortNames.Open,
            Term.Tuple(Term.A(PortDescriptorNames.Kind), Term.I(0), Term.I(1)),
            Term.List(Term.A(PortNames.WriteOnlyOption))
        ));
        try
        {
            foreach (Term identifier in new Term[] { (await actor.Ready).Self, foreign.MakeReference(), port })
            {
                await RejectForeignFactoryEnvironment(identifier, false);
                await RejectForeignFactoryEnvironment(identifier, true);
            }
        }
        finally
        {
            await actor.InvokeAsync(async context => await foreign.Modules.Call(
                context,
                ErlangModuleNames.Module,
                PortNames.Close,
                port
            ));
        }
    }

    private static async Task RejectForeignFactoryEnvironment(Term identifier, bool localFactory)
    {
        await using var runtime = new ProcessRuntime();
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await kernel.Invoke(context =>
        {
            runtime.Register(RuntimeSetNodeHostNames.Kernel, context.Self);

            return true;
        });
        var identity = new LocalFunctionIdentity(
            Term.A(RuntimeSetNodeHostNames.FactoryModule),
            null,
            0,
            1,
            new BitString(new byte[RuntimeSetNodeHostParameters.UniqueBytes])
        );
        var injected = new FunctionTerm(
            identity,
            0,
            [Term.Tuple(new MapTerm([new(Term.I(0), identifier)]))],
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        RejectImportedEnvironment(
            runtime,
            injected,
            identity,
            localFactory
        );
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await caller.InvokeAsync(async context => await SetNode(context, RuntimeSetNodeHostNames.Next, Term.I(RuntimeSetNodeHostParameters.Requested)));
        Check((await caller.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            RuntimeSetNodeHostNames.SystemInfo,
            Term.A(RuntimeSetNodeHostNames.Creation)
        ))).Equals(Term.I(RuntimeSetNodeHostParameters.MinimumCreation)));
        GC.KeepAlive(injected);
        GC.KeepAlive(identifier);
    }

    private static void RejectImportedEnvironment(
        ProcessRuntime runtime,
        FunctionTerm injected,
        LocalFunctionIdentity identity,
        bool localFactory
    )
    {
        var owner = new Pid(RuntimeSetNodeHostNames.Next, RuntimeSetNodeHostParameters.ExternalPid, RuntimeSetNodeHostParameters.MinimumCreation);
        Term source = localFactory
            ? new FunctionTerm(
                identity,
                0,
                [],
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            )
            : Term.Tuple(
                new FunctionTerm(
                    new ExternalFunctionIdentity(Term.A(ErlangModuleNames.Module), Term.A(ErlangModuleNames.Self), 0),
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                ),
                owner
            );
        byte[] bytes = ExternalTermFormat.Encode(source, new EtfEncodingOptions { FunctionOwner = owner });
        bool rejected = false;
        int externalCalls = 0;
        int localCalls = 0;
        try
        {
            runtime.ImportExternalTerm(
                bytes,
                new EtfDecodeOptions
                {
                    ExternalFunctionFactory = (_, _) =>
                    {
                        externalCalls++;

                        return injected;
                    },
                    LocalFunctionFactory = (_, _, _) =>
                    {
                        localCalls++;

                        return injected;
                    }
                }
            );
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            rejected = true;
        }
        Check(rejected && externalCalls == (localFactory ? 0 : 1) && localCalls == (localFactory ? 1 : 0));
    }

    private static ValueTask<Term> SetNode(ProcessContext context, string node, Term creation) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        RuntimeSetNodeHostNames.SetNode,
        Term.A(node),
        creation
    );

    private static ValueTask<Term> Flag(ProcessContext context, Term flag, bool enable) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ErlangModuleNames.ProcessFlag,
        flag,
        Term.A(enable ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
    );

    private static ValueTask<Term?> Event(ProcessContext context, string name) => context.ReceiveAsync<Term>(value => value is TupleTerm tuple && tuple.Items[0].Equals(Term.A(name)) ? value : null, RuntimeSetNodeHostParameters.Deadline);

    private static async ValueTask<Term> Reject(Func<ValueTask<Term>> action)
    {
        try
        {
            await action();
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return Term.A(ErlangBooleanAtoms.True);
        }
        throw new InvalidOperationException(RuntimeSetNodeHostNames.Failure);
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(RuntimeSetNodeHostNames.Failure);
    }
}
