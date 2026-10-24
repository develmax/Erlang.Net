using Erlang;

namespace Erlang.Tests;

internal static class DistributionOriginal402Checks
{
    internal static async Task CorePortable()
    {
        await CoreMode(DistributionOriginal402Names.OrdinaryMode);
        await CoreMode(DistributionOriginal402Names.FixedMode);
    }

    internal static async Task ConnectedPortable()
    {
        await ConnectedMode(DistributionOriginal402Names.MinMode, Term.I(1));
        await ConnectedMode(DistributionOriginal402Names.MaxMode, new Integer(uint.MaxValue));
    }

    private static async Task CoreMode(string mode)
    {
        var suite = new LibraryDependencyOriginalSuite(
            DistributionOriginal402Names.CorePrefix + mode,
            DistributionOriginal402Names.CoreObserver,
            [DistributionOriginal402Names.CoreObserver, DistributionOriginal402Names.CoreSubject],
            [],
            DistributionOriginal402Names.CoreRows,
            3
        );
        await using ProcessRuntime runtime = await LibraryDependencyOriginalHostChecks.StartRetainedSuite(suite);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (mode == DistributionOriginal402Names.FixedMode)
                _ = await runtime.Modules.Call(context, suite.Observer, DistributionOriginal402Names.InitializeFixed);
            Term actualValues = await runtime.Modules.Call(context, suite.Observer, DistributionOriginal402Names.Run);
            Term actualProfiles = await runtime.Modules.Call(context, suite.Observer, DistributionOriginal402Names.Stacks);
            // Retain complete actual files before any comparison can fail.
            DistributionOriginal402Access.Retain(
                suite.Prefix,
                actualValues,
                actualProfiles,
                DistributionOriginal402Names.CorePortableRows,
                DistributionOriginal402Names.CoreRaw.Length
            );
            Term originalValues = DistributionOriginal402Access.Read(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
            Term originalProfiles = DistributionOriginal402Access.Read(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
            DistributionOriginal402Access.CompletePortableRows(
                originalValues,
                actualValues,
                suite.Rows,
                DistributionOriginal402Names.CorePortable,
                DistributionOriginal402Names.CoreRaw
            );
            DistributionOriginal402Access.CompleteProfiles(
                originalProfiles,
                actualProfiles,
                suite.Rows,
                DistributionOriginal402Names.CorePortable,
                DistributionOriginal402Names.CoreRaw
            );

            return true;
        }).WaitAsync(DistributionOriginal402Names.Deadline);
    }

