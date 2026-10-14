using Erlang;
using System.Reflection;

namespace Erlang.Tests;

internal static class RuntimeNodeIdentityFoundationHostChecks
{
    internal static async Task Run()
    {
        int checks = 0;
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Pid pid = actor.Process.Pid;
        ReferenceTerm reference = runtime.MakeReference();
        ResourceReferenceTerm resource = runtime.ReferenceResources.CreateBinary(new byte[] { 0, 128, 255 });
        ReferenceTerm alias = await actor.Invoke(context => runtime.CreateAlias(context, false, false));
        PortTerm port = (PortTerm)await Decode(ExternalTermFormat.Encode(new PortTerm(runtime.Node, 87)));
        Term terms = Term.Tuple(
            pid,
            reference,
            resource,
            alias,
            port
        );
        var map = new MapTerm(new[] { new KeyValuePair<Term, Term>(pid, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)), new KeyValuePair<Term, Term>(reference, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)) });
        var dictionary = new Dictionary<Term, int> { [pid] = 1, [reference] = 2, [resource] = 3, [alias] = 4, [port] = 5 };
        var foreignReference = new ReferenceTerm(RuntimeNodeIdentityFoundationHostNames.ForeignNode, reference.Words, 7);
        var foreignPid = new Pid(RuntimeNodeIdentityFoundationHostNames.ForeignNode, pid.Id, 7);
        var mixedBindings = new[]
        {
            new KeyValuePair<Term, Term>(reference, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)),
            new KeyValuePair<Term, Term>(foreignReference, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)),
            new KeyValuePair<Term, Term>(pid, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)),
            new KeyValuePair<Term, Term>(foreignPid, Term.A(RuntimeNodeIdentityFoundationHostNames.Key))
        };
        var mixed = new MapTerm(mixedBindings);
        var referenceBindings = mixedBindings.Take(2).ToArray();
        var pidBindings = mixedBindings.Skip(2).ToArray();
        var referenceMap = new MapTerm(referenceBindings);
        var pidMap = new MapTerm(pidBindings);
        CheckMapFamily(referenceMap, referenceBindings, true);
        CheckMapFamily(pidMap, pidBindings, true);
        CheckMapFamily(mixed, mixedBindings, true);
        Term[] mixedNativeHashes = await MixedMapHashes();
        int mixedHash = mixed.GetHashCode();
        var mapDictionary = new Dictionary<Term, int> { [mixed] = 73 };
        var nested = new MapTerm(new[] { new KeyValuePair<Term, Term>(mixed, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)) });
        int nestedHash = nested.GetHashCode();
        var nestedDictionary = new Dictionary<Term, int> { [nested] = 91 };
        int[] hashes = ((TupleTerm)terms).Items.Select(term => term.GetHashCode()).ToArray();
        Term[] nativeHashes = await actor.InvokeAsync(async context =>
        {
            var values = new List<Term>();
            foreach (Term term in ((TupleTerm)terms).Items)
                values.Add(await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    RuntimeNodeIdentityFoundationHostNames.Phash2,
                    term
                ));

            return values.ToArray();
        });
        byte[] standardBefore = await Encode(terms, false);
        byte[] localBefore = await Encode(terms, true);
        var occupied = new ReferenceTerm(RuntimeNodeIdentityFoundationHostNames.FirstNode, new uint[] { 173, 29, 53 }, 4);
        Term retained = await Decode(ExternalTermFormat.Encode(occupied));
        var select = typeof(ProcessRuntime).GetMethod(RuntimeNodeIdentityFoundationHostNames.SelectMethod, BindingFlags.Instance | BindingFlags.NonPublic)!;
        Check((uint)select.Invoke(runtime, new object[] { RuntimeNodeIdentityFoundationHostNames.FirstNode, (uint)0 })! == 5);
        Check((uint)select.Invoke(runtime, new object[] { RuntimeNodeIdentityFoundationHostNames.SecondNode, (uint)0 })! == 4);
        object identity = typeof(ProcessRuntime).GetProperty(RuntimeNodeIdentityFoundationHostNames.LocalProperty, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        MethodInfo publish = identity.GetType().GetMethod(RuntimeNodeIdentityFoundationHostNames.PublishMethod, BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var epoch in new[] { (RuntimeNodeIdentityFoundationHostNames.FirstNode, (uint)17), (ProcessNodeDefaults.Local, (uint)0), (RuntimeNodeIdentityFoundationHostNames.FirstNode, (uint)18), (RuntimeNodeIdentityFoundationHostNames.SecondNode, (uint)29) })
        {
            publish.Invoke(identity, new object[] { epoch.Item1, epoch.Item2 });
            Check(runtime.Node == epoch.Item1 && runtime.NodeAtom.Name == epoch.Item1);
            Check(((TupleTerm)terms).Items.All(term => term switch
            {
                Pid value => value.Node == epoch.Item1 && value.Creation == epoch.Item2,
                ReferenceTerm value => value.Node == epoch.Item1 && value.Creation == epoch.Item2,
                PortTerm value => value.Node == epoch.Item1 && value.Creation == epoch.Item2,
                _ => false
            }));
            Check(((TupleTerm)terms).Items.Select(term => term.GetHashCode()).SequenceEqual(hashes));
            Check(dictionary[pid] == 1 && dictionary[reference] == 2 && dictionary[resource] == 3 && dictionary[alias] == 4 && dictionary[port] == 5);
            Check(map.Get(pid).Equals(Term.A(RuntimeNodeIdentityFoundationHostNames.Key)) && map.Get(reference).Equals(Term.A(RuntimeNodeIdentityFoundationHostNames.Key)));
            var rebuilt = new MapTerm(mixedBindings);
            bool expectedEqual = epoch.Item1 != RuntimeNodeIdentityFoundationHostNames.FirstNode;
            CheckMapFamily(referenceMap, referenceBindings, expectedEqual);
            CheckMapFamily(pidMap, pidBindings, expectedEqual);
            CheckMapFamily(mixed, mixedBindings, expectedEqual);
            Check((await MixedMapHashes()).SequenceEqual(mixedNativeHashes));
            Check(mixed.Equals(rebuilt) == expectedEqual, RuntimeNodeIdentityFoundationHostNames.RetainedMapEquality);
            Check(TermOrder.Compare(mixed, rebuilt, true) == (expectedEqual ? 0 : 1));
            Check(mixed.GetHashCode() == rebuilt.GetHashCode() && mixed.GetHashCode() == mixedHash);
            Check(mapDictionary.TryGetValue(rebuilt, out int mapValue) == expectedEqual);
            Check(!expectedEqual || mapValue == 73);
            var rebuiltNested = new MapTerm(new[] { new KeyValuePair<Term, Term>(rebuilt, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)) });
            Check(nested.Equals(rebuiltNested) == expectedEqual && nested.GetHashCode() == nestedHash);
            Check(nestedDictionary.TryGetValue(rebuiltNested, out int nestedValue) == expectedEqual);
            Check(!expectedEqual || nestedValue == 91);
            var equalCurrent = new MapTerm(new[] { new KeyValuePair<Term, Term>(pid, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)), new KeyValuePair<Term, Term>(new Pid(epoch.Item1, pid.Id, epoch.Item2), Term.A(RuntimeNodeIdentityFoundationHostNames.Key)) });
            Check(equalCurrent.Entries.Count == 1);
            Check(runtime.IsAlive(pid));
            Term[] currentHashes = await actor.InvokeAsync(async context =>
            {
                var values = new List<Term>();
                foreach (Term term in ((TupleTerm)terms).Items)
                    values.Add(await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        RuntimeNodeIdentityFoundationHostNames.Phash2,
                        term
                    ));

                return values.ToArray();
            });
            Check(currentHashes.SequenceEqual(nativeHashes));
            byte[] standard = await Encode(terms, false);
            byte[] local = await Encode(terms, true);
            Check(local.SequenceEqual(localBefore));
            var restoredLocal = (TupleTerm)await Decode(localBefore);
            Check(restoredLocal.Equals(terms));
            Check(restoredLocal.Items[2] is ResourceReferenceTerm);
            Check(((TupleTerm)await Decode(standard)).Equals(terms));
            var oldStandard = (TupleTerm)await Decode(standardBefore);
            Check(oldStandard.Equals(terms) == (epoch.Item1 == ProcessNodeDefaults.Local && epoch.Item2 == 0));
            Check(dictionary.TryGetValue(new Pid(epoch.Item1, pid.Id, epoch.Item2), out int found) && found == 1);
            runtime.Send(alias, Term.A(RuntimeNodeIdentityFoundationHostNames.Sent));
            Term? received = await actor.InvokeAsync(async context => await context.ReceiveAsync<Term>(
                message => message.Equals(Term.A(RuntimeNodeIdentityFoundationHostNames.Sent)) ? message : null,
                RuntimeNodeIdentityFoundationHostNames.Deadline
            ));
            Check(received is not null);
        }
        Check(await actor.Invoke(context => runtime.Unalias(context, alias)));
        Check(!await actor.Invoke(context => runtime.Unalias(context, alias)));
        using (var lease = runtime.ReferenceResources.AcquireBinary(resource))
            Check(lease is not null && lease.Value.Equals(new BitString(new byte[] { 0, 128, 255 })));
        await CheckCapturedMapEncoding();
        await CheckCapturedDecode(false);
        await CheckCapturedDecode(true);
        var equalPid = new Pid(runtime.Node, pid.Id, pid.Creation);
        for (int warm = 0; warm < 256; warm++)
            Check(pid.Equals(equalPid));
        long beforeComparison = GC.GetAllocatedBytesForCurrentThread();
        for (int sample = 0; sample < 1024; sample++)
            Check(pid.Equals(equalPid));
        long comparisonBytes = GC.GetAllocatedBytesForCurrentThread() - beforeComparison;
        Console.WriteLine(string.Format(RuntimeNodeIdentityFoundationHostNames.AllocationStatus, comparisonBytes));
        GC.KeepAlive(retained);
        Console.WriteLine(string.Format(RuntimeNodeIdentityFoundationHostNames.ControlStatus, checks));

        async Task<byte[]> Encode(Term value, bool local)
        {
            Term encoded = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeNodeIdentityFoundationHostNames.Encode,
                value,
                local ? Term.List(Term.A(RuntimeNodeIdentityFoundationHostNames.Local)) : Nil.Value
            ));

            return ((BitString)encoded).ToArray();
        }

        async Task<Term> Decode(byte[] bytes)
        {
            return await actor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeNodeIdentityFoundationHostNames.Decode,
                new BitString(bytes)
            ));
        }

        void Check(bool condition, string? label = null)
        {
            if (!condition)
                throw new Exception(label ?? RuntimeNodeIdentityFoundationHostNames.Failure);
            checks++;
        }

        async Task CheckCapturedDecode(bool local)
        {
            byte[] bytes = await Encode(terms, local);
            object snapshot = identity.GetType().GetProperty(RuntimeNodeIdentityFoundationHostNames.SnapshotProperty, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(identity)!;
            string node = runtime.Node;
            uint creation = pid.Creation;
            MethodInfo restorePid = typeof(ProcessRuntime).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(method => method.Name == RuntimeNodeIdentityFoundationHostNames.RehydratePid && method.GetParameters().Length == 2);
            MethodInfo restorePort = typeof(ProcessRuntime).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(method => method.Name == RuntimeNodeIdentityFoundationHostNames.RehydratePort && method.GetParameters().Length == 2);
            MethodInfo restoreReference = typeof(RuntimeReferenceResources).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(method => method.Name == RuntimeNodeIdentityFoundationHostNames.RehydrateReference && method.GetParameters().Length == 2);
            uint seed = (uint)typeof(ProcessRuntime).GetProperty(RuntimeNodeIdentityFoundationHostNames.SeedProperty, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
            string destination = local ? RuntimeNodeIdentityFoundationHostNames.SecondNode : RuntimeNodeIdentityFoundationHostNames.RaceNode;
            uint destinationCreation = local ? (uint)31 : (uint)30;
            bool moved = false;
            var options = new EtfDecodeOptions
            {
                LocalNode = node,
                LocalCreation = creation,
                LocalSeed = seed,
                PidFactory = value =>
                {
                    if (!moved)
                    {
                        publish.Invoke(identity, new object[] { destination, destinationCreation });
                        moved = true;
                    }

                    return (Pid)restorePid.Invoke(runtime, new[] { (object)value, snapshot })!;
                },
                PortFactory = value => (PortTerm)restorePort.Invoke(runtime, new[] { (object)value, snapshot })!,
                ReferenceFactory = value => (ReferenceTerm)restoreReference.Invoke(runtime.ReferenceResources, new[] { (object)value, snapshot })!
            };
            typeof(EtfDecodeOptions).GetProperty(RuntimeNodeIdentityFoundationHostNames.DecodeOwner, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(options, identity);
            var restored = (TupleTerm)ExternalTermFormat.DecodeOne(bytes, options).Term;
            Check(moved && runtime.Node == destination);
            Check(restored.Equals(terms));
            Check(restored.Items[2] is ResourceReferenceTerm);
            Check(((Pid)restored.Items[0]).Node == destination && ((PortTerm)restored.Items[4]).Node == destination);
        }

        async Task CheckCapturedMapEncoding()
        {
            publish.Invoke(identity, new object[] { RuntimeNodeIdentityFoundationHostNames.FirstNode, (uint)17 });
            object snapshot = identity.GetType().GetProperty(RuntimeNodeIdentityFoundationHostNames.SnapshotProperty, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(identity)!;
            uint seed = (uint)typeof(ProcessRuntime).GetProperty(RuntimeNodeIdentityFoundationHostNames.SeedProperty, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
            var options = new EtfEncodingOptions
            {
                LocalNode = runtime.Node,
                LocalCreation = pid.Creation,
                LocalSeed = seed,
                FunctionOwner = runtime.InitializationPid
            };
            typeof(EtfEncodingOptions).GetProperty(RuntimeNodeIdentityFoundationHostNames.DecodeOwner, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(options, identity);
            typeof(EtfEncodingOptions).GetProperty(RuntimeNodeIdentityFoundationHostNames.EncodingSnapshot, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(options, snapshot);
            byte[] before = ExternalTermFormat.Encode(mixed, options);
            long expectedSize = ExternalTermFormat.EncodedSize(mixed, options);
            publish.Invoke(identity, new object[] { RuntimeNodeIdentityFoundationHostNames.SecondNode, (uint)29 });
            byte[] after = ExternalTermFormat.Encode(mixed, options);
            Check(before.SequenceEqual(after));
            Check(after.Length == expectedSize);
            Check(mixed.Equals(new MapTerm(mixedBindings)));
            await Task.CompletedTask;
        }

        void CheckMapFamily(MapTerm retainedMap, KeyValuePair<Term, Term>[] bindings, bool expectedEqual)
        {
            var reconstructed = new MapTerm(bindings);
            Check(retainedMap.Equals(reconstructed) == expectedEqual);
            Check(retainedMap.NumericEquals(reconstructed) == expectedEqual);
            Check(TermOrder.Compare(retainedMap, reconstructed, true) == (expectedEqual ? 0 : 1));
            Check(TermOrder.Compare(reconstructed, retainedMap, true) == (expectedEqual ? 0 : -1));
            Check(retainedMap.GetHashCode() == reconstructed.GetHashCode());
            var outer = new MapTerm(new[] { new KeyValuePair<Term, Term>(retainedMap, Term.A(RuntimeNodeIdentityFoundationHostNames.Key)) });
            Check(outer.TryGet(reconstructed, out _) == expectedEqual);
        }

        async Task<Term[]> MixedMapHashes()
        {
            return await actor.InvokeAsync(async context =>
            {
                var values = new List<Term>();
                foreach (var family in new[] { (referenceMap, referenceBindings), (pidMap, pidBindings), (mixed, mixedBindings) })
                {
                    Term retainedHash = await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        RuntimeNodeIdentityFoundationHostNames.Phash2,
                        family.Item1
                    );
                    Term rebuiltHash = await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        RuntimeNodeIdentityFoundationHostNames.Phash2,
                        new MapTerm(family.Item2)
                    );
                    Check(retainedHash.Equals(rebuiltHash));
                    values.Add(retainedHash);
                    values.Add(rebuiltHash);
                }

                return values.ToArray();
            });
        }
    }
}