    private static async Task ConnectedMode(string mode, Term creation)
    {
        var suite = new LibraryDependencyOriginalSuite(
            DistributionOriginal402Names.ConnectedPrefix + mode,
            DistributionOriginal402Names.ConnectedObserver,
            [DistributionOriginal402Names.ConnectedObserver],
            [],
            DistributionOriginal402Names.ConnectedRows,
            3
        );
        await using ProcessRuntime runtime = await LibraryDependencyOriginalHostChecks.StartRetainedSuite(suite);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            _ = await runtime.Modules.Call(
                context,
                suite.Observer,
                DistributionOriginal402Names.Initialize,
                creation
            );
            Term actualValues = await runtime.Modules.Call(context, suite.Observer, DistributionOriginal402Names.Run);
            Term actualProfiles = await runtime.Modules.Call(context, suite.Observer, DistributionOriginal402Names.Stacks);
            DistributionOriginal402Access.Retain(
                suite.Prefix,
                actualValues,
                actualProfiles,
                DistributionOriginal402Names.ConnectedPortableRows,
                1
            );
            Term originalValues = DistributionOriginal402Access.Read(suite.Prefix + LibraryDependencyOriginalNames.ValueSuffix);
            Term originalProfiles = DistributionOriginal402Access.Read(suite.Prefix + LibraryDependencyOriginalNames.StackSuffix);
            string[] raw = [DistributionOriginal402Names.ConnectedRaw];
            DistributionOriginal402Access.CompletePortableRows(
                originalValues,
                actualValues,
                suite.Rows,
                DistributionOriginal402Names.ConnectedPortable,
                raw
            );
            ValidateRawProtocol(originalValues);
            ValidateRawProtocol(actualValues);
            DistributionOriginal402Access.CompleteProfiles(
                originalProfiles,
                actualProfiles,
                suite.Rows,
                DistributionOriginal402Names.ConnectedPortable,
                raw,
                ValidateRawProtocol
            );

            return true;
        }).WaitAsync(DistributionOriginal402Names.Deadline);
    }

    private static void ValidateRawProtocol(Term rows)
    {
        Term row = DistributionOriginal402Access.Rows(rows, DistributionOriginal402Names.ConnectedRows)[DistributionOriginal402Names.ConnectedRaw];
        TupleTerm outcome = DistributionOriginal402Access.Tuple(DistributionOriginal402Access.Tuple(row, 2).Items[1], 2);
        DistributionOriginal402Access.Require(outcome.Items[0].Equals(DistributionOriginal402Atoms.Returned));
        TupleTerm protocol = DistributionOriginal402Access.Tuple(outcome.Items[1], 3);
        DistributionOriginal402Access.Require(protocol.Items[0].Equals(DistributionOriginal402Atoms.Protocol) && protocol.Items[1].Equals(DistributionOriginal402Atoms.True));
        TupleTerm caught = DistributionOriginal402Access.Tuple(protocol.Items[2], 4);
        DistributionOriginal402Access.Require(caught.Items[0].Equals(DistributionOriginal402Atoms.Caught) && caught.Items[1].Equals(DistributionOriginal402Atoms.Error) && caught.Items[2].Equals(DistributionOriginal402Atoms.BadArgument));
        Term[] frames = DistributionOriginal402Access.Proper(caught.Items[3]);
        DistributionOriginal402Access.Require(frames.Length > 0);
        TupleTerm first = DistributionOriginal402Access.Tuple(frames[0], 4);
        DistributionOriginal402Access.Require(first.Items[0].Equals(DistributionOriginal402Atoms.NativeModule) && first.Items[1].Equals(DistributionOriginal402Atoms.NativeAbort));
        Term[] arguments = DistributionOriginal402Access.Proper(first.Items[2]);
        DistributionOriginal402Access.Require(arguments.Length == 2 && arguments[0].Equals(DistributionOriginal402Atoms.WrongNode));
        TupleTerm handle = DistributionOriginal402Access.Tuple(arguments[1], 2);
        DistributionOriginal402Access.Require(handle.Items[0] is Integer && handle.Items[1] is ReferenceTerm);
        DistributionOriginal402Access.Require(first.Items[3].Equals(DistributionOriginal402Atoms.NativeInfo));
        // The source's own predicate proves its frame arguments refer to its same-VM handle.
        // The complete caught error and every frame remain in the retained files, without cross-VM handle equality.
    }

    internal static async Task CreationReuse()
    {
        var suite = new LibraryDependencyOriginalSuite(
            DistributionOriginal402Names.ReuseProbe,
            DistributionOriginal402Names.ConnectedObserver,
            [DistributionOriginal402Names.ConnectedObserver],
            [],
            0,
            0
        );
        await using ProcessRuntime runtime = await LibraryDependencyOriginalHostChecks.StartRetainedSuite(suite);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            _ = await runtime.Modules.Call(
                context,
                suite.Observer,
                DistributionOriginal402Names.Initialize,
                Term.I(1)
            );
            Term actual = await runtime.Modules.Call(context, suite.Observer, DistributionOriginal402Names.ReuseProbe);
            DistributionOriginal402Access.RetainProbe(Term.Tuple(DistributionOriginal402Atoms.ProbeReturned, actual));
            TupleTerm original = DistributionOriginal402Access.Tuple(DistributionOriginal402Access.Read(DistributionOriginal402Names.ProbeResource), 2);
            DistributionOriginal402Access.Require(original.Items[0].Equals(DistributionOriginal402Atoms.ProbeReturned));
            TupleTerm expectedBody = DistributionOriginal402Access.Tuple(original.Items[1], 4);
            TupleTerm actualBody = DistributionOriginal402Access.Tuple(actual, 4);
            for (int index = 0; index < 3; index++)
                DistributionOriginal402Access.Require(expectedBody.Items[index].Equals(actualBody.Items[index]));
            DistributionOriginal402Access.Require(actualBody.Items[2].Equals(DistributionOriginal402Atoms.Noname));
            TupleTerm originalHandle = DistributionOriginal402Access.Tuple(expectedBody.Items[3], 2);
            DistributionOriginal402Access.Require(originalHandle.Items[0] is Integer && originalHandle.Items[1] is ReferenceTerm);
            TupleTerm actualHandle = DistributionOriginal402Access.Tuple(actualBody.Items[3], 2);
            DistributionOriginal402Access.Require(actualHandle.Items[0] is Integer && actualHandle.Items[1] is ReferenceTerm);
            using DistributionPendingLease owned = runtime.DistributionPending.Acquire(actualHandle);
            DistributionOriginal402Access.Require(owned.Snapshot().RemoteNode.Equals(DistributionOriginal402Atoms.Noname));
            DistributionOriginal402Access.Require(owned.Snapshot().State == DistributionPendingState.Idle);
            DistributionOriginal402Access.Require((await runtime.Modules.Call(context, DistributionOriginal402Names.NativeModule, DistributionOriginal402Names.NativeCreation)).Equals(actualBody.Items[1]));

            return true;
        }).WaitAsync(DistributionOriginal402Names.Deadline);
    }
}
