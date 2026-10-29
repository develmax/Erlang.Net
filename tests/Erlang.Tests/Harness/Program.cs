extern alias NifNativeBackend;

using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Erlang;
using Erlang.Compiler;
using Erlang.Otp;

if (args is [Erlang.Tests.OriginalCounters610Names.ChildSwitch, var countersRepository610,
    var countersAdmission610, var countersAdmissionSha610, var countersMetadata610, var countersMetadataSha610,
    var countersOwned610, var countersOutput610])
{
    try
    {
        await Erlang.Tests.OriginalReference563Replay.RunCounters610(
            countersRepository610,
            countersAdmission610,
            countersAdmissionSha610,
            countersMetadata610,
            countersMetadataSha610,
            bool.Parse(countersOwned610),
            countersOutput610
        );

        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error);

        return 1;
    }
}


if (args is [Erlang.Tests.AtomicsInternalSend593Names.ChildSwitch, var send593Repository,
    var send593Admission, var send593AdmissionSha, var send593Metadata, var send593MetadataSha,
    var send593Beam])
{
    await Erlang.Tests.AtomicsInternalSend593Checks.Run(
        send593Repository,
        send593Admission,
        send593AdmissionSha,
        send593Metadata,
        send593MetadataSha,
        bool.Parse(send593Beam)
    );

    return 0;
}

if (args is [Erlang.Tests.OriginalAtomics590Names.ChildSwitch, var ownRefRepository, var ownRefAdmission,
    var ownRefAdmissionSha, var ownRefMetadata, var ownRefMetadataSha, var ownRefOwned, var ownRefOutput])
{
    await Erlang.Tests.OriginalReference563Replay.RunAtomics590(
        ownRefRepository,
        ownRefAdmission,
        ownRefAdmissionSha,
        ownRefMetadata,
        ownRefMetadataSha,
        bool.Parse(ownRefOwned),
        ownRefOutput
    );

    return 0;
}

if (args is [Erlang.Tests.OriginalAtomics588Names.ChildSwitch, var atomicsRepository,
    var atomicsAdmission, var atomicsAdmissionSha, var atomicsMetadata, var atomicsMetadataSha,
    var atomicsOwned, var atomicsOutput])
{
    await Erlang.Tests.OriginalReference563Replay.RunAtomics588(
        atomicsRepository,
        atomicsAdmission,
        atomicsAdmissionSha,
        atomicsMetadata,
        atomicsMetadataSha,
        bool.Parse(atomicsOwned),
        atomicsOutput
    );

    return 0;
}


if (args.Length > 0 && args[0] == Erlang.Tests.TermOrderHostNames.ChildSwitch)
    return Erlang.Tests.TermOrderProbeHostMain.Run(args);

if (args.Length > 0 && args[0] == Erlang.Tests.PrimTtyOriginal412Names.ChildSwitch)
    return await Erlang.Tests.PrimTtyOriginal412ChildMain.Run(args);

if (args.Length > 0 && args[0] == Erlang.Tests.StandardBootPersistent417Names.ChildSwitch)
    return await Erlang.Tests.StandardBootPersistent417ChildMain.Run(args);

if (args.Length > 0 && args[0] == Erlang.Tests.StandardBootCollectors415Names.ChildSwitch)
    return await Erlang.Tests.StandardBootCollectors415ChildMain.Run(args);

if (args.Length > 0 && args[0] == Erlang.Tests.CollectorHibernate415Names.ChildSwitch)
    return await Erlang.Tests.CollectorHibernate415ChildMain.Run(args);

if (args.Length > 0 && args[0] == Erlang.Tests.StandardBootOriginal412Names.ChildSwitch)
    return await Erlang.Tests.StandardBootOriginal412ChildMain.Run(args);

if (args.Length > 0 && args[0] == Erlang.Tests.AtomicsOriginal413Names.ChildSwitch)
    return await Erlang.Tests.AtomicsOriginal413ChildMain.Run(args);

// Dedicated explicit-input child target; do not register artifact-dependent
// Ordinary/Private methods into the canonical checkout test list.
if (args is [Erlang.Tests.DynamicApplyBoundary520Names.ChildSwitch, var repository, var completion, var sha, var owned, var output])
{
    await Erlang.Tests.DynamicApplyBoundary520Checks.Run(
        repository,
        completion,
        sha,
        bool.Parse(owned),
        output
    );

    return 0;
}

if (args is [Erlang.Tests.MapRouting517TestNames.OriginalChildMode, var admission, var admissionSha])
{
    await Erlang.Tests.MapRouting517Checks.OriginalBeamProducer(admission, admissionSha);

    return 0;
}

if (args is [Erlang.Tests.SpawnResource541Names.ChildSwitch, var spawnRepository,
    var spawnAdmission, var spawnAdmissionSha, var spawnMetadata, var spawnMetadataSha,
    var spawnOwned, var spawnOutput])
{
    await Erlang.Tests.SpawnResource541Replay.Run(
        spawnRepository,
        spawnAdmission,
        spawnAdmissionSha,
        spawnMetadata,
        spawnMetadataSha,
        bool.Parse(spawnOwned),
        spawnOutput
    );

    return 0;
}

if (args is [Erlang.Tests.SpawnResource542Names.ChildSwitch, var resourceRepository,
    var resourceAdmission, var resourceAdmissionSha, var resourceMetadata, var resourceMetadataSha,
    var resourceOwned, var resourceBackend, var resourceOutput])
{
    await Erlang.Tests.SpawnResource541Replay.Run(
        resourceRepository,
        resourceAdmission,
        resourceAdmissionSha,
        resourceMetadata,
        resourceMetadataSha,
        bool.Parse(resourceOwned),
        resourceOutput,
        resourceBackend
    );

    return 0;
}

if (args is [Erlang.Tests.OriginalFloat556Names.ChildSwitch, var floatRepository,
    var floatAdmission, var floatAdmissionSha, var floatMetadata, var floatMetadataSha,
    var floatOwned, var floatOutput])
{
    await Erlang.Tests.OriginalFloat556Replay.Run(
        floatRepository,
        floatAdmission,
        floatAdmissionSha,
        floatMetadata,
        floatMetadataSha,
        bool.Parse(floatOwned),
        floatOutput
    );

    return 0;
}

if (args is [Erlang.Tests.OriginalInteger559Names.ChildSwitch, var integerRepository,
    var integerAdmission, var integerAdmissionSha, var integerMetadata, var integerMetadataSha,
    var integerOwned, var integerOutput])
{
    await Erlang.Tests.OriginalInteger559Replay.Run(
        integerRepository,
        integerAdmission,
        integerAdmissionSha,
        integerMetadata,
        integerMetadataSha,
        bool.Parse(integerOwned),
        integerOutput
    );

    return 0;
}

if (args is [Erlang.Tests.OriginalReference563Names.ChildSwitch, var referenceRepository,
    var referenceAdmission, var referenceAdmissionSha, var referenceMetadata, var referenceMetadataSha,
    var referenceOwned, var referenceOutput])
{
    await Erlang.Tests.OriginalReference563Replay.Run(
        referenceRepository,
        referenceAdmission,
        referenceAdmissionSha,
        referenceMetadata,
        referenceMetadataSha,
        bool.Parse(referenceOwned),
        referenceOutput
    );

    return 0;
}

Erlang.Tests.TestRunSelection selection;
try
{
    selection = Erlang.Tests.TestRunSelection.Parse(args);
}
catch (ArgumentException failure)
{
    Console.Error.WriteLine(failure.Message);

    return Erlang.Tests.TestRunNames.InvalidArgumentsExitCode;
}

var tests = new List<(string Name, Func<Task> Body)>();
Test(
    Erlang.Tests.MailboxOpaqueRemove601Names.Case,
    Erlang.Tests.MailboxOpaqueRemove601Checks.BulkCallbackHasNoLaterGuestOperations
);
Test(Erlang.Tests.MailboxSnapshot601TestNames.SnapshotCase, Erlang.Tests.MailboxSnapshot601Checks.PublicSnapshot);
Test(Erlang.Tests.SnapshotIdentity601Names.PlainCase, Erlang.Tests.SnapshotIdentity601Checks.Plain);
Test(Erlang.Tests.SnapshotIdentity601Names.MixedCase, Erlang.Tests.SnapshotIdentity601Checks.Mixed);
Test(Erlang.Tests.ProcessInfoMixed601Names.Case, Erlang.Tests.ProcessInfoMixed601Checks.Run);
Test(Erlang.Tests.ProcessInfoLate601Names.CancelCase, Erlang.Tests.ProcessInfoLate601Checks.CancelAfterPublication);
Test(Erlang.Tests.ProcessInfoLate601Names.ExitBeforeCase, Erlang.Tests.ProcessInfoLate601Checks.ExitBeforeCapture);
Test(Erlang.Tests.ProcessInfoLate601Names.ExitAfterCase, Erlang.Tests.ProcessInfoLate601Checks.ExitAfterCapture);
Test(Erlang.Tests.SchedulerRetirement607Names.Case, Erlang.Tests.SchedulerRetirement607Checks.Run);
Test(Erlang.Tests.ForeignRuntimeDrain606Names.Case, Erlang.Tests.ForeignRuntimeDrain606Checks.Run);
Test(Erlang.Tests.MailboxBulkClose606Names.Case, Erlang.Tests.MailboxBulkClose606Checks.Run);
Test(Erlang.Tests.MailboxReentrantExit606Names.PendingCase, Erlang.Tests.MailboxReentrantExit606Checks.Pending);
Test(Erlang.Tests.MailboxReentrantExit606Names.DeferredCase, Erlang.Tests.MailboxReentrantExit606Checks.Deferred);
Test(Erlang.Tests.MailboxReentrantExit606Names.LateCase, Erlang.Tests.MailboxReentrantExit606Checks.Late);
Test(Erlang.Tests.ProcessInfoStorageGap601Names.Case, Erlang.Tests.ProcessInfoStorageGap601Checks.Run);
Test(Erlang.Tests.OpaqueBoundary602Names.RejectionCase, Erlang.Tests.OpaqueBoundary602Checks.RejectionRollback);
Test(Erlang.Tests.OpaqueBoundary602Names.RetainedCase, Erlang.Tests.OpaqueBoundary602Checks.RetainedFalseAndThrow);
Test(Erlang.Tests.OpaqueBoundary602Names.CloseCase, Erlang.Tests.OpaqueBoundary602Checks.PredicateClosesVm);
Test(Erlang.Tests.MailboxSnapshot601TestNames.SourceCase, Erlang.Tests.MailboxSnapshot601Checks.SourceRemoteInfo);
Test(Erlang.Tests.MailboxSnapshot601TestNames.SelfCase, Erlang.Tests.MailboxSnapshot601Checks.SourceSelfInfo);
Test(Erlang.Tests.MailboxSnapshot601TestNames.OverrideCase, Erlang.Tests.MailboxSnapshot601Checks.PublicReregister);
Test(Erlang.Tests.MailboxSnapshot601TestNames.DefaultCase, Erlang.Tests.MailboxSnapshot601Checks.DefaultProfile);
Test(Erlang.Tests.MailboxSnapshot601TestNames.MutualCase, Erlang.Tests.ProcessInfo601ProtocolChecks.Mutual);
Test(Erlang.Tests.MailboxSnapshot601TestNames.OrderingCase, Erlang.Tests.ProcessInfo601ProtocolChecks.ReplyOrdering);
Test(Erlang.Tests.MailboxSnapshot601TestNames.CancellationCase, Erlang.Tests.ProcessInfo601ProtocolChecks.Cancellation);

Test(Erlang.Tests.NativeStore600Names.EtsCase, Erlang.Tests.NativeStore600Checks.Ets);
Test(Erlang.Tests.NativeStore600Names.PersistentCase, Erlang.Tests.NativeStore600Checks.Persistent);

Test(
    Erlang.Tests.MailboxCustody593CaseNames.RecipientFallback,
    Erlang.Tests.MailboxRootRetirement593Checks.RecipientFallbackAndErrors
);
Test(
    Erlang.Tests.MailboxCustody593CaseNames.RecipientIsolation,
    Erlang.Tests.MailboxRootRetirement593Checks.RecipientDrainDoesNotJoinOtherMailbox
);
Test(
    Erlang.Tests.MailboxCustody593CaseNames.LastRecipient,
    Erlang.Tests.MailboxSnapshotSettlement593Checks.SharedSnapshotFinalRecipientSettlesActualRoots
);
Test(
    Erlang.Tests.MailboxCustody593CaseNames.SpecificRootFallback,
    Erlang.Tests.MailboxSnapshotSettlement593Checks.SpecificReadyRootFallbackRetainsFailure
);

Test(Erlang.Tests.OriginalAtomics590Names.IdentityCase, Erlang.Tests.OriginalAtomics590Checks.AliasDomainAndStackRefusal);
Test(
    Erlang.Tests.OriginalAtomics588Names.CoverageCase,
    Erlang.Tests.OriginalAtomics588CaseChecks.HiddenFailureAndMissingCoverage
);
Test(
    Erlang.Tests.AtomicsDictionaryHost589Names.GetterCase,
    Erlang.Tests.AtomicsDictionaryHost589Checks.GetterSurvivesEraseAndCollection
);
Test(
    Erlang.Tests.AtomicsDictionaryHost589Names.SnapshotCase,
    Erlang.Tests.AtomicsDictionaryHost589Checks.SnapshotSurvivesEraseAndCollection
);
Test(Erlang.Tests.AtomicsStore590Names.SendCase, Erlang.Tests.AtomicsStore590Checks.QueueHandoff);
Test(Erlang.Tests.AtomicsStore590Names.EtsCase, Erlang.Tests.AtomicsStore590Checks.EtsHandoff);
Test(Erlang.Tests.AtomicsStore590Names.PersistentCase, Erlang.Tests.AtomicsStore590Checks.PersistentHandoff);
Test(Erlang.Tests.AtomicsRetirement590Names.Test, Erlang.Tests.AtomicsRetirement590Checks.PhysicalFailureForgetsReference);
Test(
    Erlang.Tests.AtomicsPersistentSelection591Names.KeyCase,
    Erlang.Tests.AtomicsPersistentSelection591Checks.UnusedKeyDoesNotEscape
);
Test(
    Erlang.Tests.AtomicsPersistentSelection591Names.AliasCase,
    Erlang.Tests.AtomicsPersistentSelection591Checks.EscapingAliasRetainsBacking
);
Test(Erlang.Tests.AtomicsReservation591Names.ValueCase, Erlang.Tests.AtomicsReservation591Checks.PublicValueAndClose);
Test(Erlang.Tests.AtomicsReservation591Names.PinCase, Erlang.Tests.AtomicsReservation591Checks.ActivePinStillJoins);
Test(
    Erlang.Tests.AtomicsReservation591Names.CloseCase,
    Erlang.Tests.AtomicsReservation591Checks.RetainedReservationCloseOnly
);
Test(
    Erlang.Tests.AtomicsExportRollback593Names.RollbackCase,
    Erlang.Tests.AtomicsExportRollback593Checks.LateFailureReleasesActualBacking
);
Test(
    Erlang.Tests.AtomicsExportRollback593Names.CommitCase,
    Erlang.Tests.AtomicsExportRollback593Checks.SuccessfulCopyTransfersToWrappers
);
Test(Erlang.Tests.AtomicsAggregate594Names.GetAllCase, Erlang.Tests.AtomicsAggregate594Checks.GetAllLaterAreaFailure);
Test(Erlang.Tests.AtomicsAggregate594Names.SnapshotCase, Erlang.Tests.AtomicsAggregate594Checks.SnapshotLaterAreaFailure);
Test(Erlang.Tests.AtomicsAggregate594Names.PairsCase, Erlang.Tests.AtomicsAggregate594Checks.PairsLaterAreaFailure);
Test(Erlang.Tests.AtomicsEtsAggregate594Names.Case, Erlang.Tests.AtomicsEtsAggregate594Checks.LaterRowFailure);
Test(Erlang.Tests.AtomicsMagic586Names.PhysicalTest, Erlang.Tests.AtomicsMagic586Checks.PhysicalHeaders);
Test(Erlang.Tests.AtomicsMagic586Names.GenerationsTest, Erlang.Tests.AtomicsMagic586Checks.Generations);
Test(Erlang.Tests.AtomicsMagic586Names.ForeignTest, Erlang.Tests.AtomicsMagic586Checks.ForeignBeforeStale);
Test(Erlang.Tests.AtomicsP2a586Names.GuestTest, Erlang.Tests.AtomicsP2a586Checks.GuestRetirementIgnoresStaleClrEntry);
Test(Erlang.Tests.AtomicsP2a586Names.PinTest, Erlang.Tests.AtomicsP2a586Checks.ActivePinOutlivesLastGuestRoot);
Test(Erlang.Tests.AtomicsP2a586Names.HostTest, Erlang.Tests.AtomicsP2a586Checks.HostCustodyIndependentOfGuest);
Test(Erlang.Tests.AtomicsLifecycle586Names.GateTest, Erlang.Tests.AtomicsLifecycle586Checks.ShutdownWaitsOutsideRuntimeGate);
Test(
    Erlang.Tests.AtomicsLifecycle586Names.HeapTest,
    Erlang.Tests.AtomicsLifecycle586Checks.BackingSurvivesOwnedHeapRetirement
);
Test(
    Erlang.Tests.NativeResourceTypeRetirement575Names.RepeatedCase,
    Erlang.Tests.NativeResourceTypeRetirement575Checks.RepeatedEmptyRetirement
);
Test(
    Erlang.Tests.NativeResourceTypeRetirement575Names.PendingCase,
    Erlang.Tests.NativeResourceTypeRetirement575Checks.PendingRollbackAndNewOwner
);
Test(
    Erlang.Tests.NativeResourceTypeRetirement575Names.HeldCase,
    Erlang.Tests.NativeResourceTypeRetirement575Checks.HeldPayloadAndInvocation
);

Test(Erlang.Tests.NativeMagicCleanup576Names.Test, Erlang.Tests.NativeMagicCleanup576Checks.EarlyFailureRestoresAndDrains);

Test(
    Erlang.Tests.ResourceBinary577TestNames.NestedRecipeCase,
    Erlang.Tests.ResourceBinary577Checks.NestedRecipeRetainsOriginalBinRef
);

Test(Erlang.Tests.ResourceBinary577TestNames.HeaderCase, Erlang.Tests.ResourceBinary577Checks.HeaderReadersAndCorruption);
Test(Erlang.Tests.ResourceBinary577TestNames.SharingCase, Erlang.Tests.ResourceBinary577Checks.SliceSharingAndGenerations);
Test(Erlang.Tests.ResourceBinary577TestNames.EscapeCase, Erlang.Tests.ResourceBinary577Checks.RollbackAndHostEscape);
Test(
    Erlang.Tests.ResourceBinary577TestNames.BoundaryCase,
    Erlang.Tests.ResourceBinary577Checks.CanonicalPartAndOpaqueReregister
);

Test(Erlang.Tests.BeamMixedNative580Names.IdentityCase, Erlang.Tests.BeamMixedNative580Checks.IdentityAndCapture);
Test(Erlang.Tests.BeamMixedNative580Names.OpaqueCase, Erlang.Tests.BeamMixedNative580Checks.OpaqueDirectAndClosure);
Test(Erlang.Tests.BeamMixedNative580Names.PublicCase, Erlang.Tests.BeamMixedNative580Checks.PublicReregister);

Test(Erlang.Tests.BeamMixedNative580Names.PredicateCase, Erlang.Tests.BeamMixedNative580Checks.PublicPredicateReregister);

Test(Erlang.Tests.NativeResult582Names.Case, Erlang.Tests.NativeResult582Checks.RefusalsWithoutPublication);

Test(
    Erlang.Tests.NativeMagicReference576Names.PhysicalCase,
    Erlang.Tests.NativeMagicReference576Checks.PhysicalAliasAndReader
);
Test(
    Erlang.Tests.NativeMagicReference576Names.RollbackCase,
    Erlang.Tests.NativeMagicReference576Checks.GenerationsAndRollback
);
Test(
    Erlang.Tests.NativeMagicReference576Names.HostCase,
    Erlang.Tests.NativeMagicReference576Checks.HostEscapeAndForeignCustody
);

Test(Erlang.Tests.NativeRawAddress571TestNames.LiveCase, Erlang.Tests.NativeRawAddress571Checks.LiveCellsAndRanges);
Test(
    Erlang.Tests.NativeRawAddress571TestNames.RetirementCase,
    Erlang.Tests.NativeRawAddress571Checks.ForeignStaleAndRetirement
);

Test(Erlang.Tests.SpawnWorker546Names.Test, Erlang.Tests.SpawnWorker546Checks.Run);
Test(Erlang.Tests.SpawnResource540Names.ValidationTest, Erlang.Tests.SpawnResource540Checks.Validation);
Test(Erlang.Tests.SpawnResource540Names.InternalTest, Erlang.Tests.SpawnResource540Checks.InternalErrors);
Test(Erlang.Tests.SpawnResource540Names.MetadataTest, Erlang.Tests.SpawnResource540Checks.PublicMetadata);
Test(Erlang.Tests.SpawnResource541ComparisonChecks.TestName, Erlang.Tests.SpawnResource541ComparisonChecks.Run);

Test(Erlang.Tests.EtsQueriesOriginal414Names.Test, Erlang.Tests.EtsQueriesOriginal414Checks.Run);
Test(Erlang.Tests.CollectorsOriginal415Names.Test, Erlang.Tests.CollectorsOriginal415Checks.Run);
Test(Erlang.Tests.InternalPortsOriginal416Names.Test, Erlang.Tests.InternalPortsOriginal416Checks.Run);
Test(Erlang.Tests.InternalPorts416Names.AdmissionTest, Erlang.Tests.InternalPorts416Checks.Admission);
Test(Erlang.Tests.InternalPorts416Names.LiveTest, Erlang.Tests.InternalPorts416Checks.Live);
Test(Erlang.Tests.InternalPortOrder416Names.BadFlagsTest, Erlang.Tests.InternalPortOrder416Checks.BadFlags);
Test(Erlang.Tests.InternalPortOrder416Names.ConnectTest, Erlang.Tests.InternalPortOrder416Checks.ConnectOrdering);
Test(Erlang.Tests.EtsTraversal416HostNames.OrderedTest, Erlang.Tests.EtsTraversal416HostChecks.Ordered);
Test(Erlang.Tests.EtsTraversal416HostNames.HashTest, Erlang.Tests.EtsTraversal416HostChecks.HashGroups);
Test(Erlang.Tests.EtsTraversal416HostNames.RightsTest, Erlang.Tests.EtsTraversal416HostChecks.RightsAndDetached);
Test(Erlang.Tests.EtsTraversal416HostNames.RegistrationTest, Erlang.Tests.EtsTraversal416HostChecks.Registration);
Test(Erlang.Tests.EtsTraversalOriginal416Names.Test, Erlang.Tests.EtsTraversalOriginal416Checks.Run);
Test(Erlang.Tests.ApplyTailOriginal416Names.Test, Erlang.Tests.ApplyTailOriginal416Checks.Run);
Test(Erlang.Tests.ApplyTailReturn416Names.Test, Erlang.Tests.ApplyTailReturn416Checks.Run);
Test(Erlang.Tests.DynamicApplyOriginal477Names.Test, Erlang.Tests.DynamicApplyOriginal477Checks.Run);
Test(Erlang.Tests.DynamicApplyOriginal477Names.HostTest, Erlang.Tests.DynamicApplyOriginal477Checks.HostBindings);
Test(Erlang.Tests.FloatStoreOriginal479Names.Test, Erlang.Tests.FloatStoreOriginal479Checks.Run);
Test(Erlang.Tests.ClosedWaitOriginal480Names.Test, Erlang.Tests.ClosedWaitOriginal480Checks.Run);
Test(Erlang.Tests.ClosedWaitOriginal480Names.AdmissionTest, Erlang.Tests.ClosedWaitOriginal480Checks.Admission);
Test(Erlang.Tests.FrameOriginal481Names.ContextTest, Erlang.Tests.FrameOriginal481Checks.Context);
Test(Erlang.Tests.FrameOriginal481Names.RecordTest, Erlang.Tests.FrameOriginal481Checks.Record);
Test(Erlang.Tests.FrameOriginal481Names.TlsTest, Erlang.Tests.FrameOriginal481Checks.Tls);
Test(Erlang.Tests.FrameOriginal481Names.AdmissionTest, Erlang.Tests.FrameOriginal481Checks.Admission);
Test(Erlang.Tests.ComparisonOriginal482Names.ComparisonTest, Erlang.Tests.ComparisonOriginal482Checks.Comparison);
Test(Erlang.Tests.ComparisonOriginal482Names.CrashdumpTest, Erlang.Tests.ComparisonOriginal482Checks.Crashdump);
Test(Erlang.Tests.ComparisonOriginal482Names.AdmissionTest, Erlang.Tests.ComparisonOriginal482Checks.Admission);
Test(Erlang.Tests.ReceiveLength484Names.Case, Erlang.Tests.ReceiveLength484Checks.Capture);
Test(Erlang.Tests.RegexCaptures485Names.Test, Erlang.Tests.RegexCaptures485Checks.Whole);
Test(Erlang.Tests.ReceiveLength484Names.AdmissionCase, Erlang.Tests.ReceiveLength484Checks.MegacoAdmission);
Test(Erlang.Tests.ReceiveLength484Names.HostPrefix + "progress", () => Erlang.Tests.ListLengthHostChecks.BeamReceiveBoundary());
Test(
    Erlang.Tests.ReceiveLength484Names.HostPrefix + "cancel",
    () => Erlang.Tests.ListLengthHostChecks.BeamReceiveBoundary(cancel: true)
);
Test(
    Erlang.Tests.ReceiveLength484Names.HostPrefix + "priority",
    () => Erlang.Tests.ListLengthHostChecks.BeamReceiveBoundary(priority: true)
);
Test(
    Erlang.Tests.ReceiveLength484Names.HostPrefix + "removal",
    () => Erlang.Tests.ListLengthHostChecks.BeamReceiveBoundary(removal: true)
);
Test(
    Erlang.Tests.ReceiveLength484Names.HostPrefix + "suspend",
    () => Erlang.Tests.ListLengthHostChecks.BeamReceiveBoundary(suspend: true)
);
Test(Erlang.Tests.ReceiveOriginal483Names.Case, Erlang.Tests.ReceiveOriginal483Checks.Capture);
Test(Erlang.Tests.ReceiveControl483Names.SubjectTest, Erlang.Tests.ReceiveControl483Checks.Subject);
Test(Erlang.Tests.ReceiveControl483Names.ArityTest, Erlang.Tests.ReceiveControl483Checks.Arity);
Test(Erlang.Tests.ReceiveControl483Names.KindTest, Erlang.Tests.ReceiveControl483Checks.Kind);
Test(Erlang.Tests.ReceiveControl483Names.BranchTest, Erlang.Tests.ReceiveControl483Checks.Branch);
foreach (string module483 in Erlang.Tests.ReceiveOriginal483Names.Modules)
    Test(
        Erlang.Tests.ReceiveOriginal483Names.AdmissionPrefix + module483,
        () => Erlang.Tests.ReceiveOriginal483Checks.Admission(module483)
    );

Test(Erlang.Tests.ComparisonFrame482Names.UninitializedTest, Erlang.Tests.ComparisonFrame482Checks.Uninitialized);
Test(Erlang.Tests.ComparisonFrame482Names.OpaqueTest, Erlang.Tests.ComparisonFrame482Checks.Opaque);
Test(Erlang.Tests.ComparisonFrame482Names.RootsTest, Erlang.Tests.ComparisonFrame482Checks.Roots);
Test(Erlang.Tests.ComparisonFrame482Names.FailableTest, Erlang.Tests.ComparisonFrame482Checks.Failable);
Test(Erlang.Tests.ComparisonFrame482Names.DestinationTest, Erlang.Tests.ComparisonFrame482Checks.Destination);
Test(Erlang.Tests.BeamContextForward481Names.Transport, Erlang.Tests.BeamContextForward481Checks.Transport);
Test(Erlang.Tests.BeamContextForward481Names.Refusals, Erlang.Tests.BeamContextForward481Checks.Refusals);
Test(Erlang.Tests.BeamNoReturnSwap481Names.Execution, Erlang.Tests.BeamNoReturnSwap481Checks.Execution);
Test(Erlang.Tests.BeamNoReturnSwap481Names.Refusals, Erlang.Tests.BeamNoReturnSwap481Checks.Refusals);
Test(Erlang.Tests.BeamClosedWait480Names.Valid, Erlang.Tests.BeamClosedWait480Checks.Valid);
Test(Erlang.Tests.BeamClosedWait480Names.Returning, Erlang.Tests.BeamClosedWait480Checks.Returning);
Test(Erlang.Tests.BeamClosedWait480Names.Foreign, Erlang.Tests.BeamClosedWait480Checks.Foreign);
Test(Erlang.Tests.BeamClosedWait480Names.Roots, Erlang.Tests.BeamClosedWait480Checks.Roots);
Test(Erlang.Tests.BeamClosedWait480Names.Unknown, Erlang.Tests.BeamClosedWait480Checks.Unknown);
Test(Erlang.Tests.BeamFloatStore479Names.Store, Erlang.Tests.BeamFloatStore479Checks.Store);
Test(Erlang.Tests.BeamFloatStore479Names.Trimmed, Erlang.Tests.BeamFloatStore479Checks.Trim);
Test(Erlang.Tests.BeamFloatStore479Names.Gc, Erlang.Tests.BeamFloatStore479Checks.GcRoots);
Test(Erlang.Tests.BeamFloatStore479Names.YRead, Erlang.Tests.BeamFloatStore479Checks.UninitializedRead);
Test(Erlang.Tests.BeamFloatStore479Names.FloatRead, Erlang.Tests.BeamFloatStore479Checks.UninitializedFloat);
Test(Erlang.Tests.BeamFloatStore479Names.Bounds, Erlang.Tests.BeamFloatStore479Checks.DestinationBounds);
Test(Erlang.Tests.BeamFloatStore479Names.Handler, Erlang.Tests.BeamFloatStore479Checks.HandlerOwnership);
Test(Erlang.Tests.EtsCounter417HostNames.Basics, Erlang.Tests.EtsCounter417HostChecks.Basics);
Test(Erlang.Tests.EtsCounter417HostNames.Atomic, Erlang.Tests.EtsCounter417HostChecks.AtomicAndRollback);
Test(Erlang.Tests.EtsCounter417HostNames.Admission, Erlang.Tests.EtsCounter417HostChecks.Admission);
Test(Erlang.Tests.EtsCountersOriginal417Names.Test, Erlang.Tests.EtsCountersOriginal417Checks.Run);

if (OperatingSystem.IsWindows() && Environment.Is64BitProcess)
{
    Test(Erlang.Tests.DdllErrorOriginal416Names.Test, Erlang.Tests.DdllErrorOriginal416Checks.Run);
    Test(Erlang.Tests.DdllErrorHost416Names.Test, Erlang.Tests.DdllErrorHost416Checks.Run);
}

Test(Erlang.Tests.EtsMutationsOriginal414Names.Test, Erlang.Tests.EtsMutationsOriginal414Checks.Run);
Test(Erlang.Tests.EtsMutations414Names.ValuesTest, Erlang.Tests.EtsMutations414Checks.Values);
Test(Erlang.Tests.EtsMutations414Names.RaceTest, Erlang.Tests.EtsMutations414RaceChecks.SingleWinners);
Test(Erlang.Tests.EtsMutations414Names.UpdateRaceTest, Erlang.Tests.EtsMutations414RaceChecks.Updates);
Test(Erlang.Tests.EtsMutations414Names.ReaderTest, Erlang.Tests.EtsMutations414Checks.HeldReader);
Test(Erlang.Tests.EtsMutations414Names.RightsTest, Erlang.Tests.EtsMutations414AdmissionChecks.Rights);
Test(Erlang.Tests.EtsMutations414Names.RegistrationTest, Erlang.Tests.EtsMutations414AdmissionChecks.Registration);
Test(Erlang.Tests.EtsQueries414Names.ValuesTest, Erlang.Tests.EtsQueries414Checks.Values);
Test(Erlang.Tests.EtsQueries414Names.CallbackTest, Erlang.Tests.EtsQueries414CallbackChecks.ReverseBeforeCallback);
Test(Erlang.Tests.EtsQueries414Names.PermissionsTest, Erlang.Tests.EtsQueries414Checks.Permissions);
Test(Erlang.Tests.EtsQueries414Names.RegistrationTest, Erlang.Tests.EtsQueries414Checks.Registration);
Test(Erlang.Tests.ClockUnit413Names.Test, Erlang.Tests.ClockUnit413Checks.Run);
Test(Erlang.Tests.AtomicsHost413Names.AdmissionTest, Erlang.Tests.AtomicsHost413Checks.AdmissionAndValues);
Test(Erlang.Tests.AtomicsHost413Names.ConcurrencyTest, Erlang.Tests.AtomicsHost413Checks.ConcurrentTickets);
Test(Erlang.Tests.AtomicsHost413Names.LifetimeTest, Erlang.Tests.AtomicsHost413Checks.ResourceLifetime);
Test(Erlang.Tests.AtomicsHost413Names.CloseTest, Erlang.Tests.AtomicsHost413Checks.CloseBarriers);
Test(Erlang.Tests.AtomicsHost413Names.RollbackTest, Erlang.Tests.AtomicsHost413Checks.RollbackAndBinding);
Test(Erlang.Tests.AtomicsHost413Names.GcTest, Erlang.Tests.AtomicsHost413Checks.GcNoGuestRoots);
Test(Erlang.Tests.RegexThreeOriginal413Names.Test, Erlang.Tests.RegexThreeOriginal413Checks.Whole);
Test(Erlang.Tests.ReUnicodeOriginal453Names.Test, Erlang.Tests.ReUnicodeOriginal453Checks.Whole);
Test(Erlang.Tests.BeamTupleReceive454Names.Test, Erlang.Tests.BeamTupleReceive454Checks.Run);
Test(Erlang.Tests.NativeDistribution457Names.Test, Erlang.Tests.NativeDistribution457Checks.Run);
Test(Erlang.Tests.NodeMonitor458Names.Test, Erlang.Tests.NodeMonitor458Checks.Run);
Test(Erlang.Tests.NodeMonitor459Names.Test, Erlang.Tests.NodeMonitor459Checks.Run);
Test(Erlang.Tests.NodeAutoConnect460Names.Test, Erlang.Tests.NodeAutoConnect460Checks.Run);
Test(Erlang.Tests.NodeMonitor458Names.SourceTest, Erlang.Tests.SourceMonitor458Checks.Run);
Test(Erlang.Tests.DriverIndependentTurn455Names.Test, Erlang.Tests.DriverIndependentTurn455Checks.Run);
Test(Erlang.Tests.DriverExternal455Names.Test, Erlang.Tests.DriverExternal455Checks.Run);
Test(Erlang.Tests.DriverOwnTerm455Names.Test, Erlang.Tests.DriverOwnTerm455Checks.Run);
Test(Erlang.Tests.ReUnicodeOriginal453Names.RefusalTest, Erlang.Tests.ReUnicodeTrap453OwnerHostChecks.Run);
Test(Erlang.Tests.ReUnicodeCancellation453Names.Test, Erlang.Tests.ReUnicodeCancellation453Checks.Run);
Test(Erlang.Tests.KernelBootOriginal453Names.Test, Erlang.Tests.KernelBootOriginal453Checks.Run);
Test(Erlang.Tests.BootstrapOwnership453Names.Test, Erlang.Tests.BootstrapOwnership453Checks.Run);
Test(Erlang.Tests.InternalScalarOriginal413Names.Test, Erlang.Tests.InternalScalarOriginal413Checks.Whole);
Test(Erlang.Tests.InternalScalar413Names.IntegerTest, Erlang.Tests.InternalScalar413Checks.Integers);
Test(Erlang.Tests.InternalScalar413Names.OwnerTest, Erlang.Tests.InternalScalar413Checks.Ownership);
Test(Erlang.Tests.EtsDeleteOutside412Names.ReentryTest, Erlang.Tests.EtsDeleteOutside412Checks.ReentryAndUnchanged);
Test(Erlang.Tests.EtsDeleteOutside412Names.ReplaceTest, Erlang.Tests.EtsDeleteOutside412Checks.ReplacedPayload);
Test(Erlang.Tests.EtsDeletionOriginal412Names.Test, Erlang.Tests.EtsDeletionOriginal412Checks.Run);
Test(Erlang.Tests.EtsDeletion412Names.KernelTest, Erlang.Tests.EtsDeletion412Checks.Kernels);
Test(Erlang.Tests.EtsDeletion412Names.InvalidTest, Erlang.Tests.EtsDeletion412Checks.InvalidDoesNotMutate);
Test(Erlang.Tests.EtsDeletion412Names.AccessTest, Erlang.Tests.EtsDeletion412AccessChecks.Permissions);
Test(Erlang.Tests.EtsDeletion412Names.RegistrationTest, Erlang.Tests.EtsDeletion412AccessChecks.Registration);
Test(Erlang.Tests.BootstrapSchedulerOriginal412Names.Test, Erlang.Tests.BootstrapSchedulerOriginal412Checks.Run);
Test(Erlang.Tests.BootstrapScheduler412Names.BindingTest, Erlang.Tests.BootstrapScheduler412Checks.Binding);
Test(Erlang.Tests.BootstrapScheduler412Names.MeasurementTest, Erlang.Tests.BootstrapScheduler412Checks.Measurement);
Test(Erlang.Tests.BootstrapScheduler412Names.ValuesTest, Erlang.Tests.BootstrapScheduler412Checks.Values);
Test(Erlang.Tests.SystemLoggerOriginal412Names.Test, Erlang.Tests.SystemLoggerOriginal412Checks.Run);
Test(Erlang.Tests.SystemLogger412Names.ValuesTest, Erlang.Tests.SystemLogger412Checks.Values);
Test(Erlang.Tests.SystemLogger412Names.IdentityTest, Erlang.Tests.SystemLogger412Checks.Identity);
Test(Erlang.Tests.SystemLogger412Names.InvalidTest, Erlang.Tests.SystemLogger412Checks.Invalid);
Test(Erlang.Tests.RaiseEmptyOriginal412Names.Test, Erlang.Tests.RaiseEmptyOriginal412Checks.Run);
Test(Erlang.Tests.BeamRaiseEmpty412Names.OverrideTest, Erlang.Tests.BeamRaiseEmpty412Checks.ChangedBinding);
Test(Erlang.Tests.BeamRaiseEmpty412Names.ValidTest, Erlang.Tests.BeamRaiseEmpty412Checks.Valid);
Test(Erlang.Tests.BeamRaiseEmpty412Names.NegativeTest, Erlang.Tests.BeamRaiseEmpty412Checks.Negatives);
Test(Erlang.Tests.BeamRaiseEmpty412Names.ReturnTest, Erlang.Tests.BeamRaiseEmpty412Checks.OrdinaryReturn);
Test(Erlang.Tests.RegexNativeOriginal411Names.Test, Erlang.Tests.RegexNativeOriginal411Checks.Whole);
Test(Erlang.Tests.RegexNative411Names.NativeResultsTest, Erlang.Tests.RegexNative411Checks.NativeAndErrors);
Test(Erlang.Tests.RegexNative411Names.OwnershipTest, Erlang.Tests.RegexNative411Checks.OwnershipAndRollback);
Test(Erlang.Tests.RegexNative411Names.UnicodeTest, Erlang.Tests.RegexNative411ConcurrencyChecks.LargeUnicodeResume);
Test(
    Erlang.Tests.RegexNative411Names.ConcurrentTest,
    Erlang.Tests.RegexNative411ConcurrencyChecks.ConcurrentRunsAndHeldCapability
);
Test(Erlang.Tests.RegexNative411Names.CancellationTest, Erlang.Tests.RegexNative411LifecycleChecks.RealCancellation);
Test(Erlang.Tests.RegexNative411Names.DisposeTest, Erlang.Tests.RegexNative411LifecycleChecks.DisposeClose);
Test(Erlang.Tests.RegexNative411Names.HaltTest, Erlang.Tests.RegexNative411LifecycleChecks.HaltClose);
Test(Erlang.Tests.RegexNative411Names.InitializerTest, Erlang.Tests.RegexNative411LifecycleChecks.InitializerClose);
Test(Erlang.Tests.ClosedWaitOriginal412Names.Test, Erlang.Tests.ClosedWaitOriginal412Checks.Run);
Test(Erlang.Tests.ClosedWaitOriginal412Names.AdmissionTest, Erlang.Tests.ClosedWaitOriginal412Admission.Run);
Test(Erlang.Tests.BeamClosedWait412Names.ExecutionTest, Erlang.Tests.BeamClosedWait412Checks.Execution);
Test(Erlang.Tests.BeamClosedWait412Names.ComponentTest, Erlang.Tests.BeamClosedWait412Checks.Component);
Test(Erlang.Tests.BeamClosedWait412Names.NegativeTest, Erlang.Tests.BeamClosedWait412Checks.Negatives);
Test(Erlang.Tests.NativeProviderMetadataOriginal411Names.Test, Erlang.Tests.NativeProviderMetadataOriginal411Checks.Run);
Test(Erlang.Tests.NativeProviderMetadata411Names.ValuesTest, Erlang.Tests.NativeProviderMetadata411Checks.Values);
Test(Erlang.Tests.NativeProviderMetadata411Names.ErrorsTest, Erlang.Tests.NativeProviderMetadata411Checks.Errors);
Test(Erlang.Tests.NativeProviderMetadata411Names.OwnershipTest, Erlang.Tests.NativeProviderMetadata411Checks.Ownership);
Test(Erlang.Tests.WritableReceiveOriginal411Names.Test, Erlang.Tests.WritableReceiveOriginal411Checks.Run);
Test(Erlang.Tests.WritableReceiveOriginal411Names.AdmissionTest, Erlang.Tests.WritableReceiveOriginal411Admission.Run);
Test(Erlang.Tests.StandaloneTimeout411Names.Positive, Erlang.Tests.StandaloneTimeout411Checks.Positive);
Test(Erlang.Tests.StandaloneTimeout411Names.Refusal, Erlang.Tests.StandaloneTimeout411Checks.Refusal);

Test(Erlang.Tests.BeamWritableReceive411Names.AfterTest, Erlang.Tests.BeamWritableReceive411Checks.After);
Test(Erlang.Tests.BeamWritableReceive411Names.BeforeTest, Erlang.Tests.BeamWritableReceive411Checks.Before);
Test(Erlang.Tests.BeamWritableReceive411Names.RefuseTest, Erlang.Tests.BeamWritableReceive411Checks.Refuse);
Test(Erlang.Tests.DelayBinaryOriginal411Names.Test, Erlang.Tests.DelayBinaryOriginal411Checks.Run);
Test(Erlang.Tests.DelayBinaryOriginal411Names.AdmissionTest, Erlang.Tests.DelayBinaryOriginal411Admission.Run);
Test(Erlang.Tests.BeamDelayBinary411Names.SafeTest, Erlang.Tests.BeamDelayBinary411Checks.Safe);
Test(Erlang.Tests.BeamDelayBinary411Names.OpaqueTest, Erlang.Tests.BeamDelayBinary411Checks.Opaque);
Test(Erlang.Tests.BeamDelayBinary411Names.JoinTest, Erlang.Tests.BeamDelayBinary411Checks.Join);
tests.Add((Erlang.Tests.EtsMatchOriginal411Names.Test, Erlang.Tests.EtsMatchOriginal411Checks.Run));

Test(Erlang.Tests.EtsMatch411Names.ValuesTest, Erlang.Tests.EtsMatch411Checks.Values);
Test(Erlang.Tests.EtsMatch411Names.ErrorsTest, Erlang.Tests.EtsMatch411Checks.Errors);
Test(Erlang.Tests.EtsMatch411Names.ResourcesTest, Erlang.Tests.EtsMatch411Checks.Resources);

tests.Add((Erlang.Tests.EtsLookupOriginal411Names.Test, Erlang.Tests.EtsLookupOriginal411Checks.Run));

tests.Add((Erlang.Tests.MessageQueueOriginal411Names.Test, Erlang.Tests.MessageQueueOriginal411Checks.Run));

Test(Erlang.Tests.EtsLookupElement411Names.ValuesTest, Erlang.Tests.EtsLookupElement411Checks.Values);
Test(Erlang.Tests.EtsLookupElement411Names.ErrorsTest, Erlang.Tests.EtsLookupElement411Checks.Errors);
Test(Erlang.Tests.EtsLookupElement411Names.ResourcesTest, Erlang.Tests.EtsLookupElement411Checks.Resources);
Test(Erlang.Tests.EtsLookupElement411Names.BoundaryTest, Erlang.Tests.EtsLookupElement411Checks.Boundary);
tests.Add((Erlang.Tests.UnicodeSkipOriginal411Names.Test, Erlang.Tests.UnicodeSkipOriginal411Checks.Run));

Test(Erlang.Tests.BeamUnicodeSkip411Names.Success, Erlang.Tests.BeamUnicodeSkip411Checks.Success);
Test(Erlang.Tests.BeamUnicodeSkip411Names.Failure, Erlang.Tests.BeamUnicodeSkip411Checks.Failure);
Test(Erlang.Tests.BeamUnicodeSkip411Names.Admission, Erlang.Tests.BeamUnicodeSkip411Checks.Admission);
tests.Add((Erlang.Tests.EtsConcurrencyOriginal411Names.Test, Erlang.Tests.EtsConcurrencyOriginal411Checks.Run));

Test(Erlang.Tests.EtsConcurrency411Names.FlagsTest, Erlang.Tests.EtsConcurrency411Checks.Flags);
Test(Erlang.Tests.EtsConcurrency411Names.InvalidTest, Erlang.Tests.EtsConcurrency411Checks.Invalid);
Test(Erlang.Tests.EtsCompressed441Names.StorageTest, Erlang.Tests.EtsCompressed441Checks.Storage);
Test(Erlang.Tests.EtsCompressed441Names.LeaseTest, Erlang.Tests.EtsCompressed441Checks.LeaseAndCancellation);
Test(Erlang.Tests.EtsCompressed441Names.FunctionTest, Erlang.Tests.EtsCompressed441Checks.FunctionsAndCensus);
Test(Erlang.Tests.EtsCompressed441Names.CollectionTest, Erlang.Tests.EtsCompressed441Checks.SourceRootsAndRelease);
Test(Erlang.Tests.EtsCodecCancellation442Names.EncodeTest, Erlang.Tests.EtsCompressed441Checks.MidEncodeCancellation442);
Test(Erlang.Tests.EtsCodecCancellation442Names.DecodeTest, Erlang.Tests.EtsCompressed441Checks.MidDecodeCancellation442);
Test(Erlang.Tests.EtsCodecCancellation442Names.ForeignTest, Erlang.Tests.EtsCompressed441Checks.ForeignOwners442);
Test(Erlang.Tests.EtsCompressedMutations442Names.Test, Erlang.Tests.EtsCompressedMutations442Checks.Run);
Test(Erlang.Tests.PortPumpTeardown442Names.Test, Erlang.Tests.PortPumpTeardown442Checks.Run);
Test(Erlang.Tests.EtsConcurrency411Names.ConcurrentTest, Erlang.Tests.EtsConcurrency411Checks.ConcurrentRoundTrip);
tests.Add((Erlang.Tests.CodeServerInteractive410Names.EmptyTest, Erlang.Tests.CodeServerInteractive410Checks.Empty));
tests.Add((Erlang.Tests.CodeServerInteractive410Names.AppTest, Erlang.Tests.CodeServerInteractive410Checks.App));

Test(Erlang.Tests.BeamLoaderDependencies410Names.Positive, Erlang.Tests.BeamLoaderDependencies410Checks.Declared);
Test(Erlang.Tests.BeamLoaderDependencies410Names.Undeclared, Erlang.Tests.BeamLoaderDependencies410Checks.Undeclared);
Test(Erlang.Tests.BeamLoaderDependencies410Names.Host, Erlang.Tests.BeamLoaderDependencies410Checks.Host);
Test(Erlang.Tests.BeamLoaderDependencies410Names.Foreign, Erlang.Tests.BeamLoaderDependencies410Checks.Foreign);
Test(Erlang.Tests.BeamLoaderDependencies410Names.Ended, Erlang.Tests.BeamLoaderDependencies410Checks.Ended);
Test(Erlang.Tests.BeamLoaderDependencies410Names.Nearest, Erlang.Tests.BeamLoaderDependencies410Checks.Nearest);

Test(Erlang.Tests.CodeServerBootstrap410LifecycleNames.Test, Erlang.Tests.CodeServerBootstrap410LifecycleChecks.Run);
Test(Erlang.Tests.BeamNifMarkers410Names.ActualTest, Erlang.Tests.BeamNifMarkers410Checks.ActualPrimFile);
Test(Erlang.Tests.BeamNifMarkers410Names.VariantsTest, Erlang.Tests.BeamNifMarkers410Checks.Variants);
Test(Erlang.Tests.BootstrapPid410Names.Test, Erlang.Tests.BootstrapPid410Checks.Run);
if (OperatingSystem.IsWindows())
    Test(Erlang.Tests.PrimFileEncodingOriginal410Names.Test, Erlang.Tests.PrimFileEncodingOriginal410Checks.Whole);
Test(Erlang.Tests.CodeServerRuntime410Names.Test, Erlang.Tests.CodeServerRuntime410Checks.Run);
Test(Erlang.Tests.MessageQueuePolicy411Names.FlagsTest, Erlang.Tests.MessageQueuePolicy411Checks.Flags);
Test(Erlang.Tests.MessageQueuePolicy411Names.SpawnTest, Erlang.Tests.MessageQueuePolicy411Checks.Spawn);
Test(Erlang.Tests.MessageQueuePolicy411Names.InvalidTest, Erlang.Tests.MessageQueuePolicy411Checks.Invalid);
Test(Erlang.Tests.InterpreterCode411Names.Test, Erlang.Tests.InterpreterCode411Checks.Run);
Test(Erlang.Tests.DebuggerDisabled413Names.Test, Erlang.Tests.DebuggerDisabled413Checks.Run);
Test(Erlang.Tests.ManagedCollectors413Names.AuthorityTest, Erlang.Tests.ManagedCollectors413Checks.Authority);
Test(Erlang.Tests.ManagedCollectorPrestart415Names.Test, Erlang.Tests.ManagedCollectorPrestart415Checks.Run);
Test(Erlang.Tests.ManagedCollectors413Names.ProtocolTest, Erlang.Tests.ManagedCollectors413Checks.Protocol);
Test(Erlang.Tests.ManagedCollectors413Names.ShutdownTest, Erlang.Tests.ManagedCollectors413Checks.Shutdown);
Test(Erlang.Tests.PersistentArena416HostNames.ValuesTest, Erlang.Tests.PersistentArena416ValuesChecks.Run);
Test(Erlang.Tests.PersistentArena416HostNames.RetiredTest, Erlang.Tests.PersistentArena416RetirementChecks.Run);
Test(Erlang.Tests.PersistentArena416HostNames.RollbackTest, Erlang.Tests.PersistentArena416RollbackChecks.Run);
Test(Erlang.Tests.PersistentArena416HostNames.WeakTest, Erlang.Tests.PersistentArena416WeakChecks.Run);
Test(Erlang.Tests.PersistentArena416HostNames.WritersTest, Erlang.Tests.PersistentArena416WritersChecks.Run);
Test(Erlang.Tests.PersistentArena416HostNames.SlotsTest, Erlang.Tests.PersistentArena416SlotsChecks.Run);
Test(Erlang.Tests.PersistentEpoch416HostNames.PidTest, Erlang.Tests.PersistentEpoch416HostChecks.PidKey);
Test(Erlang.Tests.PersistentEpoch416HostNames.PortTest, Erlang.Tests.PersistentEpoch416HostChecks.PortKey);
Test(Erlang.Tests.PersistentEpoch416HostNames.ReferenceTest, Erlang.Tests.PersistentEpoch416HostChecks.ReferenceKey);
Test(Erlang.Tests.PersistentEpoch416HostNames.CreationTest, Erlang.Tests.PersistentEpoch416HostChecks.CreationReuse);
Test(Erlang.Tests.PersistentNative417Names.ImageTest, Erlang.Tests.PersistentNative417Checks.OriginalImage);
Test(Erlang.Tests.PersistentNative417Names.OwnerTest, Erlang.Tests.PersistentNative417Checks.Owner);
Test(Erlang.Tests.PersistentNative417Names.HaltHeldTest, Erlang.Tests.PersistentNative417Checks.HaltHeld);
Test(Erlang.Tests.PersistentNative417Names.InitHeldTest, Erlang.Tests.PersistentNative417Checks.InitializerHeld);
Test(Erlang.Tests.PersistentNative417Names.AmbientTest, Erlang.Tests.PersistentNative417AmbientChecks.Run);
Test(Erlang.Tests.BeamfileChunk418Names.ValidTest, Erlang.Tests.BeamfileChunk418Checks.Valid);
Test(Erlang.Tests.BeamfileChunk418Names.MalformedTest, Erlang.Tests.BeamfileChunk418Checks.Malformed);
Test(Erlang.Tests.BeamfileChunk418Names.OwnerTest, Erlang.Tests.BeamfileChunk418Checks.Owner);
Test(Erlang.Tests.CountersOriginal421Names.Test, Erlang.Tests.CountersOriginal421Checks.Run);
Test(Erlang.Tests.CountersHost421Names.ValuesTest, Erlang.Tests.CountersHost421Checks.Values);
Test(Erlang.Tests.CountersHost421Names.UnusedInitializerTest, Erlang.Tests.CountersHost421Checks.UnusedInitializer);
Test(Erlang.Tests.CountersHost421Names.ConcurrencyTest, Erlang.Tests.CountersHost421Checks.Concurrency);
Test(Erlang.Tests.CountersHost421Names.GcTest, Erlang.Tests.CountersHost421Checks.Gc);
Test(Erlang.Tests.CountersHost421Names.CloseTest, Erlang.Tests.CountersHost421Checks.Shutdown);
Test(Erlang.Tests.EtsReplace420Names.BindingTest, Erlang.Tests.EtsReplace420Checks.Binding);
Test(Erlang.Tests.EtsReplace420Names.BasicsTest, Erlang.Tests.EtsReplace420Checks.Basics);
Test(Erlang.Tests.EtsReplace420Names.MutableTest, Erlang.Tests.EtsReplace420Checks.Mutable);
Test(Erlang.Tests.EtsReplaceOriginal420Names.Test, Erlang.Tests.EtsReplaceOriginal420Checks.Run);
Test(Erlang.Tests.EtsInspection420Names.BindingTest, Erlang.Tests.EtsInspection420Checks.Binding);
Test(Erlang.Tests.EtsInspection420Names.PrivilegeTest, Erlang.Tests.EtsInspection420Checks.Privilege);
Test(Erlang.Tests.EtsInspection420Names.RawTest, Erlang.Tests.EtsInspection420Checks.Raw);
Test(Erlang.Tests.EtsInfo420Names.BindingTest, Erlang.Tests.EtsInfo420Checks.Binding);
Test(Erlang.Tests.EtsInfo420Names.MemoryTest, Erlang.Tests.EtsInfo420Checks.MemoryGraph);
Test(Erlang.Tests.EtsInfo420Names.CensusTest, Erlang.Tests.EtsInfo420Checks.CensusBoundaries);
Test(Erlang.Tests.EtsInfoOriginal422Names.Test, Erlang.Tests.EtsInfoOriginal422Checks.Run);
Test(Erlang.Tests.DirtyQueryOriginal423Names.Test, Erlang.Tests.DirtyQueryOriginal423Checks.Run);
Test(Erlang.Tests.CheckProcessCodeHost423Names.BindingTest, Erlang.Tests.CheckProcessCodeHost423Checks.Bindings);
Test(Erlang.Tests.CheckProcessCodeHost423Names.OwnershipTest, Erlang.Tests.CheckProcessCodeHost423Checks.Ownership);
Test(Erlang.Tests.CheckProcessCodeHost423Names.ExitTest, Erlang.Tests.CheckProcessCodeHost423Checks.Exit);
Test(Erlang.Tests.CheckProcessCodeOriginal424Names.Test, Erlang.Tests.CheckProcessCodeOriginal424Checks.Run);
Test(Erlang.Tests.ProcessAlive425HostNames.BindingTest, Erlang.Tests.ProcessAlive425HostChecks.Binding);
Test(Erlang.Tests.ProcessAlive425HostNames.OrderingTest, Erlang.Tests.ProcessAlive425HostChecks.Ordering);
Test(Erlang.Tests.ProcessAliveOriginal425Names.Test, Erlang.Tests.ProcessAliveOriginal425Checks.Run);
Test(Erlang.Tests.InternalProcess425HostNames.StateTest, Erlang.Tests.InternalProcess425HostChecks.StateAndReplies);
Test(Erlang.Tests.InternalProcess425HostNames.SuspendTest, Erlang.Tests.InternalProcess425HostChecks.Suspension);
Test(Erlang.Tests.InternalProcess425HostNames.OwnerTest, Erlang.Tests.InternalProcess425HostChecks.OwnersAndMonitor);
Test(Erlang.Tests.InternalProcessOriginal425Names.Test, Erlang.Tests.InternalProcessOriginal425Checks.Run);
Test(Erlang.Tests.InternalProcess425HostNames.TerminationTest, Erlang.Tests.InternalProcess425HostChecks.TerminationReplies);
Test(Erlang.Tests.InternalTraceOriginal427Names.Test, Erlang.Tests.InternalTraceOriginal427Checks.Run);
Test(Erlang.Tests.MailboxCompletion427Names.Test, Erlang.Tests.MailboxCompletion427Checks.Run);
Test(Erlang.Tests.ExportGlobal431Names.Test, Erlang.Tests.ExportGlobal431Checks.Run);
Test(Erlang.Tests.CodePurger432HostNames.TestRole, Erlang.Tests.CodePurger432HostChecks.RoleAndWindow);
Test(Erlang.Tests.CodePurger432HostNames.TestFence, Erlang.Tests.CodePurger432HostChecks.PrepareAbortFence);
Test(Erlang.Tests.CodePurger432HostNames.TestOnLoad, Erlang.Tests.CodePurger432HostChecks.OnLoadSavedOld);
Test(Erlang.Tests.CodePurger432HostNames.TestDeath, Erlang.Tests.CodePurger432HostChecks.OwnerDeath);
Test(Erlang.Tests.CodePurger432HostNames.TestShutdown, Erlang.Tests.CodePurger432HostChecks.Shutdown);
Test(Erlang.Tests.CodePurgerOriginal432Names.Test, Erlang.Tests.CodePurgerOriginal432Checks.Run);
Test(Erlang.Tests.DebugUniqueOriginal436Names.Test, Erlang.Tests.DebugUniqueOriginal436Checks.Run);
Test(Erlang.Tests.SpawnMonitor431Names.LifeTest, Erlang.Tests.SpawnMonitor431Checks.Lifetime);
Test(Erlang.Tests.SpawnMonitor431Names.DemonitorTest, Erlang.Tests.SpawnMonitor431Checks.Demonitor);
Test(Erlang.Tests.SpawnMonitor431Names.AbandonTest, Erlang.Tests.SpawnMonitor431Checks.Abandon);
Test(Erlang.Tests.SpawnMonitorOriginal431Names.Test, Erlang.Tests.SpawnMonitorOriginal431Checks.Run);
Test(Erlang.Tests.EtsCounterHintsOriginal436Names.Test, Erlang.Tests.EtsCounterHintsOriginal436Checks.Run);
Test(Erlang.Tests.EtsCompressedOriginal442Names.Test, Erlang.Tests.EtsCompressedOriginal442Checks.Run);
Test(Erlang.Tests.DebugUnique436HostNames.Isolation, Erlang.Tests.DebugUnique436HostChecks.Isolation);
Test(Erlang.Tests.DebugUnique436HostNames.Parallel, Erlang.Tests.DebugUnique436HostChecks.Parallel);
Test(Erlang.Tests.DistSpawnOriginal430Names.Test, Erlang.Tests.DistSpawnOriginal430Checks.Run);
Test(Erlang.Tests.AllocatorGatherOriginal434Names.Test, Erlang.Tests.AllocatorGatherOriginal434Checks.Run);
Test(Erlang.Tests.AllocatorGatherHost434Names.HistTest, Erlang.Tests.AllocatorGatherHost434Checks.NifSpans);
Test(Erlang.Tests.AllocatorGatherHost434Names.ArrayTest, Erlang.Tests.AllocatorGatherHost434Checks.ArrayAndPersistent);
Test(Erlang.Tests.AllocatorGatherHost434Names.RetirementTest, Erlang.Tests.AllocatorGatherHost434Checks.PhysicalRetirement);
Test(Erlang.Tests.DistChannel429HostNames.SameTest, Erlang.Tests.DistChannel429HostChecks.Same);
Test(Erlang.Tests.DistChannel429HostNames.CrossTest, Erlang.Tests.DistChannel429HostChecks.Cross);
Test(Erlang.Tests.DistChannel429HostNames.GenerationTest, Erlang.Tests.DistChannel429HostChecks.Generation);
Test(Erlang.Tests.DistChannel429HostNames.LegacyTest, Erlang.Tests.DistChannel429HostChecks.Legacy);
Test(Erlang.Tests.DistChannelOriginal429Names.Test, Erlang.Tests.DistChannelOriginal429Checks.Run);
Test(Erlang.Tests.DistSpawn430HostNames.ErrorTest, Erlang.Tests.DistSpawn430HostChecks.Errors);
Test(Erlang.Tests.DistSpawn430HostNames.RoundTripTest, Erlang.Tests.DistSpawn430HostChecks.RoundTrip);
Test(Erlang.Tests.DistSpawn430HostNames.FrontierTest, Erlang.Tests.DistSpawn430HostChecks.Frontier);
Test(Erlang.Tests.DistSpawn430HostNames.AsyncTest, Erlang.Tests.DistSpawn430HostChecks.AsyncBusy);
Test(Erlang.Tests.DistributionProfileOriginal429Names.Test, Erlang.Tests.DistributionProfileOriginal429Checks.Run);
Test(Erlang.Tests.DebugDispatch432Names.Test, Erlang.Tests.DebugDispatch432Checks.Run);
Test(Erlang.Tests.DebugDispatchOriginal432Names.Test, Erlang.Tests.DebugDispatchOriginal432Checks.Run);
Test(Erlang.Tests.DebugDispatch432Names.CloseTest, Erlang.Tests.DebugDispatch432LifecycleChecks.Run);
Test(Erlang.Tests.DistributionProfile429Names.BindingTest, Erlang.Tests.DistributionProfile429Checks.Binding);
Test(Erlang.Tests.DistributionProfile429Names.CodecTest, Erlang.Tests.DistributionProfile429Checks.Codec);
Test(Erlang.Tests.DistributionProfile429Names.SourceTest, Erlang.Tests.DistributionProfile429Checks.OriginalReaders);
Test(Erlang.Tests.HamtIndex429HostNames.Graph, Erlang.Tests.HamtIndex429HostChecks.Graph);
Test(Erlang.Tests.HamtIndex429HostNames.Lifetime, Erlang.Tests.HamtIndex429HostChecks.Lifetime);
Test(Erlang.Tests.HamtIndex429HostNames.Close, Erlang.Tests.HamtIndex429HostChecks.Close);
Test(Erlang.Tests.ActorControlOwner428Names.Test, Erlang.Tests.ActorControlOwner428Checks.Exclusion);
Test(Erlang.Tests.SystemTaskOwner428Names.BindingTest, Erlang.Tests.SystemTaskOwner428Checks.Binding);
Test(Erlang.Tests.SystemTaskOwner428Names.PriorityTest, Erlang.Tests.SystemTaskOwner428Checks.Priority);
Test(Erlang.Tests.SystemTaskOwner428Names.DeathTest, Erlang.Tests.SystemTaskOwner428Checks.Death);
Test(Erlang.Tests.SystemTaskOwner428Names.DirtyTest, Erlang.Tests.SystemTaskOwner428Checks.DirtySignals);
Test(Erlang.Tests.HamtNative430Names.Graph, Erlang.Tests.HamtNative430Checks.Graph);
Test(Erlang.Tests.HamtNative430Names.Types, Erlang.Tests.HamtNative430Checks.Types);
Test(Erlang.Tests.HamtOriginal430Names.Test, Erlang.Tests.HamtOriginal430Checks.Run);
Test(Erlang.Tests.InternalPortLoader428HostNames.StatusTest, Erlang.Tests.InternalPortLoader428HostChecks.StatusAndOwner);
Test(Erlang.Tests.InternalPortLoader428HostNames.PrepareTest, Erlang.Tests.InternalPortLoader428HostChecks.PreparedImage);
Test(Erlang.Tests.InternalSpawnRequest429HostNames.StatusTest, Erlang.Tests.InternalSpawnRequest429HostChecks.StatusAndOwner);
Test(Erlang.Tests.InternalSpawnRequest429HostNames.AbandonTest, Erlang.Tests.InternalSpawnRequest429HostChecks.Abandonment);
Test(Erlang.Tests.InternalSpawnRequest429HostNames.OwnerTest, Erlang.Tests.InternalSpawnRequest429HostChecks.OwnerDeath);
Test(Erlang.Tests.InternalSpawnOriginal429Names.Test, Erlang.Tests.InternalSpawnOriginal429Checks.Run);
Test(Erlang.Tests.InternalTextAux430HostNames.TextTest, Erlang.Tests.InternalTextAux430HostChecks.TextBudget);
Test(Erlang.Tests.InternalTextAux430HostNames.OwnerTest, Erlang.Tests.InternalTextAux430HostChecks.OwnerAndFailure);
Test(Erlang.Tests.InternalTextAux430HostNames.AuxiliaryTest, Erlang.Tests.InternalTextAux430HostChecks.AuxiliaryParticipants);
Test(Erlang.Tests.InternalTextAuxOriginal430Names.Test, Erlang.Tests.InternalTextAuxOriginal430Checks.Run);
Test(Erlang.Tests.InternalSystemCheck431HostNames.StatusTest, Erlang.Tests.InternalSystemCheck431HostChecks.StatusAndOwner);
Test(Erlang.Tests.InternalSystemCheck431HostNames.AckTest, Erlang.Tests.InternalSystemCheck431HostChecks.ActualAcks);
Test(Erlang.Tests.InternalSystemCheck431HostNames.CancelTest, Erlang.Tests.InternalSystemCheck431HostChecks.CallerExitJoin);
Test(Erlang.Tests.InternalSystemCheckOriginal431Names.Test, Erlang.Tests.InternalSystemCheckOriginal431Checks.Run);
Test(Erlang.Tests.SystemTasksOriginal428Names.Test, Erlang.Tests.SystemTasksOriginal428Checks.Run);
Test(Erlang.Tests.ExportGlobalOriginal431Names.Test, Erlang.Tests.ExportGlobalOriginal431Checks.Run);
Test(
    Erlang.Tests.InternalGarbageCollection428HostNames.KernelTest,
    Erlang.Tests.InternalGarbageCollection428HostChecks.Kernel
);
Test(
    Erlang.Tests.InternalGarbageCollection428HostNames.BindingTest,
    Erlang.Tests.InternalGarbageCollection428HostChecks.Binding
);
Test(Erlang.Tests.InternalGcOriginal428Names.Test, Erlang.Tests.InternalGcOriginal428Checks.Run);

Test(Erlang.Tests.InternalMonitor425HostNames.Test, Erlang.Tests.InternalMonitor425HostChecks.Run);

Test(Erlang.Tests.TraceOriginIdentity427Names.IdentityTest, Erlang.Tests.TraceOriginIdentity427Checks.Identity);
Test(Erlang.Tests.TraceOriginIdentity427Names.OriginTest, Erlang.Tests.TraceOriginIdentity427Checks.Origin);

Test(Erlang.Tests.InternalTrace425HostNames.EventsTest, Erlang.Tests.InternalTrace425HostChecks.Events);
Test(Erlang.Tests.InternalTrace425HostNames.CallsTest, Erlang.Tests.InternalTrace425HostChecks.Calls);
Test(Erlang.Tests.InternalTrace425HostNames.LifetimeTest, Erlang.Tests.InternalTrace425HostChecks.Lifetime);

Test(Erlang.Tests.MapKeysOriginal426Names.StorageTest, Erlang.Tests.MapKeysHost426Checks.Storage);
Test(Erlang.Tests.MapKeysOriginal426Names.OwnerTest, Erlang.Tests.MapKeysHost426Checks.Ownership);
Test(Erlang.Tests.MapKeysOriginal426Names.Test, Erlang.Tests.MapKeysOriginal426Checks.Run);
Test(
    Erlang.Tests.DebugDirtyAtomics595Names.ValueCase,
    Erlang.Tests.DebugDirtyAtomics595Checks.CopiedWrapperSurvivesSourceDrop
);
Test(Erlang.Tests.DebugDirtyAtomics595Names.ForeignCase, Erlang.Tests.DebugDirtyAtomics595Checks.ForeignBeforeStale);
Test(Erlang.Tests.DebugDirtyOriginal424Names.Test, Erlang.Tests.DebugDirtyOriginal424Checks.Run);
Test(Erlang.Tests.DebugDirty424Names.Values, Erlang.Tests.DebugDirty424Checks.Values);
Test(Erlang.Tests.DebugDirty424Names.Copies, Erlang.Tests.DebugDirty424Checks.Copies);
Test(Erlang.Tests.DebugDirty424Names.Reschedule, Erlang.Tests.DebugDirty424Checks.Reschedule);
Test(Erlang.Tests.DebugDirty424Names.Messages, Erlang.Tests.DebugDirty424Checks.Messages);
Test(Erlang.Tests.DebugDirty424Names.Exiting, Erlang.Tests.DebugDirty424Checks.Exiting);
Test(Erlang.Tests.DebugDirty424Names.Binding, Erlang.Tests.DebugDirty424Checks.Binding);
Test(Erlang.Tests.DebugSharing425Names.Graph, Erlang.Tests.DebugSharing425Checks.Graph);
Test(Erlang.Tests.DebugSharing425Names.Storage, Erlang.Tests.DebugSharing425Checks.Storage);
Test(Erlang.Tests.DebugSharing425Names.Functions, Erlang.Tests.DebugSharing425Checks.Functions);
Test(Erlang.Tests.DebugSharing425Names.Source, Erlang.Tests.DebugSharing425Checks.Source);
Test(Erlang.Tests.DebugSharing425Names.Binding, Erlang.Tests.DebugSharing425Checks.Binding);
Test(Erlang.Tests.DebugSharingOriginal425Names.Test, Erlang.Tests.DebugSharingOriginal425Checks.Run);
Test(Erlang.Tests.DebugCode427Names.Owner, Erlang.Tests.DebugCode427Checks.Owner);
Test(Erlang.Tests.DebugCode427Names.Values, Erlang.Tests.DebugCode427Checks.Values);
Test(Erlang.Tests.DebugCodeOriginal427Names.Test, Erlang.Tests.DebugCodeOriginal427Checks.Run);
Test(Erlang.Tests.DistDecode423Names.CacheTest, Erlang.Tests.DistDecode423Checks.Cache);
Test(Erlang.Tests.DistDecode423Names.EpochTest, Erlang.Tests.DistDecode423Checks.Epoch);
Test(Erlang.Tests.DistDecode423Names.LocalTest, Erlang.Tests.DistDecode423Checks.Local);
Test(Erlang.Tests.DistDecode423Names.OwnerTest, Erlang.Tests.DistDecode423Checks.Owner);
Test(Erlang.Tests.DistDecode423Names.RawAtomTest, Erlang.Tests.DistDecode423Checks.RawAtom);
Test(Erlang.Tests.LocalFunctionScalars423Names.LocalTest, Erlang.Tests.LocalFunctionScalars423Checks.Local);
Test(Erlang.Tests.LocalFunctionScalars423Names.ExportTest, Erlang.Tests.LocalFunctionScalars423Checks.Export);
Test(Erlang.Tests.BeamfileMd5Host423Names.RawTest, Erlang.Tests.BeamfileMd5Host423Checks.Raw);
Test(Erlang.Tests.BeamfileMd5Host423Names.OwnerTest, Erlang.Tests.BeamfileMd5Host423Checks.Owner);
Test(Erlang.Tests.BeamfileMd5Host423Names.LiteralTest, Erlang.Tests.BeamfileMd5Host423Checks.Literals);
Test(Erlang.Tests.BeamfileMd5WidthOriginal423Names.Test, Erlang.Tests.BeamfileMd5WidthOriginal423Checks.Run);
Test(Erlang.Tests.BeamfileMd5Original423Names.Test, Erlang.Tests.BeamfileMd5Original423Checks.Run);
Test(Erlang.Tests.DistDecodeOriginal423Names.Test, Erlang.Tests.DistDecodeOriginal423Checks.Run);
Test(Erlang.Tests.DirtyQuery423Names.CpuTest, Erlang.Tests.DirtyQuery423Checks.Cpu);
Test(Erlang.Tests.DirtyQuery423Names.IoTest, Erlang.Tests.DirtyQuery423Checks.Io);
Test(Erlang.Tests.DirtyQuery423Names.BindingTest, Erlang.Tests.DirtyQuery423Checks.Binding);
Test(Erlang.Tests.EtsCursorEnd420Names.Test, Erlang.Tests.EtsCursorEnd420Checks.Run);
Test(Erlang.Tests.EtsCursors420Names.Binding, Erlang.Tests.EtsCursors420Checks.Binding);
Test(Erlang.Tests.EtsCursors420Names.Tree, Erlang.Tests.EtsCursors420Checks.Tree);
Test(Erlang.Tests.EtsCursors420Names.Hash, Erlang.Tests.EtsCursors420Checks.Hash);
Test(Erlang.Tests.EtsCursors420Names.Mutable, Erlang.Tests.EtsCursors420Checks.Mutable);
Test(Erlang.Tests.EtsCursorsOriginal420Names.Test, Erlang.Tests.EtsCursorsOriginal420Checks.Run);
Test(Erlang.Tests.MatchSpecLeafOriginal420Names.Test, Erlang.Tests.MatchSpecLeafOriginal420Checks.Run);
Test(Erlang.Tests.EtsIndex420Names.Binding, Erlang.Tests.EtsIndex420Checks.Binding);
Test(Erlang.Tests.EtsIndex420Names.Buckets, Erlang.Tests.EtsIndex420Checks.Buckets);
Test(Erlang.Tests.EtsIndex420Names.Fixed, Erlang.Tests.EtsIndex420Checks.Fixed);
Test(Erlang.Tests.EtsIndex420Names.Death, Erlang.Tests.EtsIndex420Checks.Death);
Test(Erlang.Tests.EtsIndex420Names.Mutations, Erlang.Tests.EtsIndex420Checks.Mutations);
Test(Erlang.Tests.EtsIndexOriginal420Names.Test, Erlang.Tests.EtsIndexOriginal420Checks.Run);
Test(Erlang.Tests.EtsAll420Names.Binding, Erlang.Tests.EtsAll420Checks.Binding);
Test(Erlang.Tests.EtsAll420Names.Directory, Erlang.Tests.EtsAll420Checks.Directory);
Test(Erlang.Tests.EtsAll420Names.Auxiliary, Erlang.Tests.EtsAll420Checks.Auxiliary);
Test(Erlang.Tests.EtsAll420Names.Close, Erlang.Tests.EtsAll420Checks.Close);
Test(Erlang.Tests.EtsAllOriginal420Names.Test, Erlang.Tests.EtsAllOriginal420Checks.Run);
Test(Erlang.Tests.DirtyNative419Names.Lanes, Erlang.Tests.DirtyNative419Checks.Lanes);

Test(Erlang.Tests.DirtyNative419Names.Cancellation, Erlang.Tests.DirtyNative419Checks.Cancellation);

Test(Erlang.Tests.DirtyNative419Names.Close, Erlang.Tests.DirtyNative419Checks.Close);

Test(Erlang.Tests.DirtyNative419Names.Roots, Erlang.Tests.DirtyNative419Checks.Roots);

Test(Erlang.Tests.DirtyNative419Names.Shutdown, Erlang.Tests.DirtyNative419Checks.Shutdown);

Test(Erlang.Tests.DebugDirtyRouting420Names.Test, Erlang.Tests.DebugDirtyRouting420Checks.Run);

Test(Erlang.Tests.DebugLoggerLiveness420Names.Test, Erlang.Tests.DebugLoggerLiveness420Checks.Run);

Test(Erlang.Tests.DebugInternal420Names.SourceTest, Erlang.Tests.DebugInternal420Checks.Source);


Test(Erlang.Tests.DebugInternal420Names.OwnerTest, Erlang.Tests.DebugInternal420Checks.Owner);


Test(Erlang.Tests.DebugInternal420Names.WarningTest, Erlang.Tests.DebugInternal420Checks.Warning);


Test(Erlang.Tests.DebugInternalOriginal420Names.Test, Erlang.Tests.DebugInternalOriginal420Checks.Run);


Test(Erlang.Tests.DebugSame420HostNames.SourceTest, Erlang.Tests.DebugSame420HostChecks.Source);

Test(Erlang.Tests.DebugSame420HostNames.OwnerTest, Erlang.Tests.DebugSame420HostChecks.Owner);

Test(Erlang.Tests.DebugSameOriginal420Names.Test, Erlang.Tests.DebugSameOriginal420Checks.Run);

Test(Erlang.Tests.DebugUnaligned421Names.SourceTest, Erlang.Tests.DebugUnaligned421Checks.Source);

Test(Erlang.Tests.DebugUnaligned421Names.OwnerTest, Erlang.Tests.DebugUnaligned421Checks.Owner);

Test(Erlang.Tests.DebugUnalignedOriginal421Names.Test, Erlang.Tests.DebugUnalignedOriginal421Checks.Run);


Test(Erlang.Tests.DebugLocks419Names.SourceTest, Erlang.Tests.DebugLocks419Checks.Source);
Test(Erlang.Tests.DebugLocks419Names.OwnerTest, Erlang.Tests.DebugLocks419Checks.Owner);
Test(Erlang.Tests.DebugLocksOriginal419Names.Test, Erlang.Tests.DebugLocksOriginal419Checks.Run);
Test(Erlang.Tests.EtsCompiledOriginal419Names.StrictTest, Erlang.Tests.EtsCompiledOriginal419Checks.Strict);
Test(Erlang.Tests.EtsCompiledOriginal419Names.OwnTest, Erlang.Tests.EtsCompiledOriginal419Checks.Own);
Test(Erlang.Tests.InternalOwner419HostNames.ResetTest, Erlang.Tests.InternalOwner419HostChecks.Reset);
Test(Erlang.Tests.InternalOwner419HostNames.CursorTest, Erlang.Tests.InternalOwner419HostChecks.Cursor);
Test(Erlang.Tests.InternalOwnerOriginal419Names.Test, Erlang.Tests.InternalOwnerOriginal419Checks.Run);
Test(Erlang.Tests.EtsNewHeirOriginal419Names.Test, Erlang.Tests.EtsNewHeirOriginal419Checks.Run);
Test(Erlang.Tests.EtsNewHeir419Names.Lifetime, Erlang.Tests.EtsNewHeir419Checks.Lifetime);
Test(Erlang.Tests.EtsNewHeir419Names.Rollback, Erlang.Tests.EtsNewHeir419Checks.Rollback);
Test(Erlang.Tests.EtsCompiled419Names.BasicTest, Erlang.Tests.EtsCompiled419Checks.Basic);
Test(Erlang.Tests.EtsCompiled419Names.CanonicalTest, Erlang.Tests.EtsCompiled419Checks.Canonical);
Test(Erlang.Tests.EtsCompiled419Names.TrapTest, Erlang.Tests.EtsCompiled419Checks.Trap);
Test(Erlang.Tests.EtsCompiled419Names.OwnerTest, Erlang.Tests.EtsCompiled419OwnerChecks.Run);
Test(Erlang.Tests.EtsCompiled419Names.LifetimeTest, Erlang.Tests.EtsCompiled419LifetimeChecks.Run);
Test(Erlang.Tests.EtsHeir419HostNames.Lifetime, Erlang.Tests.EtsHeir419HostChecks.Lifetime);
Test(Erlang.Tests.EtsHeir419HostNames.AtomicQueue, Erlang.Tests.EtsHeir419HostChecks.AtomicQueue);
Test(Erlang.Tests.EtsHeir419HostNames.DeadHeir, Erlang.Tests.EtsHeir419HostChecks.DeadHeir);
Test(Erlang.Tests.EtsHeir419HostNames.Options, Erlang.Tests.EtsHeir419HostChecks.Options);
Test(Erlang.Tests.EtsHeir419HostNames.Monitor, Erlang.Tests.EtsHeir419MonitorChecks.Run);
Test(Erlang.Tests.EtsHeir419HostNames.Lease, Erlang.Tests.EtsHeir419LeaseChecks.Run);
Test(Erlang.Tests.EtsHeir419HostNames.Deferred, Erlang.Tests.EtsHeir419DeferredChecks.Run);
Test(Erlang.Tests.EtsHeirOriginal419Names.Test, Erlang.Tests.EtsHeirOriginal419Checks.Run);
Test(Erlang.Tests.EtsRename418HostNames.Basics, Erlang.Tests.EtsRename418HostChecks.Basics);
Test(Erlang.Tests.EtsRename418HostNames.Access, Erlang.Tests.EtsRename418HostChecks.Access);
Test(Erlang.Tests.EtsRename418HostNames.RetiredName, Erlang.Tests.EtsRename418HostChecks.RetiredName);
Test(Erlang.Tests.EtsRename418HostNames.Admission, Erlang.Tests.EtsRename418HostChecks.Admission);
Test(Erlang.Tests.EtsRenameOriginal418Names.Test, Erlang.Tests.EtsRenameOriginal418Checks.Run);
Test(Erlang.Tests.BeamfileChunkOriginal418Names.Test, Erlang.Tests.BeamfileChunkOriginal418Checks.Run);
Test(Erlang.Tests.DebuggerOriginal413Names.Test, Erlang.Tests.DebuggerOriginal413Checks.Default);
Test(Erlang.Tests.StrictChain411Names.OrderTest, Erlang.Tests.StrictChain411Checks.OrderAndMixedImports);
Test(Erlang.Tests.StrictChain411Names.RefusalTest, Erlang.Tests.StrictChain411Checks.Refusals);
Test(Erlang.Tests.StrictChain411Names.PreCanceledTest, Erlang.Tests.StrictChain411Checks.PreCanceled);
Test(Erlang.Tests.StrictChain411Names.HeldTest, Erlang.Tests.StrictChain411Checks.CancelActualBlockedCallback);
Test(Erlang.Tests.PrimFileEncoding410Names.RoutingTest, Erlang.Tests.PrimFileEncoding410Checks.Routing);
Test(Erlang.Tests.PrimFileEncoding410Names.WordTest, Erlang.Tests.PrimFileEncoding410Checks.Words);
Test(Erlang.Tests.PrimFileEncoding410Names.NormalizeTest, Erlang.Tests.PrimFileEncoding410Checks.NormalizeSource);
Test(Erlang.Tests.PrimFileWindows410HostNames.Test, Erlang.Tests.PrimFileWindows410HostChecks.Run);
tests.Add((Erlang.Tests.NativeProvider408Names.DefaultTest, Erlang.Tests.NativeProvider408Checks.DefaultUnchanged));
tests.Add((Erlang.Tests.NativeProvider408Names.ActualTest, Erlang.Tests.NativeProvider408Checks.ConfiguredActual));
tests.Add((Erlang.Tests.NativeProvider408Names.InvalidTest, Erlang.Tests.NativeProvider408Checks.InvalidAndForeign));
tests.Add((Erlang.Tests.NativeProvider408Names.SourceTest, Erlang.Tests.NativeProvider408Checks.ExistingSourceRefused));
tests.Add((Erlang.Tests.NativeProvider408Names.DriftTest, Erlang.Tests.NativeProvider408Checks.IdempotentAndDriftRefused));
tests.Add((Erlang.Tests.NativeProvider408Names.RollbackTest, Erlang.Tests.NativeProvider408Checks.OuterRollback));
tests.Add((Erlang.Tests.NativeProvider408Names.StubTest, Erlang.Tests.NativeProvider408Checks.WholeStubRefused));
tests.Add((Erlang.Tests.NativeProvider408Names.LifecycleTest, Erlang.Tests.NativeProvider408Checks.Lifecycle));
tests.Add((Erlang.Tests.NativeProvider408Names.FactoryTest, Erlang.Tests.NativeProvider408Checks.FactoryGuards));
tests.Add((Erlang.Tests.NativeProvider408Names.ConsumerTest, Erlang.Tests.NativeProvider408ConsumerChecks.Run));

Test(Erlang.Tests.EtsSelectOriginal409Names.Test, Erlang.Tests.EtsSelectOriginal409Checks.Whole);
Test(Erlang.Tests.EtsSelect409Names.GeneralTest, Erlang.Tests.EtsSelect409Checks.General);
Test(Erlang.Tests.EtsSelect409Names.ErrorTest, Erlang.Tests.EtsSelect409Checks.Errors);
Test(Erlang.Tests.EtsSelect409Names.CopyTest, Erlang.Tests.EtsSelect409Checks.DetachedFunction);
Test(Erlang.Tests.PersistentLifecycle408Names.Test, Erlang.Tests.PersistentLifecycle408Checks.Whole);

Test(Erlang.Tests.EtsKernelOriginal407Names.Test, Erlang.Tests.EtsKernelOriginal407Checks.Whole);
Test(
    Erlang.Tests.PersistentTerm408HostNames.ValueTest,
    Erlang.Tests.PersistentTerm408HostChecks.ExactValues
);
Test(
    Erlang.Tests.PersistentTerm408HostNames.LifetimeTest,
    Erlang.Tests.PersistentTerm408HostChecks.ProcessIndependentAndAtomicNew
);
Test(
    Erlang.Tests.PersistentTerm408HostNames.LeaseTest,
    Erlang.Tests.PersistentTerm408HostChecks.RetiredReaderAndClose
);
Test(
    Erlang.Tests.FunEntryPublication407Names.HistoryTest,
    Erlang.Tests.FunEntryPublication407Checks.History
);
Test(
    Erlang.Tests.FunEntryPublication407Names.PreflightTest,
    Erlang.Tests.FunEntryPublication407Checks.Preflight
);
Test(
    Erlang.Tests.FunEntryPublication407Names.ConcurrencyTest,
    Erlang.Tests.FunEntryPublication407Checks.LockOrder
);
Test(Erlang.Tests.EtsKeyKernel407Names.DefaultTest, Erlang.Tests.EtsKeyKernel407Checks.CodeServerDefaultVectors);
Test(Erlang.Tests.EtsKeyKernel407Names.KindsTest, Erlang.Tests.EtsKeyKernel407Checks.FourFamiliesAndAtomicBatch);
Test(Erlang.Tests.EtsKeyKernel407Names.AccessTest, Erlang.Tests.EtsKeyKernel407Checks.ProtectionAndPublicDelete);

Test(
    Erlang.Tests.OwnedFunctionOriginal407Names.Test,
    Erlang.Tests.OwnedFunctionOriginal407Checks.Whole
);
Test(
    Erlang.Tests.GuestStoredFunction407Names.AstTest,
    Erlang.Tests.GuestStoredFunction407Checks.SourceAndExternal
);
Test(
    Erlang.Tests.GuestStoredFunction407Names.RestoredTest,
    Erlang.Tests.GuestStoredFunction407Checks.RestoredHasNoReadCallback
);
Test(
    Erlang.Tests.GuestStoredFunction407Names.BeamTest,
    Erlang.Tests.GuestStoredFunction407Checks.OriginalBeamRestored
);
Test(Erlang.Tests.EtsRetirement407Names.OwnerTest, Erlang.Tests.EtsRetirement407Checks.OwnerDown);
Test(Erlang.Tests.EtsRetirement407Names.DeleteTest, Erlang.Tests.EtsRetirement407Checks.ExplicitDelete);
Test(Erlang.Tests.EtsRetirement407Names.LeaseTest, Erlang.Tests.EtsRetirement407Checks.SparseAndReader);
Test(Erlang.Tests.EtsRetirement407Names.ShutdownTest, Erlang.Tests.EtsRetirement407Checks.GlobalShutdown);
Test(
    Erlang.Tests.BeamStoredFunction406Names.GraphTest,
    Erlang.Tests.BeamStoredFunction406Checks.GraphAndAuthenticity
);
Test(
    Erlang.Tests.BeamStoredFunction406Names.PurgeTest,
    Erlang.Tests.BeamStoredFunction406Checks.OriginalImageAndPurge
);
Test(Erlang.Tests.EtsPayloadDrop406Names.LeaseTest, Erlang.Tests.EtsPayloadDrop406Checks.LastReadLeaseRoots);
Test(Erlang.Tests.EtsPayloadDrop406Names.CopyTest, Erlang.Tests.EtsPayloadDrop406Checks.CopyOutRoots);
Test(
    Erlang.Tests.EtsFoundation405Names.GraphTest,
    Erlang.Tests.EtsFoundation405Checks.DetachedGraphAndRollback
);
Test(
    Erlang.Tests.EtsFoundation405Names.LifetimeTest,
    Erlang.Tests.EtsFoundation405Checks.IdentityAndOwnerDeath
);
Test(
    Erlang.Tests.EtsFoundation405Names.ResourceTest,
    Erlang.Tests.EtsFoundation405Checks.ResourceRetentionAndReadLease
);
Test(
    Erlang.Tests.EtsFoundation405Names.CancellationTest,
    Erlang.Tests.EtsFoundation405Checks.CancellationCloseAndHalt
);
Test(
    Erlang.Tests.IoPrintableOriginal404Names.DefaultTest,
    Erlang.Tests.IoPrintableOriginal404Checks.Default
);
Test(
    Erlang.Tests.IoPrintableOriginal404Names.Latin1Test,
    Erlang.Tests.IoPrintableOriginal404Checks.Latin1
);
Test(
    Erlang.Tests.IoPrintableOriginal404Names.UnicodeTest,
    Erlang.Tests.IoPrintableOriginal404Checks.Unicode
);
Test(
    Erlang.Tests.IoPrintableRange404Names.Test,
    Erlang.Tests.IoPrintableRange404Checks.Run
);
Test(
    Erlang.Tests.IdentifierLifetimeHostNames.PidTest,
    Erlang.Tests.IdentifierLifetimeHostChecks.PidLifetime
);

Test(
    Erlang.Tests.IdentifierLifetimeHostNames.PortTest,
    Erlang.Tests.IdentifierLifetimeHostChecks.PortLifetime
);

Test(
    Erlang.Tests.IdentifierLifetimeHostNames.ReferenceTest,
    Erlang.Tests.IdentifierLifetimeHostChecks.ReferenceLifetime
);

Test(
    Erlang.Tests.IdentifierLifetimeHostNames.FunctionTest,
    Erlang.Tests.IdentifierLifetimeHostChecks.NestedFunctionLifetime
);

Test(
    Erlang.Tests.IdentifierLifetimeHostNames.DomainTest,
    Erlang.Tests.IdentifierLifetimeHostChecks.LocalForeignAndClosedRoots
);

Test(
    Erlang.Tests.IdentifierLifetimeHostNames.ConcurrentTest,
    Erlang.Tests.IdentifierLifetimeHostChecks.ConcurrentIdentityAndImportChurn
);

Test(
    Erlang.Tests.IdentifierOriginal403Names.PidTest,
    Erlang.Tests.IdentifierOriginal403Checks.PidWhole
);
Test(
    Erlang.Tests.IdentifierOriginal403Names.PortTest,
    Erlang.Tests.IdentifierOriginal403Checks.PortWhole
);
Test(
    Erlang.Tests.IdentifierOriginal403Names.ReferenceTest,
    Erlang.Tests.IdentifierOriginal403Checks.ReferenceWhole
);
Test(
    Erlang.Tests.IdentifierOriginal403Names.FunctionPidTest,
    Erlang.Tests.IdentifierOriginal403Checks.FunctionPidWhole
);
Test(
    Erlang.Tests.IdentifierOriginal403Names.NestedFunctionReferenceTest,
    Erlang.Tests.IdentifierOriginal403Checks.NestedFunctionReferenceWhole
);
Test(
    Erlang.Tests.MapHashRetentionHostNames.ScalarTest,
    Erlang.Tests.MapHashRetentionHostChecks.ScalarsAndEpochs
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.NestedTest,
    Erlang.Tests.MapHashRetentionHostChecks.NormalOrderAndNestedEnvironment
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.ForeignTest,
    Erlang.Tests.MapHashRetentionHostChecks.LocalForeignAndCapturedSnapshot
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.ClosedTest,
    Erlang.Tests.MapHashRetentionHostChecks.CloseAndRelease
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.WeakTest,
    Erlang.Tests.MapHashRetentionHostChecks.WeakKeysAndChurn
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.ConcurrentTest,
    Erlang.Tests.MapHashRetentionHostChecks.ConcurrentScopes
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.PidTest,
    Erlang.Tests.MapHashRetentionHostChecks.HeldPid
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.PortTest,
    Erlang.Tests.MapHashRetentionHostChecks.HeldPort
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.ReferenceTest,
    Erlang.Tests.MapHashRetentionHostChecks.HeldReference
);

Test(
    Erlang.Tests.MapHashRetentionHostNames.FunctionTest,
    Erlang.Tests.MapHashRetentionHostChecks.HeldFunction
);


Test(
    Erlang.Tests.IdentifierObservationShutdownHostNames.DisposeWithMonitorAndLinkTest,
    Erlang.Tests.IdentifierObservationShutdownHostChecks.DisposeWithMonitorAndLink
);
Test(
    Erlang.Tests.IdentifierObservationShutdownHostNames.InitializerDeathWithMonitorAndLinkTest,
    Erlang.Tests.IdentifierObservationShutdownHostChecks.InitializerDeathWithMonitorAndLink
);
Test(
    Erlang.Tests.IdentifierObservationShutdownHostNames.HaltWithLiveCallbacksTest,
    Erlang.Tests.IdentifierObservationShutdownHostChecks.HaltWithLiveCallbacks
);
Test(
    Erlang.Tests.IdentifierObservationShutdownHostNames.ClosedPublicationRaceTest,
    Erlang.Tests.IdentifierObservationResolverHostChecks.ClosedPublicationRace
);
Test(
    Erlang.Tests.IdentifierObservationShutdownHostNames.UnexpectedOpenResolverErrorTest,
    Erlang.Tests.IdentifierObservationResolverHostChecks.UnexpectedOpenResolverError
);
Test(
    Erlang.Tests.DistributionOriginal402Names.CoreTest,
    Erlang.Tests.DistributionOriginal402Checks.CorePortable
);
Test(
    Erlang.Tests.DistributionOriginal402Names.ConnectedTest,
    Erlang.Tests.DistributionOriginal402Checks.ConnectedPortable
);
Test(
    Erlang.Tests.DistributionOriginal402Names.ReuseTest,
    Erlang.Tests.DistributionOriginal402Checks.CreationReuse
);

Test(
    Erlang.Tests.DistributionEntryCreationHostNames.FreshTest,
    Erlang.Tests.DistributionEntryCreationHostChecks.FreshAndLazyBudget
);
Test(
    Erlang.Tests.DistributionEntryCreationHostNames.UnsignedTest,
    Erlang.Tests.DistributionEntryCreationHostChecks.StartedUnsigned
);
Test(
    Erlang.Tests.DistributionEntryCreationHostNames.RetainedTest,
    Erlang.Tests.DistributionEntryCreationHostChecks.RetainedDormantAndGeneration
);
Test(
    Erlang.Tests.DistributionEntryCreationHostNames.ClosedTest,
    Erlang.Tests.DistributionEntryCreationHostChecks.ClosedReleasesRuntimeRoots
);
Test(
    Erlang.Tests.DistributionState402Names.PendingTest,
    Erlang.Tests.DistributionState402Checks.PendingIdleStaleAndForeign
);
Test(
    Erlang.Tests.DistributionState402Names.CreationTest,
    Erlang.Tests.DistributionState402Checks.CreationAndConnected
);
Test(
    Erlang.Tests.DistributionState402Names.ExitingTest,
    Erlang.Tests.DistributionState402Checks.RealExitingTransition
);
Test(
    Erlang.Tests.NativeStubShape400Names.Test,
    Erlang.Tests.NativeStubShape400Checks.Run
);
Test(
    Erlang.Tests.ExternalTermTraversal400Names.Test,
    Erlang.Tests.ExternalTermTraversal400Checks.Run
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.NewConnection400Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.NewConnection400Whole
);
Test(
    Erlang.Tests.NewConnection400HostNames.Test,
    Erlang.Tests.NewConnection400HostChecks.Run
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.NetKernel400Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.NetKernel400Whole
);
Test(
    Erlang.Tests.NetKernel400HostNames.Test,
    Erlang.Tests.NetKernel400HostChecks.Run
);
Test(
    Erlang.Tests.PortErrorStack399Names.CompleteStacksTest,
    Erlang.Tests.PortErrorStack399Checks.CompleteOpenAndUnchangedCloseStacks
);
Test(
    Erlang.Tests.PortErrorStack399Names.SourceWrapperTest,
    Erlang.Tests.PortErrorStack399Checks.OptionalPrefixPreservesSourceWrapperRule
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Callback399Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Callback399Whole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Nested399Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Nested399Whole
);
Test(
    Erlang.Tests.MailboxBeamCallback399TestNames.CurrentSavePriority,
    Erlang.Tests.MailboxBeamCallback399Checks.PriorityConsumesCurrentSave
);
Test(
    Erlang.Tests.MailboxBeamCallback399TestNames.PrioritySourceRepeats,
    Erlang.Tests.MailboxBeamCallback399Checks.PriorityRejectionRepeatsSourceAttempt
);
Test(
    Erlang.Tests.MailboxBeamCallback399TestNames.ExceptionNestedSave,
    Erlang.Tests.MailboxBeamCallback399Checks.ExceptionAndNestedShareCursor
);
Test(
    Erlang.Tests.MailboxBeamCallback399TestNames.CancellationReset,
    Erlang.Tests.MailboxBeamCallback399Checks.VisibilityCancellationAndReset
);
Test(
    Erlang.Tests.MailboxBeamCallback399TestNames.MarkerPriority,
    Erlang.Tests.MailboxBeamCallback399Checks.MarkerReferencePriorityComposition
);
Test(
    Erlang.Tests.BeamCallbackPlan399TestNames.ShapeFrameOpaque,
    Erlang.Tests.BeamCallbackPlan399Checks.ExactShapeAndFrameProof
);
Test(
    Erlang.Tests.MailboxBeamCallback399TestNames.DeferredPriority,
    Erlang.Tests.MailboxBeamCallback399Checks.DeferredPriorityRetainsAdmissionOwnership
);
Test(
    Erlang.Tests.MailboxBeamCallback399TestNames.NestedCurrentConsume,
    Erlang.Tests.MailboxBeamCallback399Checks.NestedConsumesCurrentBeforeOuterCommit
);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Sofs398Test, Erlang.Tests.LibraryDependencyOriginalHostChecks.Sofs398Whole);
Test(Erlang.Tests.Library398TestNames.TupleGuarded, Erlang.Tests.BeamGuardedTuple398Checks.GuardedKernelAndRemoteOverride);
Test(
    Erlang.Tests.Library398TestNames.TupleRefusals,
    Erlang.Tests.BeamGuardedTuple398Checks.UnprovedMutationEntryAndRootsRefuse
);
Test(
    Erlang.Tests.Library398TestNames.TupleIdentity,
    Erlang.Tests.BeamTupleProofIdentity398Checks.WarmedCacheStillRejectsForeignInstruction
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Position398Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Position398Whole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Receive398Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Receive398Whole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.WarningDefault398Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.WarningDefault398Whole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.WarningInfo398Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.WarningInfo398Whole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.WarningError398Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.WarningError398Whole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.WarningWarning398Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.WarningWarning398Whole
);
Test(
    Erlang.Tests.Library398TestNames.PositionGuarded,
    Erlang.Tests.BeamGuardedTail398Checks.AlignedTailAfterPositionOverwrite
);
Test(Erlang.Tests.Library398TestNames.PositionRealFailures, Erlang.Tests.BeamGuardedTail398Checks.RealFailuresStillRefuse);
Test(Erlang.Tests.Library398TestNames.PositionOwner, Erlang.Tests.BeamPositionOwner398Checks.SourceIdentityStillRequired);
Test(Erlang.Tests.Library398TestNames.BinaryFresh, Erlang.Tests.BeamReceiveBinary398Checks.FreshContextAndCandidateRollback);
Test(
    Erlang.Tests.Library398TestNames.BinaryOpaque,
    Erlang.Tests.BeamReceiveBinary398Checks.IncomingOpaqueContextAndPositionRefused
);
Test(
    Erlang.Tests.Library398TestNames.BinaryRefusals,
    Erlang.Tests.BeamReceiveBinary398Checks.UnconsumedAndMutatingPairsRefused
);
Test(
    Erlang.Tests.Library398TestNames.BinaryBoundary,
    Erlang.Tests.BeamReceiveBinaryBoundary398Checks.StandaloneLegacyOutsideMatcherRefused
);
Test(Erlang.Tests.Library398TestNames.WarningOwner, Erlang.Tests.LibraryDependencyOriginalHostChecks.WarningMap398Owner);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Os399Test, Erlang.Tests.LibraryDependencyOriginalHostChecks.Os399Whole);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Port399Test, Erlang.Tests.LibraryDependencyOriginalHostChecks.Port399Whole);
Test(Erlang.Tests.Os399HostNames.ClockTest, () =>
 {
     Erlang.Tests.OsClock399HostChecks.Run();

     return Task.CompletedTask;
 });
Test(Erlang.Tests.Os399HostNames.CodecTest, Erlang.Tests.Os399HostChecks.Codec);
Test(Erlang.Tests.Os399HostNames.StoreTest, Erlang.Tests.Os399HostChecks.StoreCancelClose);
Test(Erlang.Tests.Os399HostNames.TreeTest, Erlang.Tests.Os399HostChecks.Tree);
Test(Erlang.Tests.Os399HostNames.OwnerTest, Erlang.Tests.Os399HostChecks.Owner);
Test(Erlang.Tests.Os399HostNames.ImageTest, Erlang.Tests.Os399ImageHostChecks.Run);

Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Handler397Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Handler397Whole
);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Map397Test, Erlang.Tests.LibraryDependencyOriginalHostChecks.Map397Whole);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Context397Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Context397Whole
);
Test(Erlang.Tests.Library397TestNames.HandlerAdmission, Erlang.Tests.BeamHandlerRouting396Checks.Admission);
Test(Erlang.Tests.Library397TestNames.HandlerRuntime, Erlang.Tests.BeamHandlerRouting396Checks.CaughtRuntime);
Test(
    Erlang.Tests.Library397TestNames.HandlerOverride,
    Erlang.Tests.BeamHandlerRouting396Checks.ReturningImportCannotForgeHandler
);
Test(Erlang.Tests.Library397TestNames.ContextTransport, Erlang.Tests.BeamContextForward397Checks.OptionalAndRequired);
Test(Erlang.Tests.Library397TestNames.ContextRefusals, Erlang.Tests.BeamContextForward397Checks.Refusals);
Test(Erlang.Tests.BeamContextTail412Names.TransportTest, Erlang.Tests.BeamContextTail412Checks.Transport);
Test(Erlang.Tests.BeamContextTail412Names.RefusalsTest, Erlang.Tests.BeamContextTail412Checks.Refusals);
Test(Erlang.Tests.BeamOpaqueJoin412Names.TransportTest, Erlang.Tests.BeamOpaqueJoin412Checks.Transport);
Test(Erlang.Tests.BeamOpaqueJoin412Names.RefusalsTest, Erlang.Tests.BeamOpaqueJoin412Checks.Refusals);
Test(Erlang.Tests.BeamOpaqueJoin412Names.BudgetTest, Erlang.Tests.BeamOpaqueJoin412Checks.Budget);
if (OperatingSystem.IsWindows())
{
    Test(Erlang.Tests.PrimTtyNative412Names.PureTest, Erlang.Tests.PrimTtyNative412Checks.Pure);
    Test(Erlang.Tests.PrimTtyNative412Names.ResourceTest, Erlang.Tests.PrimTtyNative412Checks.Resource);
    Test(Erlang.Tests.PrimTtyNative412Names.BarrierTest, Erlang.Tests.PrimTtyNative412Checks.Barrier);
    Test(Erlang.Tests.PrimTtyNative412Names.BlockedReadTest, Erlang.Tests.PrimTtyNative412Checks.BlockedRead);
}
Test(Erlang.Tests.Library397TestNames.MapAlias, Erlang.Tests.BeamReceiveMap397Checks.AliasAndCandidateRollback);
Test(
    Erlang.Tests.Library397TestNames.MapAtomic,
    Erlang.Tests.BeamReceiveMap397Checks.FailureWritesNoDestinationsAndOpaqueReadRefused
);
Test(
    Erlang.Tests.Library397TestNames.MapRefusals,
    Erlang.Tests.BeamReceiveMap397Checks.FrameDestinationAndCallbackRemainRefused
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Receive396Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Receive396Whole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Encoding396Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Encoding396Whole
);
Test(
    Erlang.Tests.BeamPureReceive396Names.CandidateRollbackAndOpaqueSlots,
    Erlang.Tests.BeamPureReceive396Checks.CandidateRollbackAndOpaqueSlots
);
Test(
    Erlang.Tests.BeamPureReceive396Names.PureBoundariesAndCallbackRefusal,
    Erlang.Tests.BeamPureReceive396Checks.PureBoundariesAndCallbackRefusal
);

Test(
    "runtime/library396/file-encoding-private-source-owner",
    Erlang.Tests.LibraryDependencyOriginalHostChecks.FileEncoding396Owner
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Binary395Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Binary395Whole
);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Bif395Test, Erlang.Tests.LibraryDependencyOriginalHostChecks.Bif395Whole);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Frame395Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Frame395Whole
);
Test(
    Erlang.Tests.BeamBinaryFlags395Names.IntegerEndianSignAndOffset,
    Erlang.Tests.BeamBinaryFlags395Checks.IntegerEndianSignAndOffset
);
Test(
    Erlang.Tests.BeamBinaryFlags395Names.NativeFlagsAndRefusals,
    Erlang.Tests.BeamBinaryFlags395Checks.NativeFlagsAndRefusals
);
Test(
    Erlang.Tests.BeamBinaryFlags395Names.DynamicFloatBytesAndHalfRounding,
    Erlang.Tests.BeamBinaryFlags395Checks.DynamicFloatBytesAndHalfRounding
);
Test(
    "runtime/library394/infallible-opcode-kernel-preserves-remote-overrides",
    Erlang.Tests.BeamInfallibleBifControlHostChecks.CanonicalOpcodeAndRemoteOverride
);
Test(
    "runtime/library394/infallible-bif-preserves-gc-fallible-opaque-refusals",
    Erlang.Tests.BeamInfallibleBifControlHostChecks.FallibleGcAndOpaqueRefusals
);
Test("runtime/library395/frame-Admission", Erlang.Tests.BeamFrameJoin394Checks.Admission);
Test(Erlang.Tests.BeamLiteralFailure467Names.AdmissionTest, Erlang.Tests.BeamLiteralFailure467Checks.Admission);
Test("runtime/library395/frame-ActualBodyOverride", Erlang.Tests.BeamFrameJoin394Checks.ActualBodyOverride);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Math394Test, Erlang.Tests.LibraryDependencyOriginalHostChecks.Math394Whole);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Math394OwnerTest,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Math394Owner
);
Test(Erlang.Tests.MathNative392Names.ErrorsAndConversion, Erlang.Tests.MathNative392Checks.ErrorsAndConversion);
Test(Erlang.Tests.MathNative392Names.SignedZeroUnderflowAndFmod, Erlang.Tests.MathNative392Checks.SignedZeroUnderflowAndFmod);
Test(Erlang.Tests.MathNative392Names.HostSpecialFunctions, Erlang.Tests.MathNative392Checks.HostSpecialFunctions);
Test(
    "runtime/library394/selection/ManagedRuntimeDependency",
    Erlang.Tests.LibrarySelectionPolicyControlChecks.ManagedRuntimeDependency
);
Test(
    "runtime/library394/selection/NativeBootstrapOwnership",
    Erlang.Tests.LibrarySelectionPolicyControlChecks.NativeBootstrapOwnership
);
Test("runtime/library394/selection/ExplicitPureCycle", Erlang.Tests.LibrarySelectionPolicyControlChecks.ExplicitPureCycle);
Test(
    "runtime/library394/selection/StrictMissingWrapperSelection",
    Erlang.Tests.LibrarySelectionPolicyControlChecks.StrictMissingWrapperSelection
);
Test("runtime/library394/selection/FactoryRefusals", Erlang.Tests.LibrarySelectionPolicyNegativeChecks.FactoryRefusals);
Test(
    "runtime/library394/selection/BootstrapBodyMutation",
    Erlang.Tests.LibrarySelectionPolicyNegativeChecks.BootstrapBodyMutation
);
Test(
    "runtime/library394/selection/ManagedWrapperUnbound",
    Erlang.Tests.LibrarySelectionPolicyNegativeChecks.ManagedWrapperUnbound
);
Test(
    "runtime/library394/selection/MissingCaptureAndExport",
    Erlang.Tests.LibrarySelectionPolicyNegativeChecks.MissingCaptureAndExport
);
Test(
    "runtime/library394/selection/SourceOwnerAndOldGeneration",
    Erlang.Tests.LibrarySelectionPolicyNegativeChecks.SourceOwnerAndOldGeneration
);
Test("runtime/library394/selection/NativeSourceOwner", Erlang.Tests.LibrarySelectionPolicyNegativeChecks.NativeSourceOwner);
Test("runtime/library394/selection/MissingSelectedMfa", Erlang.Tests.LibrarySelectionPolicyNegativeChecks.MissingSelectedMfa);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Small393Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.Small393Whole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.FloatString393Test,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.FloatString393Whole
);
Test(Erlang.Tests.BeamSmallOpcodeControlNames.NumericInequality, Erlang.Tests.BeamSmallOpcodeControlChecks.NumericInequality);
Test(
    Erlang.Tests.BeamSmallOpcodeControlNames.SelectiveReceiveNumericInequality,
    Erlang.Tests.BeamSmallOpcodeControlChecks.SelectiveReceiveNumericInequality
);
Test(Erlang.Tests.BeamSmallOpcodeControlNames.NegationBits, Erlang.Tests.BeamSmallOpcodeControlChecks.NegationBits);
Test(
    Erlang.Tests.BeamSmallOpcodeControlNames.NegationUninitialized,
    Erlang.Tests.BeamSmallOpcodeControlChecks.NegationUninitialized
);
Test(
    Erlang.Tests.BeamSmallOpcodeControlNames.NegationStaleAfterCall,
    Erlang.Tests.BeamSmallOpcodeControlChecks.NegationStaleAfterCall
);
Test(
    Erlang.Tests.BeamSmallOpcodeControlNames.NegationFreshAfterCall,
    Erlang.Tests.BeamSmallOpcodeControlChecks.NegationFreshAfterCall
);
Test(
    Erlang.Tests.BeamSmallOpcodeControlNames.NegationWrongDestination,
    Erlang.Tests.BeamSmallOpcodeControlChecks.NegationWrongDestination
);
Test(
    Erlang.Tests.BeamSmallOpcodeControlNames.InequalityUninitializedY,
    Erlang.Tests.BeamSmallOpcodeControlChecks.InequalityUninitializedY
);
Test(
    Erlang.Tests.BeamFloatString392Names.FloatWidthsEndianAndZero,
    Erlang.Tests.BeamFloatString392Checks.FloatWidthsEndianAndZero
);
Test(Erlang.Tests.BeamFloatString392Names.FloatFailuresAndFlags, Erlang.Tests.BeamFloatString392Checks.FloatFailuresAndFlags);
Test(
    Erlang.Tests.BeamFloatString392Names.StringOffsetBoundsAndFailure,
    Erlang.Tests.BeamFloatString392Checks.StringOffsetBoundsAndFailure
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.MetadataTest,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.MetadataWhole
);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.UnicodeFieldsTest,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.UnicodeFieldsWhole
);
Test(Erlang.Tests.UnicodeTraversalCancellationNames.Test, Erlang.Tests.UnicodeTraversalCancellationHostChecks.Run);
Test(Erlang.Tests.BeamUnicodeField392Names.RoundTrip, Erlang.Tests.BeamUnicodeField392Checks.RoundTrip);
Test(
    Erlang.Tests.BeamUnicodeField392Names.IndependentBytesAndOffset,
    Erlang.Tests.BeamUnicodeField392Checks.IndependentBytesAndOffset
);
Test(Erlang.Tests.BeamUnicodeField392Names.InvalidAndFlags, Erlang.Tests.BeamUnicodeField392Checks.InvalidAndFlags);

Test(Erlang.Tests.LibraryDependencyOriginalNames.Test0, Erlang.Tests.LibraryDependencyOriginalHostChecks.CatchWhole);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Test1, Erlang.Tests.LibraryDependencyOriginalHostChecks.Bif3Whole);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Test2, Erlang.Tests.LibraryDependencyOriginalHostChecks.WideWhole);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Test3, Erlang.Tests.LibraryDependencyOriginalHostChecks.PrettyWhole);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Test4, Erlang.Tests.LibraryDependencyOriginalHostChecks.UnicodeWhole);
Test(Erlang.Tests.LibraryDependencyOriginalNames.Test5, Erlang.Tests.LibraryDependencyOriginalHostChecks.ReceiveWhole);
Test(
    Erlang.Tests.LibraryDependencyOriginalNames.Test6,
    Erlang.Tests.LibraryDependencyOriginalHostChecks.ForeignAndMalformedNativeHelper
);
Test(Erlang.Tests.LibraryDependencyOriginalNames.FixedTest, Erlang.Tests.LibraryDependencyOriginalHostChecks.FixedWhole);
Test(Erlang.Tests.BeamFixedBinary392Names.LargeAndUnaligned, Erlang.Tests.BeamFixedBinary392Checks.LargeAndUnaligned);
Test(
    Erlang.Tests.BeamFixedBinary392Names.EmptyBitstringAndAlias,
    Erlang.Tests.BeamFixedBinary392Checks.EmptyBitstringAndAlias
);
Test(Erlang.Tests.BeamFixedBinary392Names.ShortFlagsAndOverflow, Erlang.Tests.BeamFixedBinary392Checks.ShortFlagsAndOverflow);

Test(Erlang.Tests.BeamPrettyEquality392Names.Width16, Erlang.Tests.BeamPrettyEquality392Checks.Width16);
Test(Erlang.Tests.BeamPrettyEquality392Names.Unaligned16, Erlang.Tests.BeamPrettyEquality392Checks.Unaligned16);
Test(Erlang.Tests.BeamPrettyEquality392Names.ParserBounds, Erlang.Tests.BeamPrettyEquality392Checks.ParserBounds);
Test(Erlang.Tests.BeamPrettyEquality392Names.WideHost, Erlang.Tests.BeamPrettyEquality392Checks.WideHost);
Test(Erlang.Tests.BeamWideInteger392Names.Values, Erlang.Tests.BeamWideInteger392Checks.Values);
Test(Erlang.Tests.BeamWideInteger392Names.OffsetAndUnit, Erlang.Tests.BeamWideInteger392Checks.OffsetAndUnit);
Test(Erlang.Tests.BeamWideInteger392Names.ShortAndBounds, Erlang.Tests.BeamWideInteger392Checks.ShortAndBounds);
Test(Erlang.Tests.BeamTailFailure392Names.UnitOne, Erlang.Tests.BeamTailFailure392Checks.UnitOne);
Test(
    Erlang.Tests.BeamTailFailure392Names.RealFailuresStillRefuse,
    Erlang.Tests.BeamTailFailure392Checks.RealFailuresStillRefuse
);
Test(Erlang.Tests.BeamCatchProtocolControlNames.Test0, Erlang.Tests.BeamCatchProtocolControlHostChecks.Refusals);
Test(Erlang.Tests.BeamCatchProtocolControlNames.Test1, Erlang.Tests.BeamCatchProtocolControlHostChecks.NormalAndExceptional);
Test(Erlang.Tests.BeamCatchProtocolControlNames.Test2, Erlang.Tests.BeamPureMailboxControlHostChecks.SelectionAndRetention);
Test(Erlang.Tests.BeamCatchProtocolControlNames.Test3, Erlang.Tests.BeamPureMailboxControlHostChecks.ImpureImportRefused);
Test(Erlang.Tests.LibraryProcessNames.ConstructionTest, Erlang.Tests.LibraryProcessHostChecks.Construction);
Test(Erlang.Tests.BeamBinaryField391Names.Utf8Failures, Erlang.Tests.BeamBinaryField391Checks.Utf8Failures);
Test(Erlang.Tests.BeamBinaryField391Names.Utf8Unaligned, Erlang.Tests.BeamBinaryField391Checks.Utf8Unaligned);
Test(Erlang.Tests.BeamBinaryField391Names.IntegerWide, Erlang.Tests.BeamBinaryField391Checks.IntegerWide);
Test(Erlang.Tests.BeamBinaryField391Names.SkipBounds, Erlang.Tests.BeamBinaryField391Checks.SkipBounds);
Test(Erlang.Tests.BeamBinaryField391Names.RoleRefusals, Erlang.Tests.BeamBinaryField391Checks.RoleRefusals);
Test(Erlang.Tests.BeamBinaryField391Names.OpaqueContext, Erlang.Tests.BeamBinaryField391Checks.OpaqueContext);

Test(Erlang.Tests.LibraryProcessNames.FieldsTest, Erlang.Tests.LibraryProcessHostChecks.Fields);
Test(Erlang.Tests.LibraryProcessNames.GenSysTest, Erlang.Tests.LibraryProcessHostChecks.GenSys);
Test(Erlang.Tests.LibraryProcessNames.ProcessTest, Erlang.Tests.LibraryProcessHostChecks.Process);
Test(Erlang.Tests.LibraryProcessNames.PidTest, Erlang.Tests.LibraryProcessHostChecks.OwnPid);
Test(Erlang.Tests.BeamInitializedRootNames.GcOnlyTest, Erlang.Tests.BeamInitializedRootChecks.GcOnly);
Test(Erlang.Tests.BeamInitializedRootNames.TrimGcTest, Erlang.Tests.BeamInitializedRootChecks.TrimGc);
Test(Erlang.Tests.BeamInitializedRootNames.TransportEscapeTest, Erlang.Tests.BeamInitializedRootChecks.TransportEscape);
Test(Erlang.Tests.BeamInitializedRootNames.TupleEscapeTest, Erlang.Tests.BeamInitializedRootChecks.TupleEscape);
Test(Erlang.Tests.BeamInitializedRootNames.TrimEscapeTest, Erlang.Tests.BeamInitializedRootChecks.TrimEscape);
Test(
    Erlang.Tests.BeamInitializedRootNames.ReallocateUninitializedTest,
    Erlang.Tests.BeamInitializedRootChecks.ReallocateUninitialized
);
Test(Erlang.Tests.BeamInitializedRootNames.OverwriteThenReadTest, Erlang.Tests.BeamInitializedRootChecks.OverwriteThenRead);

Test(Erlang.Tests.BeamMissingHeaderLineNames.Test, Erlang.Tests.BeamMissingHeaderLineHostChecks.Run);
Test(Erlang.Tests.BeamBinaryProtocol390Names.PositionRoundtrip, Erlang.Tests.BeamBinaryProtocol390Checks.PositionRoundtrip);
Test(
    Erlang.Tests.BeamBinaryProtocol390Names.OpaquePositionEscape,
    Erlang.Tests.BeamBinaryProtocol390Checks.OpaquePositionEscape
);
Test(
    Erlang.Tests.BeamBinaryProtocol390Names.ResumeRejectsOrdinary,
    Erlang.Tests.BeamBinaryProtocol390Checks.ResumeRejectsOrdinary
);
Test(
    Erlang.Tests.BeamBinaryProtocol390Names.LocalContextTransport,
    Erlang.Tests.BeamBinaryProtocol390Checks.LocalContextTransport
);
Test(Erlang.Tests.BeamBinaryProtocol390Names.UnitTwo, Erlang.Tests.BeamBinaryProtocol390Checks.UnitTwo);
Test(Erlang.Tests.BeamBinaryProtocol390Names.LiteralNoFail, Erlang.Tests.BeamBinaryProtocol390Checks.LiteralNoFail);
Test(Erlang.Tests.LibraryMapProtocolNames.Test, Erlang.Tests.LibraryMapProtocolHostChecks.RunIndependent);
Test(Erlang.Tests.LibraryMapProtocolNames.OwnState, Erlang.Tests.LibraryMapProtocolHostChecks.OwnLayout);
Test(Erlang.Tests.LibraryBinaryNames.Test, Erlang.Tests.LibraryBinaryHostChecks.Run);
Test(Erlang.Tests.LibraryCollectionsNames.Test, Erlang.Tests.LibraryCollectionsHostChecks.Run);
Test(Erlang.Tests.LibraryCollectionsNames.Refusals, Erlang.Tests.LibraryCollectionsHostChecks.InvalidSelections);
Test(Erlang.Tests.BeamCollectionFloatControlNames.LocalStale, Erlang.Tests.BeamCollectionFloatControlChecks.LocalCallStale);
Test(Erlang.Tests.BeamCollectionFloatControlNames.LocalFresh, Erlang.Tests.BeamCollectionFloatControlChecks.LocalCallFresh);
Test(Erlang.Tests.BeamCollectionFloatControlNames.NativeStale, Erlang.Tests.BeamCollectionFloatControlChecks.NativeCallStale);
Test(Erlang.Tests.BeamCollectionFloatControlNames.NativeFresh, Erlang.Tests.BeamCollectionFloatControlChecks.NativeCallFresh);
Test(Erlang.Tests.BeamCollectionFloatControlNames.Heap, Erlang.Tests.BeamCollectionFloatControlChecks.HeapRetains);
Test(Erlang.Tests.BeamCollectionFloatControlNames.Uninitialized, Erlang.Tests.BeamCollectionFloatControlChecks.InitialRead);
Test(
    Erlang.Tests.LibrarySelectionHostNames.Prefix + Erlang.Tests.LibrarySelectionHostNames.Defaults,
    Erlang.Tests.LibrarySelectionHostChecks.Defaults
);
Test(
    Erlang.Tests.LibrarySelectionHostNames.Prefix + Erlang.Tests.LibrarySelectionHostNames.Both,
    Erlang.Tests.LibrarySelectionHostChecks.BothOrders
);
Test(
    Erlang.Tests.LibrarySelectionHostNames.Prefix + Erlang.Tests.LibrarySelectionHostNames.Mixed,
    Erlang.Tests.LibrarySelectionHostChecks.Mixed
);
Test(
    Erlang.Tests.LibrarySelectionHostNames.Prefix + Erlang.Tests.LibrarySelectionHostNames.Immutable,
    Erlang.Tests.LibrarySelectionHostChecks.Immutable
);
Test(
    Erlang.Tests.LibrarySelectionHostNames.Prefix + Erlang.Tests.LibrarySelectionHostNames.Refusals,
    Erlang.Tests.LibrarySelectionHostChecks.Refusals
);
Test(
    Erlang.Tests.LibrarySelectionHostNames.Prefix + Erlang.Tests.LibrarySelectionHostNames.Reload,
    Erlang.Tests.LibrarySelectionHostChecks.Reload
);
Test(
    Erlang.Tests.LibrarySelectionHostNames.Prefix + Erlang.Tests.LibrarySelectionHostNames.Cancellation,
    Erlang.Tests.LibrarySelectionHostChecks.Cancellation
);
foreach (var signature in Erlang.Tests.ListsBeamSmokeCases.Create())
{
    var selected = signature;
    Test(
        Erlang.Tests.LibrarySelectionHostNames.ListsPrefix + selected.Function + "/" + selected.Arity,
        () => Erlang.Tests.ListBeamHostChecks.Export(selected.Function, selected.Arity, startupSelection: true)
    );
}
foreach (var signature in Erlang.Tests.QueueBeamFullSmokeCases.Exports)
{
    var selected = signature;
    Test(
        Erlang.Tests.LibrarySelectionHostNames.QueuePrefix + selected.Function + "/" + selected.Arity,
        () => Erlang.Tests.QueueBeamFullSmokeCases.Run(
            selected.Function,
            selected.Arity,
            beamBackend: true,
            originalLists: true,
            startupSelection: true
        )
    );
}
Test(
    Erlang.Tests.LibrarySelectionCancellationPhaseNames.Test,
    Erlang.Tests.LibrarySelectionCancellationPhaseHostChecks.Run
);
Test(
    Erlang.Tests.LibrarySelectionPublicCancellationNames.SetupTest,
    Erlang.Tests.LibrarySelectionPublicCancellationChecks.DuringSetup
);
Test(
    Erlang.Tests.LibrarySelectionPublicCancellationNames.PermitTest,
    Erlang.Tests.LibrarySelectionPublicCancellationChecks.AfterPurgerPermit
);
Test(Erlang.Tests.ProplistFlatExpectationNames.Test, Erlang.Tests.ProplistFlatExpectationHostChecks.Run);
Test(
    Erlang.Tests.ListBeamHostNames.QueuePrefix + Erlang.Tests.QueueBeamFullSmokeNames.LongSuffix,
    () => Erlang.Tests.QueueBeamEffectsHostChecks.Long(true, true)
);
Test(
    Erlang.Tests.ListBeamHostNames.QueuePrefix + Erlang.Tests.QueueBeamFullSmokeNames.CallbackSuffix,
    () => Erlang.Tests.QueueBeamEffectsHostChecks.CallbackFailures(true, true)
);
Test(
    Erlang.Tests.ListBeamHostNames.QueuePrefix + Erlang.Tests.QueueBeamFullSmokeNames.CancellationSuffix,
    () => Erlang.Tests.QueueBeamEffectsHostChecks.Cancellation(true, true)
);
Test(Erlang.Tests.ListBeamHostNames.NativeCallerTest, Erlang.Tests.ListBeamHostChecks.NativeCaller);
Test(
    Erlang.Tests.ListsOpcodeEdgeNames.Prefix + Erlang.Tests.ListsOpcodeEdgeNames.Bif,
    Erlang.Tests.ListsOpcodeEdgeHostChecks.BinaryBif
);
Test(
    Erlang.Tests.ListsOpcodeEdgeNames.Prefix + Erlang.Tests.ListsOpcodeEdgeNames.Fun,
    Erlang.Tests.ListsOpcodeEdgeHostChecks.DynamicFun
);
Test(
    Erlang.Tests.ListsOpcodeEdgeNames.Prefix + Erlang.Tests.ListsOpcodeEdgeNames.Equality,
    Erlang.Tests.ListsOpcodeEdgeHostChecks.Equality
);
Test(
    Erlang.Tests.ListsOpcodeEdgeNames.Prefix + Erlang.Tests.ListsOpcodeEdgeNames.Map,
    Erlang.Tests.ListsOpcodeEdgeHostChecks.Maps
);
Test(
    Erlang.Tests.ListsOpcodeEdgeNames.Prefix + Erlang.Tests.ListsOpcodeEdgeNames.MapReject,
    Erlang.Tests.ListsOpcodeEdgeHostChecks.MapRefusals
);
foreach (var signature in Erlang.Tests.ListsBeamSmokeCases.Create())
{
    string function = signature.Function;
    int arity = signature.Arity;
    Test(
        Erlang.Tests.ListBeamHostNames.ExportPrefix + function + "/" + arity,
        () => Erlang.Tests.ListBeamHostChecks.Export(function, arity)
    );
}

Test(Erlang.Tests.ListBeamHostNames.AdmissionTest, Erlang.Tests.ListBeamHostChecks.NativeAndPure);
foreach (var signature in Erlang.Tests.QueueBeamFullSmokeCases.Exports)
{
    var selected = signature;
    Test(
        Erlang.Tests.ListBeamHostNames.QueuePrefix + selected.Function,
        () => Erlang.Tests.QueueBeamFullSmokeCases.Run(
            selected.Function,
            selected.Arity,
            true,
            true
        )
    );
}

Test(Erlang.Tests.QueueBeamFullSmokeNames.PredicateInvalidTest, Erlang.Tests.QueueBeamEffectsHostChecks.PredicateInvalidList);
Test(Erlang.Tests.BeamOpcodeEdgeNames.AliasesTest, Erlang.Tests.BeamOpcodeEdgeHostChecks.GetListAliases);
Test(Erlang.Tests.BeamOpcodeEdgeNames.TrimTest, Erlang.Tests.BeamOpcodeEdgeHostChecks.TrimZeroTailContinuation);
Test(Erlang.Tests.BeamOpcodeEdgeNames.BifTest, Erlang.Tests.BeamOpcodeEdgeHostChecks.GuardBifDestination);
Test(Erlang.Tests.BeamOpcodeEdgeNames.SelectTest, Erlang.Tests.BeamOpcodeEdgeHostChecks.SelectTableAdmission);
foreach (bool beam in new[] { false, true })
{
    bool selected = beam;
    string prefix = selected ? Erlang.Tests.QueueBeamFullSmokeNames.BeamPrefix : Erlang.Tests.QueueBeamFullSmokeNames.CSharpPrefix;
    Test(prefix + Erlang.Tests.QueueBeamFullSmokeNames.LongSuffix, () => Erlang.Tests.QueueBeamEffectsHostChecks.Long(selected));
    Test(
        prefix + Erlang.Tests.QueueBeamFullSmokeNames.CallbackSuffix,
        () => Erlang.Tests.QueueBeamEffectsHostChecks.CallbackFailures(selected)
    );
    Test(
        prefix + Erlang.Tests.QueueBeamFullSmokeNames.CancellationSuffix,
        () => Erlang.Tests.QueueBeamEffectsHostChecks.Cancellation(selected)
    );
}
foreach (var entry in Erlang.Tests.QueueBeamFullSmokeCases.Exports)
{
    var selected = entry;
    Test(
        Erlang.Tests.QueueBeamFullSmokeNames.CSharpPrefix + selected.Function,
        () => Erlang.Tests.QueueBeamFullSmokeCases.Run(selected.Function, selected.Arity, false)
    );
    Test(
        Erlang.Tests.QueueBeamFullSmokeNames.BeamPrefix + selected.Function,
        () => Erlang.Tests.QueueBeamFullSmokeCases.Run(selected.Function, selected.Arity, true)
    );
}
Test(Erlang.Tests.QueueBeamAdmissionHostNames.DirectTest, Erlang.Tests.QueueBeamAdmissionHostChecks.Direct);
Test(Erlang.Tests.QueueBeamAdmissionHostNames.PublishedTest, Erlang.Tests.QueueBeamAdmissionHostChecks.Published);
Test(Erlang.Tests.ProplistGetValueHostNames.ParseTest, Erlang.Tests.ProplistGetValueHostChecks.ParseAll);
Test(Erlang.Tests.ProplistGetValueHostNames.IdentityTest, Erlang.Tests.ProplistGetValueHostChecks.Identity);
Test(Erlang.Tests.ProplistGetValueHostNames.ProgressTest, Erlang.Tests.ProplistGetValueHostChecks.Progress);
Test(Erlang.Tests.ProplistGetValueHostNames.CancellationTest, Erlang.Tests.ProplistGetValueHostChecks.Cancellation);
Test(Erlang.Tests.TermOrderHostNames.DeepCompareTest, Erlang.Tests.TermOrderHostChecks.DeepCompare);
Test(Erlang.Tests.TermOrderHostNames.DeepHashTest, Erlang.Tests.TermOrderHostChecks.DeepHash);
Test(Erlang.Tests.TermOrderHostNames.ShallowTest, Erlang.Tests.TermOrderHostChecks.Shallow);
Test(Erlang.Tests.TermOrderHostNames.SnapshotTest, Erlang.Tests.TermOrderHostChecks.Snapshot);
foreach (var fixture in Erlang.Differential.SourceCallBindingCases.Controls)
    Test(fixture.Name, () => Erlang.Tests.SourceCallBindingHostChecks.Check(fixture.Name));
Test(
    Erlang.Differential.SourceCallBindingCaseNames.MetadataRoundtrip,
    Erlang.Tests.SourceCallBindingHostChecks.GeneratedBindingRoundtrip
);
Test(Erlang.Tests.MfaCompatibilityHostNames.Test, Erlang.Tests.MfaCompatibilityHostChecks.Run);
Test("proc-lib-node-captured-spawn", Erlang.Tests.RemoteSourceFunHostChecks.SpawnCaptured);
Test("proc-lib-node-captured-spawn-link", Erlang.Tests.RemoteSourceFunHostChecks.SpawnLinkCaptured);
Test("proc-lib-node-captured-spawn-opt", Erlang.Tests.RemoteSourceFunHostChecks.SpawnOptCaptured);
Test(
    "source-fun-current-old-purged-sibling-captures",
    Erlang.Tests.RemoteSourceFunImageHostChecks.CurrentOldPurgedAndSiblingCaptures
);
Test(
    "source-fun-fresh-receiver-lexical-boundaries",
    Erlang.Tests.RemoteSourceFunLexicalHostChecks.FreshReceiverLexicalCaptureBoundaries
);
Test("runtime/length-parse-all", Erlang.Tests.ListLengthHostChecks.ParseAll);
Test("runtime/length-ordinary-progress", Erlang.Tests.ListLengthHostChecks.OrdinaryProgress);
Test(Erlang.Tests.ListLengthHostNames.OrdsetSizeProgressTest, Erlang.Tests.ListLengthHostChecks.OrdsetSizeProgress);
Test(Erlang.Tests.ListLengthHostNames.OrdsetSizeCancellationTest, Erlang.Tests.ListLengthHostChecks.OrdsetSizeCancellation);
Test(Erlang.Tests.ListLengthHostNames.OrdsetSizeSuspendResumeTest, Erlang.Tests.ListLengthHostChecks.OrdsetSizeSuspendResume);
Test(
    Erlang.Tests.ListLengthHostNames.LateImproperOriginalArgumentTest,
    Erlang.Tests.ListLengthHostChecks.LateImproperOriginalArgument
);
Test("runtime/length-ordinary-cancellation", Erlang.Tests.ListLengthHostChecks.OrdinaryCancellation);
Test("runtime/length-guard-progress", Erlang.Tests.ListLengthHostChecks.GuardProgress);
Test("runtime/length-guard-cancellation", Erlang.Tests.ListLengthHostChecks.GuardCancellation);
Test("runtime/length-receive-progress", Erlang.Tests.ListLengthHostChecks.ReceiveProgress);
Test("runtime/length-receive-cancellation", Erlang.Tests.ListLengthHostChecks.ReceiveCancellation);
Test("runtime/length-dynamic-map-key-progress", Erlang.Tests.ListLengthHostChecks.DynamicMapKeyProgress);
Test("runtime/length-dynamic-bit-size-progress", Erlang.Tests.ListLengthHostChecks.DynamicBitSizeProgress);
Test("runtime/length-receive-priority-revalidation", Erlang.Tests.ListLengthHostChecks.ReceivePriorityRevalidation);
Test("runtime/length-receive-candidate-removal", Erlang.Tests.ListLengthHostChecks.ReceiveCandidateRemoval);
Test("runtime/length-ordinary-suspend-resume", Erlang.Tests.ListLengthHostChecks.OrdinarySuspendResume);
Test("runtime/length-guard-suspend-resume", Erlang.Tests.ListLengthHostChecks.GuardSuspendResume);
Test("runtime/length-receive-suspend-resume", Erlang.Tests.ListLengthHostChecks.ReceiveSuspendResume);
Test(
    "runtime/length-resolved-budget-excludes-guard-suspension",
    Erlang.Tests.ListLengthReceiveTimerHostChecks.ResolvedBudgetExcludesGuardSuspension
);
Test(Erlang.Tests.ProcLibLifecycleHostNames.ParseAll, Erlang.Tests.ProcLibLifecycleMfaContracts.ParseAll);
Test(Erlang.Tests.ErrorLoggerTermLimitHostNames.Test, Erlang.Tests.ErrorLoggerTermLimitHostChecks.Run);
Test(Erlang.Tests.ProcLibLifecycleHostNames.Isolation, Erlang.Tests.ProcLibLifecycleHostChecks.IsolatedConfiguration);
Test(Erlang.Tests.ProcLibLifecycleHostNames.Await, Erlang.Tests.ProcLibLifecycleHostChecks.AwaitHandler);
Test(Erlang.Tests.ProcLibLifecycleHostNames.Streams, Erlang.Tests.ProcLibLifecycleHostChecks.ActualStandardOutput);
Test(Erlang.Tests.ProcLibLifecycleHostNames.Cancellation, Erlang.Tests.ProcLibLifecycleHostChecks.CancelHandler);
Test(Erlang.Tests.ProcLibLifecycleHostNames.Observations, Erlang.Tests.ProcLibLifecycleObservationChecks.Run);
Test(Erlang.Tests.ProcLibReportProbeNames.PrimitiveTest, Erlang.Tests.ProcLibReportProbeChecks.CapturePrimitives);
Test(Erlang.Tests.ProcLibReportProbeNames.PrettyTest, Erlang.Tests.ProcLibReportProbeChecks.PrettySourceContracts);
Test(Erlang.Tests.ProcLibReportProbeNames.CallbackTest, Erlang.Tests.ProcLibReportProbeChecks.CallbackAwaitControl);
Test(Erlang.Tests.ProcLibReportProbeNames.DeepUnicodeTest, Erlang.Tests.ProcLibUnicodeContinuationHostChecks.Run);
Test(Erlang.Tests.HaltLifecycleHostNames.Test, Erlang.Tests.HaltLifecycleHostChecks.Run);
Test(Erlang.Tests.HaltPlatformHostNames.Test, Erlang.Tests.HaltPlatformHostChecks.Run);
Test(Erlang.Tests.HaltPortHostNames.Test, Erlang.Tests.HaltPortHostChecks.Run);
Test(Erlang.Tests.HaltSubprocessHostNames.Test, Erlang.Tests.HaltSubprocessHostChecks.Run);
Test(Erlang.Tests.CpuTopologyStateHostNames.Test, Erlang.Tests.CpuTopologyStateHostChecks.Run);
Test(Erlang.Tests.RuntimeStatisticsHostParameters.Test, Erlang.Tests.RuntimeStatisticsHostChecks.Run);
Test(Erlang.Tests.MatchSpecHostNames.Test, Erlang.Tests.MatchSpecHostChecks.Run);
Test(Erlang.Tests.ProcessTraceHostParameters.Test, Erlang.Tests.ProcessTraceHostChecks.Run);
Test(Erlang.Tests.SequenceTraceHostParameters.Test, Erlang.Tests.SequenceTraceHostChecks.Run);
Test(Erlang.Tests.SystemMonitorHostNames.Test, Erlang.Tests.SystemMonitorHostChecks.Run);
Test(Erlang.Tests.SystemProfileHostParameters.Test, Erlang.Tests.SystemProfileHostChecks.Run);
Test(Erlang.Tests.ProcessDisplayHostNames.Test, Erlang.Tests.ProcessDisplayHostChecks.Run);
Test(Erlang.Tests.SequenceExitHostParameters.Test, Erlang.Tests.SequenceExitHostChecks.Run);
Test(Erlang.Tests.JointPlatformHostNames.Test, Erlang.Tests.JointPlatformHostChecks.Run);
Test(Erlang.Tests.SpawnRequestHostParameters.Test, Erlang.Tests.SpawnRequestHostChecks.Run);
Test(Erlang.Tests.NativeRecordHostNames.Test, Erlang.Tests.NativeRecordHostChecks.Run);
Test(Erlang.Tests.NativeRecordDefinitionHostNames.Test, Erlang.Tests.NativeRecordDefinitionHostChecks.Run);
Test(Erlang.Tests.NativeRecordCompletionHostNames.Test, Erlang.Tests.NativeRecordCompletionHostChecks.Run);
Test(Erlang.Tests.ModuleLoadingHostNames.TestName, Erlang.Tests.ModuleLoadingHostChecks.Run);
Test(Erlang.Tests.ProcessTableHostNames.TestName, Erlang.Tests.ProcessTableHostChecks.Run);
Test(Erlang.Tests.ReferenceTextHostNames.TestName, Erlang.Tests.ReferenceTextHostChecks.Run);
Test(Erlang.Tests.FunctionResolutionHostNames.TestName, Erlang.Tests.FunctionResolutionHostChecks.Run);
Test(Erlang.Tests.PreloadedModuleHostNames.TestName, Erlang.Tests.PreloadedModuleHostChecks.Run);
Test(Erlang.Tests.ModuleInformationHostNames.Test, Erlang.Tests.ModuleInformationHostChecks.Run);
Test(Erlang.Tests.ModuleAttributeHostNames.Test, Erlang.Tests.ModuleAttributeHostChecks.Run);
Test(Erlang.Tests.NifDeclarationHostNames.Test, Erlang.Tests.NifDeclarationHostChecks.Run);
Test(Erlang.Tests.NifImageBindingHostNames.Test, Erlang.Tests.NifImageBindingHostChecks.Run);
Test(Erlang.Tests.CodeModificationHostNames.Test, Erlang.Tests.CodeModificationHostChecks.Run);
Test(Erlang.Tests.BeamOnLoadCallbackFaultHostNames.Test, Erlang.Tests.BeamOnLoadCallbackFaultHostChecks.Run);
Test(Erlang.Tests.NifAdmissionHostNames.Test, Erlang.Tests.NifAdmissionHostChecks.Run);
Test(Erlang.Tests.NifSourceEntryHostNames.Test, Erlang.Tests.NifSourceEntryHostChecks.Run);
Test(Erlang.Tests.SourceLoadNifHostNames.Test, Erlang.Tests.SourceLoadNifHostChecks.Run);
Test(Erlang.Tests.SourcePurgeStageHostNames.Test, Erlang.Tests.SourcePurgeStageHostChecks.Run);
Test(Erlang.Tests.BeamClosurePurgeHostNames.Test, Erlang.Tests.BeamClosurePurgeHostChecks.Run);
Test(Erlang.Tests.RawOnLoadExportsHostNames.Test, Erlang.Tests.RawOnLoadExportsHostChecks.Run);
Test(Erlang.Tests.NifBeamEntryHostNames.Test, Erlang.Tests.NifBeamEntryHostChecks.Run);
Test(Erlang.Tests.NifInventoryHostNames.Test, Erlang.Tests.NifInventoryHostChecks.Run);
Test(Erlang.Tests.NifAllocationLedgerHostNames.Test, Erlang.Tests.NifAllocationLedgerHostChecks.Run);
Test(Erlang.Tests.NifAllocationDomainHostNames.Test, Erlang.Tests.NifAllocationDomainHostChecks.Run);
Test(Erlang.Tests.NifReallocationHostNames.Test, Erlang.Tests.NifReallocationHostChecks.Run);
Test(Erlang.Tests.NifLifetimeHostNames.Test, Erlang.Tests.NifLifetimeHostChecks.Run);
Test(Erlang.Tests.DistributionExitLifecycleHostNames.Test, Erlang.Tests.DistributionExitLifecycleHostChecks.Run);
Test(Erlang.Tests.DistributionSystemOutputHostNames.Test, Erlang.Tests.DistributionSystemOutputHostChecks.Run);
Test(
    Erlang.Tests.DistributionPublicationLifecycleHostNames.Test,
    Erlang.Tests.DistributionPublicationLifecycleHostChecks.Run
);
Test(Erlang.Tests.DistributionOutputAdmissionHostNames.Test, Erlang.Tests.DistributionOutputAdmissionHostChecks.Run);
Test(Erlang.Tests.DistributionGroupLeaderOutputHostNames.Test, Erlang.Tests.DistributionGroupLeaderOutputHostChecks.Run);
Test(Erlang.Tests.DistributionGuestSendHostNames.Test, Erlang.Tests.DistributionGuestSendHostChecks.Run);

Test(
    Erlang.Tests.DistributionGuestLinkHostNames.LinkTest,
    Erlang.Tests.DistributionGuestLinkPublicRouteHostChecks.LinkPublishes
);

Test(
    Erlang.Tests.DistributionGuestLinkHostNames.UnlinkTest,
    Erlang.Tests.DistributionGuestLinkPublicRouteHostChecks.InboundUnlinkPublishes
);

Test(Erlang.Tests.DistributionGuestLinkHostNames.LifecycleTest, Erlang.Tests.DistributionGuestLinkHostChecks.Run);
Test(
    Erlang.Tests.DistributionGuestExitHostNames.PublicTwoTest,
    Erlang.Tests.DistributionGuestExitPublicRouteHostChecks.ExitTwoPublishes
);

Test(
    Erlang.Tests.DistributionGuestExitHostNames.PublicThreeTest,
    Erlang.Tests.DistributionGuestExitPublicRouteHostChecks.ExitThreePublishes
);

Test(Erlang.Tests.DistributionGuestExitHostNames.LifecycleTest, Erlang.Tests.DistributionGuestExitHostChecks.Run);
Test(
    Erlang.Tests.DistributionControlWaitHostNames.QueryTest,
    Erlang.Tests.DistributionControlWaitPublicRouteHostChecks.RunQueries
);
Test(
    Erlang.Tests.DistributionControlWaitHostNames.KillTest,
    Erlang.Tests.DistributionControlWaitPublicRouteHostChecks.RunKills
);
Test(Erlang.Tests.DistributionControlWaitHostNames.ContractsTest, Erlang.Tests.DistributionControlWaitHostChecks.Run);
Test(Erlang.Tests.DistributionRetainedLinkHostNames.Test, Erlang.Tests.DistributionRetainedLinkHostChecks.Run);
Test(Erlang.Tests.ActualMinimalBeamGuestSendHostNames.Test, Erlang.Tests.ActualMinimalBeamGuestSendHostChecks.Run);
Test(Erlang.Tests.AsyncDistributionHostNames.Test, Erlang.Tests.AsyncDistributionHostChecks.Run);
Test(Erlang.Tests.ProcessInfoAsyncDistributionHostNames.Test, Erlang.Tests.ProcessInfoAsyncDistributionHostChecks.Run);
Test(Erlang.Tests.SpawnAsyncDistributionHostNames.Test, Erlang.Tests.SpawnAsyncDistributionHostChecks.Run);
Test(Erlang.Tests.ProcessErrorHandlerHostNames.Test, Erlang.Tests.ProcessErrorHandlerHostChecks.Run);
Test(Erlang.Tests.UndefinedLambdaPublicationHostNames.Test, Erlang.Tests.UndefinedLambdaPublicationHostChecks.Run);
Test(Erlang.Tests.NonTailStackLifecycleHostNames.Test, Erlang.Tests.NonTailStackLifecycleHostChecks.Run);
Test(Erlang.Tests.ModuleDeletionHostNames.Test, Erlang.Tests.ModuleDeletionHostChecks.Run);
Test(Erlang.Tests.SourceTailHostNames.Test, Erlang.Tests.SourceTailHostChecks.Run);
Test(Erlang.Tests.RuntimeTailHostNames.Test, Erlang.Tests.RuntimeTailHostChecks.Run);
Test(Erlang.Tests.CodeImageHostNames.Test, Erlang.Tests.CodeImageHostChecks.Run);
Test(Erlang.Tests.ProcessCodeHostNames.Test, Erlang.Tests.ProcessCodeHostChecks.Run);
Test(Erlang.Tests.ProcessGarbageCollectionHostNames.Test, Erlang.Tests.ProcessGarbageCollectionHostChecks.Run);
Test(Erlang.Tests.ProcessInfoObservableHostNames.Test, Erlang.Tests.ProcessInfoObservableHostChecks.Run);
Test(Erlang.Tests.FunctionReloadHostNames.Test, Erlang.Tests.FunctionReloadHostChecks.Run);
Test(Erlang.Tests.CodePurgeHostNames.Test, Erlang.Tests.CodePurgeHostChecks.Run);
Test(Erlang.Tests.ExactImageCodeHostNames.Test, Erlang.Tests.ExactImageCodeHostChecks.Run);
Test(Erlang.Tests.StandardStreamHostNames.Test, Erlang.Tests.StandardStreamHostChecks.Run);
Test(Erlang.Tests.ProcessSuspensionHostNames.Test, Erlang.Tests.ProcessSuspensionHostChecks.Run);
Test(Erlang.Tests.TermDisplayHostNames.Test, Erlang.Tests.TermDisplayHostChecks.Run);
Test(Erlang.Tests.RuntimeNodeQueryHostNames.Test, Erlang.Tests.RuntimeNodeQueryHostChecks.Run);
Test(Erlang.Tests.NodeSpawnHostNames.Test, Erlang.Tests.NodeSpawnHostChecks.Run);
Test(Erlang.Tests.RuntimeNodeControlHostNames.Test, Erlang.Tests.RuntimeNodeControlHostChecks.Run);
Test(Erlang.Tests.ProcessSavedCallHostNames.Test, Erlang.Tests.ProcessSavedCallHostChecks.Run);
Test(Erlang.Tests.PortHostParameters.Test, Erlang.Tests.PortHostChecks.Run);
Test(Erlang.Tests.PortDrainHostParameters.Test, Erlang.Tests.PortDrainHostChecks.Run);
Test(Erlang.Differential.DescriptorIoNames.CancellationTest, Erlang.Differential.DescriptorIoContracts.RunCancellation);
foreach (var fixture in Erlang.Differential.DescriptorIoCases.All)
    Test("runtime/port-fd-io/" + fixture.Name, async () => await Erlang.Differential.DescriptorIoContracts.RunSdk(fixture));
Test(Erlang.Tests.StandardStreamHostNames.ProtocolTest, Erlang.Tests.StandardStreamProtocolChecks.Run);
Test("runtime/exit-pending-kill-cleans-concurrent-alias-signals", Erlang.Tests.ProcessExitHostChecks.PendingKillCleanup);
Test("runtime/io-monitored-group-leader-concurrent-output", Erlang.Tests.IoProtocolHostChecks.ConcurrentOutput);
Test("runtime/monitor-concurrent-termination-and-cancellation", Erlang.Tests.ProcessMonitorHostChecks.ConcurrentTermination);
Test(
    "runtime/alias-pending-signal-selective-wakeup-and-reply-consumption",
    Erlang.Tests.ProcessAliasHostChecks.PendingWakeups
);
Test("runtime/alias-priority-mailbox-removal-and-wakeup", Erlang.Tests.ProcessAliasHostChecks.Mailbox);
Test("runtime/alias-concurrent-reply-wire-identity-owner-cleanup", Erlang.Tests.ProcessAliasHostChecks.Lifecycle);
Test("erlang/hash-iterative-deep-terms-and-no-binary-payload-copy", Erlang.Tests.ErlangHashHostChecks.Resources);
Test("serialization/zero-word-references-reject-out-of-packet-first-word", EtfHostChecks.ZeroWordReferences);
Test("serialization/iovec-borrowing-immutable-fragments-size-and-patching", EtfHostChecks.IoVec);
Test("serialization/decoded-function-environment-and-local-runtime-domains", EtfHostChecks.FunctionsAndLocalDomains);
Test("terms/reference-words-equality-hash-and-allocation-rollover", EtfHostChecks.References);
Test(
    "serialization/physical-zlib-boundaries-trailing-and-truncation",
    () =>
{
    EtfHostChecks.CompressionBoundary();

    return Task.CompletedTask;
}
);
Test(
    "runtime/initialization-ownership-atomic-registry-lifecycle",
    async () => Check(await InitializationLifecycleChecks.Run() == 0)
);
Test("terms/local-function-code-environment-hash-ownership", async () => Check(await LocalFunctionIdentityChecks.Run() == 0));
Test(
    "runtime/external-function-late-binding-context-and-hash",
    async () =>
{
    await using var origin = new ProcessRuntime("function_origin");
    await using var target = new ProcessRuntime("function_target");
    FunctionTerm? function = null;
    var first = origin.Spawn(async context =>
    {
        function = (FunctionTerm)await origin.Modules.Call(
            context,
            "erlang",
            "make_fun",
            Term.A("late_function"),
            Term.A("current"),
            Term.I(0)
        );
        Check(!origin.Modules.HasExport("late_function", "current", 0));
        origin.Modules.Register(
            "late_function",
            "current",
            0,
            (caller, _) => ValueTask.FromResult<Term>(caller.Self)
        );
        Equal(await function.Invoke(context, []), context.Self);
        Term same = await origin.Modules.Call(
            context,
            "erlang",
            "make_fun",
            Term.A("late_function"),
            Term.A("current"),
            Term.I(0)
        );
        Equal(function, same);
        Check(function.GetHashCode() == same.GetHashCode());
        Check(new HashSet<Term> { function }.Contains(same));

        return Term.A("done");
    });
    Equal(await first.Completion, Term.A("normal"));
    Check(function is not null);
    target.Modules.Register(
        "late_function",
        "current",
        0,
        (caller, _) => ValueTask.FromResult<Term>(caller.Self)
    );
    var second = target.Spawn(async context =>
    {
        Equal(await function!.Invoke(context, []), context.Self);

        return Term.A("done");
    });
    Equal(await second.Completion, Term.A("normal"));
}
);
Test(
    "runtime/md5-forged-pending-length-safety",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    int rejected = 0;
    var process = runtime.Spawn(async context =>
    {
        var initial = (BitString)await runtime.Modules.Call(context, "erlang", "md5_init");
        byte[] corrupted = initial.ToArray();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(corrupted.AsSpan(24), 65);
        foreach (string operation in new[] { "md5_update", "md5_final" })
        {
            Term[] arguments = operation == "md5_update" ? [new BitString(corrupted), Nil.Value] : [new BitString(corrupted)];
            try
            {
                await runtime.Modules.Call(
                    context,
                    "erlang",
                    operation,
                    arguments
                );
            }
            catch (ErlangException exception)
            {
                Equal(exception.Reason, Term.A("badarg"));
                rejected++;
            }
        }

        return Term.A("done");
    });
    Equal(await process.Completion, Term.A("normal"));
    Check(rejected == 2);
}
);
Test("oracle/prefetch-order-and-bound", Erlang.Tests.OracleExecutionPipelineTests.OrderingAndBound);
Test("oracle/prefetch-failure-provenance", Erlang.Tests.OracleExecutionPipelineTests.OrderedFailureAndCleanup);
Test("oracle/prefetch-early-dispose", Erlang.Tests.OracleExecutionPipelineTests.EarlyDisposeCleanup);
Test("oracle/prefetch-callback-cleanup", Erlang.Tests.OracleExecutionPipelineTests.CallbackFailureCleanup);
Test(
    "terms/atom-name-registry-observation",
    () =>
    {
        string name = "host_observed_atom_name_115";
        var first = new Atom(name);
        Check(AtomNameRegistry.Exists(name));
        var second = Term.A(name);
        Check(!ReferenceEquals(first, second));
        Equal(first, second);
        Check(AtomNameRegistry.Exists(ProcessExitReasons.Normal));

        return Task.CompletedTask;
    }
);
Test(
    "terms/atom-name-registry-concurrent-insertion",
    () =>
    {
        string name = "concurrent_atom_115_" + Guid.NewGuid().ToString("N");
        int before = AtomNameRegistry.Count;
        Parallel.For(0, 500, _ => AtomNameRegistry.Observe(name));
        Check(AtomNameRegistry.Exists(name));
        Check(AtomNameRegistry.Count == before + 1);

        return Task.CompletedTask;
    }
);
Test(
    "terms/identity-deep-reuse",
    () =>
{
    Term value = Nil.Value;
    for (int depth = 0; depth < 20000; depth++)
        value = new Cons(value, Nil.Value);
    Check(value.Equals(value));
    Check(value.NumericEquals(value));
    Check(value.CompareTo(value) == 0);

    return Task.CompletedTask;
}
);
void AddComparatorSortTests()
{
    Test(
        "lists/sort-permutations-stability",
        async () =>
    {
        await using var runtime = new ProcessRuntime();
        var completed = new TaskCompletionSource();
        runtime.Spawn(async context =>
        {
            try
            {
                var random = new Random(791);
                var comparer = new FunctionTerm(
                    2,
                    (_, arguments) => ValueTask.FromResult<Term>(
                    ((TupleTerm)arguments[0]).Items[0].CompareTo(((TupleTerm)arguments[1]).Items[0]) <= 0 ? Term.A(ErlangBooleanAtoms.True) : Term.A(ErlangBooleanAtoms.False))
                );
                for (int sample = 0; sample < 200; sample++)
                {
                    var input = Enumerable.Range(0, sample).Select(index => Term.Tuple(Term.I(random.Next(12)), Term.I(index))).ToArray();
                    var expected = input.OrderBy(value => (int)((Integer)value.Items[0]).Value).Cast<Term>();
                    Equal(await ComparatorSort.Sort(comparer, Cons.From(input), context), Cons.From(expected));
                }
                completed.SetResult();
            }
            catch (Exception error)
            {
                completed.SetException(error);
            }

            return Term.A(ErlangBooleanAtoms.True);
        });
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }
    );
    Test(
        "lists/sort-async-large-stack",
        async () =>
    {
        await using var runtime = new ProcessRuntime();
        var completed = new TaskCompletionSource();
        runtime.Spawn(async context =>
        {
            try
            {
                int calls = 0;
                var comparer = new FunctionTerm(
                    2,
                    async (_, arguments) =>
                {
                    if (++calls % 1024 == 0)
                        await Task.Yield();

                    return arguments[0].CompareTo(arguments[1]) <= 0 ? Term.A(ErlangBooleanAtoms.True) : Term.A(ErlangBooleanAtoms.False);
                }
                );
                var values = Enumerable.Range(0, 20000).Select(value => Term.I(20000 - value));
                var sorted = await ComparatorSort.Sort(comparer, Cons.From(values), context);
                Equal(sorted, Cons.From(Enumerable.Range(1, 20000).Select(value => Term.I(value))));
                Check(calls == 19999);
                completed.SetResult();
            }
            catch (Exception error)
            {
                completed.SetException(error);
            }

            return Term.A(ErlangBooleanAtoms.True);
        });
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }
    );
}
AddComparatorSortTests();
void Test(string name, Func<Task> body) => tests.Add((name, body));
void Check(bool value, string message = "Assertion failed")
{
    if (!value)
        throw new InvalidOperationException(message);
}
void Equal(Term a, Term b) => Check(a.Equals(b), $"Expected {b}, got {a}");
void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException("Expected " + typeof(T).Name);
}
Test(
    "patterns/alias-mismatch-transaction",
    () =>
{
    var bindings = new Dictionary<string, Term> { ["incoming"] = Term.I(7) };
    var pattern = new AliasPattern(new Pattern.Variable("fresh"), new Pattern.Literal(Term.I(2)));
    Check(!pattern.Match(Term.I(1), bindings));
    Check(bindings.Count == 1 && !bindings.ContainsKey("fresh"));
    Equal(bindings["incoming"], Term.I(7));

    return Task.CompletedTask;
}
);
Test(
    "patterns/alias-same-term-and-bindings",
    () =>
{
    var bindings = new Dictionary<string, Term>();
    var pattern = new AliasPattern(new Pattern.Variable("whole"), new Pattern.Tuple([new Pattern.Variable("part")]));
    var value = Term.Tuple(Term.I(42));
    Check(pattern.Match(value, bindings));
    Check(ReferenceEquals(bindings["whole"], value));
    Equal(bindings["part"], Term.I(42));

    return Task.CompletedTask;
}
);
foreach (string source in new[] { "1 ?= 1", "maybe end", "maybe ok else end", "maybe ok ?= wrong ?= nope end", "maybe (ok ?= ok) end", "maybe ok ?= wrong else _ -> ok ?= wrong end" })
{
    Test(
        "compiler/maybe/invalid-grammar/" + source,
        () =>
    {
        Throws<CompileException>(() => new Parser(source).ParseExpression());

        return Task.CompletedTask;
    }
    );
}
Test(
    "compiler/maybe/illegal-guard",
    () =>
{
    try
    {
        new Parser("-module(maybe_guard). -export([run/0]). run()->if maybe true end -> ok end.").ParseModule();
        Check(false);
    }
    catch (CompileException exception)
    {
        Check(exception.Code == "ERL007" && exception.Message == "maybe is not legal in a guard");
    }

    return Task.CompletedTask;
}
);
Test(
    "hybrid/maybe/csharp-method-preserved",
    () =>
{
    string source = "class C { int maybe() => 1; int M() { return maybe(); } }";
    string result = CodeGeneration.Preprocess(source, "maybe-method.cs");
    Check(result.EndsWith(source, StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/maybe/nested-and-following-csharp",
    () =>
{
    string result = CodeGeneration.Preprocess("var x = maybe ok ?= maybe ok end,42 else _ -> 0 end. var y = 7;", "maybe.cs");
    Check(result.Contains("Expr.Maybe(", StringComparison.Ordinal) && result.EndsWith(" var y = 7;", StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "oracle/unicode-source-ascii-transport",
    () =>
{
    string source = "try ('𐀀' > '\uffff') of X -> {ok,X} end";
    string command = Erlang.Differential.OracleProtocol.EvaluationCommand(source);
    Check(command.All(c => c <= 127));
    string encoded = command.Split("base64:decode(\"", StringSplitOptions.None)[1].Split('"')[0];
    Check(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded)) == source + ".");
    Check(command.Contains("unicode:characters_to_list") && command.Contains("erl_scan:string") && command.Contains("erl_eval:exprs"));

    return Task.CompletedTask;
}
);
Test(
    "oracle/source-quoting-roundtrip",
    () =>
{
    string source = "{\"quote\\\" slash\\\\\", 'Привет 🌍',\n42}";
    string command = Erlang.Differential.OracleProtocol.EvaluationCommand(source);
    Check(command.All(c => c <= 127) && !command.Contains('\n'));
    string encoded = command.Split("base64:decode(\"", StringSplitOptions.None)[1].Split('"')[0];
    Check(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded)) == source + ".");

    return Task.CompletedTask;
}
);
Test(
    "style/documentation-comment-boundary",
    () =>
{
    string source = "class Example { public void First() {}\n/// <summary>Preserve this text.</summary>\npublic void Second() {} }";
    string formatted = SourceLayout.Apply(source);
    Check(formatted.Contains("/// <summary>Preserve this text.</summary>"));
    Check(formatted.Contains("}\n\n    ///"));
    Check(SourceLayout.Apply(formatted) == formatted);

    return Task.CompletedTask;
}
);
Test(
    "style/multiline-documentation-comment",
    () =>
{
    string source = "class Example { public void First() {}\n/// <summary>\n/// Preserve both lines.\n/// </summary>\npublic void Second() {} }";
    string formatted = SourceLayout.Apply(source);
    Check(formatted.Contains("/// <summary>\n    /// Preserve both lines.\n    /// </summary>"));
    Check(SourceLayout.Apply(formatted) == formatted);

    return Task.CompletedTask;
}
);
async Task<Term> Eval(string source)
{
    await using var runtime = new ProcessRuntime();
    Term? result = null;
    var e = new Parser(source).ParseExpression();
    Semantics.Validate(e);
    var p = runtime.Spawn(async c =>
 {
     result = await Execution.EvaluateAsync(e, c);

     return Term.A("ok");
 });
    Equal(await p.Completion.WaitAsync(TimeSpan.FromSeconds(3)), Term.A("normal"));

    return result!;
}
Test(
    "terms/common-atoms-reused",
    () =>
{
    string[] names = ["ok", "error", "true", "false", "undefined", "normal", "timeout", "badarg", "noproc", "shutdown"];
    foreach (string name in names)
    {
        var first = Term.A(name);
        var second = Term.A(new string(name.ToCharArray()));
        Check(ReferenceEquals(first, second));
        Equal(first, new Atom(name));
        Check(first.GetHashCode() == new Atom(name).GetHashCode());
    }

    return Task.CompletedTask;
}
);
Test(
    "terms/common-atoms-concurrent",
    () =>
{
    var expected = Term.A("ok");
    var results = new Atom[1024];
    Parallel.For(0, results.Length, index => results[index] = Term.A("ok"));
    Check(results.All(atom => ReferenceEquals(atom, expected)));

    return Task.CompletedTask;
}
);
Test(
    "terms/common-atoms-no-per-call-allocation",
    () =>
{
    var expected = Term.A("ok");
    long before = GC.GetAllocatedBytesForCurrentThread();
    bool same = true;
    for (int index = 0; index < 1024; index++)
        same &= ReferenceEquals(expected, Term.A("ok"));
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Check(same && allocated == 0);

    return Task.CompletedTask;
}
);
Test(
    "terms/arbitrary-atoms-not-retained",
    () =>
{
    foreach (string name in new[] { "application_specific_atom", "OK", "", "𐀀", "ok\0" })
    {
        var first = Term.A(name);
        var second = Term.A(name);
        Check(!ReferenceEquals(first, second));
        Equal(first, second);
        Check(first.GetHashCode() == second.GetHashCode());
    }

    return Task.CompletedTask;
}
);
Test(
    "terms/atom-factory-null-contract",
    () =>
{
    try
    {
        Term.A(null!);
        Check(false);
    }
    catch (ArgumentNullException exception)
    {
        Check(exception.ParamName == "name");
    }

    return Task.CompletedTask;
}
);
Test(
    "terms/atom-reuse-parser-etf",
    () =>
{
    var atom = Term.A("ok");
    var parsed = new Parser("ok").ParseExpression();
    Check(parsed is Expr.Literal literal && ReferenceEquals(literal.Value, atom));
    Check(ReferenceEquals(ExternalTermFormat.Decode(ExternalTermFormat.Encode(atom)), atom));

    return Task.CompletedTask;
}
);
Test(
    "terms/exact-numeric-equality",
    () =>
 {
     Check(!Term.I(1).Equals(new FloatTerm(1)));
     Check(Term.I(1).NumericEquals(new FloatTerm(1)));

     return Task.CompletedTask;
 }
);
Test(
    "terms/large-integer-double-comparison",
    () =>
 {
     Check(new Integer(BigInteger.Parse("9007199254740993")).CompareTo(new FloatTerm(9007199254740992)) > 0);
     Check(new Integer(BigInteger.One << 2000).CompareTo(new FloatTerm(double.MaxValue)) > 0);

     return Task.CompletedTask;
 }
);
Test(
    "terms/integer-double-nearest-even",
    () =>
 {
     foreach ((string source, double expected) in new[] { ("9007199254740993", 9007199254740992.0), ("9007199254740995", 9007199254740996.0), ("-9007199254740995", -9007199254740996.0), ("18014398509481983", 18014398509481984.0) })
     {
         Check(new Integer(BigInteger.Parse(source)).TryToDouble(out double actual));
         Check(actual == expected);
     }

     return Task.CompletedTask;
 }
);
Test(
    "terms/integer-double-overflow",
    () =>
 {
     Check(!new Integer(BigInteger.One << 2000).TryToDouble(out _));
     BigInteger max = (BigInteger.One << 1024) - (BigInteger.One << 971);
     Check(new Integer(max + (BigInteger.One << 970) - 1).TryToDouble(out double value) && value == double.MaxValue);
     Check(!new Integer(max + (BigInteger.One << 970)).TryToDouble(out _));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/numeric-mixed-nearest-even",
    async () => Equal(
        await Eval("{9007199254740995+0.0,0.0+9007199254740995,9007199254740995*1.0,9007199254740995-9007199254740996.0}"),
        Term.Tuple(
            new FloatTerm(9007199254740996.0),
            new FloatTerm(9007199254740996.0),
            new FloatTerm(9007199254740996.0),
            new FloatTerm(0.0)
        )
    )
);
Test(
    "compiler/numeric-integer-division-rounding",
    async () =>
 {
     Equal(await Eval("9007199254740995/1"), new FloatTerm(9007199254740996.0));
     Equal(await Eval("1/9007199254740995"), new FloatTerm(1.0 / 9007199254740996.0));
 }
);
Test(
    "compiler/numeric-conversion-overflow-badarith",
    async () =>
 {
     string huge = (BigInteger.One << 2000).ToString(System.Globalization.CultureInfo.InvariantCulture);
     foreach (string source in new[] { "1/" + huge, huge + "*0.0", "0.0/" + huge })
         Equal(await MapError(source), Term.A("badarith"));
 }
);
Test(
    "compiler/numeric-conversion-guard-failure",
    async () =>
 {
     string huge = (BigInteger.One << 2000).ToString(System.Globalization.CultureInfo.InvariantCulture);
     Equal(await Eval("case " + huge + " of X when 0.0/X =:= 0.0 -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "terms/negative-fraction-comparison",
    () =>
 {
     Check(Term.I(-1).CompareTo(new FloatTerm(-1.5)) > 0);
     Check(Term.I(0).CompareTo(new FloatTerm(double.Epsilon)) < 0);

     return Task.CompletedTask;
 }
);
Test(
    "terms/type-order",
    () =>
 {
     Term[] terms = [Term.I(1), Term.A("a"), new ReferenceTerm("n", 1), new FunctionTerm(0, (c, a) => ValueTask.FromResult<Term>(Term.A("ok"))), new PortTerm("n", 1), new Pid("n", 1), Term.Tuple(), new MapTerm([]), Nil.Value, Term.List(Term.I(0)), new BitString([])];
     for (int i = 1; i < terms.Length; i++)
         Check(terms[i - 1].CompareTo(terms[i]) < 0);

     return Task.CompletedTask;
 }
);
Test(
    "terms/tuple-arity-first",
    () =>
 {
     Check(Term.Tuple(Term.I(999)).CompareTo(Term.Tuple(Term.I(0), Term.I(0))) < 0);

     return Task.CompletedTask;
 }
);
Test(
    "terms/improper-list",
    () =>
 {
     var l = new Cons(Term.I(1), Term.A("tail"));
     Equal(l, new Cons(Term.I(1), Term.A("tail")));
     Throws<ErlangException>(() => Cons.Items(l).ToArray());

     return Task.CompletedTask;
 }
);
Test(
    "terms/map-exact-keys",
    () =>
 {
     var m = new MapTerm([new(Term.I(1), Term.A("int")), new(new FloatTerm(1), Term.A("float"))]);
     Check(m.Entries.Count == 2);
     Equal(m.Get(Term.I(1)), Term.A("int"));
     Equal(m.Get(new FloatTerm(1)), Term.A("float"));

     return Task.CompletedTask;
 }
);
Test(
    "terms/map-integer-before-all-floats",
    () =>
 {
     var m = new MapTerm([new(new FloatTerm(-100), Term.A("float")), new(Term.I(100), Term.A("integer"))]);
     Check(m.Entries[0].Key is Integer);

     return Task.CompletedTask;
 }
);
Test(
    "terms/signed-zero-otp29",
    () =>
 {
     Term positive = new FloatTerm(0.0), negative = new FloatTerm(-0.0);
     Check(!positive.Equals(negative));
     Check(positive.NumericEquals(negative));
     Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(negative)), negative);

     return Task.CompletedTask;
 }
);
Test("terms/atom-codepoint-order", () =>
 {
     Check(Term.A("\U00010000").CompareTo(Term.A("\uffff")) > 0);

     return Task.CompletedTask;
 });
Test("compiler/quoted-unicode-valid-codepoint-order", async () => Equal(await Eval("'𐀀' > '\ufffd'"), Term.A("true")));
Test(
    "compiler/quoted-unicode-illegal-character",
    () =>
{
    foreach (string invalid in new[] { "\ufffe", "\uffff", "\ud800", "\udfff" })
        foreach (string quote in new[] { "'", "\"" })
            Throws<CompileException>(() => new Parser(quote + invalid + quote).ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "terms/structural-hash",
    () =>
 {
     Term[] a = [Term.Tuple(Term.I(1), Term.List(Term.A("a"))), new FloatTerm(-0.0), new BitString([255], 3), new MapTerm([new(Term.A("x"), Term.I(2))])];
     foreach (var t in a)
         Check(t.GetHashCode() == ExternalTermFormat.Decode(ExternalTermFormat.Encode(t)).GetHashCode());

     return Task.CompletedTask;
 }
);
Test("terms/unicode-list", () =>
 {
     string s = "Привет 🌍";
     Check(Term.Text(Term.String(s)) == s);

     return Task.CompletedTask;
 });
Test(
    "terms/immutable-binary",
    () =>
 {
     byte[] bytes = [255];
     var b = new BitString(bytes, 3);
     bytes[0] = 0;
     Check(b.ToArray()[0] == 224);
     var copy = b.ToArray();
     copy[0] = 0;
     Check(b.ToArray()[0] == 224);

     return Task.CompletedTask;
 }
);
Test(
    "patterns/repeated-variable",
    () =>
 {
     var p = new Pattern.Tuple([new Pattern.Variable("X"), new Pattern.Variable("X")]);
     var b = new Dictionary<string, Term>();
     Check(!p.Match(Term.Tuple(Term.I(1), Term.I(2)), b));
     Check(b.Count == 0);
     Check(p.Match(Term.Tuple(Term.I(1), Term.I(1)), b));
     Equal(b["X"], Term.I(1));

     return Task.CompletedTask;
 }
);
Test(
    "patterns/already-bound",
    () =>
 {
     var b = new Dictionary<string, Term> { { "X", Term.I(1) } };
     Check(!new Pattern.Variable("X").Match(new FloatTerm(1), b));
     Equal(b["X"], Term.I(1));

     return Task.CompletedTask;
 }
);
Test(
    "patterns/list-tail",
    () =>
 {
     var p = new Pattern.List([new Pattern.Variable("H")], new Pattern.Variable("T"));
     var b = new Dictionary<string, Term>();
     Check(p.Match(Term.List(Term.I(1), Term.I(2)), b));
     Equal(b["T"], Term.List(Term.I(2)));

     return Task.CompletedTask;
 }
);
Test(
    "mailbox/later-match-preserves-order",
    async () =>
 {
     var m = new Mailbox();
     m.Send(Term.I(1));
     m.Send(Term.A("pick"));
     m.Send(Term.I(2));
     Equal((await m.ReceiveAsync(t => t is Atom ? t : null, TimeSpan.Zero))!, Term.A("pick"));
     Equal((await m.ReceiveAsync(t => t, TimeSpan.Zero))!, Term.I(1));
     Equal((await m.ReceiveAsync(t => t, TimeSpan.Zero))!, Term.I(2));
 }
);
Test(
    "mailbox/zero-timeout-retains",
    async () =>
 {
     var m = new Mailbox();
     m.Send(Term.I(1));
     Check(await m.ReceiveAsync(t => t is Atom ? t : null, TimeSpan.Zero) == null);
     Check(m.Count == 1);
 }
);
Test(
    "mailbox/timeout",
    async () =>
 {
     var m = new Mailbox();
     var sw = Stopwatch.StartNew();
     Check(await m.ReceiveAsync(t => t, TimeSpan.FromMilliseconds(25)) == null);
     Check(sw.ElapsedMilliseconds >= 15);
 }
);
Test(
    "mailbox/concurrent-arrival",
    async () =>
 {
     var m = new Mailbox();
     var receive = m.ReceiveAsync(t => t, TimeSpan.FromSeconds(2));
     await Task.Yield();
     m.Send(Term.I(42));
     Equal((await receive)!, Term.I(42));
 }
);
Test(
    "mailbox/cancel",
    async () =>
 {
     var m = new Mailbox();
     using var cancel = new CancellationTokenSource();
     var task = m.ReceiveAsync(t => t, null, cancel.Token).AsTask();
     cancel.Cancel();
     try
     {
         await task;
         throw new InvalidOperationException();
     }
     catch (OperationCanceledException) { }
 }
);
Test(
    "mailbox/sender-order-stress",
    async () =>
 {
     var m = new Mailbox();
     await Task.WhenAll(Enumerable.Range(0, 4).Select(sender => Task.Run(() =>
     {
         for (int i = 0; i < 250; i++)
             m.Send(Term.Tuple(Term.I(sender), Term.I(i)));
     })));
     for (int sender = 0; sender < 4; sender++)
         for (int i = 0; i < 250; i++)
         {
             int s = sender;
             var v = (TupleTerm)(await m.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.I(s)) ? x : null, TimeSpan.Zero))!;
             Equal(v.Items[1], Term.I(i));
         }
     Check(m.Count == 0);
 }
);
Test(
    "runtime/process-self-and-dictionary",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var p = r.Spawn(async c =>
     {
         Equal(await r.Modules.Call(c, "erlang", "self"), c.Self);
         Equal(await r.Modules.Call(
             c,
             "erlang",
             "put",
             Term.A("key"),
             Term.I(42)
         ), Term.A("undefined"));
         Equal(await r.Modules.Call(
             c,
             "erlang",
             "get",
             Term.A("key")
         ), Term.I(42));
         Equal(await r.Modules.Call(
             c,
             "erlang",
             "erase",
             Term.A("key")
         ), Term.I(42));

         return Term.A("ok");
     });
     Equal(await p.Completion, Term.A("normal"));
 }
);
Test(
    "runtime/registration-cleanup",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
     var p = r.Spawn(async c =>
     {
         await gate.Task;

         return Term.A("ok");
     });
     r.Register("test", p.Pid);
     Equal(r.WhereIs("test"), p.Pid);
     Throws<ErlangException>(() => r.Register("other", p.Pid));
     gate.SetResult();
     Equal(await p.Completion, Term.A("normal"));
     Equal(r.WhereIs("test"), Term.A("undefined"));
 }
);
Test(
    "runtime/monitor-down",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
     var target = r.Spawn(async c =>
     {
         await release.Task;
         throw new ErlangException(Term.A("boom"), "exit");
     });
     Term? down = null;
     var observer = r.Spawn(async c =>
     {
         var reference = r.Monitor(c, target.Pid);
         release.TrySetResult();
         down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("DOWN")) && x.Items[1].Equals(reference) ? x : null, TimeSpan.FromSeconds(2));

         return Term.A("ok");
     });
     Equal(await observer.Completion, Term.A("normal"));
     Check(down is TupleTerm { Items.Count: 5 } d && d.Items[4].Equals(Term.A("boom")));
 }
);
Test(
    "runtime/monitor-noproc-and-flush",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var observer = r.Spawn(c =>
     {
         var reference = r.Monitor(c, new Pid(r.Node, 999));
         Check(c.Mailbox.Count == 1);
         r.Demonitor(c, reference, true);
         Check(c.Mailbox.Count == 0);

         return ValueTask.FromResult<Term>(Term.A("ok"));
     });
     Equal(await observer.Completion, Term.A("normal"));
 }
);
Test(
    "runtime/link-trap-exit",
    async () =>
 {
     await using var r = new ProcessRuntime();
     Term? signal = null;
     var parent = r.Spawn(async c =>
     {
         c.TrapExits = true;
         var child = r.Spawn(x => throw new ErlangException(Term.A("boom"), "exit"), c, true);
         signal = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("EXIT")) ? x : null, TimeSpan.FromSeconds(2));

         return Term.A("ok");
     });
     Equal(await parent.Completion, Term.A("normal"));
     Check(signal is TupleTerm x && x.Items[2].Equals(Term.A("boom")));
 }
);
Test(
    "runtime/link-propagation",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var parent = r.Spawn(async c =>
     {
         r.Spawn(x => throw new ErlangException(Term.A("boom"), "exit"), c, true);
         await c.ReceiveAsync(t => t);

         return Term.A("unreachable");
     });
     Equal(await parent.Completion, Term.A("boom"));
 }
);
Test(
    "runtime/normal-link-exit-ignored",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var parent = r.Spawn(async c =>
     {
         var child = r.Spawn(x => ValueTask.FromResult<Term>(Term.A("ok")), c, true);
         await child.Completion;
         Check(r.IsAlive(c.Self));

         return Term.A("ok");
     });
     Equal(await parent.Completion, Term.A("normal"));
 }
);
Test(
    "runtime/kill-untrappable",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
     var target = r.Spawn(async c =>
     {
         c.TrapExits = true;
         ready.SetResult();
         await c.ReceiveAsync(t => t);

         return Term.A("unreachable");
     });
     await ready.Task;
     var sender = r.Spawn(c =>
     {
         r.Exit(c, target.Pid, Term.A("kill"));

         return ValueTask.FromResult<Term>(Term.A("ok"));
     });
     await sender.Completion;
     Equal(await target.Completion, Term.A("killed"));
     Check(!r.IsAlive(target.Pid));
 }
);
Test(
    "runtime/1000-waiting-processes",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var ps = Enumerable.Range(0, 1000).Select(_ => r.Spawn(async c =>
     {
         Equal((await c.ReceiveAsync(t => t, TimeSpan.FromSeconds(5)))!, Term.A("go"));

         return Term.A("ok");
     })).ToArray();
     foreach (var p in ps)
         r.Send(p.Pid, Term.A("go"));
     var reasons = await Task.WhenAll(ps.Select(p => p.Completion));
     Check(reasons.All(x => x.Equals(Term.A("normal"))));
 }
);
Test("compiler/arithmetic-precedence", async () => Equal(await Eval("1 + 2 * 3"), Term.I(7)));
Test(
    "compiler/exact-equality",
    async () => Equal(await Eval("{1 == 1.0, 1 =:= 1.0}"), Term.Tuple(Term.A("true"), Term.A("false")))
);
Test(
    "compiler/case-pattern-guard",
    async () => Equal(await Eval("case {hello, 42} of {hello, X} when is_integer(X), X > 0 -> X; _ -> bad end"), Term.I(42))
);
Test(
    "compiler/guard-error-rejects",
    async () => Equal(await Eval("case a of X when hd(X) == 1 -> bad; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/guard-alternative",
    async () => Equal(await Eval("case a of X when hd(X) == 1; is_atom(X) -> ok end"), Term.A("ok"))
);
Test(
    "compiler/list-fun-map",
    async () => Equal(await Eval("lists:map(fun(X) -> X * 2 end, [1,2,3])"), Term.List(Term.I(2), Term.I(4), Term.I(6)))
);
Test(
    "compiler/receive-after-zero",
    async () => Equal(await Eval("receive {hello, X} -> X after 0 -> timeout end"), Term.A("timeout"))
);
Test("compiler/binding-and-repeat", async () => Equal(await Eval("case {1,1} of {X,X} -> X; _ -> bad end"), Term.I(1)));
Test(
    "compiler/single-assignment-mismatch",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var e = new Parser("case ok of ok -> X = 1, X = 2 end").ParseExpression();
     Semantics.Validate(e);
     var p = r.Spawn(c => Execution.EvaluateAsync(e, c));
     Check((await p.Completion) is TupleTerm t && t.Items[0] is TupleTerm error && error.Items[0].Equals(Term.A("badmatch")));
 }
);
Test(
    "compiler/unbound-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("X + 1").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/unsafe-variable-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(A) -> case A of 1 -> X = 1; _ -> ok end, X.").ParseModule());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/illegal-guard-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(X) when io:format(\"~p\", [X]) -> ok.").ParseModule());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/quoted-keyword-atom",
    async () => Equal(await Eval("{'receive', 'end'}"), Term.Tuple(Term.A("receive"), Term.A("end")))
);
Test(
    "compiler/module-clauses-private-function",
    async () =>
 {
     var m = new Parser("-module(a). -export([f/1]). f(0) -> zero; f(X) when is_integer(X) -> g(X). g(X) -> X + 1.").ParseModule();
     await using var r = new ProcessRuntime();
     m.Register(r.Modules);
     Term? result = null;
     var p = r.Spawn(async c =>
     {
         result = await r.Modules.Call(
             c,
             "a",
             "f",
             Term.I(2)
         );

         return Term.A("ok");
     });
     Equal(await p.Completion, Term.A("normal"));
     Equal(result!, Term.I(3));
     Check(!r.Modules.Exports.Contains(("a", "g", 1)));
 }
);
Test(
    "compiler/spawn-fun-child-self",
    async () =>
 {
     await using var r = new ProcessRuntime();
     Term? result = null;
     var e = new Parser("spawn(fun() -> self() end)").ParseExpression();
     var p = r.Spawn(async c =>
     {
         result = await Execution.EvaluateAsync(e, c);
         Check(result is Pid child && !child.Equals(c.Self));

         return Term.A("ok");
     });
     Equal(await p.Completion, Term.A("normal"));
 }
);
Test(
    "compiler/selective-receive-clause-order",
    async () =>
 {
     await using var r = new ProcessRuntime();
     Term? result = null;
     var e = new Parser("receive {n, X} when X > 10 -> high; {n, X} -> X after 1000 -> timeout end").ParseExpression();
     var p = r.Spawn(async c =>
     {
         result = await Execution.EvaluateAsync(e, c);

         return Term.A("ok");
     });
     r.Send(p.Pid, Term.Tuple(Term.A("other"), Term.I(0)));
     r.Send(p.Pid, Term.Tuple(Term.A("n"), Term.I(1)));
     r.Send(p.Pid, Term.Tuple(Term.A("n"), Term.I(20)));
     Equal(await p.Completion, Term.A("normal"));
     Equal(result!, Term.I(1));
 }
);
Test(
    "hybrid/csharp-strings-comments-raw-preserved",
    () =>
 {
     string source = "class A { string s = \"receive {a} -> ok end.\"; string r = \"\"\"case x of a -> b end.\"\"\"; /* receive {a} -> ok end. */ int receive() => 1; }";
     string generated = CodeGeneration.Preprocess(source, "a.cs");
     Check(generated.EndsWith(source, StringComparison.Ordinal));

     return Task.CompletedTask;
 }
);
Test(
    "hybrid/source-map-and-lowering",
    () =>
 {
     string source = "using Erlang; class A { async Task<Term> Run(ProcessContext erlangProcess) { return receive {hello, Name} -> Name after 0 -> ok end. } }";
     string generated = CodeGeneration.Preprocess(source, "a.cs");
     Check(generated.Contains("#line 1"));
     Check(generated.Contains("await global::Erlang.Compiler.Execution.EvaluateAsync"));
     Check(!generated.Contains("return receive"));

     return Task.CompletedTask;
 }
);
Test(
    "hybrid/nested-case-and-fun",
    () =>
 {
     string source = "class A { async Task Run(ProcessContext erlangProcess) { var result = case 1 of X -> fun(Y) -> X + Y end end. } }";
     string generated = CodeGeneration.Preprocess(source, "a.cs");
     Check(generated.Contains("Expr.Case"));
     Check(generated.Contains("Expr.Fun"));

     return Task.CompletedTask;
 }
);
Test(
    "etf/reference-vectors",
    () =>
 {
     Equal(ExternalTermFormat.Decode([131, 97, 42]), Term.I(42));
     Equal(ExternalTermFormat.Decode([131, 100, 0, 2, 111, 107]), Term.A("ok"));
     Equal(ExternalTermFormat.Decode([131, 104, 2, 119, 2, 111, 107, 97, 42]), Term.Tuple(Term.A("ok"), Term.I(42)));
     Check(ExternalTermFormat.Encode(Term.I(42)).SequenceEqual(new byte[] { 131, 97, 42 }));

     return Task.CompletedTask;
 }
);
Test(
    "etf/roundtrip-all-supported-kinds",
    () =>
 {
     Term[] terms = [new Integer(BigInteger.One << 300), new Integer(-(BigInteger.One << 300)), Term.I(-42), new FloatTerm(1.5), Term.A("世界"), Term.List(Term.I(1)), new Cons(Term.A("x"), Term.A("tail")), Term.Tuple(Term.A("ok"), Nil.Value), new BitString([128], 1), new BitString([0, 255]), new Pid("n@h", ulong.MaxValue, 42), new PortTerm("n@h", ulong.MaxValue, 42), new ReferenceTerm("n@h", ulong.MaxValue, 42), new MapTerm([new(Term.I(1), Term.A("a")), new(new FloatTerm(1), Term.A("b"))])];
     foreach (var t in terms)
         Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(t)), t);

     return Task.CompletedTask;
 }
);
Test(
    "etf/malformed-input-limits",
    () =>
 {
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 108, 255, 255, 255, 255]));
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 97]));
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 97, 1, 0]));
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 119, 1, 255]));
     Throws<ErlangException>(() => ExternalTermFormat.Decode([131, 112]));

     return Task.CompletedTask;
 }
);
Test(
    "etf/random-integer-property",
    () =>
 {
     var random = new Random(42);
     for (int i = 0; i < 200; i++)
     {
         byte[] bytes = new byte[random.Next(1, 128)];
         random.NextBytes(bytes);
         Term n = new Integer(new BigInteger(bytes));
         Equal(ExternalTermFormat.Decode(ExternalTermFormat.Encode(n)), n);
     }

     return Task.CompletedTask;
 }
);
Test("modules/lists-member-exact", async () => Equal(await Eval("lists:member(1, [1.0])"), Term.A("false")));
Test(
    "modules/lists-and-maps",
    async () =>
 {
     Equal(
         await Eval("{length([1,2]), lists:reverse([1,2]), lists:sum([1,2,3]), hd([ok])}"),
         Term.Tuple(
             Term.I(2),
             Term.List(Term.I(2), Term.I(1)),
             Term.I(6),
             Term.A("ok")
         )
     );
 }
);
Test(
    "compiler/tail-recursion-50000",
    async () =>
 {
     var m = new Parser("-module(loop). -export([run/1]). run(0) -> ok; run(N) -> case N > 0 of true -> run(N - 1) end.").ParseModule();
     await using var r = new ProcessRuntime();
     m.Register(r.Modules);
     Term? result = null;
     var p = r.Spawn(async c =>
     {
         result = await r.Modules.Call(
             c,
             "loop",
             "run",
             Term.I(50000)
         );

         return Term.A("ok");
     });
     Equal(await p.Completion.WaitAsync(TimeSpan.FromSeconds(10)), Term.A("normal"));
     Equal(result!, Term.A("ok"));
 }
);
Test(
    "hybrid/ordinary-fun-call",
    () =>
 {
     string source = "class C { int fun(int x)=>x; int F()=>fun(1); int G(){return fun(2);} }";
     Check(CodeGeneration.Preprocess(source, "c.cs").EndsWith(source, StringComparison.Ordinal));

     return Task.CompletedTask;
 }
);
Test(
    "hybrid/csharp-switch-and-erlang-case",
    () =>
 {
     string source = "class C { async Task F(ProcessContext erlangProcess) { switch(1){case 1: break; case 2: break; default: break;} var result = case 2 of X -> X end. } }";
     var generated = CodeGeneration.Preprocess(source, "c.cs");
     Check(generated.Contains("switch(1){case 1: break; case 2: break; default: break;}"));
     Check(generated.Contains("Expr.Case"));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/unsafe-variable-rebinding-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("-module(a). -export([f/1]). f(A) -> case A of 1 -> X = 1; _ -> ok end, X = 2.").ParseModule());

     return Task.CompletedTask;
 }
);
Test("compiler/signed-zero-pattern", async () => Equal(await Eval("case -0.0 of 0.0 -> wrong; -0.0 -> ok end"), Term.A("ok")));
Test(
    "host_proc_lib_stop_bare_runtime_ack_consumed",
    Erlang.Tests.ProcLibStopHostChecks.Run
);
Test(
    "otp/gen-server-call-cast-info-stop",
    async () =>
{
    await using var r = new ProcessRuntime();
    var callback = new CounterServer();
    var server = await GenServer.Start(r, callback, Term.I(0));
    var client = r.Spawn(async c =>
 {
     GenServer.Cast(c, server.Pid, Term.I(2));
     r.Send(server.Pid, Term.I(3));
     Equal(await GenServer.Call(c, server.Pid, Term.A("get")), Term.I(5));
     Equal(await GenServer.Call(c, server.Pid, Term.A("stop")), Term.A("ok"));

     return Term.A("ok");
 });
    Equal(await client.Completion, Term.A("normal"));
    Equal(await server.Completion, Term.A("normal"));
    Check(callback.Terminated);
}
);
Test(
    "otp/gen-server-crash-monitor",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var server = await GenServer.Start(r, new CounterServer(), Term.I(0));
     var client = r.Spawn(async c =>
     {
         try
         {
             await GenServer.Call(c, server.Pid, Term.A("crash"));
             throw new InvalidOperationException("Expected crash");
         }
         catch (ErlangException ex)
         {
             Check(ex.ExceptionClass == "exit");
         }

         return Term.A("ok");
     });
     Equal(await client.Completion, Term.A("normal"));
     Check(!r.IsAlive(server.Pid));
 }
);
Test(
    "otp/gen-server-timeout",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var server = await GenServer.Start(r, new CounterServer(), Term.I(0));
     var client = r.Spawn(async c =>
     {
         try
         {
             await GenServer.Call(
                 c,
                 server.Pid,
                 Term.A("noreply"),
                 TimeSpan.FromMilliseconds(20)
             );
             throw new InvalidOperationException("Expected timeout");
         }
         catch (ErlangException ex)
         {
             Equal(ex.Reason, Term.A("timeout"));
         }

         return Term.A("ok");
     });
     Equal(await client.Completion, Term.A("normal"));
 }
);
Test(
    "otp/gen-server-init-error",
    async () =>
 {
     await using var r = new ProcessRuntime();
     try
     {
         await GenServer.Start(r, new CounterServer(), Term.A("fail"));
         throw new InvalidOperationException("Expected init failure");
     }
     catch (ErlangException ex)
     {
         Equal(ex.Reason, Term.A("init_failed"));
     }
 }
);
foreach (var strategy in new[] { RestartStrategy.OneForOne, RestartStrategy.OneForAll, RestartStrategy.RestForOne })
    Test(
        "otp/supervisor-" + strategy,
        async () =>
    {
        await using var r = new ProcessRuntime();
        int[] starts = [0, 0, 0];
        var restartObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ChildSpec Spec(int index) => new(
            Term.I(index),
            c =>
 {
     if (Interlocked.Increment(ref starts[index]) == 2 && index == 1)
         restartObserved.TrySetResult();

     return ValueTask.FromResult(r.Spawn(async child =>
     {
         await child.ReceiveAsync(t => t);

         return Term.A("ok");
     }, c, true));
 }
        );
        var supervisor = await Supervisor.Start(
            r,
            [Spec(0), Spec(1), Spec(2)],
            strategy,
            5
        );
        var initial = supervisor.Children.Select(x => x.Pid).ToArray();
        var killer = r.Spawn(c =>
 {
     r.Exit(c, initial[1], Term.A("boom"));

     return ValueTask.FromResult<Term>(Term.A("ok"));
 });
        await killer.Completion;
        await restartObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        // Wait for the restart batch to finish publishing its complete child list.
        var deadline = Stopwatch.StartNew();
        while (supervisor.Children.Count != 3 && deadline.Elapsed < TimeSpan.FromSeconds(2))
            await Task.Delay(1);
        Check(starts[0] == (strategy == RestartStrategy.OneForAll ? 2 : 1));
        Check(starts[1] == 2);
        Check(starts[2] == (strategy == RestartStrategy.OneForOne ? 1 : 2));
        await Supervisor.Stop(r, supervisor);
        Equal(await supervisor.Process.Completion, Term.A("shutdown"));
        Check(supervisor.Children.Count == 0);
    }
    );
Test(
    "otp/supervisor-simple-host-template-required",
    async () =>
    {
        await using var runtime = new ProcessRuntime();
        try
        {
            await Supervisor.Start(runtime, [], RestartStrategy.SimpleOneForOne);
            throw new InvalidOperationException("Expected missing simple MFA template rejection");
        }
        catch (ErlangException error)
        {
            Equal(error.Reason, Term.A("badarg"));
        }
    }
);
Test(
    "otp/supervisor-transient-temporary-normal",
    async () =>
 {
     await using var r = new ProcessRuntime();
     int starts = 0;
     var childReady = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
     var supervisor = await Supervisor.Start(
         r,
         [new ChildSpec(
             Term.A("t"),
             c =>
 {
 starts++;
 var p = r.Spawn(async x =>
 {
 await x.ReceiveAsync(t => t);

 return Term.A("ok");
 }, c, true);
 childReady.SetResult(p.Pid);

 return ValueTask.FromResult(p);
 },
             RestartPolicy.Transient
         )]
     );
     var pid = await childReady.Task;
     r.Send(pid, Term.A("finish"));
     var deadline = Stopwatch.StartNew();
     while (supervisor.Children.Count != 0 && deadline.Elapsed < TimeSpan.FromSeconds(2))
         await Task.Delay(1);
     Check(starts == 1 && supervisor.Children.Count == 0);
     await Supervisor.Stop(r, supervisor);
 }
);
Test(
    "otp/supervisor-restart-intensity",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var trigger = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
     var supervisor = await Supervisor.Start(
         r,
         [new ChildSpec(
             Term.A("child"),
             c =>
 {
 var p = r.Spawn(async x =>
 {
 await x.ReceiveAsync(t => t);
 x.Exit(Term.A("boom"));

 return Term.A("ok");
 }, c, true);
 trigger.TrySetResult(p.Pid);

 return ValueTask.FromResult(p);
 }
         )],
         intensity: 0
     );
     r.Send(await trigger.Task, Term.A("crash"));
     Equal(await supervisor.Process.Completion.WaitAsync(TimeSpan.FromSeconds(2)), Term.A("shutdown"));
     Check(supervisor.Children.Count == 0);
 }
);
Test(
    "otp/supervisor-temporary-abnormal-no-restart",
    async () =>
 {
     await using var r = new ProcessRuntime();
     int starts = 0;
     var supervisor = await Supervisor.Start(
         r,
         [new ChildSpec(
             Term.A("child"),
             c =>
 {
 starts++;

 return ValueTask.FromResult(r.Spawn(async x =>
 {
 await x.ReceiveAsync(t => t);
 x.Exit(Term.A("boom"));

 return Term.A("ok");
 }, c, true));
 },
             RestartPolicy.Temporary
         )],
         intensity: 0
     );
     r.Send(supervisor.Children[0].Pid, Term.A("crash"));
     var deadline = Stopwatch.StartNew();
     while (supervisor.Children.Count != 0 && deadline.Elapsed < TimeSpan.FromSeconds(2))
         await Task.Delay(1);
     Check(starts == 1 && supervisor.Children.Count == 0);
     Check(r.IsAlive(supervisor.Process.Pid));
     await Supervisor.Stop(r, supervisor);
 }
);
Test(
    "runtime/link-cascade-10000",
    async () =>
 {
     await using var r = new ProcessRuntime();
     ProcessContext? previous = null;
     var processes = new List<ProcessHandle>();
     for (int i = 0; i < 10000; i++)
     {
         var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
         var p = r.Spawn(async c =>
         {
             ready.SetResult(c);
             await c.ReceiveAsync(t => t);

             return Term.A("ok");
         }, previous, previous is not null);
         processes.Add(p);
         previous = await ready.Task;
     }
     var killer = r.Spawn(c =>
     {
         r.Exit(c, processes[0].Pid, Term.A("boom"));

         return ValueTask.FromResult<Term>(Term.A("ok"));
     });
     await killer.Completion;
     var reasons = await Task.WhenAll(processes.Select(p => p.Completion));
     Check(reasons.All(x => x.Equals(Term.A("boom"))));
 }
);
Test(
    "runtime/spawn-monitor-immediate-exit-atomic",
    async () =>
 {
     await using var r = new ProcessRuntime();
     var parent = r.Spawn(async c =>
     {
         for (int i = 0; i < 200; i++)
         {
             var spawn = r.SpawnMonitor(c, x => throw new ErlangException(Term.A("boom"), "exit"));
             var down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items.Count == 5 && x.Items[1].Equals(spawn.Reference) ? x : null, TimeSpan.FromSeconds(1));
             Check(down is not null && down.Items[4].Equals(Term.A("boom")));
         }

         return Term.A("ok");
     });
     Equal(await parent.Completion, Term.A("normal"));
 }
);
Test("terms/bitstring-format", () =>
 {
     Check(new BitString([224], 3).ToString() == "<<7:3>>");

     return Task.CompletedTask;
 });
Test(
    "compiler/large-literal-codegeneration",
    () =>
 {
     var term = Term.String(new string('a', 10000));
     var generated = CodeGeneration.TermCode(term);
     Check(generated.StartsWith("global::Erlang.Cons.From", StringComparison.Ordinal));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/reserved-keywords-rejected",
    () =>
 {
     Throws<CompileException>(() => new Parser("end").ParseExpression());
     Throws<CompileException>(() => new Parser("try ok end").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/unsupported-escape-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("\"\\x{41}\"").ParseExpression());

     return Task.CompletedTask;
 }
);
Test("compiler/quoted-operator-atom", async () => Equal(await Eval("{'not', 'div'}"), Term.Tuple(Term.A("not"), Term.A("div"))));
Test(
    "hybrid/nullable-context",
    () =>
 {
     Check(CodeGeneration.Preprocess("class C { string? value; }", "c.cs").StartsWith("#nullable enable", StringComparison.Ordinal));
     Check(CodeGeneration.Preprocess("class C {}", "c.cs", "disable").StartsWith("#nullable disable", StringComparison.Ordinal));

     return Task.CompletedTask;
 }
);

Test(Erlang.Tests.ProcLibMonitorHostNames.Test, Erlang.Tests.ProcLibMonitorHostChecks.Run);
Test(Erlang.Tests.ProcLibStartupWaitHostNames.Test, Erlang.Tests.ProcLibStartupWaitHostChecks.Run);
Test(Erlang.Tests.ProcLibStartupWaitHostNames.InterpretedTest, Erlang.Tests.ProcLibStartupWaitHostChecks.RunInterpreted);

Test(Erlang.Tests.ProcLibInitialCallHostNames.Test, Erlang.Tests.ProcLibInitialCallHostChecks.Run);

Test(Erlang.Tests.ConnectedDependencyHostNames.ParseAll, Erlang.Tests.ConnectedDependencyMfaContracts.ParseAll);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host1, Erlang.Tests.ConnectedSpawnHostChecks.CodecFrames);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host2, Erlang.Tests.ConnectedSpawnHostChecks.RealRequestReplyAndDeath);
Test(Erlang.Tests.ConnectedRegisteredSendHostNames448.Test, Erlang.Tests.ConnectedRegisteredSendHostChecks448.Run);
Test(Erlang.Tests.ConnectedRemoteEpochHostNames449.Test, Erlang.Tests.ConnectedRemoteEpochHostChecks449.Run);
Test(Erlang.Tests.ConnectedCandidateHostNames450.Test, Erlang.Tests.ConnectedCandidateHostChecks450.Run);
Test(Erlang.Tests.PendingCandidateHostNames451.Test, Erlang.Tests.PendingCandidateHostChecks451.Run);
Test(Erlang.Tests.RetiredCandidateHostNames452.Test, Erlang.Tests.RetiredCandidateHostChecks452.Run);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host3, Erlang.Tests.ConnectedSpawnRetirementHostChecks.StaleReplyCannotRebind);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host4, Erlang.Tests.ConnectedSpawnCleanupHostChecks.ParentDeathBeforeReply);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host5, Erlang.Tests.ConnectedSpawnCleanupHostChecks.AbandonBeforeReply);
Test(
    Erlang.Tests.ConnectedDependencyHostNames.Host6,
    Erlang.Tests.ConnectedSpawnCleanupHostChecks.ParentDeathAfterReplyBeforeSelection
);
Test(
    Erlang.Tests.ConnectedDependencyHostNames.Host7,
    Erlang.Tests.ConnectedSpawnCapacityHostChecks.ProducerDeathDuringCapacityWait
);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host8, Erlang.Tests.ConnectedSpawnTraceHostChecks.TracedSuccess);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host9, Erlang.Tests.ConnectedSpawnTraceHostChecks.TracedCreatorError);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host10, Erlang.Tests.ConnectedSpawnMonitorOptionHostChecks.ExplicitAlias);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host11, Erlang.Tests.ConnectedSpawnMonitorOptionHostChecks.DemonitorAlias);
Test(
    Erlang.Tests.ConnectedDependencyHostNames.Host12,
    Erlang.Tests.ConnectedSpawnMonitorOptionHostChecks.ReplyDemonitorAlias
);
Test(
    Erlang.Tests.ConnectedDependencyHostNames.Host13,
    Erlang.Tests.ConnectedSpawnMonitorOptionHostChecks.DemonitorAfterSuccess
);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host14, Erlang.Tests.ConnectedSpawnPriorityLinkHostChecks.PriorityLinkedExit);
Test(
    Erlang.Tests.ConnectedDependencyHostNames.Host15,
    Erlang.Tests.ConnectedSpawnWireOptionHostChecks.WireOrderAndLastTuplePrecedence
);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host16, Erlang.Tests.ConnectedSpawnAliasWireHostChecks.PlainSend2);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host17, Erlang.Tests.ConnectedSpawnAliasWireHostChecks.TracedSend2);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host18, Erlang.Tests.ConnectedSpawnAliasWireHostChecks.PlainSend3);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host19, Erlang.Tests.ConnectedSpawnAliasWireHostChecks.AlternativeTracedSend3);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host20, Erlang.Tests.ConnectedRpcLocalHostChecks.LocalCallerAndExceptions);
Test(
    Erlang.Tests.ConnectedDependencyHostNames.Host21,
    Erlang.Tests.ConnectedRpcLabelHostChecks.VisibleLabelDeathAndNodeRetirement
);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host22, Erlang.Tests.ConnectedRpcTimeoutHostChecks.QueuedDownBeforeZeroWait);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host23, Erlang.Tests.ConnectedRpcTimeoutHostChecks.TimeoutLosesToAdmittedDown);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host24, Erlang.Tests.ConnectedProcLibNodeHostChecks.SpawnFun);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host25, Erlang.Tests.ConnectedProcLibNodeHostChecks.SpawnMfa);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host26, Erlang.Tests.ConnectedProcLibNodeHostChecks.SpawnLinkFun);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host27, Erlang.Tests.ConnectedProcLibNodeHostChecks.SpawnLinkMfa);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host28, Erlang.Tests.ConnectedProcLibNodeHostChecks.SpawnOptFun);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host29, Erlang.Tests.ConnectedProcLibNodeHostChecks.SpawnOptMfa);
Test(Erlang.Tests.ConnectedDependencyHostNames.Host30, Erlang.Tests.RuntimeConnectedNodeQueryHostChecks.Run);

Test(
    Erlang.Tests.ConnectedDependencyHostNames.NativeAliases,
    Erlang.Tests.ConnectedSpawnNativeAliasHostChecks.ThreeMonitorAliasModes
);
Test(
    Erlang.Tests.ConnectedDependencyHostNames.LegacyTracedSend,
    Erlang.Tests.ConnectedSpawnAliasWireHostChecks.LegacyTracedSend3
);

// Every registered MFA has a direct contract smoke test. Error/option completeness still needs OTP differential coverage.
var exportRegistry = new ModuleRegistry();
CoreModules.Register(exportRegistry);
OtpModules.Register(exportRegistry);
foreach (var export in exportRegistry.Exports.Where(export => Erlang.Tests.FunctionalMfaContracts.SubprocessContract(export.Module, export.Function, export.Arity) is null && !Erlang.Tests.BeamLoaderMfaChecks.Owns(export.Module, export.Function, export.Arity)).OrderBy(x => x.Module).ThenBy(x => x.Function).ThenBy(x => x.Arity))
    Test(
        $"mfa/{export.Module}:{export.Function}/{export.Arity}",
        async () =>
    {
        if (Erlang.Tests.LibraryStringFloatMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.LibraryStringFloatMfaContracts.Run();

            return;
        }
        if (Erlang.Tests.DistributionState402MfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.DistributionState402MfaContracts.Run(export.Function);

            return;
        }
        if (Erlang.Tests.NewConnection400MfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.NewConnection400MfaContracts.Run();

            return;
        }
        if (Erlang.Tests.LibraryRuntimePrimitiveMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.LibraryRuntimePrimitiveMfaContracts.Run(export.Module, export.Function);

            return;
        }
        if (Erlang.Tests.BinarySearchMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.BinarySearchMfaContracts.Run(export.Function, export.Arity);

            return;
        }

        if (Erlang.Tests.BinaryWrappersMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.BinaryWrappersMfaContracts.Run(export.Function, export.Arity);

            return;
        }

        if (Erlang.Tests.BinaryPrimitiveMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.BinaryPrimitiveMfaContracts.Run(export.Function, export.Arity);

            return;
        }

        if (Erlang.Tests.QueueEndsMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.QueueEndsMfaContracts.Run(export.Function, export.Arity);

            return;
        }

        if (Erlang.Tests.QueueHigherMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.QueueHigherMfaContracts.Run(export.Function, export.Arity);

            return;
        }

        if (Erlang.Tests.OrdsetFullMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.OrdsetFullMfaContracts.Run(export.Function, export.Arity);

            return;
        }

        if (Erlang.Tests.OrdsetCoreMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.OrdsetCoreMfaContracts.Run(export.Function, export.Arity);

            return;
        }

        if (Erlang.Tests.QueueCoreMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.QueueCoreMfaContracts.Run(export.Function, export.Arity);

            return;
        }

        if (Erlang.Tests.ProplistGetValueMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ProplistGetValueMfaContracts.Run(export.Arity);

            return;
        }
        if (Erlang.Tests.ProplistFamilyHostChecks.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ProplistFamilyHostChecks.Mfa(export.Function, export.Arity);

            return;
        }
        if (Erlang.Tests.ConnectedDependencyMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ConnectedDependencyMfaContracts.Run(export.Module, export.Function, export.Arity);

            return;
        }
        if (Erlang.Tests.ProcLibMetadataMfaContracts.Owns(export.Module, export.Function))
        {
            await Erlang.Tests.ProcLibMetadataMfaContracts.Run(export.Function);

            return;
        }
        if (Erlang.Tests.ProcLibStopMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ProcLibStopMfaContracts.Run(export.Module, export.Function, export.Arity);

            return;
        }
        if (Erlang.Tests.ProcLibOptionMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ProcLibOptionMfaContracts.Run(export.Function, export.Arity);

            return;
        }
        if (Erlang.Tests.ProcLibStartupMfaContracts.Owns(export.Module, export.Function))
        {
            await Erlang.Tests.ProcLibStartupMfaContracts.Run(export.Function, export.Arity);

            return;
        }
        if (Erlang.Tests.ProcLibLifecycleMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ProcLibLifecycleMfaContracts.Run(export.Module, export.Function, export.Arity);

            return;
        }
        if (Erlang.Tests.ProcLibReportMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ProcLibReportMfaContracts.Run(export.Function, export.Arity);

            return;
        }
        if (Erlang.Tests.ProcLibMfaContracts.Owns(export.Module))
        {
            await Erlang.Tests.ProcLibMfaContracts.Run(export.Function, export.Arity);

            return;
        }
        if (export.Module is "gen_server" or "supervisor")
        {
            await Erlang.Tests.OtpMfaContracts.Run(export.Module, export.Function, export.Arity);

            return;
        }
        if (Erlang.Tests.ClrMemoryPublicHostChecks.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ClrMemoryPublicHostChecks.RunMfa(export.Arity);

            return;
        }
        if (Erlang.Tests.SystemAllocatorPublicHostChecks.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.SystemAllocatorPublicHostChecks.RunMfa(export.Function);

            return;
        }
        if (Erlang.Tests.FunctionalMfaContracts.FixtureName(export.Module, export.Function, export.Arity) is { } functional)
        {
            await Erlang.Tests.FunctionalMfaContracts.Run(functional);

            return;
        }
        if (Erlang.Tests.GarbageCollectionResultSmokeContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.GarbageCollectionResultSmokeContracts.Run();

            return;
        }
        if (Erlang.Tests.DistributionMfaSmokeContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.DistributionMfaSmokeContracts.Run(export.Module, export.Function, export.Arity);

            return;
        }
        using var output = new StringWriter();
        await using var runtime = new ProcessRuntime(output: output);
        var process = runtime.Spawn(async c =>
        {
            async ValueTask<Term> Call(params Term[] a) => await runtime.Modules.Call(
                c,
                export.Module,
                export.Function,
                a
            );
            (Term[] Args, Term Expected)? pure = (export.Module, export.Function, export.Arity) switch
            {
                ("erlang", "length", 1) => ([Term.List(Term.I(1), Term.I(2))], Term.I(2)),
                ("erlang", "hd", 1) => ([Term.List(Term.A("x"))], Term.A("x")),
                ("erlang", "tl", 1) => ([Term.List(Term.I(1), Term.I(2))], Term.List(Term.I(2))),
                ("erlang", "element", 2) => ([Term.I(2), Term.Tuple(Term.A("a"), Term.A("b"))], Term.A("b")),
                ("erlang", "tuple_size", 1) => ([Term.Tuple(Term.A("a"))], Term.I(1)),
                ("erlang", "is_atom", 1) => ([Term.A("a")], Term.A("true")),
                ("erlang", "is_integer", 1) => ([new FloatTerm(1)], Term.A("false")),
                ("erlang", "is_float", 1) => ([new FloatTerm(1)], Term.A("true")),
                ("erlang", "is_number", 1) => ([Term.I(1)], Term.A("true")),
                ("erlang", "is_tuple", 1) => ([Term.Tuple()], Term.A("true")),
                ("erlang", "is_binary", 1) => ([new BitString([128], 1)], Term.A("false")),
                ("erlang", "is_bitstring", 1) => ([new BitString([128], 1)], Term.A("true")),
                ("erlang", "bit_size", 1) => ([new BitString([128, 128], 9)], Term.I(9)),
                ("erlang", "byte_size", 1) => ([new BitString([128, 128], 9)], Term.I(2)),
                ("erlang", "is_list", 1) => ([new Cons(Term.I(1), Term.A("tail"))], Term.A("true")),
                ("erlang", "is_pid", 1) => ([c.Self], Term.A("true")),
                ("erlang", "is_map", 1) => ([new MapTerm([])], Term.A("true")),
                ("erlang", "map_size", 1) => ([new MapTerm([new(Term.A("a"), Term.I(1))])], Term.I(1)),
                ("erlang", "map_get", 2) => ([Term.A("a"), new MapTerm([new(Term.A("a"), Term.I(42))])], Term.I(42)),
                ("erlang", "is_map_key", 2) => ([Term.I(1), new MapTerm([new(new FloatTerm(1), Term.A("float"))])], Term.A("false")),
                ("lists", "keyfind", 3) => ([Term.I(1), Term.I(1), Term.List(Term.Tuple(new FloatTerm(1), Term.A("found")))], Term.Tuple(new FloatTerm(1), Term.A("found"))),
                ("lists", "keymember", 3) => ([Term.A("a"), Term.I(1), Term.List(Term.Tuple(Term.A("a")))], Term.A("true")),
                ("lists", "keysearch", 3) => ([Term.A("a"), Term.I(1), Term.List(Term.Tuple(Term.A("a")))], Term.Tuple(Term.A("value"), Term.Tuple(Term.A("a")))),
                ("lists", "nth", 2) => ([Term.I(2), Term.List(Term.A("a"), Term.A("b"))], Term.A("b")),
                ("lists", "nthtail", 2) => ([Term.I(1), new Cons(Term.A("a"), Term.A("tail"))], Term.A("tail")),
                ("lists", "last", 1) => ([Term.List(Term.I(1), Term.A("last"))], Term.A("last")),
                ("lists", "split", 2) => ([Term.I(1), Term.List(Term.A("a"), Term.A("b"))], Term.Tuple(Term.List(Term.A("a")), Term.List(Term.A("b")))),
                ("lists", "seq", 2) => ([Term.I(1), Term.I(3)], Term.List(Term.I(1), Term.I(2), Term.I(3))),
                ("lists", "seq", 3) => ([Term.I(5), Term.I(1), Term.I(-2)], Term.List(Term.I(5), Term.I(3), Term.I(1))),
                ("lists", "reverse", 1) => ([Term.List(Term.I(1), Term.I(2))], Term.List(Term.I(2), Term.I(1))),
                ("lists", "reverse", 2) => ([Term.List(Term.I(1), Term.I(2)), Term.A("tail")], new Cons(Term.I(2), new Cons(Term.I(1), Term.A("tail")))),
                ("lists", "append", 2) => ([Term.List(Term.I(1)), Term.A("tail")], new Cons(Term.I(1), Term.A("tail"))),
                ("lists", "append", 1) => ([Term.List(Term.List(Term.I(1)), Term.A("tail"))], new Cons(Term.I(1), Term.A("tail"))),
                ("lists", "duplicate", 2) => ([Term.I(2), Term.A("a")], Term.List(Term.A("a"), Term.A("a"))),
                ("lists", "flatten", 1) => ([Term.List(Term.List(Term.A("a")), Term.A("b"))], Term.List(Term.A("a"), Term.A("b"))),
                ("lists", "flatten", 2) => ([Term.List(Term.List(Term.A("a"))), new Cons(Term.A("b"), Term.A("tail"))], new Cons(Term.A("a"), new Cons(Term.A("b"), Term.A("tail")))),
                ("lists", "member", 2) => ([Term.I(1), Term.List(new FloatTerm(1))], Term.A("false")),
                ("lists", "sort", 2) => ([new FunctionTerm(2, (_, a) => ValueTask.FromResult<Term>(Term.A(a[0].CompareTo(a[1]) <= 0 ? "true" : "false"))), Term.List(Term.I(2), Term.I(1))], Term.List(Term.I(1), Term.I(2))),
                ("lists", "sum", 1) => ([Term.List(Term.I(1), new FloatTerm(2.5))], new FloatTerm(3.5)),
                ("maps", "get", 2) => ([Term.A("k"), new MapTerm([new(Term.A("k"), Term.I(42))])], Term.I(42)),
                ("maps", "iterator", 1) => ([new MapTerm([])], new Cons(Term.I(0), new MapTerm([]))),
                ("maps", "iterator", 2) => ([new MapTerm([]), Term.A("ordered")], new Cons(Nil.Value, new MapTerm([]))),
                ("maps", "next", 1) => ([Term.A("none")], Term.A("none")),
                ("maps", "size", 1) => ([new MapTerm([new(Term.A("k"), Term.I(42))])], Term.I(1)),
                _ => null
            };
            if (pure is { } test)
            {
                Equal(await Call(test.Args), test.Expected);

                return Term.A("ok");
            }
            switch (export.Function)
            {
                case "self":
                    Equal(await Call(), c.Self);
                    break;
                case "make_ref":
                    var first = await Call();
                    var second = await Call();
                    Check(first is ReferenceTerm && !first.Equals(second));
                    break;
                case "register":
                    Equal(await Call(Term.A("name"), c.Self), Term.A("true"));
                    Equal(runtime.WhereIs("name"), c.Self);
                    break;
                case "whereis":
                    runtime.Register("name", c.Self);
                    Equal(await Call(Term.A("name")), c.Self);
                    Equal(await Call(Term.A("absent")), Term.A("undefined"));
                    break;
                case "unregister":
                    runtime.Register("name", c.Self);
                    Equal(await Call(Term.A("name")), Term.A("true"));
                    Equal(runtime.WhereIs("name"), Term.A("undefined"));
                    break;
                case "link":
                    c.TrapExits = true;
                    Equal(await Call(new Pid(runtime.Node, 999999)), Term.A("true"));
                    var exit = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("EXIT")) ? x : null, TimeSpan.Zero);
                    Check(exit is not null && exit.Items[2].Equals(Term.A("noproc")));
                    break;
                case "unlink":
                    var linked = runtime.Spawn(async x =>
 {
     await x.ReceiveAsync(t => t);

     return Term.A("ok");
 }, c, true);
                    Equal(await Call(linked.Pid), Term.A("true"));
                    runtime.Exit(c, linked.Pid, Term.A("boom"));
                    Equal(await linked.Completion, Term.A("boom"));
                    Check(runtime.IsAlive(c.Self));
                    break;
                case "monitor":
                    var reference = await Call(Term.A("process"), new Pid(runtime.Node, 999999));
                    Check(reference is ReferenceTerm);
                    var down = await c.ReceiveAsync(t => t is TupleTerm x && x.Items[0].Equals(Term.A("DOWN")) ? x : null, TimeSpan.Zero);
                    Check(down is not null && down.Items[1].Equals(reference) && down.Items[4].Equals(Term.A("noproc")));
                    break;
                case "demonitor":
                    var monitored = runtime.Spawn(async x =>
 {
     await x.ReceiveAsync(t => t);

     return Term.A("ok");
 });
                    var monitor = runtime.Monitor(c, monitored.Pid);
                    Equal(await Call(monitor), Term.A("true"));
                    runtime.Exit(c, monitored.Pid, Term.A("kill"));
                    await monitored.Completion;
                    Check(c.Mailbox.Count == 0);
                    break;
                case "process_flag":
                    Equal(await Call(Term.A("trap_exit"), Term.A("true")), Term.A("false"));
                    Check(c.TrapExits);
                    break;
                case "exit" when export.Arity == 2:
                    var target = runtime.Spawn(async x =>
 {
     await x.ReceiveAsync(t => t);

     return Term.A("ok");
 });
                    Equal(await Call(target.Pid, Term.A("kill")), Term.A("true"));
                    Equal(await target.Completion, Term.A("killed"));
                    break;
                case "exit":
                case "error":
                case "throw":
                    try
                    {
                        await Call(Term.A("reason"));
                        throw new InvalidOperationException("Expected exception");
                    }
                    catch (ErlangException ex)
                    {
                        Equal(ex.Reason, Term.A("reason"));
                        Check(ex.ExceptionClass == export.Function);
                    }
                    break;
                case "put":
                    Equal(await Call(Term.A("k"), Term.I(1)), Term.A("undefined"));
                    Equal(await Call(Term.A("k"), Term.I(2)), Term.I(1));
                    break;
                case "get":
                    c.Dictionary[Term.A("k")] = Term.I(42);
                    Equal(await Call(Term.A("k")), Term.I(42));
                    Equal(await Call(Term.A("missing")), Term.A("undefined"));
                    break;
                case "erase":
                    c.Dictionary[Term.A("k")] = Term.I(42);
                    Equal(await Call(Term.A("k")), Term.I(42));
                    Check(!c.Dictionary.ContainsKey(Term.A("k")));
                    break;
                case "spawn":
                case "spawn_link":
                    var selfObserved = new TaskCompletionSource<Pid>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var fun = new FunctionTerm(
                        0,
                        (execution, a) =>
 {
     selfObserved.SetResult(((ProcessContext)execution).Self);

     return ValueTask.FromResult<Term>(Term.A("ok"));
 }
                    );
                    var spawned = await Call(fun);
                    Equal(await selfObserved.Task, spawned);
                    Check(!spawned.Equals(c.Self));
                    break;
                case "spawn_monitor":
                    var instant = new FunctionTerm(0, (execution, a) => ValueTask.FromResult<Term>(Term.A("ok")));
                    var pair = (TupleTerm)await Call(instant);
                    Check(pair.Items[0] is Pid && pair.Items[1] is ReferenceTerm);
                    var notification = await c.ReceiveAsync(t => t is TupleTerm x && x.Items.Count == 5 && x.Items[1].Equals(pair.Items[1]) ? x : null, TimeSpan.FromSeconds(1));
                    Check(notification is not null && notification.Items[4].Equals(Term.A("normal")));
                    break;
                case "map":
                    var doubleFun = new FunctionTerm(1, (execution, a) => ValueTask.FromResult(CoreModules.Arithmetic("*", a[0], Term.I(2))));
                    Equal(await Call(doubleFun, Term.List(Term.I(1), Term.I(2))), Term.List(Term.I(2), Term.I(4)));
                    break;
                case "format":
                    Equal(await Call(Term.String("~s ~p~~ ~n"), Term.List(Term.String("hi"), Term.I(42))), Term.A("ok"));
                    Check(output.ToString() == "hi 42~ \n");
                    break;
                default:
                    throw new InvalidOperationException("Missing direct MFA test for " + export);
            }

            return Term.A("ok");
        });
        Equal(await process.Completion.WaitAsync(TimeSpan.FromSeconds(3)), Term.A("normal"));
    }
    );


async Task<Term> MapError(string source)
{
    await using var runtime = new ProcessRuntime();
    Term? reason = null;
    var expression = new Parser(source).ParseExpression();
    Semantics.Validate(expression);
    var process = runtime.Spawn(async c =>
    {
        try
        {
            await Execution.EvaluateAsync(expression, c);
            throw new InvalidOperationException("Expected map error");
        }
        catch (ErlangException ex)
        {
            Check(ex.ExceptionClass == "error");
            reason = ex.Reason;
        }

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));

    return reason!;
}
Test(
    "compiler/map-empty-and-nonmap-pattern",
    async () => Equal(await Eval("case #{a => 1} of #{} -> case a of #{} -> wrong; _ -> ok end end"), Term.A("ok"))
);
Test("compiler/map-duplicate-last-wins", async () => Equal(await Eval("maps:get(a, #{a => 1, a => 42})"), Term.I(42)));
Test(
    "compiler/map-exact-numeric-keys",
    async () => Equal(
        await Eval("case #{1 => integer, 1.0 => float, 0.0 => positive, -0.0 => negative} of #{1 := A, 1.0 := B, 0.0 := C, -0.0 := D} -> {A,B,C,D} end"),
        Term.Tuple(
            Term.A("integer"),
            Term.A("float"),
            Term.A("positive"),
            Term.A("negative")
        )
    )
);
Test(
    "compiler/map-assoc-and-exact-update",
    async () => Equal(
        await Eval("case #{a => 1} of M -> N = M#{a := 2, b => 42}, {maps:get(a,M),maps:get(a,N),maps:get(b,N)} end"),
        Term.Tuple(Term.I(1), Term.I(2), Term.I(42))
    )
);
Test("compiler/map-mixed-duplicate-update", async () => Equal(await Eval("maps:get(a, #{}#{a => 1, a := 2, a => 3})"), Term.I(3)));
Test("compiler/map-chained-update", async () => Equal(await Eval("maps:get(a, #{}#{a => 1}#{a := 42})"), Term.I(42)));
Test(
    "compiler/map-badkey",
    async () => Equal(await MapError("#{}#{missing := 1}"), Term.Tuple(Term.A("badkey"), Term.A("missing")))
);
Test(
    "compiler/map-exact-before-assoc-fails",
    async () => Equal(await MapError("#{}#{a := 1, a => 2}"), Term.Tuple(Term.A("badkey"), Term.A("a")))
);
Test(
    "compiler/map-badmap-empty-update",
    async () => Equal(await MapError("atom#{}"), Term.Tuple(Term.A("badmap"), Term.A("atom")))
);
Test(
    "compiler/map-error-expression-before-badmap",
    async () => Equal(await MapError("atom#{a := error(blurf)}"), Term.A("blurf"))
);
Test("compiler/map-error-expression-before-badkey", async () => Equal(await MapError("#{}#{a := error(blurf)}"), Term.A("blurf")));
Test(
    "compiler/map-subset-nested-repeat",
    async () => Equal(await Eval("case #{a => {7,7}, extra => ok} of #{a := {X,X}} -> X; _ -> no end"), Term.I(7))
);
Test(
    "compiler/map-repeated-variable-reject",
    async () => Equal(await Eval("case #{a => 1,b => 1.0} of #{a := X,b := X} -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/map-duplicate-pattern-keys",
    async () => Equal(await Eval("case #{a => 42} of #{a := X,a := Y} -> {X,Y} end"), Term.Tuple(Term.I(42), Term.I(42)))
);
Test(
    "compiler/map-bound-key-and-guard-expression",
    async () => Equal(await Eval("case 1 of K -> case #{2 => 42} of #{K + 1 := V} -> V end end"), Term.I(42))
);
Test(
    "compiler/map-structured-key",
    async () => Equal(await Eval("case #{{a,[1]} => 42} of #{{a,[1]} := X} -> X end"), Term.I(42))
);
Test(
    "compiler/map-guard-key-error-rejects",
    async () => Equal(await Eval("case #{a => 1} of #{hd(atom) := X} -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/map-construction-in-guard",
    async () => Equal(await Eval("case ok of X when #{a => 1} =:= #{a => 1} -> X end"), Term.A("ok"))
);
Test(
    "compiler/map-update-guard-error-rejects",
    async () => Equal(await Eval("case #{} of M when M#{a := 1} =:= #{} -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/map-unbound-key-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("-module(m). -export([f/2]). f(K, #{K := V}) -> V.").ParseModule());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-sibling-binding-key-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case #{a => b,b => 1} of #{a := K,K := V} -> V end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-assoc-pattern-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("case #{} of #{a => X} -> X end").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-exact-construction-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("#{a := 1}").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-illegal-key-call-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case #{} of #{put(k,1) := X} -> X end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "patterns/map-rollback",
    () =>
 {
     var p = Parser.ToPattern(new Parser("#{a := X,b := X}").ParseExpression());
     var b = new Dictionary<string, Term>();
     Check(!p.Match(new MapTerm([new(Term.A("a"), Term.I(1)), new(Term.A("b"), Term.I(2))]), b));
     Check(b.Count == 0);

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-receive-preserves-unmatched",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    Term? value = null;
    var expression = new Parser("receive #{a := X,b := X} -> X after 1000 -> timeout end").ParseExpression();
    Semantics.Validate(expression);
    var p = runtime.Spawn(async c =>
 {
     value = await Execution.EvaluateAsync(expression, c);
     Check(c.Mailbox.Count == 1);

     return Term.A("ok");
 });
    runtime.Send(p.Pid, new MapTerm([new(Term.A("a"), Term.I(1)), new(Term.A("b"), Term.I(2))]));
    runtime.Send(p.Pid, new MapTerm([new(Term.A("a"), Term.I(42)), new(Term.A("b"), Term.I(42))]));
    Equal(await p.Completion, Term.A("normal"));
    Equal(value!, Term.I(42));
}
);
Test(
    "hybrid/map-case-directive-trivia",
    () =>
 {
     var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = case #{a => 1} of #{a := X} -> X end. } }", "m.cs");
     Check(generated.Contains("Expr.Map"));
     Check(generated.Contains("MapPatternField"));

     return Task.CompletedTask;
 }
);

Test(
    "compiler/map-closure-captured-key",
    async () => Equal(await Eval("case a of K -> F = fun(#{K := V}) -> V end, F(#{a => 42}) end"), Term.I(42))
);
Test(
    "compiler/map-closure-key-value-shadow",
    async () => Equal(await Eval("case a of K -> F = fun(#{K := K}) -> K end, F(#{a => 42}) end"), Term.I(42))
);
Test(
    "compiler/map-context-guard-key",
    async () => Equal(await Eval("case #{self() => 42} of #{self() := V} -> V end"), Term.I(42))
);
Test(
    "hybrid/map-inline-receive",
    () =>
 {
     var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = receive #{a := X} -> X end. } }", "m.cs");
     Check(generated.Contains("MapPatternField"));

     return Task.CompletedTask;
 }
);
Test(
    "hybrid/map-case-remote-call",
    () =>
 {
     var generated = CodeGeneration.Preprocess("class C { async Task F(ProcessContext erlangProcess) { var x = case maps:get(a,#{a => 42}) of X -> X end. } }", "m.cs");
     Check(generated.Contains("Expr.Case"));
     Check(generated.Contains("Expr.Map"));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/map-bifs-positive-negative-types",
    async () => Equal(
        await Eval("{is_map(#{}),is_map([]),map_size(#{a => 1,b => 2}),is_map_key(a,#{a => 1}),is_map_key(b,#{a => 1})}"),
        Term.Tuple(
            Term.A("true"),
            Term.A("false"),
            Term.I(2),
            Term.A("true"),
            Term.A("false")
        )
    )
);
Test(
    "compiler/map-get-exact-key-badkey",
    async () => Equal(await MapError("map_get(1.0,#{1 => value})"), Term.Tuple(Term.A("badkey"), new FloatTerm(1)))
);
Test(
    "compiler/map-get-badmap",
    async () => Equal(await MapError("map_get(key,not_map)"), Term.Tuple(Term.A("badmap"), Term.A("not_map")))
);
Test("compiler/map-size-badmap", async () => Equal(await MapError("map_size([])"), Term.Tuple(Term.A("badmap"), Nil.Value)));
Test(
    "compiler/is-map-key-badmap",
    async () => Equal(await MapError("is_map_key(key,42)"), Term.Tuple(Term.A("badmap"), Term.I(42)))
);
Test(
    "compiler/map-key-signed-zero",
    async () => Equal(
        await Eval("{is_map_key(-0.0,#{0.0 => a}),is_map_key(0.0,#{0.0 => a}),map_get(-0.0,#{-0.0 => b})}"),
        Term.Tuple(Term.A("false"), Term.A("true"), Term.A("b"))
    )
);
Test(
    "compiler/map-guard-bifs-qualified",
    async () => Equal(
        await Eval("case #{a => 42} of M when erlang:is_map(M), erlang:map_size(M) =:= 1, erlang:is_map_key(a,M), erlang:map_get(a,M) =:= 42 -> ok; _ -> no end"),
        Term.A("ok")
    )
);
Test(
    "compiler/map-guard-missing-key-alternative",
    async () => Equal(await Eval("case #{} of M when map_get(a,M) =:= 42; map_size(M) =:= 0 -> ok; _ -> no end"), Term.A("ok"))
);
Test(
    "compiler/map-guard-nonmap-rejection",
    async () => Equal(
        await Eval("case atom of M when map_size(M) =:= 0; is_map_key(a,M); map_get(a,M) =:= 1 -> no; _ -> ok end"),
        Term.A("ok")
    )
);
Test(
    "compiler/map-pattern-key-map-get",
    async () => Equal(await Eval("case #{a => key} of Keys -> case #{key => 42} of #{map_get(a,Keys) := Value} -> Value end end"), Term.I(42))
);
Test(
    "compiler/map-pattern-key-map-get-failure",
    async () => Equal(await Eval("case #{} of Keys -> case #{key => 42} of #{map_get(a,Keys) := Value} -> no; _ -> ok end end"), Term.A("ok"))
);
Test(
    "compiler/maps-module-not-guard-legal",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case #{} of M when maps:get(a,M) =:= 42 -> ok end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-empty-default-bytes",
    async () =>
 {
     Equal(await Eval("<<>>"), new BitString([]));
     Equal(await Eval("<<1,2,255>>"), new BitString([1, 2, 255]));
 }
);
Test("compiler/bits-truncate-negative", async () => Equal(await Eval("<<511,-1,16:4,31:4>>"), new BitString([255, 255, 15])));
Test("compiler/bits-unaligned-concatenation", async () => Equal(await Eval("<<5:3,17:5,3:2>>"), new BitString([177, 192], 10)));
Test(
    "compiler/bits-big-little-native",
    async () =>
 {
     Equal(await Eval("<<4660:16/big>>"), new BitString([18, 52]));
     Equal(await Eval("<<4660:16/little>>"), new BitString([52, 18]));
     Equal(await Eval("<<4660:16/native>>"), new BitString(BitConverter.IsLittleEndian ? [52, 18] : [18, 52]));
 }
);
Test("compiler/bits-little-partial-octet", async () => Equal(await Eval("<<291:12/little>>"), new BitString([35, 16], 12)));
Test("compiler/bits-explicit-unit", async () => Equal(await Eval("<<4660:2/unit:8>>"), new BitString([18, 52])));
Test("compiler/bits-string-literal", async () => Equal(await Eval("<<\"abc\">>"), new BitString([97, 98, 99])));
Test(
    "compiler/bits-bound-expression-size",
    async () => Equal(await Eval("case 4 of S -> <<(2+3):(S+1)>> end"), new BitString([40], 5))
);
Test("compiler/bits-binary-prefix", async () => Equal(await Eval("<<(<<1,2,3>>):2/binary>>"), new BitString([1, 2])));
Test(
    "compiler/bits-bitstring-interpolation",
    async () => Equal(await Eval("<<1:1,(<<2:2>>)/bitstring,3:2>>"), new BitString([216], 5))
);
Test("compiler/bits-binary-unit-one", async () => Equal(await Eval("<<(<<1:1>>)/binary-unit:1>>"), new BitString([128], 1)));
Test("compiler/bits-zero-size", async () => Equal(await Eval("<<-123:0,42:8>>"), new BitString([42])));
Test("compiler/bits-bad-value", async () => Equal(await MapError("<<atom>>"), Term.A("badarg")));
Test("compiler/bits-negative-size", async () => Equal(await MapError("<<1:(-1)>>"), Term.A("badarg")));
Test(
    "compiler/bits-size-prefix-requires-parentheses",
    async () =>
{
    foreach (string source in new[] { "<<1:-1>>", "<<1:+8>>", "<<1:bnot 1>>", "<<1:not true>>" })
        Throws<CompileException>(() => new Parser(source).ParseExpression());
    Equal(await Eval("<<1:(+8)>>"), new BitString([1]));
    Equal(await Eval("<<-1>>"), new BitString([255]));
}
);
Test("compiler/bits-float-size", async () => Equal(await MapError("<<1:1.0>>"), Term.A("badarg")));
Test("compiler/bits-short-binary", async () => Equal(await MapError("<<(<<1>>):2/binary>>"), Term.A("badarg")));
Test("compiler/bits-binary-unit-alignment", async () => Equal(await MapError("<<(<<1:1>>)/binary>>"), Term.A("badarg")));
Test(
    "compiler/bits-invalid-unit-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<1:8/unit:0>>").ParseExpression());
     Throws<CompileException>(() => new Parser("<<1:8/unit:257>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-utf-size-unit-diagnostic",
    () =>
 {
     foreach (string type in new[] { "utf8", "utf16", "utf32" })
         foreach (string source in new[] { "<<65:8/" + type + ">>", "<<65/" + type + "-unit:1>>" })
             Throws<CompileException>(() => new Parser(source).ParseExpression());

     return Task.CompletedTask;
 }
);
Test("compiler/bits-float16-vector", async () => Equal(await Eval("<<1.5:16/float>>"), new BitString([62, 0])));
Test("compiler/bits-float32-vector", async () => Equal(await Eval("<<1.5:32/float>>"), new BitString([63, 192, 0, 0])));
Test("compiler/bits-float64-default", async () => Equal(await Eval("<<1.5/float>>"), new BitString([63, 248, 0, 0, 0, 0, 0, 0])));
Test(
    "compiler/bits-float-little-native",
    async () =>
 {
     Equal(await Eval("<<1.5:16/float-little,1.5:32/float-little>>"), new BitString([0, 62, 0, 0, 192, 63]));
     Equal(await Eval("<<1.5:16/float-native>>"), new BitString(BitConverter.IsLittleEndian ? [0, 62] : [62, 0]));
 }
);
Test(
    "compiler/bits-float-integer-coercion",
    async () => Equal(await Eval("<<1:16/float,2:32/float>>"), new BitString([60, 0, 64, 0, 0, 0]))
);
Test("compiler/bits-float-unit", async () => Equal(await Eval("<<1.5:2/float-unit:8>>"), new BitString([62, 0])));
Test(
    "compiler/bits-float-bad-size",
    async () =>
 {
     foreach (string source in new[] { "<<1.0:0/float>>", "<<1.0:8/float>>", "<<1.0:128/float>>" })
         Equal(await MapError(source), Term.A("badarg"));
 }
);
Test(
    "compiler/bits-float-bad-value",
    async () =>
 {
     Equal(await MapError("<<atom/float>>"), Term.A("badarg"));
     Equal(
         await MapError("<<" + (BigInteger.One << 2000).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/float>>"),
         Term.A("badarg")
     );
 }
);
Test(
    "compiler/bits-float16-round-once",
    async () =>
 {
     Equal(await Eval("<<1.00048828125:16/float>>"), new BitString([60, 0]));
     Equal(await Eval("<<1.000488282181322574615478515625:16/float>>"), new BitString([60, 1]));
     Equal(await Eval("<<1.00048840045928955078125:16/float>>"), new BitString([60, 1]));
 }
);
Test(
    "compiler/bits-float16-subnormal-vectors",
    async () =>
 {
     Equal(await Eval("<<3.039836883544921875e-6:16/float>>"), new BitString([0, 51]));
     Equal(await Eval("<<2.98023223876953125e-7:16/float>>"), new BitString([0, 5]));
     Equal(await Eval("<<3.0517578125e-5:16/float>>"), new BitString([2, 0]));
 }
);
Test(
    "compiler/bits-float-narrow-overflow",
    async () =>
 {
     Equal(await Eval("<<1000000000:16/float>>"), new BitString([124, 0]));
     Equal(await Eval("<<1.0e100:32/float>>"), new BitString([127, 128, 0, 0]));
 }
);
Test(
    "compiler/bits-float-pattern16-rounded",
    async () => Equal(await Eval("case <<0.1:16/float>> of <<F:16/float>> -> F end"), new FloatTerm(0.0999755859375))
);
Test(
    "compiler/bits-float-integer-rounding",
    async () =>
{
    Equal(
        await Eval("case <<9007199254740995/float,-9007199254740995/float,9007199254740993/float>> of <<A/float,B/float,C/float>> -> {A,B,C} end"),
        Term.Tuple(new FloatTerm(9007199254740996.0), new FloatTerm(-9007199254740996.0), new FloatTerm(9007199254740992.0))
    );
    Equal(await Eval("case <<9007199254740995/float>> of <<9007199254740995/float>> -> ok; _ -> no end"), Term.A("ok"));
    BigInteger anchor = BigInteger.One << 100;
    Equal(
        await Eval("case <<" + (anchor + (BigInteger.One << 47) + 1) + "/float>> of <<F/float>> -> F end"),
        new FloatTerm(Math.ScaleB(1.0, 100) + Math.ScaleB(1.0, 48))
    );
}
);
Test(
    "compiler/bits-float-integer-max-boundary",
    async () =>
{
    BigInteger max = (BigInteger.One << 1024) - (BigInteger.One << 971), halfway = max + (BigInteger.One << 970);
    Equal(await Eval("case <<" + (halfway - 1) + "/float>> of <<F/float>> -> F end"), new FloatTerm(double.MaxValue));
    Equal(await MapError("<<" + halfway + "/float>>"), Term.A("badarg"));
}
);
Test(
    "compiler/bits-float-pattern32",
    async () => Equal(await Eval("case <<1.5:32/float>> of <<F:32/float>> -> F end"), new FloatTerm(1.5))
);
Test(
    "compiler/bits-float-pattern64-default",
    async () => Equal(await Eval("case <<1.5/float>> of <<F/float>> -> F end"), new FloatTerm(1.5))
);
Test(
    "compiler/bits-float-pattern-unaligned",
    async () => Equal(
        await Eval("case <<1:1,1.5:16/float-little,5:3>> of <<_:1,F:16/float-little,T:3>> -> {F,T} end"),
        Term.Tuple(new FloatTerm(1.5), Term.I(5))
    )
);
Test("compiler/bits-float-pattern-zero", async () => Equal(await Eval("case <<>> of <<F:0/float>> -> F end"), new FloatTerm(0.0)));
Test(
    "compiler/bits-float-pattern-nonfinite-reject",
    async () =>
 {
     foreach (string bits in new[] { "<<31744:16>>", "<<32256:16>>", "<<2139095040:32>>", "<<2143289344:32>>", "<<9218868437227405312:64>>", "<<9221120237041090560:64>>" })
     {
         int size = bits.Contains(":16") ? 16 : bits.Contains(":32") ? 32 : 64;
         Equal(await Eval("case " + bits + " of <<F:" + size + "/float>> -> wrong; _ -> ok end"), Term.A("ok"));
     }
 }
);
Test(
    "compiler/bits-float-pattern-invalid-short",
    async () =>
 {
     Equal(await Eval("case <<0:8>> of <<F:8/float>> -> wrong; _ -> ok end"), Term.A("ok"));
     Equal(await Eval("case <<0:8>> of <<F:16/float>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-float-pattern-numeric-literal",
    async () => Equal(await Eval("case <<1:32/float>> of <<1:32/float>> -> ok; _ -> no end"), Term.A("ok"))
);
Test(
    "compiler/bits-float-pattern-bound-integer",
    async () => Equal(await Eval("case 1 of X -> case <<1:32/float>> of <<X:32/float>> -> wrong; _ -> ok end end"), Term.A("ok"))
);
Test(
    "compiler/bits-float-signed-zero",
    async () =>
 {
     Equal(await Eval("<<-0.0:16/float>>"), new BitString([128, 0]));
     Equal(await Eval("case <<-0.0:32/float>> of <<F:32/float>> -> F end"), new FloatTerm(-0.0));
     Equal(await Eval("case <<-0.0:16/float>> of <<0.0:16/float>> -> wrong; <<-0.0:16/float>> -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-float-pattern-prior-width",
    async () => Equal(await Eval("case <<16,1.5:16/float>> of <<N,F:N/float>> -> F end"), new FloatTerm(1.5))
);
Test(
    "compiler/bits-float-guard-and-map-key",
    async () => Equal(
        await Eval("case #{<<1.5:16/float>> => 42} of #{<<1.5:16/float>> := X} when <<1:16/float>> =:= <<1.0:16/float>> -> X end"),
        Term.I(42)
    )
);
Test(
    "compiler/bits-float-unit-without-size-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<1.0/float-unit:8>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-float16-all-finite-roundtrip",
    async () =>
{
    var pattern = Parser.ToPattern(new Parser("<<F:16/float>>").ParseExpression());
    await using var runtime = new ProcessRuntime();
    var process = runtime.Spawn(async ctx =>
    {
        for (int bits = 0; bits <= ushort.MaxValue; bits++)
        {
            if ((bits & 0x7c00) == 0x7c00)
                continue;
            var input = new BitString([(byte)(bits >> 8), (byte)bits]);
            var bindings = new Dictionary<string, Term>();
            Check(pattern.Match(input, bindings, ctx));
            var expression = new Expr.Bits([new BitSegment(new Expr.Literal(bindings["F"]), new Expr.Literal(Term.I(16)), "float")]);
            Equal(await Execution.EvaluateAsync(expression, ctx), input);
        }

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
Test(
    "compiler/bits-duplicate-spec-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<1:8/big-little>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-utf8-boundary-vectors",
    async () =>
{
    (int Code, byte[] Bytes)[] vectors = [(0, [0]), (127, [127]), (128, [194, 128]), (2047, [223, 191]), (2048, [224, 160, 128]), (55295, [237, 159, 191]), (57344, [238, 128, 128]), (65535, [239, 191, 191]), (65536, [240, 144, 128, 128]), (1114111, [244, 143, 191, 191])];
    foreach (var vector in vectors)
        Equal(await Eval("<<" + vector.Code + "/utf8>>"), new BitString(vector.Bytes));
}
);
Test(
    "compiler/bits-utf16-surrogate-vector",
    async () =>
 {
     Equal(await Eval("<<128512/utf16>>"), new BitString([216, 61, 222, 0]));
     Equal(await Eval("<<128512/utf16-little>>"), new BitString([61, 216, 0, 222]));
 }
);
Test(
    "compiler/bits-utf32-vector",
    async () =>
 {
     Equal(await Eval("<<128512/utf32>>"), new BitString([0, 1, 246, 0]));
     Equal(await Eval("<<128512/utf32-little>>"), new BitString([0, 246, 1, 0]));
 }
);
Test(
    "compiler/bits-utf-endian-native",
    async () =>
 {
     foreach (string type in new[] { "utf16", "utf32" })
         Equal(
             await Eval("<<128512/" + type + "-native>>"),
             await Eval("<<128512/" + type + (BitConverter.IsLittleEndian ? "-little" : "-big") + ">>")
         );
     Equal(await Eval("<<128512/utf8-little>>"), await Eval("<<128512/utf8-big>>"));
 }
);
Test(
    "compiler/bits-utf-invalid-values",
    async () =>
 {
     foreach (string type in new[] { "utf8", "utf16", "utf32" })
         foreach (string value in new[] { "-1", "55296", "57343", "1114112", "9007199254740993", "1.0", "atom", "[65]" })
             Equal(await MapError("<<(" + value + ")/" + type + ">>"), Term.A("badarg"));
 }
);
Test(
    "compiler/bits-utf-undefined-size-diagnostic",
    () =>
 {
     foreach (string type in new[] { "utf8", "utf16", "utf32" })
         foreach (string source in new[] { "<<65:undefined/" + type + ">>", "case <<65>> of <<X:undefined/" + type + ">> -> X end" })
             Throws<CompileException>(() => new Parser(source).ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-utf-noncharacters",
    async () =>
 {
     foreach (string type in new[] { "utf8", "utf16", "utf32" })
         Equal(
             await Eval("case <<65534/" + type + ",65535/" + type + ">> of <<A/" + type + ",B/" + type + ">> -> {A,B} end"),
             Term.Tuple(Term.I(65534), Term.I(65535))
         );
 }
);
Test(
    "compiler/bits-utf8-prefix-rest",
    async () => Equal(
        await Eval("case <<240,159,152,128,42>> of <<X/utf8,Rest/binary>> -> {X,Rest} end"),
        Term.Tuple(Term.I(128512), new BitString([42]))
    )
);
Test(
    "compiler/bits-utf16-pattern-pair",
    async () => Equal(await Eval("case <<216,61,222,0>> of <<X/utf16>> -> X end"), Term.I(128512))
);
Test(
    "compiler/bits-utf32-pattern",
    async () => Equal(await Eval("case <<0,246,1,0>> of <<X/utf32-little>> -> X end"), Term.I(128512))
);
Test(
    "compiler/bits-utf8-invalid-sequences",
    async () =>
 {
     foreach (string bytes in new[] { "128", "192,175", "193,191", "224,128,128", "237,160,128", "240,128,128,128", "244,144,128,128", "245,128,128,128", "254", "255", "226,130", "194,65" })
         Equal(await Eval("case <<" + bytes + ">> of <<X/utf8,Rest/binary>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-utf16-invalid-sequences",
    async () =>
 {
     foreach (string bytes in new[] { "216,0", "220,0", "216,0,0,65", "220,0,216,0", "0" })
         Equal(await Eval("case <<" + bytes + ">> of <<X/utf16,Rest/binary>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-utf32-invalid-scalars",
    async () =>
 {
     foreach (string value in new[] { "55296", "57343", "1114112", "4294967295" })
         Equal(await Eval("case <<" + value + ":32>> of <<X/utf32>> -> wrong; _ -> ok end"), Term.A("ok"));
     Equal(await Eval("case <<0,0,65>> of <<X/utf32>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-utf-unaligned",
    async () =>
 {
     foreach (string type in new[] { "utf8", "utf16-little", "utf32-big" })
         for (int offset = 1; offset <= 7; offset++)
             Equal(
                 await Eval("case <<1:" + offset + ",128512/" + type + ",5:3>> of <<_:" + offset + ",X/" + type + ",T:3>> -> {X,T} end"),
                 Term.Tuple(Term.I(128512), Term.I(5))
             );
 }
);
Test(
    "compiler/bits-utf-truncated-bit-tail",
    async () => Equal(await Eval("case <<240,159,152,64:7>> of <<X/utf8>> -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bits-utf-binding-rollback",
    async () => Equal(await Eval("case <<65,128>> of <<X/utf8,Y/utf8>> -> wrong; <<X:16>> -> X end"), Term.I(16768))
);
Test("compiler/bits-utf-size-binding", async () => Equal(await Eval("case <<3/utf8,5:3>> of <<N/utf8,X:N>> -> X end"), Term.I(5)));
Test(
    "compiler/bits-utf-literal-and-bound",
    async () =>
 {
     Equal(await Eval("case <<128512/utf8>> of <<128512/utf8>> -> ok; _ -> no end"), Term.A("ok"));
     Equal(await Eval("case 128512 of X -> case <<128512/utf16>> of <<X/utf16>> -> X end end"), Term.I(128512));
 }
);
Test(
    "compiler/bits-utf-string-construction",
    async () =>
 {
     Equal(await Eval("<<\"A😀\"/utf8>>"), new BitString([65, 240, 159, 152, 128]));
     Equal(await Eval("<<\"A😀\"/utf16-little>>"), new BitString([65, 0, 61, 216, 0, 222]));
     Equal(await Eval("<<\"\"/utf32>>"), new BitString([]));
 }
);
Test(
    "compiler/bits-utf-string-pattern",
    async () => Equal(await Eval("case <<\"A😀\"/utf8,42>> of <<\"A😀\"/utf8,X>> -> X end"), Term.I(42))
);
Test(
    "compiler/bits-utf-guard-map-key",
    async () => Equal(await Eval("case #{<<128512/utf8>> => 42} of #{<<128512/utf8>> := X} when <<65/utf8>> =:= <<65>> -> X end"), Term.I(42))
);
Test(
    "compiler/bits-utf-deterministic-roundtrips",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    var process = runtime.Spawn(async ctx =>
    {
        var random = new Random(14014);
        foreach (string type in new[] { "utf8", "utf16", "utf16-little", "utf32", "utf32-little" })
        {
            Pattern pattern = Parser.ToPattern(new Parser("<<_:3,X/" + type + ",T:2>>").ParseExpression());
            for (int i = 0; i < 256; i++)
            {
                int scalar = random.Next(0x110000);
                if (scalar is >= 0xd800 and <= 0xdfff)
                {
                    i--;
                    continue;
                }
                var input = await Execution.EvaluateAsync(new Parser("<<5:3," + scalar + "/" + type + ",2:2>>").ParseExpression(), ctx);
                var bindings = new Dictionary<string, Term>();
                Check(pattern.Match(input, bindings, ctx));
                Equal(bindings["X"], Term.I(scalar));
                Equal(bindings["T"], Term.I(2));
            }
        }

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
Test("compiler/bits-pattern-default", async () => Equal(await Eval("case <<42>> of <<X>> -> X end"), Term.I(42)));
Test("compiler/bits-pattern-signed", async () => Equal(await Eval("case <<255>> of <<X:8/signed>> -> X end"), Term.I(-1)));
Test("compiler/bits-pattern-unsigned", async () => Equal(await Eval("case <<255>> of <<X:8/unsigned>> -> X end"), Term.I(255)));
Test(
    "compiler/bits-pattern-signed-little",
    async () => Equal(await Eval("case <<-257:16/little>> of <<X:16/signed-little>> -> X end"), Term.I(-257))
);
Test(
    "compiler/bits-pattern-little-partial",
    async () => Equal(await Eval("case <<291:12/little>> of <<X:12/little>> -> X end"), Term.I(291))
);
Test(
    "compiler/bits-pattern-native",
    async () => Equal(await Eval("case <<4660:16/native>> of <<X:16/native>> -> X end"), Term.I(4660))
);
Test(
    "compiler/bits-pattern-prior-size",
    async () => Equal(
        await Eval("case <<3,5:3,2:2>> of <<N, X:N, Rest/bitstring>> -> {X,Rest} end"),
        Term.Tuple(Term.I(5), new BitString([128], 2))
    )
);
Test(
    "compiler/bits-pattern-bound-size-expression",
    async () => Equal(await Eval("case 3 of N -> case <<17:5>> of <<X:(N+2)>> -> X end end"), Term.I(17))
);
Test(
    "compiler/bits-pattern-shadow-size",
    async () => Equal(await Eval("case 8 of L -> F = fun(<<L:L,B:L>>) -> B end, F(<<16:8,7:16>>) end"), Term.I(7))
);
Test(
    "compiler/bits-pattern-shadow-repeat",
    async () => Equal(await Eval("case 8 of L -> F = fun(<<L:L,B:L,L:L>>) -> B; (_) -> no end, F(<<16:8,7:16,16:16>>) end"), Term.I(7))
);
Test(
    "compiler/fun-clause-local-shadow-capture",
    async () => Equal(await Eval("case 42 of X -> F = fun({X}) -> X; (_) -> X end, F(atom) end"), Term.I(42))
);
Test(
    "compiler/fun-guard-fallback-capture",
    async () => Equal(await Eval("case 42 of X -> F = fun(X) when is_integer(X) -> X; (_) -> X end, F(atom) end"), Term.I(42))
);
Test(
    "compiler/fun-bit-clause-fallback-capture",
    async () => Equal(
        await Eval("case {8,42} of {L,B} -> F = fun(<<L:L,B:L>>) -> {L,B}; (_) -> {L,B} end, F(atom) end"),
        Term.Tuple(Term.I(8), Term.I(42))
    )
);
Test(
    "compiler/bits-pattern-repeat-mismatch",
    async () => Equal(await Eval("case <<1,2>> of <<X,X>> -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bits-pattern-bound-value",
    async () => Equal(await Eval("case 42 of X -> case <<42>> of <<X>> -> ok; _ -> no end end"), Term.A("ok"))
);
Test(
    "compiler/bits-pattern-short-extra-nonbits",
    async () =>
 {
     foreach (string source in new[] { "<<1:7>>", "<<1,2>>", "atom" })
         Equal(await Eval("case " + source + " of <<X:8>> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test("compiler/bits-pattern-zero-signed", async () => Equal(await Eval("case <<>> of <<X:0/signed>> -> X end"), Term.I(0)));
Test(
    "compiler/bits-pattern-binary-prefix",
    async () => Equal(await Eval("case <<1,2,3>> of <<B:2/binary,T/binary>> -> {B,T} end"), Term.Tuple(new BitString([1, 2]), new BitString([3])))
);
Test(
    "compiler/bits-pattern-unaligned-prefix",
    async () => Equal(await Eval("case <<1:1,5:3>> of <<_:1,B:3/bitstring>> -> B end"), new BitString([160], 3))
);
Test("compiler/bits-pattern-unit", async () => Equal(await Eval("case <<1,2>> of <<X:2/unit:8>> -> X end"), Term.I(258)));
Test(
    "compiler/bits-pattern-rest-unit-mismatch",
    async () => Equal(await Eval("case <<1:1>> of <<B/binary>> -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bits-pattern-invalid-size-alternative",
    async () =>
 {
     foreach (string size in new[] { "(-1)", "atom", "1.0", "999999999999999999999999", "(hd(atom))" })
         Equal(await Eval("case <<1>> of <<X:" + size + ">> -> wrong; _ -> ok end"), Term.A("ok"));
 }
);
Test(
    "compiler/bits-pattern-string-prefix",
    async () => Equal(await Eval("case <<\"OK\",42>> of <<\"OK\",X>> -> X end"), Term.I(42))
);
Test(
    "compiler/bits-pattern-literal-no-truncation",
    async () => Equal(await Eval("case <<0>> of <<256>> -> wrong; _ -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bits-pattern-signed-literal",
    async () => Equal(await Eval("case <<255>> of <<-1:8/signed>> -> ok; _ -> no end"), Term.A("ok"))
);
Test("compiler/bits-pattern-empty", async () => Equal(await Eval("case <<>> of <<>> -> ok; _ -> no end"), Term.A("ok")));
Test(
    "compiler/bits-pattern-rollback",
    () =>
 {
     var p = Parser.ToPattern(new Parser("<<N,1>>").ParseExpression());
     var b = new Dictionary<string, Term>();
     Check(!p.Match(new BitString([42, 2]), b));
     Check(b.Count == 0);

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-forward-size-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case <<1>> of <<X:N,N>> -> X end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-nonlast-rest-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("case <<1>> of <<B/binary,X>> -> X end").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-size-call-diagnostic",
    () =>
 {
     Throws<CompileException>(() => Semantics.Validate(new Parser("case <<1>> of <<X:(lists:sum([8]))>> -> X end").ParseExpression()));

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-nested-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("case <<1>> of <<(<<X>>)/binary>> -> X end").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-pattern-map-value",
    async () => Equal(await Eval("case #{a => <<3,5:3>>} of #{a := <<N,X:N>>} -> X end"), Term.I(5))
);
Test(
    "compiler/bits-pattern-match-badmatch",
    async () => Equal(await MapError("<<X:16>> = <<1>>"), Term.Tuple(Term.A("badmatch"), new BitString([1])))
);
Test(
    "compiler/bits-pattern-receive-preserves-unmatched",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    var process = runtime.Spawn(async ctx =>
    {
        ctx.Mailbox.Send(Term.A("earlier"));
        ctx.Mailbox.Send(new BitString([2, 42]));
        ctx.Mailbox.Send(new BitString([1, 7]));
        var expression = new Parser("receive <<1,X>> -> X after 0 -> no end").ParseExpression();
        Semantics.Validate(expression);
        Equal(await Execution.EvaluateAsync(expression, ctx), Term.I(7));
        Equal((await ctx.ReceiveAsync(t => t, TimeSpan.Zero))!, Term.A("earlier"));
        Equal((await ctx.ReceiveAsync(t => t, TimeSpan.Zero))!, new BitString([2, 42]));

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
Test(
    "compiler/bits-guard-and-map-key",
    async () => Equal(await Eval("case #{<<1,2>> => 42} of #{<<1,2>> := V} when <<1:3>> =:= <<1:3>> -> V end"), Term.I(42))
);
Test(
    "compiler/bits-unit-without-size-diagnostic",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<1/unit:8>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test(
    "compiler/bits-string-little-per-codepoint",
    async () => Equal(await Eval("<<\"ab\":16/little>>"), new BitString([97, 0, 98, 0]))
);
Test("compiler/bits-string-unit-per-codepoint", async () => Equal(await Eval("<<\"AB\":2/unit:8>>"), new BitString([0, 65, 0, 66])));
Test("compiler/bits-string-unaligned", async () => Equal(await Eval("<<1:1,\"AB\":4,3:2>>"), new BitString([137, 96], 11)));
Test("compiler/bits-string-unicode-integer-truncation", async () => Equal(await Eval("<<\"Ā😀\"/integer>>"), new BitString([0, 0])));
Test("compiler/bits-string-float-half", async () => Equal(await Eval("<<\"AB\":16/float>>"), new BitString([84, 16, 84, 32])));
Test(
    "compiler/bits-string-float-single-little",
    async () => Equal(await Eval("<<\"A\":32/float-little>>"), new BitString([0, 0, 130, 66]))
);
Test("compiler/bits-string-float-default", async () => Equal(await Eval("<<\"A\"/float>>"), new BitString([64, 80, 64, 0, 0, 0, 0, 0])));
Test(
    "compiler/bits-string-size-evaluated-once",
    async () => Equal(
        await Eval("case ok of ok -> put(counter,0),B = <<\"ab\":(put(counter,get(counter)+1))>>, {B,get(counter)} end"),
        Term.Tuple(new BitString([], 0), Term.I(1))
    )
);
Test(
    "compiler/bits-string-size-binding",
    async () => Equal(await Eval("case ok of ok -> B = <<\"ab\":(S = 8)>>,{B,S} end"), Term.Tuple(new BitString([97, 98]), Term.I(8)))
);
Test(
    "compiler/bits-empty-string-valid-modifiers",
    async () => Equal(await Eval("<<\"\":16/little,\"\":32/float,42>>"), new BitString([42]))
);
Test("compiler/bits-empty-string-size-evaluation", async () => Equal(await MapError("<<\"\":(error(boom))>>"), Term.A("boom")));
Test("compiler/bits-string-zero-width", async () => Equal(await Eval("<<\"abc\":0,42>>"), new BitString([42])));
Test(
    "compiler/bits-string-invalid-sizes",
    async () =>
{
    foreach (string source in new[] { "<<\"ab\":(-1)>>", "<<\"\":(-1)>>", "case all of S -> <<\"\":S>> end", "<<\"ab\":1.0>>", "<<\"\":1/float>>", "<<\"ab\":0/float>>", "<<\"a\"/binary>>", "<<\"\"/binary>>", "<<[65,66]:8>>", "<<[]:8>>" })
        Equal(await MapError(source), Term.A("badarg"));
}
);
Test(
    "compiler/bits-string-modifier-pattern-diagnostic",
    () =>
{
    foreach (string source in new[] { "case <<0,97>> of <<\"a\":16>> -> yes end", "case <<97>> of <<\"a\"/integer>> -> yes end", "case <<>> of <<\"\":0>> -> yes end", "fun(<<\"a\"/float>>) -> ok end" })
    {
        try
        {
            new Parser(source).ParseExpression();
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == "ERL004");
        }
    }

    return Task.CompletedTask;
}
);
Test(
    "compiler/bits-string-guard-and-map-key",
    async () => Equal(
        await Eval("case #{<<\"ab\":16/little>> => 42} of #{<<\"ab\":16/little>> := X} when <<\"A\":16/float>> =:= <<84,16>> -> X end"),
        Term.I(42)
    )
);
Test(
    "compiler/bits-numeric-literal-all-diagnostic",
    () =>
{
    foreach (string source in new[] { "<<\"\":all>>", "<<\"ab\":all>>", "<<1:all>>", "<<1:all/float>>", "<<1.0:all/float>>", "<<\"\":(all)/float>>" })
    {
        try
        {
            new Parser(source).ParseExpression();
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == "ERL002");
        }
    }

    return Task.CompletedTask;
}
);
Test(
    "compiler/bits-identical-specifiers",
    async () => Equal(await Eval("<<1:8/integer-integer-big-big-unit:1-unit:1>>"), new BitString([1]))
);
Test(
    "compiler/bits-alias-specifier-unit-conflict",
    () =>
 {
     Throws<CompileException>(() => new Parser("<<(<<1>>)/bytes-unit:1>>").ParseExpression());
     Throws<CompileException>(() => new Parser("<<(<<1>>)/unit:8-bits>>").ParseExpression());

     return Task.CompletedTask;
 }
);
Test("compiler/bits-explicit-all-binary", async () => Equal(await Eval("<<(<<1,2>>):all/binary>>"), new BitString([1, 2])));
Test(
    "compiler/bit-size-byte-rounding",
    async () => Equal(
        await Eval("{bit_size(<<>>),byte_size(<<>>),bit_size(<<1:1>>),byte_size(<<1:1>>),bit_size(<<1:8>>),byte_size(<<1:8>>),bit_size(<<1:9>>),byte_size(<<1:9>>)}"),
        Term.Tuple(
            Term.I(0),
            Term.I(0),
            Term.I(1),
            Term.I(1),
            Term.I(8),
            Term.I(1),
            Term.I(9),
            Term.I(2)
        )
    )
);
Test(
    "compiler/bitstring-predicate-distinction",
    async () => Equal(
        await Eval("{is_bitstring(<<>>),is_binary(<<>>),is_bitstring(<<1:1>>),is_binary(<<1:1>>),is_bitstring([])}"),
        Term.Tuple(
            Term.A("true"),
            Term.A("true"),
            Term.A("true"),
            Term.A("false"),
            Term.A("false")
        )
    )
);
Test("compiler/bit-size-invalid-badarg", async () => Equal(await MapError("bit_size(atom)"), Term.A("badarg")));
Test("compiler/byte-size-invalid-badarg", async () => Equal(await MapError("byte_size([1,2])"), Term.A("badarg")));
Test(
    "compiler/bit-bifs-qualified-guard",
    async () => Equal(
        await Eval("case <<1:9>> of Bits when erlang:is_bitstring(Bits), erlang:bit_size(Bits) =:= 9, erlang:byte_size(Bits) =:= 2 -> ok; _ -> no end"),
        Term.A("ok")
    )
);
Test(
    "compiler/bit-bif-guard-failure-alternative",
    async () => Equal(await Eval("case atom of X when bit_size(X) =:= 0; byte_size(X) =:= 0; is_atom(X) -> ok end"), Term.A("ok"))
);
Test(
    "compiler/bit-bif-map-pattern-key",
    async () => Equal(await Eval("case <<1:9>> of B -> case #{2 => 42} of #{byte_size(B) := X} -> X end end"), Term.I(42))
);
var results = new List<object>();
foreach (var sample in new (string Name, string Source, Term Expected)[]
{
    ("duplicate-zero", "lists:duplicate(0,anything)", Nil.Value),
    ("duplicate-one", "lists:duplicate(1,a)", Term.List(Term.A("a"))),
    ("duplicate-value", "lists:duplicate(3,{a,1})", Term.List(Term.Tuple(Term.A("a"), Term.I(1)), Term.Tuple(Term.A("a"), Term.I(1)), Term.Tuple(Term.A("a"), Term.I(1)))),
    ("duplicate-improper-element", "lists:duplicate(2,[a|tail])", Term.List(new Cons(Term.A("a"), Term.A("tail")), new Cons(Term.A("a"), Term.A("tail")))),
    ("flatten-empty", "lists:flatten([])", Nil.Value),
    ("flatten-flat", "lists:flatten([a,b,c])", Term.List(Term.A("a"), Term.A("b"), Term.A("c"))),
    ("flatten-nested-empty", "lists:flatten([[],[[],[]],[]])", Nil.Value),
    ("flatten-mixed-opaque-leaves", "lists:flatten([a,[[b],[]],{[c]},<<1>>])", Term.List(
        Term.A("a"),
        Term.A("b"),
        Term.Tuple(Term.List(Term.A("c"))),
        new BitString([1])
    )),
    ("flatten-unicode-list", "lists:flatten([\"A😀\",[66]])", Term.List(Term.I(65), Term.I(128512), Term.I(66))),
    ("flatten-two-empty-keeps-tail", "lists:flatten([],[[]])", Term.List(Nil.Value)),
    ("flatten-two-preserves-nested-tail", "lists:flatten([[a],b],[[c]])", Term.List(Term.A("a"), Term.A("b"), Term.List(Term.A("c")))),
    ("flatten-two-improper-tail", "lists:flatten([[a],b],[c|tail])", new Cons(Term.A("a"), new Cons(Term.A("b"), new Cons(Term.A("c"), Term.A("tail")))))
})
    Test($"lists/{sample.Name}", async () => Equal(await Eval(sample.Source), sample.Expected));

foreach (var sample in new (string Name, string Source)[]
{
    ("duplicate-negative", "lists:duplicate(-1,a)"),
    ("duplicate-big-negative", "lists:duplicate(-999999999999999999999999,a)"),
    ("duplicate-float-count", "lists:duplicate(0.0,a)"),
    ("duplicate-atom-count", "lists:duplicate(atom,a)"),
    ("duplicate-list-count", "lists:duplicate([],a)"),
    ("flatten-nonlist", "lists:flatten(atom)"),
    ("flatten-tuple-input", "lists:flatten({a})"),
    ("flatten-improper-outer", "lists:flatten([a|tail])"),
    ("flatten-improper-nested", "lists:flatten([[a|tail]])"),
    ("flatten-two-invalid-tail", "lists:flatten([],atom)"),
    ("flatten-two-invalid-input", "lists:flatten(atom,[])"),
    ("flatten-two-improper-input", "lists:flatten([a|tail],[])"),
    ("flatten-two-improper-nested", "lists:flatten([[a|tail]],[b|tail])")
})
    Test($"lists/{sample.Name}", async () => Equal(await MapError(sample.Source), Term.A("function_clause")));

Test(
    "lists/duplicate-local-length-limit",
    async () =>
{
    Equal(await MapError("lists:duplicate(2147483648,a)"), Term.A("system_limit"));
    Equal(await MapError("lists:duplicate(999999999999999999999999,a)"), Term.A("system_limit"));
}
);
Test(
    "lists/duplicate-flatten-deep-wide-identity",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    Term marker = Term.Tuple(Term.List(Term.A("marker")));
    Term deep = marker;
    Term wide = Nil.Value;
    for (int i = 0; i < 100000; i++)
    {
        deep = Term.List(deep);
        wide = new Cons(marker, wide);
    }
    Term suffix = new Cons(Term.List(Term.A("tail-value")), Term.A("tail"));
    var process = runtime.Spawn(async context =>
    {
        Term copies = await runtime.Modules.Call(
            context,
            "lists",
            "duplicate",
            [Term.I(100000), marker]
        );
        Check(Cons.Items(copies).Count() == 100000);
        Check(Cons.Items(copies).All(value => ReferenceEquals(value, marker)));
        var flattened = (Cons)await runtime.Modules.Call(
            context,
            "lists",
            "flatten",
            [deep, suffix]
        );
        Check(ReferenceEquals(flattened.Head, marker));
        Check(ReferenceEquals(flattened.Tail, suffix));
        Term flatWide = await runtime.Modules.Call(
            context,
            "lists",
            "flatten",
            [wide]
        );
        Check(Cons.Items(flatWide).Count() == 100000);
        Check(Cons.Items(flatWide).All(value => ReferenceEquals(value, marker)));

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
foreach (var sample in new (string Name, string Source, Term Expected)[]
{
    ("append-empty", "lists:append([])", Nil.Value),
    ("append-singleton-atom", "lists:append([tail])", Term.A("tail")),
    ("append-singleton-number", "lists:append([42])", Term.I(42)),
    ("append-singleton-improper", "lists:append([[a|tail]])", new Cons(Term.A("a"), Term.A("tail"))),
    ("append-many", "lists:append([[a,b],[],[c,d]])", Term.List(
        Term.A("a"),
        Term.A("b"),
        Term.A("c"),
        Term.A("d")
    )),
    ("append-arbitrary-final-tail", "lists:append([[a],[b],tail])", new Cons(Term.A("a"), new Cons(Term.A("b"), Term.A("tail")))),
    ("append-empty-prefix-arbitrary-tail", "lists:append([[],42])", Term.I(42)),
    ("append-improper-final-list", "lists:append([[a],[b|tail]])", new Cons(Term.A("a"), new Cons(Term.A("b"), Term.A("tail")))),
    ("member-empty", "lists:member(a,[])", Term.A("false")),
    ("member-found-before-improper", "lists:member(a,[a|tail])", Term.A("true")),
    ("member-found-later-before-improper", "lists:member(b,[a,b|tail])", Term.A("true")),
    ("member-tuple-exact", "lists:member({1},[{1.0}])", Term.A("false")),
    ("member-map-exact-keys", "lists:member(#{1=>a},[#{1.0=>a}])", Term.A("false")),
    ("member-map-equal", "lists:member(#{a=>1,b=>2},[#{b=>2,a=>1}])", Term.A("true")),
    ("member-bits-exact", "lists:member(<<1:1>>,[<<1:2>>])", Term.A("false")),
    ("member-improper-value-equal", "lists:member([a|tail],[[a|tail]])", Term.A("true")),
    ("member-signed-zero", "lists:member(0.0,[-0.0])", Term.A("false"))
})
    Test($"lists/{sample.Name}", async () => Equal(await Eval(sample.Source), sample.Expected));

foreach (var sample in new (string Name, string Source, string Reason)[]
{
    ("append-nonlist", "lists:append(atom)", "function_clause"),
    ("append-improper-outer", "lists:append([[a]|tail])", "function_clause"),
    ("append-invalid-prefix", "lists:append([atom,[]])", "badarg"),
    ("append-improper-prefix", "lists:append([[a|tail],[]])", "badarg"),
    ("append-outer-error-before-prefix", "lists:append([atom|tail])", "function_clause"),
    ("append-outer-error-after-prefix", "lists:append([atom,[a]|tail])", "function_clause"),
    ("member-nonlist", "lists:member(a,atom)", "badarg"),
    ("member-absent-improper", "lists:member(b,[a|tail])", "badarg"),
    ("member-numeric-inexact-improper", "lists:member(1,[1.0|tail])", "badarg")
})
    Test($"lists/{sample.Name}", async () => Equal(await MapError(sample.Source), Term.A(sample.Reason)));

Test(
    "lists/append-long-outer-suffix-identity",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    Term suffix = new Cons(Term.A("marker"), Term.A("tail"));
    Term lists = Term.List(suffix);
    for (int i = 0; i < 100000; i++)
        lists = new Cons(Nil.Value, lists);
    var process = runtime.Spawn(async context =>
    {
        Term result = await runtime.Modules.Call(
            context,
            "lists",
            "append",
            [lists]
        );
        Check(ReferenceEquals(result, suffix));
        var copied = (Cons)await runtime.Modules.Call(
            context,
            "lists",
            "append",
            [Term.List(Term.List(Term.I(1)), suffix)]
        );
        Check(ReferenceEquals(copied.Tail, suffix));

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
foreach (var sample in new (string Name, string Source, Term Expected)[]
{
    ("last-singleton", "lists:last([a])", Term.A("a")),
    ("last-nested", "lists:last([a,[],{b,2}])", Term.Tuple(Term.A("b"), Term.I(2))),
    ("split-empty", "lists:split(0,[])", Term.Tuple(Nil.Value, Nil.Value)),
    ("split-zero", "lists:split(0,[a,b])", Term.Tuple(Nil.Value, Term.List(Term.A("a"), Term.A("b")))),
    ("split-middle", "lists:split(1,[a,b,c])", Term.Tuple(Term.List(Term.A("a")), Term.List(Term.A("b"), Term.A("c")))),
    ("split-exact", "lists:split(2,[a,b])", Term.Tuple(Term.List(Term.A("a"), Term.A("b")), Nil.Value)),
    ("split-improper-zero", "lists:split(0,[a|tail])", Term.Tuple(Nil.Value, new Cons(Term.A("a"), Term.A("tail")))),
    ("split-improper-middle", "lists:split(1,[a,b|tail])", Term.Tuple(Term.List(Term.A("a")), new Cons(Term.A("b"), Term.A("tail")))),
    ("split-improper-exact", "lists:split(2,[a,b|tail])", Term.Tuple(Term.List(Term.A("a"), Term.A("b")), Term.A("tail"))),
    ("split-integer-tail", "lists:split(1,[a|42])", Term.Tuple(Term.List(Term.A("a")), Term.I(42)))
})
    Test($"lists/{sample.Name}", async () => Equal(await Eval(sample.Source), sample.Expected));

foreach (var sample in new (string Name, string Source, string Reason)[]
{
    ("last-empty", "lists:last([])", "function_clause"),
    ("last-nonlist", "lists:last(atom)", "function_clause"),
    ("last-improper", "lists:last([a,b|tail])", "function_clause"),
    ("split-negative", "lists:split(-1,[a])", "badarg"),
    ("split-float-count", "lists:split(1.0,[a])", "badarg"),
    ("split-atom-count", "lists:split(atom,[a])", "badarg"),
    ("split-nonlist-zero", "lists:split(0,atom)", "badarg"),
    ("split-empty-overrun", "lists:split(1,[])", "badarg"),
    ("split-proper-overrun", "lists:split(3,[a,b])", "badarg"),
    ("split-improper-overrun", "lists:split(3,[a,b|tail])", "function_clause"),
    ("split-huge-proper", "lists:split(999999999999999999999999,[a])", "badarg"),
    ("split-huge-improper", "lists:split(999999999999999999999999,[a|tail])", "function_clause")
})
    Test($"lists/{sample.Name}", async () => Equal(await MapError(sample.Source), Term.A(sample.Reason)));

Test(
    "lists/last-split-long-list-identity",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    Term marker = Term.Tuple(Term.A("marker"));
    Term suffix = Term.List(marker);
    Term list = suffix;
    for (int i = 0; i < 100000; i++)
        list = new Cons(Term.I(i), list);
    var process = runtime.Spawn(async context =>
    {
        Term last = await runtime.Modules.Call(
            context,
            "lists",
            "last",
            [list]
        );
        Check(ReferenceEquals(last, marker));
        var split = (TupleTerm)await runtime.Modules.Call(
            context,
            "lists",
            "split",
            [Term.I(100000), list]
        );
        Check(ReferenceEquals(split.Items[1], suffix));
        Check(Cons.Items(split.Items[0]).Count() == 100000);
        Check(Cons.Items(list).Count() == 100001);

        return Term.A("ok");
    });
    Equal(await process.Completion, Term.A("normal"));
}
);
Test(
    "lists/nth-improper-prefix",
    async () => Equal(await Eval("{lists:nth(1,[a|tail]),lists:nth(2,[a,b|tail])}"), Term.Tuple(Term.A("a"), Term.A("b")))
);
Test(
    "lists/nth-invalid-function-clause",
    async () =>
{
    foreach (string source in new[] { "lists:nth(0,[a])", "lists:nth(-1,[a])", "lists:nth(1.0,[a])", "lists:nth(2,[a|tail])", "lists:nth(1,[])", "lists:nth(1,atom)", "lists:nth(999999999999999999999999,[a])" })
        Equal(await MapError(source), Term.A("function_clause"));
}
);
Test(
    "lists/nthtail-empty-and-improper",
    async () => Equal(
        await Eval("{lists:nthtail(0,[]),lists:nthtail(0,[a|tail]),lists:nthtail(2,[a,b|tail])}"),
        Term.Tuple(Nil.Value, new Cons(Term.A("a"), Term.A("tail")), Term.A("tail"))
    )
);
Test(
    "lists/nthtail-invalid-function-clause",
    async () =>
{
    foreach (string source in new[] { "lists:nthtail(0,atom)", "lists:nthtail(-1,[a])", "lists:nthtail(1.0,[a])", "lists:nthtail(3,[a,b|tail])", "lists:nthtail(1,[])" })
        Equal(await MapError(source), Term.A("function_clause"));
}
);
Test(
    "lists/seq-default-empty-and-negative",
    async () => Equal(
        await Eval("{lists:seq(2,1),lists:seq(-2,1)}"),
        Term.Tuple(Nil.Value, Term.List(
            Term.I(-2),
            Term.I(-1),
            Term.I(0),
            Term.I(1)
        ))
    )
);
Test(
    "lists/seq-two-arg-error-contract",
    async () =>
{
    foreach (string source in new[] { "lists:seq(3,1)", "lists:seq(1.0,3)", "lists:seq(1,atom)" })
        Equal(await MapError(source), Term.A("function_clause"));
}
);
Test(
    "lists/seq-step-boundaries",
    async () => Equal(
        await Eval("{lists:seq(1,6,2),lists:seq(6,1,-2),lists:seq(3,1,2),lists:seq(3,5,-2)}"),
        Term.Tuple(
            Term.List(Term.I(1), Term.I(3), Term.I(5)),
            Term.List(Term.I(6), Term.I(4), Term.I(2)),
            Nil.Value,
            Nil.Value
        )
    )
);
Test("lists/seq-zero-step", async () => Equal(await Eval("lists:seq(7,7,0)"), Term.List(Term.I(7))));
Test(
    "lists/seq-three-arg-error-contract",
    async () =>
{
    foreach (string source in new[] { "lists:seq(1,2,0)", "lists:seq(4,1,2)", "lists:seq(3,6,-2)", "lists:seq(1,3,1.0)", "lists:seq(1.0,3,1)", "lists:seq(1,atom,1)" })
        Equal(await MapError(source), Term.A("badarg"));
}
);
Test(
    "lists/seq-big-integer-values",
    async () => Equal(
        await Eval("lists:seq(9223372036854775808,9223372036854775812,2)"),
        Term.List(
            new Integer(BigInteger.Parse("9223372036854775808")),
            new Integer(BigInteger.Parse("9223372036854775810")),
            new Integer(BigInteger.Parse("9223372036854775812"))
        )
    )
);
Test("lists/seq-length-system-limit", async () => Equal(await MapError("lists:seq(0,2147483647)"), Term.A("system_limit")));

Test(
    "lists/reverse-short-error-contract",
    async () =>
 {
     foreach (string source in new[] { "lists:reverse(atom)", "lists:reverse([a|tail])" })
         Equal(await MapError(source), Term.A("function_clause"));
 }
);
Test(
    "lists/reverse-long-improper-badarg",
    async () =>
 {
     foreach (string source in new[] { "lists:reverse([a,b|tail])", "lists:reverse([a,b,c|tail])", "lists:reverse(atom,tail)", "lists:reverse([a|tail],[])" })
         Equal(await MapError(source), Term.A("badarg"));
 }
);
Test(
    "lists/reverse-tail-and-empty",
    async () =>
 {
     Equal(await Eval("lists:reverse([],tail)"), Term.A("tail"));
     Equal(await Eval("lists:reverse([a,b],tail)"), new Cons(Term.A("b"), new Cons(Term.A("a"), Term.A("tail"))));
 }
);
Test(
    "lists/keyfind-skip-and-first",
    async () => Equal(
        await Eval("lists:keyfind(a,2,[atom,{}, {a},{x,a,first},{y,a,second}])"),
        Term.Tuple(Term.A("x"), Term.A("a"), Term.A("first"))
    )
);
Test(
    "lists/keyfind-improper-prefix",
    async () => Equal(await Eval("lists:keyfind(a,1,[{a,found}|tail])"), Term.Tuple(Term.A("a"), Term.A("found")))
);
Test(
    "lists/keysearch-missing",
    async () =>
 {
     foreach (string source in new[] { "lists:keyfind(a,2,[{},atom,{a}])", "lists:keymember(a,1,[])", "lists:keysearch(a,1,[{b}])" })
         Equal(await Eval(source), Term.A("false"));
 }
);
Test(
    "lists/keysearch-invalid-position",
    async () =>
 {
     foreach (string source in new[] { "lists:keyfind(a,0,[])", "lists:keymember(a,-1,[{a}])", "lists:keysearch(a,1.0,[])", "lists:keyfind(a,576460752303423488,[])" })
         Equal(await MapError(source), Term.A("badarg"));
 }
);
Test(
    "lists/keysearch-improper-missing",
    async () =>
 {
     foreach (string source in new[] { "lists:keyfind(a,1,[{b}|tail])", "lists:keymember(a,1,atom)", "lists:keysearch(a,1,[atom|tail])" })
         Equal(await MapError(source), Term.A("badarg"));
 }
);
Test(
    "lists/keysearch-large-position",
    async () => Equal(await Eval("lists:keyfind(a,576460752303423487,[{a}])"), Term.A("false"))
);
Test(
    "lists/keysearch-small-integer-rounded-float",
    async () => Equal(await Eval("lists:keyfind(9007199254740993,1,[{9007199254740992.0}])"), Term.Tuple(new FloatTerm(9007199254740992d)))
);
Test(
    "lists/keysearch-float-key-exact-rational",
    async () => Equal(await Eval("lists:keyfind(9007199254740992.0,1,[{9007199254740993}])"), Term.A("false"))
);
Test(
    "lists/keysearch-nested-numeric-key",
    async () => Equal(
        await Eval("lists:keysearch({[1],#{a=>2}},1,[{{[1.0],#{a=>2.0}},found}])"),
        Term.Tuple(
            Term.A("value"),
            Term.Tuple(
                Term.Tuple(Term.List(new FloatTerm(1)), new MapTerm(new[] { new KeyValuePair<Term, Term>(Term.A("a"), new FloatTerm(2)) })),
                Term.A("found")
            )
        )
    )
);

Test(
    "terms/map-index-exact-and-structural-keys",
    () =>
{
    var map = new MapTerm([
        new(Term.I(1), Term.A("integer")),
        new(new FloatTerm(1), Term.A("float")),
        new(Term.Tuple(Term.List(Term.I(2))), Term.A("nested")),
        new(new FloatTerm(0), Term.A("first")),
        new(new FloatTerm(-0d), Term.A("last"))
    ]);
    Equal(map.Get(Term.I(1)), Term.A("integer"));
    Equal(map.Get(new FloatTerm(1)), Term.A("float"));
    Equal(map.Get(Term.Tuple(Term.List(Term.I(2)))), Term.A("nested"));
    Equal(map.Get(new FloatTerm(0)), Term.A("first"));
    Equal(map.Get(new FloatTerm(-0d)), Term.A("last"));
    Check(!map.TryGet(Term.I(0), out var missing) && missing is null);
    Check(map.Entries.Count == 5);

    return Task.CompletedTask;
}
);
Test(
    "terms/map-index-owned-input-and-readonly-entries",
    () =>
{
    var input = new Dictionary<Term, Term> { [Term.A("a")] = Term.I(1) };
    var map = new MapTerm(input);
    input[Term.A("a")] = Term.I(2);
    Equal(map.Get(Term.A("a")), Term.I(1));
    var entries = (IList<KeyValuePair<Term, Term>>)map.Entries;
    Throws<NotSupportedException>(() => entries[0] = new(Term.A("a"), Term.I(3)));
    Equal(map.Get(Term.A("a")), Term.I(1));

    return Task.CompletedTask;
}
);
Test(
    "terms/map-index-nested-map-key-order",
    () =>
{
    var key1 = new MapTerm([new(Term.A("a"), Term.I(1)), new(Term.A("b"), Term.I(2))]);
    var key2 = new MapTerm([new(Term.A("b"), Term.I(2)), new(Term.A("a"), Term.I(1))]);
    var map = new MapTerm([new(key1, Term.A("found"))]);
    Equal(map.Get(key2), Term.A("found"));
    Check(map.TryGet(key2, out var value));
    Equal(value!, Term.A("found"));

    return Task.CompletedTask;
}
);
Test(
    "compiler/comprehension/binary-invalid-patterns",
    () =>
    {
        foreach (string source in new[] { "[X || X <= <<1>>]", "[X || <<X/binary>> <= <<1>>]", "[X || <<X:all/bits>> <:= <<1>>]", "[1 || <<>> <= <<1>>]", "<< <<X>> || >>" })
            Throws<CompileException>(() => new Parser(source).ParseExpression());

        return Task.CompletedTask;
    }
);
Test(
    "compiler/comprehension/map-invalid-syntax",
    () =>
    {
        foreach (string source in new[] { "#{K=>V || K=>V <- #{}}", "#{K=>V || K := V <= <<1>>}", "M#{a=>1 || true}" })
            Throws<CompileException>(() => new Parser(source).ParseExpression());

        return Task.CompletedTask;
    }
);
Test(
    "compiler/comprehension/map-exact-template-rejected",
    () =>
    {
        Throws<CompileException>(() => Semantics.Validate(new Parser("#{a:=1 || true}").ParseExpression()));

        return Task.CompletedTask;
    }
);
Test(
    "compiler/comprehension/map-illegal-guard",
    () =>
    {
        Throws<CompileException>(() => Semantics.Validate(new Parser("if #{a=>1 || true} =:= #{} -> ok end").ParseExpression()));

        return Task.CompletedTask;
    }
);
Test(
    "hybrid/comprehension/map-ast-emission",
    () =>
    {
        string source = "class A { async Task F(ProcessContext erlangProcess) { var x = begin #{K=>V+1 || K := V <:- #{a=>1}} end. } }";
        string generated = CodeGeneration.Preprocess(source, "map.cs");
        Check(generated.Contains("Expr.MapComprehension") && generated.Contains("ComprehensionQualifier.MapGenerator"));

        return Task.CompletedTask;
    }
);
Test(
    "compiler/comprehension/binary-illegal-guard",
    () =>
    {
        Throws<CompileException>(() => Semantics.Validate(new Parser("if << <<1>> || true >> =:= <<1>> -> ok end").ParseExpression()));

        return Task.CompletedTask;
    }
);
Test(
    "compiler/comprehension/binary-zero-progress-resource-policy",
    async () =>
    {
        var value = await Eval("try [X || <<X:0>> <= <<1>>] catch error:R -> R end");
        Equal(value, Term.A(ErlangErrorReasons.SystemLimit));
    }
);
Test(
    "hybrid/comprehension/binary-ast-emission",
    () =>
    {
        string source = "class A { async Task F(ProcessContext erlangProcess) { var x = begin Encoded= << <<N:3>> || N <- [1,2] >>,[N || <<N:3>> <:= Encoded] end. } }";
        string generated = CodeGeneration.Preprocess(source, "binary.cs");
        Check(generated.Contains("Expr.BinaryComprehension") && generated.Contains("ComprehensionQualifier.BinaryGenerator"));

        return Task.CompletedTask;
    }
);
Test(
    "compiler/zip/invalid-group",
    () =>
{
    foreach (string source in new[] { "[X || X <- [1] && true]", "[X || true && X <- [1]]", "[X || X <- [1] &&]", "1 && 2" })
        Throws<CompileException>(() => new Parser(source).ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "compiler/zip/illegal-guard",
    () =>
{
    Throws<CompileException>(() => Semantics.Validate(new Parser("case ok of X when [Y || Y <- [] && Z <- []] =:= [] -> X end").ParseExpression()));

    return Task.CompletedTask;
}
);
Test(
    "compiler/zip/large-iterative",
    async () =>
{
    var result = await Eval("[X+Y || X <- lists:seq(1,20000) && Y <- lists:seq(1,20000)]");
    Equal(result, Cons.From(Enumerable.Range(1, 20000).Select(value => Term.I(value * 2))));
}
);
Test(
    "compiler/zip/no-progress-local-policy",
    async () =>
{
    await using var runtime = new ProcessRuntime();
    var expression = new Parser("[X+Y || <<X:0>> <= <<>> && <<Y:0>> <= <<>>]").ParseExpression();
    Semantics.Validate(expression);
    var process = runtime.Spawn(async context =>
    {
        try
        {
            await Execution.EvaluateAsync(expression, context);
            throw new InvalidOperationException("Expected local zero-progress failure");
        }
        catch (ErlangException exception)
        {
            Equal(exception.Reason, Term.A("system_limit"));
        }

        return Term.A("ok");
    });
    Equal(await process.Completion.WaitAsync(TimeSpan.FromSeconds(3)), Term.A("normal"));
}
);
Test(
    "hybrid/comprehension/zip-ast-emission",
    () =>
{
    string source = "class A { async Task F(ProcessContext erlangProcess) { var value = begin [X+Y || X <- [1] && Y <- [2]] end. if (true && false) { } } }";
    string generated = CodeGeneration.Preprocess(source, "zip.cs");
    Check(generated.Contains("ComprehensionQualifier.Zip") && generated.Contains("true && false"));

    return Task.CompletedTask;
}
);
foreach (var fixture in Erlang.Differential.BeginOperatorCases.All.Concat(Erlang.Differential.ExpressionListCases.All).Concat(Erlang.Differential.MapBindingCases.All).Concat(Erlang.Differential.BitEvaluationCases.All).Concat(Erlang.Differential.BitEmptyStringCases.All).Concat(Erlang.Differential.MatchTimingCases.All).Concat(Erlang.Differential.TryExpressionCases.All).Concat(Erlang.Differential.CatchPatternCases.All).Concat(Erlang.Differential.StackGuardScopeCases.All).Concat(Erlang.Differential.MaybeExpressionCases.All).Concat(Erlang.Differential.AliasPatternCases.All).Concat(Erlang.Differential.ListComprehensionCases.All).Concat(Erlang.Differential.BinaryComprehensionCases.All).Concat(Erlang.Differential.MapComprehensionCases.All).Concat(Erlang.Differential.MapTemplateOrderCases.All).Concat(Erlang.Differential.ComparatorSortCases.All).Concat(Erlang.Differential.ZipGeneratorCases.All))
{
    Test(
        "compiler/operators/" + fixture.Name,
        async () =>
    {
        var expression = new Parser(fixture.Source).ParseExpression();
        Semantics.Validate(expression);
        await using var runtime = new ProcessRuntime();
        Term? outcome = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                outcome = Term.Tuple(Term.A("ok"), await Execution.EvaluateAsync(expression, context));
            }
            catch (ErlangException exception)
            {
                outcome = Term.Tuple(Term.A("error"), Term.A(exception.ExceptionClass), exception.Reason);
            }

            return Term.A("ok");
        });
        Equal(await process.Completion, Term.A("normal"));
        Equal(outcome!, fixture.Expected);
    }
    );
}
Test(
    "compiler/operators/chained-comparison-diagnostic",
    () =>
{
    foreach (string source in new[] { "1 < 2 < 3", "1 == 1 =:= true", "1 >= 0 /= false" })
        Throws<CompileException>(() => new Parser(source).ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "compiler/operators/empty-block-diagnostic",
    () =>
{
    Throws<CompileException>(() => new Parser("begin end").ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "compiler/operators/block-catch-illegal-guard",
    () =>
{
    foreach (string source in new[] { "if (begin true end) -> yes end", "if (catch true) -> yes end" })
    {
        try
        {
            Semantics.Validate(new Parser(source).ParseExpression());
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == "ERL007");
        }
    }

    return Task.CompletedTask;
}
);
Test(
    "hybrid/operators/begin-and-catch",
    () =>
{
    string source = "class A { async Task F(ProcessContext erlangProcess) { var x = begin X=40,if X>0 -> begin X+2 end end end. var y = catch throw(done). } }";
    string generated = CodeGeneration.Preprocess(source, "a.cs");
    Check(generated.Contains("Expr.Block") && generated.Contains("Expr.Catch"));
    Check(!generated.Contains("var y = catch"));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/operators/csharp-try-catch-preserved",
    () =>
{
    string source = "class A { void F() { try { throw new Exception(); } catch(Exception e) { Console.WriteLine(e); } catch { } } string s=\"begin catch value end.\"; int begin()=>42; }";
    Check(CodeGeneration.Preprocess(source, "a.cs").EndsWith(source, StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "compiler/operators/catch-logical-stack-frame",
    async () =>
{
    var value = await Eval("catch error(boom)");
    Check(value is TupleTerm { Items.Count: 2 } outer && outer.Items[0].Equals(Term.A("EXIT")));
    var reasonAndStack = (TupleTerm)((TupleTerm)value).Items[1];
    var frame = (TupleTerm)Cons.Items(reasonAndStack.Items[1]).First();
    Equal(frame.Items[0], Term.A("erlang"));
    Equal(frame.Items[1], Term.A("error"));
    Equal(frame.Items[2], Term.List(Term.A("boom")));
}
);
foreach (var fixture in Erlang.Differential.IfExpressionCases.All)
{
    Test(
        "compiler/if/" + fixture.Name,
        async () =>
    {
        var expression = new Parser(fixture.Source).ParseExpression();
        Semantics.Validate(expression);
        await using var runtime = new ProcessRuntime();
        Term? outcome = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                outcome = Term.Tuple(Term.A("ok"), await Execution.EvaluateAsync(expression, context));
            }
            catch (ErlangException exception)
            {
                outcome = Term.Tuple(Term.A("error"), Term.A(exception.ExceptionClass), exception.Reason);
            }

            return Term.A("ok");
        });
        Equal(await process.Completion, Term.A("normal"));
        Equal(outcome!, fixture.Expected);
    }
    );
}
Test(
    "compiler/if/empty-syntax",
    () =>
{
    Throws<CompileException>(() => new Parser("if end").ParseExpression());

    return Task.CompletedTask;
}
);
Test(
    "compiler/if/illegal-guards",
    () =>
{
    foreach (string source in new[] { "if put(key,value) -> ok end", "if X=1 -> ok end", "if lists:member(a,[]) -> ok end", "if (if true -> true end) -> ok end", "if self() ! message -> ok end" })
    {
        try
        {
            Semantics.Validate(new Parser(source).ParseExpression());
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == "ERL007");
        }
    }

    return Task.CompletedTask;
}
);
Test(
    "compiler/if/unsafe-rematch",
    () =>
{
    try
    {
        Semantics.Validate(new Parser("case ok of ok -> if true -> X=1; false -> no end,X=2 end").ParseExpression());
        Check(false);
    }
    catch (CompileException exception)
    {
        Check(exception.Code == "ERL006" && exception.Message == "Unsafe match variable 'X'");
    }

    return Task.CompletedTask;
}
);
Test(
    "hybrid/if/csharp-statements-preserved",
    () =>
{
    string source = "class A { int F(bool x) { if(x) { return 1; } else if(!x) return 2; return 3; } string s=\"if true -> no end.\"; /* if false -> no end. */ }";
    Check(CodeGeneration.Preprocess(source, "a.cs").EndsWith(source, StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/if/nested-and-following-csharp",
    () =>
{
    string source = "class A { async Task F(ProcessContext erlangProcess) { var x = if true -> if false -> no; true -> 42 end end. if(x.Equals(Term.I(42))) return; var y = case ok of ok -> if true -> 7 end end. } }";
    string generated = CodeGeneration.Preprocess(source, "a.cs");
    Check(generated.Contains("Expr.If") && generated.Contains("Expr.Case"));
    Check(generated.Contains("if(x.Equals(Term.I(42))) return;"));
    Check(!generated.Contains("var x = if"));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/if/parenthesized-guard-alternatives",
    () =>
{
    string source = "class A { async Task<Term> F(ProcessContext erlangProcess) { return if (false); (true) -> 42 end. } }";
    string generated = CodeGeneration.Preprocess(source, "a.cs");
    Check(generated.Contains("Expr.GuardAlternatives") && !generated.Contains("return if"));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/if/missing-terminator",
    () =>
{
    Throws<CompileException>(() => CodeGeneration.Preprocess("class A { async Task F(ProcessContext erlangProcess) { var x = if true -> ok end; } }", "a.cs"));

    return Task.CompletedTask;
}
);
foreach (string source in new[]
{
    "try ok end",
    "try ok of catch _ -> no end",
    "try error(reason) catch error:R:[] -> R end"
})
{
    Test(
        "compiler/try/invalid-grammar/" + source,
        () =>
    {
        Throws<CompileException>(() => new Parser(source).ParseExpression());

        return Task.CompletedTask;
    }
    );
}
foreach (var fixture in new (string Source, string Code, string Message)[]
{
    ("begin S=[],try error(reason) catch error:R:S -> R end end", "ERL006", "Stacktrace variable 'S' must be fresh"),
    ("try throw(reason) catch C:S:S -> C end", "ERL006", "Stacktrace variable 'S' must be fresh"),
    ("try error(reason) catch error:R:S when is_list(S) -> R end", "ERL007", "Stacktrace variable 'S' is not legal in a guard")
})
{
    Test(
        "compiler/try/stack-diagnostic/" + fixture.Source,
        () =>
    {
        try
        {
            Semantics.Validate(new Parser(fixture.Source).ParseExpression());
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == fixture.Code && exception.Message == fixture.Message);
        }

        return Task.CompletedTask;
    }
    );
}
Test(
    "hybrid/try/csharp-statements-preserved",
    () =>
{
    string source = "class A { int F() { try { return 1; } catch (Exception) { return 2; } finally { G(); } } void G() {} }";
    Check(CodeGeneration.Preprocess(source, "a.cs").EndsWith(source, StringComparison.Ordinal));

    return Task.CompletedTask;
}
);
Test(
    "hybrid/try/nested-and-following-csharp",
    () =>
{
    string source = "class A { async Task F(ProcessContext erlangProcess) { var x = try try throw(7) catch N -> N end after ignored end. try { G(); } finally { G(); } } }";
    string generated = CodeGeneration.Preprocess(source, "a.cs");
    Check(generated.Contains("Expr.Try") && !generated.Contains("var x = try"));
    Check(generated.Contains("try { G(); } finally { G(); }"));

    return Task.CompletedTask;
}
);
Test(
    "oracle/result-frame-noisy-output",
    () =>
{
    Term expected = Term.Tuple(Term.A("ok"), Term.Tuple(Term.A("error"), Term.A("failed")));
    string encoded = Convert.ToBase64String(ExternalTermFormat.Encode(expected));
    string frame = Erlang.Differential.OracleResultProtocol.Begin + encoded + Erlang.Differential.OracleResultProtocol.End;
    Equal(
        Erlang.Differential.OracleResultProtocol.Decode("=CRASH REPORT=\nstartup error\n" + frame + "\n=WARNING REPORT=\nlate log"),
        expected
    );

    return Task.CompletedTask;
}
);
Test(
    "oracle/result-frame-reject-ambiguous",
    () =>
{
    string begin = Erlang.Differential.OracleResultProtocol.Begin, end = Erlang.Differential.OracleResultProtocol.End;
    foreach (string output in new[] { "no frame", begin + "missing end", end + begin, begin + end + begin + end })
    {
        Throws<InvalidOperationException>(() => Erlang.Differential.OracleResultProtocol.Decode(output));
    }

    return Task.CompletedTask;
}
);
Test(
    "oracle/result-frame-reject-invalid-payload",
    () =>
{
    try
    {
        Erlang.Differential.OracleResultProtocol.Decode(Erlang.Differential.OracleResultProtocol.Begin + "not base64!" + Erlang.Differential.OracleResultProtocol.End);
        Check(false);
    }
    catch (FormatException) { }

    return Task.CompletedTask;
}
);
Test(
    "oracle/map-display-output-contract-keeps-exact-byte-boundaries",
    () =>
{
    var map = Erlang.Differential.TermDisplayCases.Modules.Single(fixture => fixture.Name == "erlang-term-display-map");
    Check(Erlang.Differential.CompiledStandardOutputContract.Matches(map, map.ExpectedStandardOutputHex!, ""));
    Check(Erlang.Differential.CompiledStandardOutputContract.Matches(map, map.ExpectedStandardOutputAlternatives!.Single(), ""));
    foreach (string output in new[] { "", map.ExpectedStandardOutputHex![..^4] + "0A", map.ExpectedStandardOutputHex + "00", map.ExpectedStandardOutputAlternatives!.Single() + "0A" })
        Check(!Erlang.Differential.CompiledStandardOutputContract.Matches(map, output, ""));
    Check(!Erlang.Differential.CompiledStandardOutputContract.Matches(map, map.ExpectedStandardOutputHex!, "00"));
    var atom = Erlang.Differential.TermDisplayCases.Modules.Single(fixture => fixture.Name == "erlang-term-display-atom");
    Check(atom.ExpectedStandardOutputAlternatives is null);
    Check(!Erlang.Differential.CompiledStandardOutputContract.Matches(atom, map.ExpectedStandardOutputAlternatives!.Single(), ""));

    return Task.CompletedTask;
}
);
foreach (var fixture in Erlang.Differential.CompiledModuleCases.All.Where(fixture => !Erlang.Differential.SchedulerAffinityCases.Modules.Contains(fixture) && !Erlang.Differential.SizeErrorCases.Modules.Contains(fixture) && !Erlang.Tests.RuntimeIncrementCompiledCases.Names.Contains(fixture.Name)))
{
    Test(
        "oracle/compiled-module/" + fixture.Name,
        async () =>
    {
        using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
        if (fixture.ExpectedStandardOutputHex is not null || fixture.ExpectedStandardErrorHex is not null)
        {
            var captured = await Erlang.Differential.CompiledModuleExecution.RunCaptured(artifact, fixture.CallerContext);
            Equal(captured.Result, fixture.Expected);
            Check(Erlang.Differential.CompiledStandardOutputContract.Matches(fixture, captured.StandardOutputHex, captured.StandardErrorHex));
        }
        else
            Equal(await Erlang.Differential.CompiledModuleExecution.Run(artifact, fixture.CallerContext), fixture.Expected);
        Check(artifact.CSharpSha256.Length == 64);
    }
    );
}
foreach (var fixture in Erlang.Differential.CompiledDiagnosticCases.All)
{
    Test(
        "oracle/compiler-diagnostic/" + fixture.Name,
        () =>
    {
        try
        {
            using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
            Check(false);
        }
        catch (CompileException exception)
        {
            Check(exception.Code == Erlang.Differential.CompiledDiagnosticExpectations.Code(fixture));
            Check(exception.Message == Erlang.Differential.CompiledDiagnosticExpectations.Message(fixture));
        }

        return Task.CompletedTask;
    }
    );
}
Test(
    "oracle/compiler-diagnostic-ascii-protocol",
    () =>
{
    string command = Erlang.Differential.CompiledModuleProtocol.DiagnosticCommand("-module(scope_unicode). -export([run/0]). run()-><<('𐀀'):S>>.");
    Check(command.All(character => character <= 127));
    Check(command.Contains("compile:forms") && command.Contains("lists:usort"));
    Check(!command.Contains("code:load_binary"));

    return Task.CompletedTask;
}
);
Test(
    "oracle/native-investigation-ascii-context-and-compiler-modes",
    () =>
    {
        const string source = "-module(investigation_unicode). -export([run/0]). run()->'𐀀'.";
        var commands = new HashSet<string>(StringComparer.Ordinal);
        foreach (bool disableOptimizations in new[] { false, true })
        {
            foreach (bool? trapExits in new bool?[] { null, false, true })
            {
                string command = Erlang.Differential.NativeCompilationInvestigation.Command(source, disableOptimizations, trapExits);
                Check(command.All(character => character <= 127));
                Check(command.Contains("beam_disasm:file(Bin)"));
                Check(command.Contains("Result={Outcome,Disassembly}"));
                Check(command.Contains("no_copt,no_ssa_opt,no_postopt") == disableOptimizations);
                Check(command.Contains("spawn_monitor") == (trapExits is not null));
                if (trapExits is not null)
                    Check(command.Contains(trapExits.Value ? "process_flag(trap_exit,true)" : "process_flag(trap_exit,false)"));
                Check(commands.Add(command));
            }
        }

        return Task.CompletedTask;
    }
);
Test(
    "oracle/native-investigation-isolated-compiler-passes",
    () =>
    {
        const string source = "-module(pass_investigation). -export([run/0]). run()->done.";
        string[] passes = [
            Erlang.Differential.OracleInvestigationModes.DisabledCoreOptimization,
            Erlang.Differential.OracleInvestigationModes.DisabledSsaOptimization,
            Erlang.Differential.OracleInvestigationModes.DisabledPostOptimization
        ];
        foreach (string pass in passes)
        {
            string command = Erlang.Differential.NativeCompilationInvestigation.Command(source, pass, false);
            Check(command.Contains("return_warnings," + pass + "]"));
            Check(passes.Where(other => other != pass).All(other => !command.Contains(other)));
            Check(command.Contains("process_flag(trap_exit,false)"));
        }

        return Task.CompletedTask;
    }
);
Test(
    "oracle/explicit-compiled-caller-trap-context",
    async () =>
    {
        const string source = "-module(caller_context_contract). -export([run/0]). run()->process_flag(trap_exit,true).";
        using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(source);
        Equal(
            await Erlang.Differential.CompiledModuleExecution.Run(artifact),
            Term.Tuple(Term.A(Erlang.Differential.CompiledContractOutcomeNames.Success), Term.A(ErlangBooleanAtoms.False))
        );
        Equal(
            await Erlang.Differential.CompiledModuleExecution.Run(artifact, Erlang.Differential.CompiledModuleCallerContext.TrappedFresh),
            Term.Tuple(Term.A(Erlang.Differential.CompiledContractOutcomeNames.Success), Term.A(ErlangBooleanAtoms.True))
        );
        Check(Erlang.Differential.CompiledModuleProtocol.Command(source).Contains("process_flag(trap_exit,false)"));
        Check(Erlang.Differential.CompiledModuleProtocol.Command(source, Erlang.Differential.CompiledModuleCallerContext.TrappedFresh).Contains("process_flag(trap_exit,true)"));
    }
);
Test(
    "oracle/two-preserved-spawn-ssa-policies",
    () =>
    {
        var fixtures = Erlang.Differential.CompiledModuleCases.All.Where(fixture => fixture.Name != "erlang-node-spawn-tuple-callback" && fixture.CompilerPolicy != Erlang.Differential.CompiledModuleCompilerPolicy.Default).ToArray();
        Check(fixtures.Length == 2);
        Check(fixtures.Select(fixture => fixture.Name).SequenceEqual(new[] { "erlang-spawn-group-callback-tuple", "erlang-spawn-group-callback-wrong-arity-child-error" }));
        foreach (var fixture in fixtures)
        {
            string command = Erlang.Differential.CompiledModuleProtocol.Command(fixture.Source, fixture.CallerContext, fixture.CompilerPolicy);
            Check(command.Contains("return_warnings,no_ssa_opt]"));
            Check(!command.Contains("no_copt") && !command.Contains("no_postopt"));
            Check(!Erlang.Differential.CompiledModuleProtocol.Command(fixture.Source).Contains("no_ssa_opt"));
        }

        return Task.CompletedTask;
    }
);
Test(
    "oracle/node-qualified-tuple-spawn-ssa-policy-is-narrow-and-source-preserving",
    () =>
    {
        var fixture = Erlang.Differential.NodeSpawnCases.Modules.Single(fixture => fixture.Name == "erlang-node-spawn-tuple-callback");
        Check(fixture.CompilerPolicy == Erlang.Differential.CompiledModuleCompilerPolicy.DisableSsaOptimization);
        Check(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fixture.Source))) == "1FE0A6EB579160174B11A54630FA4BAD787FCEBAA5B616199CBBF5FC8424AD00");
        Check(Erlang.Differential.CompiledModuleCases.All.Count(fixture => fixture.CompilerPolicy != Erlang.Differential.CompiledModuleCompilerPolicy.Default) == 3);
        Check(Erlang.Differential.CompiledModuleProtocol.Command(fixture.Source, fixture.CallerContext, fixture.CompilerPolicy).Contains("return_warnings,no_ssa_opt]"));
        Check(!Erlang.Differential.CompiledModuleProtocol.Command(fixture.Source).Contains("no_ssa_opt"));

        return Task.CompletedTask;
    }
);
Test(
    "oracle/compiled-module-ascii-protocol",
    () =>
{
    string command = Erlang.Differential.CompiledModuleProtocol.Command("-module(unicode_fixture). -export([run/0]). run()->'𐀀'.");
    Check(command.All(character => character <= 127));
    Check(command.Contains("compile:forms"));
    Check(command.Contains("code:load_binary"));
    Check(!command.Contains("erl_eval:exprs"));

    return Task.CompletedTask;
}
);
Test(
    "oracle/compiled-module-repeat-context",
    async () =>
{
    var fixture = Erlang.Differential.CompiledModuleCases.All[0];
    using var first = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
    using var second = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
    Equal(await Erlang.Differential.CompiledModuleExecution.Run(first, fixture.CallerContext), fixture.Expected);
    Equal(await Erlang.Differential.CompiledModuleExecution.Run(second, fixture.CallerContext), fixture.Expected);
    Check(first.CSharpSha256 == second.CSharpSha256);
}
);
Test(
    "oracle/compiled-module-invalid-source",
    () =>
{
    Throws<Erlang.Compiler.CompileException>(() => Erlang.Differential.GeneratedModuleCompiler.Compile("-module(invalid). -export([run/0]). run()-><<1:all>>."));

    return Task.CompletedTask;
}
);

Test(
    "compiler/character-literal/scanner-offsets-and-invalid-escapes",
    () =>
{
    Erlang.Tests.CharacterLiteralScannerContracts.Run();

    return Task.CompletedTask;
}
);
Test("terms/atom-channel-index-concurrent-retention", Erlang.Tests.AtomChannelIndexContracts.Run);
Test(
    "oracle/report-checkpoint-retains-failure-final-and-abort-progress",
    () =>
{
    var checkpoints = new Erlang.Differential.OracleReportCheckpoint();
    Check(checkpoints.ShouldSave(
        0,
        0,
        false,
        null
    ));
    Check(!checkpoints.ShouldSave(
        1,
        0,
        false,
        null
    ));
    Check(!checkpoints.ShouldSave(
        63,
        0,
        false,
        null
    ));
    Check(checkpoints.ShouldSave(
        64,
        0,
        false,
        null
    ));
    Check(checkpoints.ShouldSave(
        65,
        1,
        false,
        null
    ));
    Check(!checkpoints.ShouldSave(
        66,
        1,
        false,
        null
    ));
    Check(checkpoints.ShouldSave(
        67,
        1,
        true,
        null
    ));
    Check(checkpoints.ShouldSave(
        68,
        1,
        false,
        "owned-infrastructure-failure"
    ));

    return Task.CompletedTask;
}
);
Test(Erlang.Tests.PriorityPermissionHostNames.Test, Erlang.Tests.PriorityPermissionHostChecks.Run);
Test(Erlang.Tests.BeamHostCheckNames.Test, Erlang.Tests.BeamHostChecks.Run);

Test(Erlang.Tests.BeamReceiveHostNames.Test, Erlang.Tests.BeamReceiveHostChecks.Run);
Test(Erlang.Tests.BeamClosureHostNames.Test, Erlang.Tests.BeamClosureHostChecks.Run);
Test(Erlang.Tests.MailboxSelectionCheckNames.Test, Erlang.Tests.MailboxSelectionChecks.Run);

Test(Erlang.Tests.ModuleRegistryLeaseCheckNames.Test, Erlang.Tests.ModuleRegistryLeaseChecks.Run);

foreach (var export in exportRegistry.Exports.Where(export => Erlang.Tests.FunctionalMfaContracts.SubprocessContract(export.Module, export.Function, export.Arity) is not null).OrderBy(export => export.Arity))
    Test(
        $"mfa/{export.Module}:{export.Function}/{export.Arity}",
        () => Erlang.Tests.FunctionalMfaContracts.RunSubprocess(Erlang.Tests.FunctionalMfaContracts.SubprocessContract(export.Module, export.Function, export.Arity)!)
    );

Test(Erlang.Tests.BeamPreparedHostNames.Test, Erlang.Tests.BeamPreparedHostChecks.Run);

Test(Erlang.Tests.BeamOnLoadHostNames.Test, Erlang.Tests.BeamOnLoadHostChecks.Run);

Test(Erlang.Tests.ReferenceResourceHostNames.Test, Erlang.Tests.ReferenceResourceHostChecks.Run);

Test(
    Erlang.Tests.BeamLoaderMfaNames.Prepare,
    () => Erlang.Tests.BeamLoaderMfaChecks.Run(Erlang.Tests.BeamLoaderMfaCase.Prepare)
);
Test(Erlang.Tests.BeamLoaderMfaNames.Query, () => Erlang.Tests.BeamLoaderMfaChecks.Run(Erlang.Tests.BeamLoaderMfaCase.Query));
Test(Erlang.Tests.BeamLoaderMfaNames.Finish, () => Erlang.Tests.BeamLoaderMfaChecks.Run(Erlang.Tests.BeamLoaderMfaCase.Finish));
Test(
    Erlang.Tests.BeamLoaderMfaNames.Callback,
    () => Erlang.Tests.BeamLoaderMfaChecks.Run(Erlang.Tests.BeamLoaderMfaCase.Callback)
);
Test(
    Erlang.Tests.BeamLoaderMfaNames.Finalize,
    () => Erlang.Tests.BeamLoaderMfaChecks.Run(Erlang.Tests.BeamLoaderMfaCase.Finalize)
);
Test(Erlang.Tests.BeamLoaderMfaNames.Load, () => Erlang.Tests.BeamLoaderMfaChecks.Run(Erlang.Tests.BeamLoaderMfaCase.Load));

Test(Erlang.Tests.BeamLoaderMfaNames.AdmissionHost, Erlang.Tests.BeamLoaderMfaChecks.Admission);
Test(Erlang.Tests.BeamLoaderMfaNames.RaceHost, Erlang.Tests.BeamLoaderMfaChecks.Races);
Test(Erlang.Tests.BeamLoaderMfaNames.PaddedHost, Erlang.Tests.BeamLoaderMfaChecks.Padded);
Test(Erlang.Tests.BeamLoaderMfaNames.DirectHost, Erlang.Tests.BeamLoaderMfaChecks.Direct);
Test(Erlang.Tests.BeamLoaderMfaNames.LifecycleHost, Erlang.Tests.BeamLoaderLifecycleChecks.Run);

Test(Erlang.Tests.StaticApplyOnLoadNames.Test, Erlang.Tests.StaticApplyOnLoadChecks.Run);

Test(Erlang.Tests.BeamGzipNativeNames.Test, Erlang.Tests.BeamGzipNativeChecks.Run);

Test(Erlang.Tests.HostDistributionTestNames.Test, Erlang.Tests.HostDistributionChecks.Run);

Test(Erlang.Tests.DistributionPendingTestNames.Test, Erlang.Tests.DistributionPendingChecks.Run);

Test(Erlang.Tests.DistributionPacketTestNames.Test, Erlang.Tests.DistributionPacketChecks.Run);

Test(Erlang.Tests.AtomNameValidationTestNames.Test, Erlang.Tests.AtomNameValidationChecks.Run);
Test(Erlang.Tests.RuntimeNodeIdentityFoundationHostNames.Test, Erlang.Tests.RuntimeNodeIdentityFoundationHostChecks.Run);
Test(Erlang.Tests.RuntimeNodeAdmissionFoundationHostNames.Test, Erlang.Tests.RuntimeNodeAdmissionFoundationHostChecks.Run);
Test(Erlang.Tests.HashIntrinsicHostNames.Test, Erlang.Tests.HashIntrinsicHostChecks.Run);
Test(Erlang.Tests.ProcessInfoLogicalHostNames.Test, Erlang.Tests.ProcessInfoLogicalHostChecks.Run);
Test(
    Erlang.Tests.ErlangHashAllocationControlNames.Test,
    Erlang.Tests.ErlangHashHostChecks.AllocationPositiveControl
);

Test(Erlang.Tests.RuntimeSetNodeHostNames.Test, Erlang.Tests.RuntimeSetNodeHostChecks.Run);
Test(Erlang.Tests.RuntimeNodeTextActivationHostNames.Test, Erlang.Tests.RuntimeNodeTextActivationHostChecks.Run);
Test(Erlang.Tests.ProcessBumpCapHostNames.Test, Erlang.Tests.ProcessBumpCapHostChecks.Run);
Test(Erlang.Tests.RuntimeNodeCreationHostNames.Test, Erlang.Tests.RuntimeNodeCreationHostChecks.Run);
Test(Erlang.Tests.DistributionRegisteredHostNames.Test, Erlang.Tests.DistributionRegisteredHostChecks.Run);
Test(Erlang.Tests.ConnectedDistributionHostNames.Test, Erlang.Tests.ConnectedDistributionHostChecks.Run);
Test(Erlang.Tests.ConnectedIngressHostNames.Test, Erlang.Tests.ConnectedIngressHostChecks.Run);
Test(Erlang.Tests.DistributionRawInputHostNames.Test, Erlang.Tests.DistributionRawInputHostChecks.Run);
Test(Erlang.Tests.DistributionRawSignalHostNames.Test, Erlang.Tests.DistributionRawSignalHostChecks.Run);
Test(Erlang.Tests.DistributionMonitorHostNames.Test, Erlang.Tests.DistributionMonitorHostChecks.Run);
Test(Erlang.Tests.DistributionGroupLeaderHostNames.Test, Erlang.Tests.DistributionGroupLeaderHostChecks.Run);

Test(Erlang.Tests.RuntimeActorDispatchHostNames.Test, Erlang.Tests.RuntimeActorDispatchHostChecks.Run);
Test(Erlang.Tests.GarbageCollectionResultHostNames.Test, Erlang.Tests.GarbageCollectionResultHostChecks.Run);
Test(Erlang.Tests.TraceHookRollbackHostNames.Test, Erlang.Tests.TraceHookRollbackHostChecks.Run);

Test(Erlang.Tests.ProcessInfoDefaultHostNames.Test, Erlang.Tests.ProcessInfoDefaultHostChecks.Run);
Test(Erlang.Tests.ProcessInfoDefaultSubsetHostNames.Test, Erlang.Tests.ProcessInfoDefaultSubsetHostChecks.Run);

Test(Erlang.Tests.DistributionTraceHostNames.Test, Erlang.Tests.DistributionTraceHostChecks.Run);
Test(Erlang.Tests.DistributionSenderHostNames.Test, Erlang.Tests.DistributionSenderHostChecks.Run);
Test(Erlang.Tests.DistributionDeferredReasonHostNames.Test, Erlang.Tests.DistributionDeferredReasonHostChecks.Run);
Test(Erlang.Tests.DeferredOwnerDrainHostNames.Test, Erlang.Tests.DeferredOwnerDrainHostChecks.Run);
Test(Erlang.Tests.DistributionPayloadExitHostNames.Test, Erlang.Tests.DistributionPayloadExitHostChecks.Run);
Test(Erlang.Tests.DistributionTrappedTraceHostNames.Test, Erlang.Tests.DistributionTrappedTraceHostChecks.Run);
Test(Erlang.Tests.DistributionPayloadMonitorHostNames.Test, Erlang.Tests.DistributionPayloadMonitorHostChecks.Run);

Test(Erlang.Tests.DistributionCapacityHostNames.Test, Erlang.Tests.DistributionCapacityHostChecks.Run);

Test(
    Erlang.Tests.DistributionNativePayloadContractHostNames.Test,
    Erlang.Tests.DistributionNativePayloadContractHostChecks.Run
);

Test(Erlang.Tests.DistributionRegisteredInputHostNames.Test, Erlang.Tests.DistributionRegisteredInputHostChecks.Run);

Test(Erlang.Tests.DistributionStatisticsHostNames.Test, Erlang.Tests.DistributionStatisticsHostChecks.Run);

Test(Erlang.Tests.NifBeamOwnerHostNames.Test, Erlang.Tests.NifBeamOwnerHostChecks.Run);

Test(
    Erlang.Tests.AllocatorQueryMailboxHostNames.Test,
    Erlang.Tests.AllocatorQueryMailboxHostChecks.Run
);
Test(
    Erlang.Tests.NativePayloadProviderHostNames.Test,
    Erlang.Tests.NativePayloadProviderHostChecks.Run
);
Test(
    Erlang.Tests.SystemAllocatorPublicHostNames.Test,
    Erlang.Tests.SystemAllocatorPublicHostChecks.Run
);
Test(Erlang.Tests.ClrMemoryPublicHostNames.Test, Erlang.Tests.ClrMemoryPublicHostChecks.Run);

Test(Erlang.Tests.SpawnPriorityTestNames.AllReadyOrder, Erlang.Tests.SpawnPriorityAdmissionHostChecks.AllReadyPriorityOrder);
Test(
    Erlang.Tests.SpawnPriorityTestNames.EscapedRegisteredCallback,
    Erlang.Tests.SpawnPriorityGuestReentryHostChecks.EscapedCallbackMustReadmitOwner
);
Test(
    Erlang.Tests.SpawnPriorityTestNames.SynchronousTurnBorrow,
    Erlang.Tests.SpawnPrioritySynchronousBorrowHostChecks.SameTurnCallbackStaysSynchronous
);
Test(
    Erlang.Tests.SpawnPriorityTestNames.LowLogicalEpochs,
    Erlang.Tests.SpawnPriorityEpochHostChecks.LowPaysOncePerLogicalReduction
);
Test(
    Erlang.Tests.SpawnPriorityTestNames.WaitingHighCapacity,
    Erlang.Tests.SpawnPriorityWaitingHostChecks.WaitingHighDoesNotHoldCapacity
);
Test(
    Erlang.Tests.SpawnPriorityTestNames.QueuedPriorityChange,
    Erlang.Tests.SpawnPriorityQueuedChangeHostChecks.ReprioritizesAlreadyQueuedOwner
);
Test(
    Erlang.Tests.SpawnPriorityTestNames.HaltCleanup,
    Erlang.Tests.SpawnPriorityRetirementHostChecks.HaltSettlesEscapedCallbackWithoutInvokingTarget
);
Test(
    Erlang.Tests.SpawnPriorityTestNames.KilledOwnerRetirement,
    Erlang.Tests.SpawnPriorityRetirementHostChecks.KilledOwnerStillSettlesQueuedCleanup
);

foreach (var fixture in Erlang.Differential.SchedulerAffinityCases.Modules)
{
    Test(
        Erlang.Tests.SchedulerAffinityTestNames.CompiledPrefix + fixture.Name,
        async () =>
    {
        using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
        if (fixture.ExpectedStandardOutputHex is not null || fixture.ExpectedStandardErrorHex is not null)
        {
            var captured = await Erlang.Differential.CompiledModuleExecution.RunCaptured(artifact, fixture.CallerContext);
            Equal(captured.Result, fixture.Expected);
            Check(Erlang.Differential.CompiledStandardOutputContract.Matches(fixture, captured.StandardOutputHex, captured.StandardErrorHex));
        }
        else
            Equal(await Erlang.Differential.CompiledModuleExecution.Run(artifact, fixture.CallerContext), fixture.Expected);
        Check(artifact.CSharpSha256.Length == 64);
    }
    );
}

Test(
    Erlang.Tests.SchedulerAffinityTestNames.First,
    Erlang.Tests.SchedulerAffinityHostChecks.FirstInstructionAndMigration
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Park,
    Erlang.Tests.SchedulerAffinityHostChecks.OfflineChildParksAndResumes
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Init,
    Erlang.Tests.SchedulerInitializerHostChecks.MailboxReadmissionAndRecursiveConfigure
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Retire,
    Erlang.Tests.SchedulerInitializerHostChecks.RetirementWaitsForGuestThenInitializer
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Invalid,
    Erlang.Tests.SchedulerPublicApiHostChecks.InvalidValuesPreserveBindingAndOnlineCount
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Public,
    Erlang.Tests.SchedulerPublicApiHostChecks.QueriesAndAllLocalSpawnOptionForms
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Granted,
    Erlang.Tests.SchedulerOnlineConcurrentHostChecks.ParkedGrantedHeadDefersGuestKillUntilAdministrativeRetirement
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Active,
    Erlang.Tests.SchedulerOnlineConcurrentHostChecks.ActiveCancellationCannotAdvanceBeforeQuiescence
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Queued,
    Erlang.Tests.SchedulerOnlineConcurrentHostChecks.CanceledQueuedWaiterDoesNotStallSuccessor
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.GuestKill,
    Erlang.Tests.SchedulerOfflineGuestExitHostChecks.PendingKillIsAdmittedOnlyAfterLaneReturnsOnline
);
Test(
    Erlang.Tests.SchedulerAffinityTestNames.Offline,
    Erlang.Tests.SchedulerOfflineRetirementHostChecks.RunningParksPendingGuestKillAndRetirementSeparatelyJoinsCleanup
);

foreach (var fixture in Erlang.Differential.SizeErrorCases.Modules)
{
    Test(
        "oracle/compiled-module/" + fixture.Name,
        async () =>
    {
        using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
        if (fixture.ExpectedStandardOutputHex is not null || fixture.ExpectedStandardErrorHex is not null)
        {
            var captured = await Erlang.Differential.CompiledModuleExecution.RunCaptured(artifact, fixture.CallerContext);
            Equal(captured.Result, fixture.Expected);
            Check(Erlang.Differential.CompiledStandardOutputContract.Matches(fixture, captured.StandardOutputHex, captured.StandardErrorHex));
        }
        else
            Equal(await Erlang.Differential.CompiledModuleExecution.Run(artifact, fixture.CallerContext), fixture.Expected);
        Check(artifact.CSharpSha256.Length == 64);
    }
    );
}

Test(Erlang.Tests.BacktraceDepthHostNames.Test, Erlang.Tests.BacktraceDepthHostChecks.IndependentRuntimes);
Test(
    Erlang.Tests.SizeErrorHostTestNames.Identity,
    Erlang.Tests.CommonErrorCaptureHostChecks.RunIdentityRollbackAndAsyncSnapshot
);
Test(
    Erlang.Tests.SizeErrorHostTestNames.Fallback,
    Erlang.Tests.CommonErrorCaptureHostChecks.RunUncapturedFallbackAndPriorHost
);
Test(Erlang.Tests.SizeErrorHostTestNames.ConfiguredLoadNif, Erlang.Tests.SourceLoadNifHostChecks.RunCommonFailureCapture);

Test(Erlang.Tests.SizeErrorHostTestNames.Reinstall, Erlang.Tests.CommonPreloadedReinstallHostChecks.Run);
Test(Erlang.Tests.SizeErrorHostTestNames.Lambda, Erlang.Tests.CommonLambdaHandlerCaptureHostChecks.Run);

Test(Erlang.Tests.CleanupStealingHostNames.Test, Erlang.Tests.CleanupStealingHostChecks.Run);

Test(
    Erlang.Tests.NifNativeDefaultRouteHostNames.Test,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeDefaultRouteHostEntry.Run
);

Test(
    Erlang.Tests.NifNativeDefaultRouteHostNames.FilenameTest,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeDefaultRouteHostEntry.Filename
);

foreach (var fixture in Erlang.Tests.RuntimeIncrementCompiledCases.Modules)
{
    Test(
        Erlang.Tests.RuntimeIncrementCompiledCases.TestPrefix + fixture.Name,
        async () =>
    {
        using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
        if (fixture.ExpectedStandardOutputHex is not null || fixture.ExpectedStandardErrorHex is not null)
        {
            var captured = await Erlang.Differential.CompiledModuleExecution.RunCaptured(artifact, fixture.CallerContext);
            Equal(captured.Result, fixture.Expected);
            Check(Erlang.Differential.CompiledStandardOutputContract.Matches(fixture, captured.StandardOutputHex, captured.StandardErrorHex));
        }
        else
            Equal(await Erlang.Differential.CompiledModuleExecution.Run(artifact, fixture.CallerContext), fixture.Expected);
        Check(artifact.CSharpSha256.Length == 64);
    }
    );
}

Test(Erlang.Tests.RuntimeOptionRegistrationNames.FullSweep, Erlang.Tests.FullSweepHostChecks.Run);
Test(Erlang.Tests.RuntimeReductionHostNames.Test, Erlang.Tests.RuntimeReductionHostChecks.Run);
Test(Erlang.Tests.RemoteMonitorOptionNames.SpawnTest, Erlang.Tests.RemoteMonitorOptionHostChecks.RunSpawnOpt);
Test(Erlang.Tests.RemoteMonitorOptionNames.RequestTest, Erlang.Tests.RemoteMonitorOptionHostChecks.RunSpawnRequest);
Test(Erlang.Tests.RuntimeOptionRegistrationNames.Queued, Erlang.Tests.SchedulerStatisticsHostChecks.QueuedOwnerOnce);
Test(Erlang.Tests.RuntimeOptionRegistrationNames.Offline, Erlang.Tests.SchedulerStatisticsHostChecks.OfflineLaneOrder);
Test(Erlang.Tests.RuntimeOptionRegistrationNames.Initializer, Erlang.Tests.SchedulerStatisticsHostChecks.InitializerExcluded);

Test(Erlang.Tests.BusyPortMonitorHostNames.ConfigurationTest, Erlang.Tests.BusyPortMonitorHostChecks.Configuration);
Test(Erlang.Tests.BusyPortMonitorHostNames.SuspensionTest, Erlang.Tests.BusyPortMonitorHostChecks.Run);
Test(Erlang.Tests.BusyPortCancellationHostNames.Test, Erlang.Tests.BusyPortMonitorHostChecks.QueuedCommandCancellation);

Test(Erlang.Tests.PortWaitCancellationHostNames.Info, Erlang.Tests.BusyPortMonitorHostChecks.QueuedInfoCancellation);
Test(Erlang.Tests.PortWaitCancellationHostNames.InfoItem, Erlang.Tests.BusyPortMonitorHostChecks.QueuedInfoItemCancellation);
Test(Erlang.Tests.PortWaitCancellationHostNames.Close, Erlang.Tests.BusyPortMonitorHostChecks.QueuedCloseCancellation);

foreach (var fixture in Erlang.Tests.RuntimeIncrementCompiledCases.NativeHostModules)
{
    Test(
        Erlang.Tests.RuntimeIncrementCompiledCases.TestPrefix + fixture.Name,
        async () =>
    {
        using var artifact = Erlang.Differential.GeneratedModuleCompiler.Compile(fixture.Source);
        if (fixture.ExpectedStandardOutputHex is not null || fixture.ExpectedStandardErrorHex is not null)
        {
            var captured = await Erlang.Differential.CompiledModuleExecution.RunCaptured(artifact, fixture.CallerContext);
            Equal(captured.Result, fixture.Expected);
            Check(Erlang.Differential.CompiledStandardOutputContract.Matches(fixture, captured.StandardOutputHex, captured.StandardErrorHex));
        }
        else
            Equal(await Erlang.Differential.CompiledModuleExecution.Run(artifact, fixture.CallerContext), fixture.Expected);
        Check(artifact.CSharpSha256.Length == 64);
    }
    );
}

Test(Erlang.Tests.NifArrayCallbackHostNames.Empty, NifNativeBackend::Erlang.NifNativeBackend.NifNativeArrayHostEntry.Empty);
Test(Erlang.Tests.NifArrayCallbackHostNames.Order, NifNativeBackend::Erlang.NifNativeBackend.NifNativeArrayHostEntry.Order);
Test(Erlang.Tests.NifArrayCallbackHostNames.Limits, NifNativeBackend::Erlang.NifNativeBackend.NifNativeArrayHostEntry.Limits);
Test(
    Erlang.Tests.NifArrayCallbackHostNames.Containment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeArrayHostEntry.Containment
);
Test(Erlang.Tests.RemoteSpawnMetadataHostNames.ParserTest, Erlang.Tests.RemoteSpawnMetadataHostChecks.ParserMetadata);
Test(Erlang.Tests.RemoteSpawnMetadataHostNames.AsyncTest, Erlang.Tests.RemoteSpawnMetadataHostChecks.ConnectedAsync);
Test(Erlang.Tests.RemoteSpawnMetadataHostNames.PriorityTest, Erlang.Tests.RemoteSpawnMetadataHostChecks.PriorityDuplicates);
Test(
    "runtime/nif-load-reason-original-five-controls",
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLoadReasonHostEntry.Run
);

Test(
    Erlang.Tests.NifTypeCallbackHostNames.Classification,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeTypeHostEntry.Classification
);
Test(
    Erlang.Tests.NifTypeCallbackHostNames.EnvironmentIndependence,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeTypeHostEntry.EnvironmentIndependence
);
Test(
    Erlang.Tests.NifTypeCallbackHostNames.ListCells,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeTypeHostEntry.ListCells
);
Test(
    Erlang.Tests.NifTypeCallbackHostNames.Containment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeTypeHostEntry.Containment
);

Test(
    Erlang.Tests.NifNumericCallbackHostNames.IntegerGetters,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeNumericHostEntry.IntegerGetters
);

Test(
    Erlang.Tests.NifNumericCallbackHostNames.IntegerBuilders,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeNumericHostEntry.IntegerBuilders
);

Test(
    Erlang.Tests.NifNumericCallbackHostNames.Doubles,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeNumericHostEntry.Doubles
);

Test(
    Erlang.Tests.NifNumericCallbackHostNames.EnvironmentAndContainment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeNumericHostEntry.EnvironmentAndContainment
);

Test(
    Erlang.Tests.NifSequenceTextCallbackHostNames.TupleLifetime,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeSequenceTextHostEntry.TupleLifetime
);

Test(
    Erlang.Tests.NifSequenceTextCallbackHostNames.ListLengths,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeSequenceTextHostEntry.ListLengths
);

Test(
    Erlang.Tests.NifSequenceTextCallbackHostNames.Atoms,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeSequenceTextHostEntry.Atoms
);

Test(
    Erlang.Tests.NifSequenceTextCallbackHostNames.Strings,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeSequenceTextHostEntry.Strings
);

Test(
    Erlang.Tests.NifSequenceTextCallbackHostNames.InvocationOwnership,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeSequenceTextHostEntry.InvocationOwnership
);

Test(
    Erlang.Tests.NifSequenceTextCallbackHostNames.Containment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeSequenceTextHostEntry.Containment
);


Test(
    Erlang.Tests.NifLegacyStringCallbackHostNames.Latin1,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLegacyStringHostEntry.Latin1
);

Test(
    Erlang.Tests.NifLegacyStringCallbackHostNames.Utf8,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLegacyStringHostEntry.Utf8
);

Test(
    Erlang.Tests.NifLegacyStringCallbackHostNames.LongInputs,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLegacyStringHostEntry.LongInputs
);

Test(
    Erlang.Tests.NifLegacyStringCallbackHostNames.Termination,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLegacyStringHostEntry.Termination
);

Test(
    Erlang.Tests.NifLegacyStringCallbackHostNames.RejectedUtf8,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLegacyStringHostEntry.RejectedUtf8
);

Test(
    Erlang.Tests.NifLegacyStringCallbackHostNames.EnvironmentAndContainment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLegacyStringHostEntry.EnvironmentAndContainment
);


Test(
    Erlang.Tests.NifImmediateCallbackHostNames.Getter,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeImmediateHostEntry.Getter
);
Test(
    Erlang.Tests.NifImmediateCallbackHostNames.Ownership,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeImmediateHostEntry.Ownership
);
Test(
    Erlang.Tests.NifImmediateCallbackHostNames.Containment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeImmediateHostEntry.Containment
);


Test(
    Erlang.Tests.NifLinuxImmediateHostNames.Boundaries,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLinuxImmediateHostEntry.Boundaries
);
Test(
    Erlang.Tests.NifLinuxImmediateHostNames.Ownership,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLinuxImmediateHostEntry.Ownership
);
Test(
    Erlang.Tests.NifLinuxImmediateHostNames.Containment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeLinuxImmediateHostEntry.Containment
);


Test(
    Erlang.Tests.NifBridgeFixedHostNames.Layout,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBridgeFixedHostEntry.Layout
);
Test(
    Erlang.Tests.NifBridgeFixedHostNames.LongWidth,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBridgeFixedHostEntry.LongWidth
);
Test(
    Erlang.Tests.NifBridgeFixedHostNames.CompoundTerms,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBridgeFixedHostEntry.CompoundTerms
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.Inspection,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.Inspection
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.Iolists,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.Iolists
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.AllocationAcrossCalls,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.AllocationAcrossCalls
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.ReadonlyReallocation,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.ReadonlyReallocation
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.DeferredComposites,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.DeferredComposites
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.SubviewLifetimes,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.SubviewLifetimes
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.ExceptionalCleanup,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.ExceptionalCleanup
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.ExceptionGraph,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.ExceptionGraph
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.DeepGraph,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.DeepGraph
);

Test(
    Erlang.Tests.NifBinaryCallbackHostNames.Containment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeBinaryHostEntry.Containment
);


Test(
    Erlang.Tests.NifComparisonReferenceHostNames.Ordering,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeComparisonHostEntry.Ordering
);

Test(
    Erlang.Tests.NifComparisonReferenceHostNames.ExactEquality,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeComparisonHostEntry.ExactEquality
);

Test(
    Erlang.Tests.NifComparisonReferenceHostNames.PendingReads,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeComparisonHostEntry.PendingReads
);

Test(
    Erlang.Tests.NifComparisonReferenceHostNames.ReferenceUniqueness,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeComparisonHostEntry.ReferenceUniqueness
);

Test(
    Erlang.Tests.NifComparisonReferenceHostNames.OwnershipBoundaries,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeComparisonHostEntry.OwnershipBoundaries
);

Test(
    Erlang.Tests.NifCompletionHostNames.MapSizeShapeAndOutput,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapSizeHostEntry.ShapeAndOutput
);

Test(
    Erlang.Tests.NifCompletionHostNames.MapSizePureEnvironment,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapSizeHostEntry.PureEnvironment
);

Test(
    Erlang.Tests.NifCompletionHostNames.MapSizeOwnerLifetime,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapSizeHostEntry.OwnerLifetime
);

Test(
    Erlang.Tests.NifCompletionHostNames.ReferenceEnvironmentLifetime,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeReferenceEnvironmentHostEntry.Lifetime
);

Test(
    Erlang.Tests.NifCompletionHostNames.ReferenceForeignThreads,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeReferenceEnvironmentHostEntry.ForeignThreads
);

Test(
    Erlang.Tests.NifCompletionHostNames.ReferenceCounterBoundaries,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeReferenceCounterHostEntry.Boundaries
);

Test(
    Erlang.Tests.NifCompletionHostNames.ReferenceCounterConcurrentBoundaries,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeReferenceCounterHostEntry.ConcurrentBoundaries
);

IReadOnlyList<(string Name, Func<Task> Body)> selectedTests;
Test(
    Erlang.Tests.NifCompletionHostNames.CapsuleMetadata,
    NifNativeBackend::Erlang.NifNativeBackend.NifNativeElfCapsuleHostEntry.Metadata
);

try
{
    Test(Erlang.Tests.ProplistStackHostNames.Test, Erlang.Tests.ProplistStackHostChecks.Run);
    tests = tests.OrderBy(test => Erlang.Tests.ProplistGetValueMfaContracts.OwnsTest(test.Name)).ToList();
    Test(Erlang.Tests.ProplistFamilyTestNames.DirectTest, Erlang.Tests.ProplistFamilyHostChecks.Direct);
    Test(Erlang.Tests.ProplistFamilyTestNames.CompiledTest, Erlang.Tests.ProplistFamilyHostChecks.Compiled);
    Test(Erlang.Tests.ProplistFamilyTestNames.AstTest, Erlang.Tests.ProplistContinuationHostChecks.Ast);
    Test(Erlang.Tests.ProplistFamilyTestNames.SafetyTest, Erlang.Tests.ProplistContinuationHostChecks.Runtime);
    Test(Erlang.Tests.ProplistFamilyTestNames.ProperTest, Erlang.Tests.ProplistFamilyHistoryHostChecks.Proper);
    Test(Erlang.Tests.ProplistFamilyTestNames.UnfoldTest, Erlang.Tests.ProplistFamilyHistoryHostChecks.Unfold);
    Test(Erlang.Tests.ProplistFamilyTestNames.CompactTest, Erlang.Tests.ProplistFamilyHistoryHostChecks.Compact);
    Test(Erlang.Tests.ProplistFamilyTestNames.DeleteTest, Erlang.Tests.ProplistFamilyHistoryHostChecks.Delete);
    Test(Erlang.Tests.ProplistFamilyTestNames.CancellationTest, Erlang.Tests.ProplistFamilyHistoryHostChecks.Cancellation);
    Test(Erlang.Tests.ProplistIteratorTestNames.Test, Erlang.Tests.ProplistIteratorHostChecks.Run);
    Test(Erlang.Tests.ProplistHashHostNames.HashTest, Erlang.Tests.ProplistInternalHashHostChecks.NativeVectors);
    Test(Erlang.Tests.ProplistHashHostNames.CursorTest, Erlang.Tests.ProplistRawHamtCursorHostChecks.IntegerAndBinaryStates);
    Test(Erlang.Tests.ProplistOwnCursorNames.DomainsTest, Erlang.Tests.ProplistOwnCursorDomainHostChecks.Run);
    Test(Erlang.Tests.ProplistOwnCursorNames.LifetimesTest, Erlang.Tests.ProplistOwnCursorLifetimeHostChecks.Run);
    Test(Erlang.Tests.ProplistOwnCursorNames.CollisionsTest, Erlang.Tests.ProplistOwnCursorCollisionHostChecks.Run);
    Test(Erlang.Tests.ProplistOwnCursorNames.CompilerTest, Erlang.Tests.ProplistOwnCursorCompilerHostChecks.Run);
    Test(Erlang.Tests.ProplistOwnCursorNames.AliasTest, Erlang.Tests.ProplistOwnCursorAliasHostChecks.CountSensitive);

    Test(Erlang.Tests.QueueEndsHostNames.Representation, Erlang.Tests.QueueEndsHostChecks.Representation);
    Test(Erlang.Tests.QueueEndsHostNames.Improper, Erlang.Tests.QueueEndsHostChecks.ImproperLists);
    Test(Erlang.Tests.QueueEndsHostNames.Compiled, Erlang.Tests.QueueEndsHostChecks.Compiled);
    Test(Erlang.Tests.QueueHigherHostNames.CallbackOrder, Erlang.Tests.QueueHigherHostChecks.CallbackOrderAndImproperEvaluation);
    Test(Erlang.Tests.QueueHigherCooperativeHostNames.Test, Erlang.Tests.QueueHigherCooperativeHostChecks.Run);
    Test(Erlang.Tests.QueueCallbackRegressionNames.Test, Erlang.Tests.QueueCallbackRegressionHostChecks.Run);
    Test(Erlang.Tests.StaticApplyRegressionNames.Test, Erlang.Tests.StaticApplyRegressionHostChecks.Run);
    Test(Erlang.Tests.BinarySearchHostNames.ResourceTest, Erlang.Tests.BinarySearchResourceHostChecks.Run);
    Test(Erlang.Tests.BinarySearchHostNames.OptionsTest, Erlang.Tests.BinarySearchOptionsHostChecks.Run);
    Test(Erlang.Tests.BinarySearchHostNames.CooperativeTest, Erlang.Tests.BinarySearchCooperativeHostChecks.Run);
    Test(Erlang.Tests.BinarySearchHostNames.CompiledTest, Erlang.Tests.BinarySearchCompiledHostChecks.Run);
    Test(Erlang.Tests.BinaryWrappersHostNames.CallbackEffects, Erlang.Tests.BinaryWrappersHostChecks.CallbackEffectsAndOptions);
    Test(Erlang.Tests.BinaryPrimitiveHostNames.Cooperative, Erlang.Tests.BinaryPrimitiveCooperativeHostChecks.Run);
    Test(
        Erlang.Tests.BinaryWrapperBackingHostNames.Test,
        Erlang.Tests.BinaryWrapperBackingHostChecks.JoinBackingAndCallbackContext
    );
    Test(Erlang.Tests.BinaryPrimitiveHostNames.Storage, Erlang.Tests.BinaryPrimitiveHostChecks.Storage);
    Test(Erlang.Tests.BinaryInsertHostNames.Test, Erlang.Tests.BinaryInsertHostChecks.Run);
    Test(Erlang.Tests.QueueMemberHostNames.Test, Erlang.Tests.QueueMemberHostChecks.Run);
    Test(Erlang.Tests.QueueCoreHostNames.Representation, Erlang.Tests.QueueCoreHostChecks.Representation);
    Test(Erlang.Tests.QueueCoreHostNames.ImproperLists, Erlang.Tests.QueueCoreHostChecks.ImproperLists);
    Test(Erlang.Tests.QueueCoreHostNames.Ordering, Erlang.Tests.QueueCoreHostChecks.Ordering);

    Test(Erlang.Tests.QueueCooperativeHostNames.Test, Erlang.Tests.QueueCooperativeHostChecks.Run);

    Test(Erlang.Tests.SizeSourceLocationHostNames.TestName, Erlang.Tests.SizeSourceLocationHostChecks.Run);

    Test(Erlang.Tests.DynamicApplyHostNames.Test, Erlang.Tests.DynamicApplyHostChecks.Run);

    Test(Erlang.Tests.SourceBifContinuationHostNames.Test, Erlang.Tests.SourceBifContinuationHostChecks.Run);

    Test(Erlang.Tests.OrdsetCoreHostNames.Test, Erlang.Tests.OrdsetCoreHostChecks.Run);

    Test(Erlang.Tests.BoundedBifHostNames.TestName, Erlang.Tests.BoundedBifHostChecks.Run);

    Test(Erlang.Tests.OrdsetFullHostNames.Test, Erlang.Tests.OrdsetFullHostChecks.Run);

    Test(Erlang.Tests.NumericSourceContractHostNames.Test, Erlang.Tests.NumericSourceContractHostChecks.Run);

    Test(Erlang.Tests.NumericSmallParseHostNames.Test, Erlang.Tests.NumericSmallParseHostChecks.Run);

    Test(Erlang.Tests.MapFunKeyOrderHostNames.TestName, Erlang.Tests.MapFunKeyOrderHostChecks.Run);

    Test(Erlang.Tests.OrdsetListsDispatchHostNames.Test, Erlang.Tests.OrdsetListsDispatchHostChecks.Run);
    Test(Erlang.Tests.OrdsetFoldFilterHostNames.LateTest, Erlang.Tests.OrdsetFoldFilterHostChecks.LateFailures);
    Test(Erlang.Tests.OrdsetFoldFilterHostNames.IdentityTest, Erlang.Tests.OrdsetFoldFilterHostChecks.IdentityAsync);
    Test(Erlang.Tests.OrdsetFoldFilterHostNames.DeepTest, Erlang.Tests.OrdsetFoldFilterHostChecks.Deep);
    Test(Erlang.Tests.OrdsetFoldFilterHostNames.CancellationTest, Erlang.Tests.OrdsetFoldFilterHostChecks.Cancellation);
    Test(Erlang.Tests.NumericMapEqualityHostNames.TestName, Erlang.Tests.NumericMapEqualityHostChecks.Run);
    Test(Erlang.Tests.HamtNumericEqualityHostNames.Test, Erlang.Tests.HamtNumericEqualityHostChecks.Run);
    Test(
        Erlang.Tests.LocalFunctionCreatorHostNames.CanonicalizationTest,
        Erlang.Tests.LocalFunctionCreatorHostChecks.Canonicalization
    );
    Test(Erlang.Tests.LocalFunctionCreatorHostNames.ValidationTest, Erlang.Tests.LocalFunctionCreatorHostChecks.Validation);

    Test(Erlang.Tests.OperatorSourceLocationHostNames.Test, Erlang.Tests.OperatorSourceLocationHostChecks.Run);

    Test(Erlang.Tests.ListUniqueSourceHostNames.TestName, Erlang.Tests.ListUniqueSourceHostChecks.Run);
    Test(Erlang.Tests.ListUniqueSortSourceHostNames.IdentityTest, Erlang.Tests.ListUniqueSortSourceHostChecks.Identity);
    Test(Erlang.Tests.ListUniqueSortSourceHostNames.MalformedTest, Erlang.Tests.ListUniqueSortSourceHostChecks.Malformed);
    Test(Erlang.Tests.ListUniqueSortSourceHostNames.DeepTest, Erlang.Tests.ListUniqueSortSourceHostChecks.Deep);
    Test(Erlang.Tests.ListUniqueSortSourceHostNames.ReverseTest, Erlang.Tests.ListUniqueSortSourceHostChecks.DynamicReverse);

    foreach (string scenario in Erlang.Tests.BeamTryHostCaseNames.Ordered)
    {
        string selected = scenario;
        Test(
            Erlang.Tests.BeamTryHostNames.Prefix + selected,
            () => Erlang.Tests.BeamTryHostChecks.Run(selected)
        );
    }

    Test(Erlang.Tests.BeamTryLifecycleHostNames.NormalTest, Erlang.Tests.BeamTryLifecycleHostChecks.Normal);

    Test(Erlang.Tests.BeamTryLifecycleHostNames.ErlangTest, Erlang.Tests.BeamTryLifecycleHostChecks.ErlangException);

    Test(Erlang.Tests.BeamTryLifecycleHostNames.ClrTest, Erlang.Tests.BeamTryLifecycleHostChecks.ClrFailure);

    Test(Erlang.Tests.BeamTryLifecycleHostNames.CancellationTest, Erlang.Tests.BeamTryLifecycleHostChecks.Cancellation);

    Test(Erlang.Tests.BeamTryExtensionHostNames.AsyncTest, Erlang.Tests.BeamTryExtensionHostChecks.AsyncErlangFailure);
    Test(Erlang.Tests.BeamTryExtensionHostNames.TailErrorsTest, Erlang.Tests.BeamTryExtensionHostChecks.TailErrors);
    Test(Erlang.Tests.BeamTryExtensionHostNames.TailReturnTest, Erlang.Tests.BeamTryExtensionHostChecks.TailReturn);

    Test(Erlang.Tests.HaltElectionHostNames.Test, Erlang.Tests.HaltElectionHostChecks.Run);

    Test(Erlang.Tests.BeamTupleSelectHostNames.Branches, Erlang.Tests.BeamTupleSelectHostChecks.Branches);
    Test(Erlang.Tests.BeamTupleSelectHostNames.Rejections, Erlang.Tests.BeamTupleSelectHostChecks.Rejections);
    Test(Erlang.Tests.BeamTupleSelectHostNames.Receive, Erlang.Tests.BeamTupleSelectHostChecks.Receive);

    Test(Erlang.Tests.BeamApplyHostNames.Calls, Erlang.Tests.BeamApplyHostChecks.Calls);
    Test(Erlang.Tests.BeamApplyHostNames.Errors, Erlang.Tests.BeamApplyHostChecks.Errors);

    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CasePositionAndClear,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.PositionAndClear
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CaseRemovalAndExpiration,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.RemovalAndExpiration
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CaseInvalidAndForeign,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.InvalidAndForeign
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CasePassExpiration,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.PassExpiration
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CaseDuplicateBindingRefused,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.DuplicateBindingRefused
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CaseControlOnlyWait,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.ControlOnlyWait
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CasePriorityRefusalBeforeUse,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.PriorityRefusalBeforeUse
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CasePriorityRefusalAfterAwait,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.PriorityRefusalAfterAwait
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CaseDeferredPhysicalOrder,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.DeferredPhysicalOrder
    );
    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CaseDeferredPriorityRefused,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.DeferredPriorityRefused
    );

    Test(
        Erlang.Tests.MailboxReceiveMarkerHostNames.CasePriorityRefusalAfterExecutionWait,
        Erlang.Tests.MailboxReceiveMarkerHostChecks.PriorityRefusalAfterExecutionWait
    );

    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseSlotLayout, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.SlotLayout);
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseStorageIdentity,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.StorageIdentity
    );
    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseReadRefusals, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.ReadRefusals);
    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseLiveRoots, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.LiveRoots);
    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseBindTypes, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.BindTypes);
    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseOperandRoles, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.OperandRoles);
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseConsumerLeaks,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.ConsumerLeaks
    );
    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseJoinKinds, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.JoinKinds);
    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseCallClobber, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.CallClobber);
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseTimeoutClobber,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.TimeoutClobber
    );
    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseTrimFlow, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.TrimFlow);
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseHandlerOverlap,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.HandlerOverlap
    );
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseReceiveSelected,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.ReceiveSelected
    );
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseReceiveTimedOut,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.ReceiveTimedOut
    );
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseRejectedScratch,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.RejectedScratch
    );
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseReceiveTryRefused,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.ReceiveTryRefused
    );
    Test(Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseGcRoots, Erlang.Tests.BeamReceiveMarkerTypeHostChecks.GcRoots);
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseMissingReserve,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.MissingReserve
    );
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseClearBeforeRejection,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.ClearBeforeRejection
    );
    Test(
        Erlang.Tests.BeamReceiveMarkerTypeHostNames.CaseClearThenExtract,
        Erlang.Tests.BeamReceiveMarkerTypeHostChecks.ClearThenExtract
    );

    Test(Erlang.Tests.BeamOrdsetsCoreHostNames.Case, Erlang.Tests.BeamOrdsetsCoreHostChecks.Run);

    Test(Erlang.Tests.BeamLiteralExportHostNames.Pointer, Erlang.Tests.BeamLiteralExportHostChecks.Pointer);
    Test(Erlang.Tests.BeamLiteralExportHostNames.Interning, Erlang.Tests.BeamLiteralExportHostChecks.Interning);
    Test(Erlang.Tests.BeamLiteralExportHostNames.General, Erlang.Tests.BeamLiteralExportHostChecks.GeneralPolicy);
    Test(Erlang.Tests.BeamLiteralExportHostNames.Malformed, Erlang.Tests.BeamLiteralExportHostChecks.Malformed);
    Test(Erlang.Tests.BeamLiteralExportHostNames.Local, Erlang.Tests.BeamLiteralExportHostChecks.LocalStillRequiresFactory);

    Test(Erlang.Tests.BeamBinaryHostNames.CaseStorage, Erlang.Tests.BeamBinaryHostChecks.Storage);
    Test(Erlang.Tests.BeamBinaryHostNames.CaseStartAlias, Erlang.Tests.BeamBinaryHostChecks.StartAlias);
    Test(Erlang.Tests.BeamBinaryHostNames.CaseCursorConsumption, Erlang.Tests.BeamBinaryHostChecks.CursorConsumption);
    Test(Erlang.Tests.BeamBinaryHostNames.CaseTailAlias, Erlang.Tests.BeamBinaryHostChecks.TailAlias);
    Test(Erlang.Tests.BeamBinaryHostNames.CaseIntegerTruncation, Erlang.Tests.BeamBinaryHostChecks.IntegerTruncation);
    Test(Erlang.Tests.BeamBinaryHostNames.CaseBinaryAll, Erlang.Tests.BeamBinaryHostChecks.BinaryAll);
    Test(Erlang.Tests.BeamBinaryHostNames.CaseStringRange, Erlang.Tests.BeamBinaryHostChecks.StringRange);
    Test(Erlang.Tests.BeamBinaryHostNames.CaseMalformedShapes, Erlang.Tests.BeamBinaryHostChecks.MalformedShapes);
    Test(Erlang.Tests.BeamBinaryHostNames.CaseContextFlow, Erlang.Tests.BeamBinaryHostChecks.ContextFlow);

    foreach (var group in Erlang.Tests.BeamOrdsetsFamilyHostNames.Groups)
        Test(group.Case, () => Erlang.Tests.BeamOrdsetsFamilyHostChecks.Run(group.Group));

    Test("serialization/flatmap382/opposing-unique-native-golden", Erlang.Tests.FlatMapEtfHostChecks.OpposingUniqueNativeGolden);
    Test("serialization/flatmap382/exact-numeric-bindings", Erlang.Tests.FlatMapEtfHostChecks.ExactNumericBindings);
    Test("serialization/flatmap382/equal-old-unique-distinct-code", Erlang.Tests.FlatMapEtfHostChecks.EqualOldUniqueDistinctCode);
    Test("serialization/flatmap382/deterministic-baseline", Erlang.Tests.FlatMapEtfHostChecks.DeterministicBaseline);
    Test("serialization/flatmap382/cardinality-boundaries", Erlang.Tests.FlatMapEtfHostChecks.CardinalityBoundaries);
    Test("serialization/flatmap382/nested-map-value", Erlang.Tests.FlatMapEtfHostChecks.NestedMapValue);
    Test("serialization/flatmap382/scalar-encoding-unchanged", Erlang.Tests.FlatMapEtfHostChecks.ScalarEncodingUnchanged);
    Test("serialization/flatmap382/construction-node-snapshot", Erlang.Tests.FlatMapEtfHostChecks.ConstructionNodeSnapshot);

    Test(
        Erlang.Tests.BeamBinaryErrorPriorityHostNames.IntegerSizeFirst,
        Erlang.Tests.BeamBinaryErrorPriorityHostChecks.IntegerSizeFirst
    );
    Test(
        Erlang.Tests.BeamBinaryErrorPriorityHostNames.BinarySizeFirst,
        Erlang.Tests.BeamBinaryErrorPriorityHostChecks.BinarySizeFirst
    );
    Test(
        Erlang.Tests.BeamBinaryErrorPriorityHostNames.ZeroWidthChecksType,
        Erlang.Tests.BeamBinaryErrorPriorityHostChecks.ZeroWidthChecksType
    );
    Test(
        Erlang.Tests.BeamBinaryErrorPriorityHostNames.LaterSizeBeforeEarlierValue,
        Erlang.Tests.BeamBinaryErrorPriorityHostChecks.LaterSizeBeforeEarlierValue
    );
    Test(
        Erlang.Tests.BeamBinaryErrorPriorityHostNames.BinaryAllTypeStaysFirstPass,
        Erlang.Tests.BeamBinaryErrorPriorityHostChecks.BinaryAllTypeStaysFirstPass
    );
    Test(
        Erlang.Tests.BeamBinaryErrorPriorityHostNames.BinaryAllUnitAfterLaterSize,
        Erlang.Tests.BeamBinaryErrorPriorityHostChecks.BinaryAllUnitAfterLaterSize
    );

    Test(
        "runtime/beam/artifact-inspector383/retained-original-callbacks",
        Erlang.Tests.ArtifactInspectorHostChecks.RetainedOriginalCallbacks
    );
    Test(
        "runtime/beam/artifact-inspector383/no-runtime-table-or-invocation-leak",
        Erlang.Tests.ArtifactInspectorHostChecks.InspectionDoesNotPopulateRuntime
    );
    Test(
        "runtime/beam/artifact-inspector383/default-and-runtime-policies-distinct",
        Erlang.Tests.ArtifactInspectorHostChecks.DefaultAndRuntimePoliciesRemainDistinct
    );
    Test(
        "runtime/beam/artifact-inspector383/malformed-export-refuses",
        Erlang.Tests.ArtifactInspectorHostChecks.MalformedExportRemainsInvalid
    );
    Test(
        "runtime/beam/artifact-inspector383/on-load-metadata-no-execution",
        Erlang.Tests.ArtifactInspectorHostChecks.OnLoadIsMetadataNotExecution
    );
    Test(
        "runtime/beam/artifact-inspector383/new-fun-literal-still-refuses",
        Erlang.Tests.ArtifactInspectorHostChecks.NewFunLiteralStillRefuses
    );

    Test(Erlang.Tests.BeamRaiseTraceHostNames.Test, Erlang.Tests.BeamRaiseTraceHostChecks.Run);

    Test(Erlang.Tests.BeamProtectedReceiveProtocolNames.AdmissionTest, Erlang.Tests.BeamProtectedReceiveProtocolChecks.Admission);
    Test(
        Erlang.Tests.BeamProtectedReceiveProtocolNames.IdentityTest,
        Erlang.Tests.BeamProtectedReceiveProtocolChecks.CloneAndTrim
    );
    Test(Erlang.Tests.BeamProtectedReceiveHostNames.Test, Erlang.Tests.BeamProtectedReceiveHostChecks.Run);
    Test(Erlang.Tests.BeamReceiveExtendedHostAssets.LazyTest, Erlang.Tests.BeamReceiveExtendedHostAssets.Lazy);
    Test(Erlang.Tests.BeamReceiveExtendedHostAssets.AsmTest, Erlang.Tests.BeamReceiveExtendedHostAssets.Asm);
    Test(Erlang.Tests.BeamAwaitEntryHostNames.RemoteTest, Erlang.Tests.BeamAwaitEntryHostChecks.Remote);
    Test(Erlang.Tests.BeamAwaitEntryHostNames.NativeTest, Erlang.Tests.BeamAwaitEntryHostChecks.Native);
    Test(Erlang.Tests.BeamDelayProtocolNames.AdmissionTest, Erlang.Tests.BeamDelayProtocolChecks.Admission);
    Test(Erlang.Tests.BeamDelayProtocolNames.OriginalTest, Erlang.Tests.BeamAwaitEntryHostChecks.Delay);
    Test(Erlang.Tests.BeamLifecycleHostNames.DeepTest, Erlang.Tests.BeamLifecycleHostChecks.Deep);
    Test(Erlang.Tests.BeamLifecycleHostNames.ImageTest, Erlang.Tests.BeamLifecycleHostChecks.Image);
    Test(Erlang.Tests.BeamLifecycleHostNames.PhysicalTest, Erlang.Tests.BeamDeepFrameHostChecks.Run);
    Test(Erlang.Tests.BeamClosureProtocolNames.AllRaise, Erlang.Tests.BeamClosureProtocolHostChecks.AllRaise);
    Test(Erlang.Tests.BeamClosureProtocolNames.OneReturns, Erlang.Tests.BeamClosureProtocolHostChecks.OneReturns);
    Test(Erlang.Tests.BeamClosureProtocolNames.DefaultReturns, Erlang.Tests.BeamClosureProtocolHostChecks.DefaultReturns);
    Test(Erlang.Tests.BeamClosureProtocolNames.EntryCycle, Erlang.Tests.BeamClosureProtocolHostChecks.EntryCycle);
    Test(Erlang.Tests.BeamClosureProtocolNames.LocalCycle, Erlang.Tests.BeamClosureProtocolHostChecks.LocalCycle);
    Test(Erlang.Tests.BeamClosureProtocolNames.CaughtReturn, Erlang.Tests.BeamClosureProtocolHostChecks.CaughtReturn);
    Test(Erlang.Tests.BeamClosureProtocolNames.SharedDag, Erlang.Tests.BeamClosureProtocolHostChecks.SharedDag);
    Test(Erlang.Tests.BeamClosureProtocolNames.YCommit, Erlang.Tests.BeamClosureProtocolHostChecks.YCommit);
    Test(Erlang.Tests.BeamClosureProtocolNames.YReject, Erlang.Tests.BeamClosureProtocolHostChecks.YReject);
    Test(Erlang.Tests.BeamClosureProtocolNames.HandlerOverwrite, Erlang.Tests.BeamClosureProtocolHostChecks.HandlerOverwrite);

    Test(Erlang.Tests.BeamLifecycleHostNames.AlternatingTest, Erlang.Tests.BeamLifecycleHostChecks.Alternating);
    Test(Erlang.Tests.LibraryManifestHostNames.Beam, Erlang.Tests.LibraryManifestHostChecks.Beam);
    Test(Erlang.Tests.LibraryManifestHostNames.Mixed, Erlang.Tests.LibraryManifestHostChecks.Mixed);
    Test(Erlang.Tests.LibraryManifestHostNames.Invalid, Erlang.Tests.LibraryManifestHostChecks.Invalid);
    Test(Erlang.Tests.LibraryManifestHostNames.NoFallback, Erlang.Tests.LibraryManifestHostChecks.NoFallback);
    Test(Erlang.Tests.LibraryManifestOriginalNames.Test, Erlang.Tests.LibraryManifestOriginalHostChecks.Run);
    Test(Erlang.Tests.LibraryManifestOriginalNames.FrameTest, Erlang.Tests.LibraryManifestOriginalHostChecks.StoredFunBounds);

    Test(Erlang.Tests.ContractReaudit487Names.Test, Erlang.Tests.ContractReaudit487Checks.Run);
    Test(Erlang.Tests.NumericContract487Names.Test, Erlang.Tests.NumericContract487Checks.Run);
    Test(Erlang.Tests.TupleContract488Names.Test, Erlang.Tests.TupleContract488Checks.Run);
    foreach (var group in Erlang.Tests.TupleMutation489Names.Groups)
        Test(Erlang.Tests.TupleMutation489Names.TestPrefix + group.Group, () => Erlang.Tests.TupleMutation489Checks.Run(group));
    foreach (var group in Erlang.Tests.OperatorContract490Names.Groups)
        Test(Erlang.Tests.OperatorContract490Names.TestPrefix + group.Group, () => Erlang.Tests.OperatorContract490Checks.Run(group));
    foreach (var group in Erlang.Tests.ArithmeticContract491Names.Groups)
        Test(Erlang.Tests.ArithmeticContract491Names.TestPrefix + group.Group, () => Erlang.Tests.ArithmeticContract491Checks.Run(group));
    foreach (var group in Erlang.Tests.AtomContract492Names.Groups)
        Test(Erlang.Tests.AtomContract492Names.TestPrefix + group.Group, () => Erlang.Tests.AtomContract492Checks.Run(group));
    foreach (var group in Erlang.Tests.IntegerContract493Names.Groups)
        Test(Erlang.Tests.IntegerContract493Names.TestPrefix + group.Group, () => Erlang.Tests.IntegerContract493Checks.Run(group));

    Test(Erlang.Tests.AtomVm494Names.Isolation, Erlang.Tests.AtomVm494Checks.Isolation);
    Test(Erlang.Tests.AtomVm494Names.Import, Erlang.Tests.AtomVm494Checks.Import);
    Test(Erlang.Tests.AtomVm494Names.BinaryIdentity, Erlang.Tests.AtomVm494Checks.BinaryIdentity);
    Test(Erlang.Tests.AtomVm494Names.Exhaustion, Erlang.Tests.AtomVm494Checks.Exhaustion);
    Test(Erlang.Tests.AtomVm494Names.BinaryExhaustion, Erlang.Tests.AtomVm494Checks.BinaryExhaustion);
    Test(Erlang.Tests.AtomVm494Names.EncodedExhaustion, Erlang.Tests.AtomVm494Checks.EncodedExhaustion);
    Test(Erlang.Tests.AtomVm494Names.SafeDecode, Erlang.Tests.AtomVm494Checks.SafeDecode);

    foreach (var group in Erlang.Tests.MapContract495Names.Groups)
        Test(Erlang.Tests.MapContract495Names.TestPrefix + group.Group, () => Erlang.Tests.MapContract495Checks.Run(group));

    foreach (var group in Erlang.Tests.RecordContract496Names.Groups)
        Test(Erlang.Tests.RecordContract496Names.TestPrefix + group.Group, () => Erlang.Tests.RecordContract496Checks.Run(group));

    foreach (var group in Erlang.Tests.BuiltinContract497Names.Groups)
        Test(Erlang.Tests.BuiltinContract497Names.TestPrefix + group.Group, () => Erlang.Tests.BuiltinContract497Checks.Run(group));

    Test(
        Erlang.Tests.ProcessLiveness499Names.Test,
        () => Erlang.Tests.ProcessLiveness499Checks.Run(("", "process_liveness499", 65, 325))
    );
    Test(Erlang.Tests.ProcessLiveness499Names.OrderingTest, Erlang.Tests.ProcessLiveness499OrderingChecks.Run);
    Test(Erlang.Tests.ProcessRetirement500Names.Test, Erlang.Tests.ProcessRetirement500Checks.Run);
    Test(Erlang.Tests.LogicalRetirement502Names.Test, Erlang.Tests.LogicalRetirement502Checks.Run);
    Test(Erlang.Tests.LogicalAllocatorRetirement502Names.Test, Erlang.Tests.LogicalAllocatorRetirement502Checks.Run);
    Test(Erlang.Tests.LogicalStorageRetirement502Names.Test, Erlang.Tests.LogicalStorageRetirement502Checks.Run);
    foreach (var group in Erlang.Tests.NodeNative501Names.Groups)
        Test(Erlang.Tests.NodeNative501Names.TestPrefix + group.Family, () => Erlang.Tests.NodeNative501Checks.Run(group));
    Test(
        Erlang.Tests.NewConnection501CapacityNames.DefaultTest,
        Erlang.Tests.NewConnection501CapacityChecks.DefaultRetainedCapacityAndCleanup
    );
    Test(
        Erlang.Tests.NewConnection501CapacityNames.ExplicitTest,
        Erlang.Tests.NewConnection501CapacityChecks.ExplicitQuotaAndIsolation
    );
    Test(
        Erlang.Tests.NifMapCreation501TestNames.Creation,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapCreation501HostEntry.CreationAndOwnership
    );
    Test(
        Erlang.Tests.NifMapCreation501TestNames.Invalid,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapCreation501HostEntry.InvalidEnvironments
    );
    Test(
        Erlang.Tests.NifMapCreation501TestNames.Threads,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapCreation501HostEntry.ThreadAccess
    );
    Test(
        Erlang.Tests.NifMapValue503TestNames.Shape,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapValue503HostEntry.ShapeAndOutput
    );
    Test(
        Erlang.Tests.NifMapValue503TestNames.Owners,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapValue503HostEntry.OwnersAndHints
    );
    Test(
        Erlang.Tests.NifMapValue503TestNames.Pending,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapValue503HostEntry.PendingKeys
    );
    Test(
        Erlang.Tests.NifMapValue503TestNames.Invalid,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapValue503HostEntry.InvalidOwnersAndThreads
    );
    Test(
        Erlang.Tests.NifMapReview504TestNames.Functions,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapReview504HostEntry.SourceFunUniquePreconditions
    );
    Test(
        Erlang.Tests.NifMapReview504TestNames.Static,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapReview504HostEntry.StaticLookupAllocations
    );
    Test(
        Erlang.Tests.NifMapReview504TestNames.Misses,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapReview504HostEntry.IndexedStaticLookupMisses
    );
    Test(
        Erlang.Tests.NifMapReview504TestNames.Collision,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapReview504HostEntry.CollisionCmpUsesActualFirstRole
    );
    Test(
        Erlang.Tests.NifMapMaterialized505TestNames.Flat,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapMaterialized505HostEntry.FlatPhysicalLayoutAndEquality
    );
    Test(
        Erlang.Tests.NifMapMaterialized505TestNames.Saved,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapMaterialized505HostEntry.NativeBufferReleaseAndSavedIndex
    );
    Test(
        Erlang.Tests.NifMapMaterialized505TestNames.Deep,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapMaterialized505HostEntry.DeepNativeMapEquality
    );
    Test(
        Erlang.Tests.NifMapMaterialized505TestNames.Owners,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapMaterialized505HostEntry.CrossRuntimeAndClosedOwner
    );
    Test(
        Erlang.Tests.NifNativeFlatDuplicate505TestNames.Materialized,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeFlatDuplicate505HostEntry.MaterializedSameReferenceRows
    );
    Test(
        Erlang.Tests.NifNativeFlatDuplicate505TestNames.Factory,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeFlatDuplicate505HostEntry.FactoryRowsAndPeerEquality
    );
    Test(Erlang.Tests.OwnedTrampoline506Names.CarriersTest, Erlang.Tests.OwnedTrampoline506Checks.AllCarriers);
    Test(Erlang.Tests.OwnedTrampoline506Names.PlateauTest, Erlang.Tests.OwnedTrampoline506Checks.TailPlateau);
    Test(Erlang.Tests.OwnedTrampoline506Names.CancelTest, Erlang.Tests.OwnedTrampoline506Checks.CancelHandoff);
    Test(Erlang.Tests.OwnedTrampoline506Names.CancelFunctionTest, Erlang.Tests.OwnedTrampoline506Checks.CancelFunctionTransfer);
    Test(Erlang.Tests.OwnedGuest506Names.Test, Erlang.Tests.OwnedGuest506Checks.Payload);
    Test(Erlang.Tests.OwnedGuest506Names.MessageTest, Erlang.Tests.OwnedGuest506Checks.SourceMessageRetirement);
    Test(Erlang.Tests.OwnedGuestOriginal506Names.Test, Erlang.Tests.OwnedGuestOriginal506Checks.Run);
    Test(Erlang.Tests.OwnedShutdown506Names.Test, Erlang.Tests.OwnedShutdown506Checks.NestedForeignInvocation);
    Test(Erlang.Tests.TrustedGuest507Names.SharingTest, Erlang.Tests.TrustedGuest507Checks.Sharing);
    Test(Erlang.Tests.TrustedGuest507Names.AllocationTest, Erlang.Tests.TrustedGuest507Checks.AllocationAndSlots);
    Test(Erlang.Tests.TrustedGuest507Names.OverrideTest, Erlang.Tests.TrustedGuest507Checks.OpaqueOverride);
    Test(Erlang.Tests.HybridBoundary507Names.Test, Erlang.Tests.HybridBoundary507Checks.MutablePatterns);
    Test(Erlang.Tests.HybridKeyScope507Names.Test, Erlang.Tests.HybridKeyScope507Checks.Parity);
    Test(Erlang.Tests.WindowsVariadic526Names.Test, Erlang.Tests.WindowsVariadic526Checks.RequiredSlots);
    Test(Erlang.Tests.BeamOrdinaryBinaryReceive525Names.Test, Erlang.Tests.BeamOrdinaryBinaryReceive525Checks.OriginalWholeImage);
    Test(Erlang.Tests.DirtyIoConcurrency522Names.HeldTest, Erlang.Tests.DirtyIoConcurrency522Checks.HeldIoLeavesRealControlLane);
    Test(Erlang.Tests.DirtyIoConcurrency522Names.CloseTest, Erlang.Tests.DirtyIoConcurrency522Checks.CloseJoinsEveryIoWorker);
    Test(Erlang.Tests.NifPhysicalCustody521Names.ReleaseTest, Erlang.Tests.NifPhysicalCustody521Checks.DeferredPhysicalRelease);
    Test(
        Erlang.Tests.NifPhysicalCustody521Names.CleanupTest,
        Erlang.Tests.NifPhysicalCustody521Checks.CleanupFailureStillReleasesImage
    );
    Test(
        Erlang.Tests.NifPhysicalCustody521Names.RetentionTest,
        Erlang.Tests.NifPhysicalCustody521Checks.FinalBookkeepingFailureRetainsImage
    );
    Test(
        Erlang.Tests.DynamicApplyBoundary520Names.SourceHashTest,
        Erlang.Tests.DynamicApplySourceAdmission520Checks.DeclaredLineEndings
    );
    Test(Erlang.Tests.NativeMetadataWitness527Names.Test, Erlang.Tests.NativeMetadataWitness527Checks.Run);
    Test(Erlang.Tests.NativeMetadataImage527Names.Test, Erlang.Tests.NativeMetadataImage527Checks.Original);
    Test(Erlang.Tests.NativeMetadataImage527Names.LinelessTest, Erlang.Tests.NativeMetadataImage527Checks.Lineless);
    Test(
        Erlang.Tests.BoxedInteger559TestNames.TransferCase,
        Erlang.Tests.BoxedInteger559Checks.ImmediateChildrenImportAndAdoption
    );
    Test(Erlang.Tests.Allocation567TestNames.ScalarCase, Erlang.Tests.Allocation567Checks.ScalarGenerationsAndReclaim);
    Test(Erlang.Tests.Allocation567TestNames.MapCase, Erlang.Tests.Allocation567Checks.SharedMapCustody);
    Test(Erlang.Tests.Allocation567TestNames.RollbackCase, Erlang.Tests.Allocation567Checks.FailedPublicationRollback);
    Test(Erlang.Tests.Allocation567TestNames.RetirementCase, Erlang.Tests.Allocation567Checks.IsolationAndRetirement);
    Test(Erlang.Tests.OrdinaryReference562TestNames.PhysicalCase, Erlang.Tests.OrdinaryReference562Checks.PhysicalAndRetirement);
    Test(
        Erlang.Tests.OrdinaryReference562TestNames.ImportCase,
        Erlang.Tests.OrdinaryReference562Checks.AdoptionImportAndRollback
    );
    Test(
        Erlang.Tests.OrdinaryReference562TestNames.ProvenanceCase,
        Erlang.Tests.OrdinaryReference562Checks.ProvenanceIdentityAndForeign
    );
    Test(
        Erlang.Tests.BeamLoaderMapKeys565Names.CustodyCase,
        Erlang.Tests.BeamLoaderMapKeys565Checks.HiddenKeyCustodyRefusals
    );
    Test(
        Erlang.Tests.BeamLoaderMapKeys565Names.PredicateCase,
        Erlang.Tests.BeamLoaderMapKeys565Checks.SyntacticPredicateAndImages
    );
    Test(Erlang.Tests.BeamLoaderMapKeys565Names.PhysicalCase, Erlang.Tests.BeamLoaderMapKeys565Checks.PhysicalCustodyAndExport);
    Test(Erlang.Tests.BoxedInteger559TestNames.PhysicalCase, Erlang.Tests.BoxedInteger559Checks.PayloadAndCollection);
    Test(Erlang.Tests.BoxedInteger559TestNames.AdoptionCase, Erlang.Tests.BoxedInteger559Checks.ImmediateAliasLiteralAndLimits);
    Test(Erlang.Tests.BoxedInteger559TestNames.LifetimeCase, Erlang.Tests.BoxedInteger559Checks.ForeignAndHeldRetirement);
    Test(Erlang.Tests.BoxedFloat556Names.PhysicalCase, Erlang.Tests.BoxedFloat556Checks.PhysicalGetterAndGc);
    Test(Erlang.Tests.BoxedFloat556Names.AdoptionCase, Erlang.Tests.BoxedFloat556Checks.AdoptionAndRootOnly);
    Test(Erlang.Tests.BoxedFloat556Names.LifetimeCase, Erlang.Tests.BoxedFloat556Checks.ForeignAndHeldRetirement);
    Test(Erlang.Tests.BeamListCurrentRoot550Names.MatrixTest, Erlang.Tests.BeamListCurrentRoot550Checks.Matrix);
    Test(Erlang.Tests.BeamListCurrentRoot550Names.CustodyTest, Erlang.Tests.BeamListCurrentRoot550Checks.Custody);
    Test(Erlang.Tests.BeamListCurrentRoot550Names.FailureTest, Erlang.Tests.BeamListCurrentRoot550Checks.ClosedYStore);
    Test(
        Erlang.Tests.BeamConstructedCommit548TestNames.MatrixTest,
        Erlang.Tests.BeamConstructedCommit548Checks.PublicationMatrix
    );
    Test(
        Erlang.Tests.BeamConstructedCommit548TestNames.RollbackTest,
        Erlang.Tests.BeamConstructedCommit548Checks.ClosedPhysicalStoreRollsBack
    );
    Test(
        Erlang.Tests.BeamConstructedCommit548TestNames.GuardTest,
        Erlang.Tests.BeamConstructedCommit548Checks.StaleCandidateRejected
    );
    Test(Erlang.Tests.MapPayload517Names.FlatTest, Erlang.Tests.MapPayload517Checks.FlatOccurrenceCustody);
    Test(Erlang.Tests.MapPayload517Names.HamtTest, Erlang.Tests.MapPayload517Checks.HamtNodeCustody);
    Test(Erlang.Tests.MapPayload517Names.RawTest, Erlang.Tests.MapPayload517Checks.RawInteriorRetainsOnlySubtree);
    Test(Erlang.Tests.MapPayload517Names.RollbackTest, Erlang.Tests.MapPayload517Checks.PostAllocationHeaderFaultRollsBack);
    Test(Erlang.Tests.MapPayload517Names.BoundaryTest, Erlang.Tests.MapPayload517Checks.FlatHamtBoundaryRoutes);
    Test(Erlang.Tests.MapRouting517TestNames.SourceTest, Erlang.Tests.MapRouting517Checks.SourceProducer);
    Test(Erlang.Tests.OwnedWords526Names.Test, Erlang.Tests.OwnedWords526Checks.PhysicalHeadersAndRetirement);
    Test(Erlang.Tests.MapSharingSize528Names.Test, Erlang.Tests.MapSharingSize528Checks.Run);
    Test(Erlang.Tests.NativeResourceLogical529Names.UnlinkTest, Erlang.Tests.NativeResourceLogical529Checks.UnlinkLivePayload);
    Test(Erlang.Tests.NativeResourceLogical529Names.CallbackTest, Erlang.Tests.NativeResourceLogical529Checks.HeldCallbackPin);
    Test(Erlang.Tests.NativeHostScope531Names.Test, Erlang.Tests.NativeHostScope531Checks.ParallelExports);
    Test(Erlang.Tests.NativeGuest535Names.ReadersTest, Erlang.Tests.NativeGuest535Checks.ReadersAndPublicRefusal);
    Test(Erlang.Tests.NativeGuest535Names.ReregisterTest, Erlang.Tests.NativeGuest535Checks.PublicReregisterStaysOpaque);
    Test(Erlang.Tests.NativeGuest535Names.FakeTest, Erlang.Tests.NativeGuest535Checks.ManagedNifProviderStaysOpaque);
    Test(
        Erlang.Tests.NativeResource536TestNames.NormalTest,
        Erlang.Tests.NativeResource536Checks.GuestGcUsesActualNormalAuxiliaryOnce
    );
    Test(
        Erlang.Tests.NativeResource536TestNames.HeldTest,
        Erlang.Tests.NativeResource536Checks.HeldNormalDestructorKeepsImageAndClosePhysical
    );
    Test(
        Erlang.Tests.NativeResource536TestNames.ForeignTest,
        Erlang.Tests.NativeResource536Checks.NonGcRouteIsNotSilentlyReclassified
    );
    Test(
        Erlang.Tests.NativeCons539CaseNames.Shape,
        NifNativeBackend::Erlang.NifNativeBackend.NifConsCell539HostEntry.ShapeAndAbi
    );
    Test(
        Erlang.Tests.NativeCons539CaseNames.Pending,
        NifNativeBackend::Erlang.NifNativeBackend.NifConsCell539HostEntry.PendingHeadAndImproperTail
    );
    Test(
        Erlang.Tests.NativeCons539CaseNames.Owners,
        NifNativeBackend::Erlang.NifNativeBackend.NifConsCell539HostEntry.InvalidOwnersAndThreads
    );
    Test(
        Erlang.Tests.NativeCons539CaseNames.Resource,
        Erlang.Tests.NativeCons539ResourceChecks.CopiedCellKeepsResourceUntilLastEnvironmentClear
    );
    Test(
        Erlang.Tests.NativeResourceScope528Names.ViewAndGuestCollection,
        Erlang.Tests.NativeResource524Checks.ViewAndGuestCollection
    );
    Test(
        Erlang.Tests.NativeResourceScope528Names.PhysicalDestructorPinsImage,
        Erlang.Tests.NativeResource524Checks.PhysicalDestructorPinsImage
    );
    Test(
        Erlang.Tests.NativeResourceScope528Names.PhysicalMagicHeaderAndIdentity,
        Erlang.Tests.NativeResource527Checks.PhysicalMagicHeaderAndIdentity
    );
    Test(
        Erlang.Tests.NativeResourceScope528Names.CreateDuplicateOrderAndRollback,
        Erlang.Tests.NativeResource527Checks.CreateDuplicateOrderAndRollback
    );
    Test(
        Erlang.Tests.NativeResourceScope528Names.TakeoverUsesNewPhysicalDestructor,
        Erlang.Tests.NativeResource527Checks.TakeoverUsesNewPhysicalDestructor
    );
    Test(
        Erlang.Tests.NativeResourceScope528Names.ArenaSurvivesImageCloseAndSupportsForeignFree,
        Erlang.Tests.NativeResourceArena528Checks.ArenaSurvivesImageCloseAndSupportsForeignFree
    );
    Test(
        Erlang.Tests.NativeResourceScope528Names.NewDestructorFreesOldImageChildAllocation,
        Erlang.Tests.NativeResourceArena528Checks.NewDestructorFreesOldImageChildAllocation
    );
    Test(
        Erlang.Tests.NativeResourceAllocation570Names.Test,
        Erlang.Tests.NativeResourceAllocation570Checks.SeparateNativeBytesAndGuestCells
    );
    Test(Erlang.Tests.AllocatorFirstFit519Names.MixedTest, Erlang.Tests.AllocatorFirstFit519Checks.MixedFirstFit);
    Test(Erlang.Tests.AllocatorFirstFit519Names.FragmentedTest, Erlang.Tests.AllocatorFirstFit519Checks.FragmentedLinearScan);
    Test(Erlang.Tests.AllocatorFirstFit519Names.HeapTest, Erlang.Tests.AllocatorFirstFit519Checks.HeapLinearScan);
    Test(
        Erlang.Tests.AllocatorFirstFit519Names.ReuseTest,
        Erlang.Tests.AllocatorFirstFit519Checks.CollectionAndRollbackReuse
    );
    Test(Erlang.Tests.PrivateSelfGc519Names.BifTest, Erlang.Tests.PrivateSelfGc519Checks.RealBifReclaims);
    Test(Erlang.Tests.PrivateSelfGc519Names.LiveTest, Erlang.Tests.PrivateSelfGc519Checks.LiveHiddenAndBinary);
    Test(Erlang.Tests.PrivateSelfGc519Names.OrdinaryTest, Erlang.Tests.PrivateSelfGc519Checks.OrdinaryNoHeap);
    Test(Erlang.Tests.PrivateSelfGc519Names.Test, Erlang.Tests.PrivateSelfGc519Checks.ExplicitCollection);
    Test(Erlang.Tests.NifAppendOnly516Names.ScalingTest, Erlang.Tests.NifAppendOnly516Checks.StaticTailScaling);
    Test(Erlang.Tests.NifAppendOnly516Names.HiddenTest, Erlang.Tests.NifAppendOnly516Checks.HiddenAndNewComposite);
    Test(Erlang.Tests.NifAppendOnly516Names.UnknownTest, Erlang.Tests.NifAppendOnly516Checks.UnknownRemainsConservative);
    Test(Erlang.Tests.NifEnvironmentRoots516Names.BoundTest, Erlang.Tests.NifEnvironmentRoots516Checks.BoundPendingAndCopy);
    Test(Erlang.Tests.NifEnvironmentRoots516Names.RetirementTest, Erlang.Tests.NifEnvironmentRoots516Checks.HeldRetirement);
    Test(Erlang.Tests.NifEnvironmentRoots516Names.OrdinaryTest, Erlang.Tests.NifEnvironmentRoots516Checks.OrdinaryProfile);
    Test(Erlang.Tests.LinearConstruction514Names.ScalingTest, Erlang.Tests.LinearConstruction514Checks.Scaling);
    Test(Erlang.Tests.LinearConstruction514Names.EmptyTest, Erlang.Tests.LinearConstruction514Checks.NestedEmptyTuples);
    Test(Erlang.Tests.LinearConstruction514Names.HiddenTest, Erlang.Tests.LinearConstruction514Checks.HiddenPhysicalMap);
    Test(Erlang.Tests.LinearConstruction514Names.RollbackTest, Erlang.Tests.LinearConstruction514Checks.RejectAndRollback);
    Test(Erlang.Tests.ResourcePolicy508TestNames.TableTest, Erlang.Tests.ResourcePolicy508Checks.Table);
    Test(Erlang.Tests.ResourcePolicy508TestNames.DefaultsTest, Erlang.Tests.ResourcePolicy508Checks.DefaultsAndConversion);
    Test(Erlang.Tests.ResourcePolicy508TestNames.GenerationsTest, Erlang.Tests.ResourcePolicy508Checks.Generations);
    Test(Erlang.Tests.NativeMapOwned510Names.FlatDuplicateTest, Erlang.Tests.NativeMapOwned510Checks.FlatDuplicateOccurrences);
    Test(Erlang.Tests.NativeMapOwned510Names.HiddenHamtTest, Erlang.Tests.NativeMapOwned510Checks.HiddenHamtRootsAndTopology);
    Test(Erlang.Tests.NativeMapOwned510Names.ForeignDomainTest, Erlang.Tests.NativeMapOwned510Checks.ForeignDomainFailsClosed);
    Test(Erlang.Tests.SourceLiveness511Names.Test, Erlang.Tests.SourceLiveness511Checks.Run);
    Test(Erlang.Tests.BinaryOwned512Names.SharingTest, Erlang.Tests.BinaryOwned512Checks.SharingAndEscapes);
    Test(Erlang.Tests.BinaryOwned512Names.SourceTest, Erlang.Tests.BinaryOwned512Checks.SourceAndRollback);
    Test(Erlang.Tests.BinaryOwned512Names.NativeTest, Erlang.Tests.BinaryOwned512Checks.NativeLifetime);
    Test(Erlang.Tests.NativeMapComposition510Names.Test, Erlang.Tests.NativeMapComposition510Checks.HiddenMajorBinaryAndClose);
    Test(Erlang.Tests.SourceBinaryComposition511Names.Test, Erlang.Tests.SourceBinaryComposition511Checks.RetirementAndHiddenMap);
    Test(
        Erlang.Tests.TrustedCapabilities507Names.Test,
        Erlang.Tests.TrustedCapabilities507Checks.FactoryInheritedAndPublicOverride
    );
    Test(
        Erlang.Tests.TrustedCapabilities507Names.EmptyTest,
        Erlang.Tests.TrustedCapabilities507Checks.RepeatedGuestOnlyPreparation
    );
    Test(Erlang.Tests.BeamGuestReturn507Names.Test, Erlang.Tests.BeamGuestReturn507Checks.AliasAndHost);
    Test(Erlang.Tests.BeamOwnedRoots507Names.ReplayTest, Erlang.Tests.BeamOwnedRoots507Checks.OriginalTryReplay);
    Test(Erlang.Tests.BeamOwnedRoots507Names.AliasTest, Erlang.Tests.BeamOwnedRoots507Checks.AliasAndTrim);
    Test(Erlang.Tests.BeamOwnedRoots507Names.CancelTest, Erlang.Tests.BeamOwnedRoots507Checks.CancelAtTuple);
    Test(Erlang.Tests.BeamOwnedReceive507Names.ProtectedTest, Erlang.Tests.BeamOwnedReceive507Checks.OriginalProtectedReceive);
    Test(Erlang.Tests.BeamOwnedReceive507Names.BeforeCallbackTest, Erlang.Tests.BeamOwnedReceive507Checks.CancelBeforeCallback);
    Test(Erlang.Tests.BeamOwnedReceive507Names.AfterSelectionTest, Erlang.Tests.BeamOwnedReceive507Checks.CancelAfterSelection);
    Test(
        Erlang.Tests.BeamOwnedReceive507Names.SourceRetirementTest,
        Erlang.Tests.BeamOwnedReceive507Checks.MixedSourceSenderRetirement
    );
    Test(Erlang.Tests.BeamOwnedReceive507Names.MetadataTest, Erlang.Tests.BeamOwnedReceive507Checks.MetadataRootOnly);
    Test(Erlang.Tests.BeamOwnedReceive507Names.RollbackTest, Erlang.Tests.BeamOwnedReceive507Checks.RemovedCandidateRollback);
    Test(Erlang.Tests.BeamOwnedReceive507Names.PendingTest, Erlang.Tests.BeamOwnedReceive507Checks.PendingAdmissionCancellation);
    Test(Erlang.Tests.BeamOwnedReceive507Names.TraceFailureTest, Erlang.Tests.BeamOwnedReceive507Checks.TraceFailureBothRoutes);
    Test(Erlang.Tests.OwnedGuest506Names.HeldTest, Erlang.Tests.OwnedGuest506Checks.AwaitAndClosure);
    Test(Erlang.Tests.OwnedGuest506Names.CapturedTaskTest, Erlang.Tests.OwnedGuest506Checks.CapturedTask);
    Test(Erlang.Tests.OwnedGuest506Names.DictionaryTest, Erlang.Tests.OwnedGuest506Checks.ConcurrentDictionary);
    Test(Erlang.Tests.OwnedGuest506Names.DescriptorTest, Erlang.Tests.OwnedGuest506Checks.DescriptorLockOrder);
    Test(Erlang.Tests.OwnedGuest506Names.ErrorsTest, Erlang.Tests.OwnedGuest506Checks.Errors);
    Test(Erlang.Tests.OwnedGuest506Names.PhysicalTest, Erlang.Tests.OwnedGuest506Checks.PhysicalRetirement);
    Test(Erlang.Tests.OwnedGuest506Names.LifecycleInfoTest, Erlang.Tests.OwnedGuest506Checks.LifecycleInfo);
    Test(Erlang.Tests.ProcessOwnedWord504Names.RootsTest, Erlang.Tests.ProcessOwnedWord504Checks.Roots);
    Test(Erlang.Tests.ProcessOwnedTerm504Names.GraphTest, Erlang.Tests.ProcessOwnedTerm504Checks.Graph);
    Test(Erlang.Tests.ProcessOwnedTerm504Names.RetirementTest, Erlang.Tests.ProcessOwnedTerm504Checks.Retirement);
    Test(
        Erlang.Tests.RuntimeQueueStatistics504TestNames.Held,
        NifNativeBackend::Erlang.NifNativeBackend.RuntimeQueueStatistics504HostEntry.HeldDirtyQueues
    );
    Test(Erlang.Tests.ProcessOwnedWord504Names.FramesTest, Erlang.Tests.ProcessOwnedWord504Checks.Frames);
    Test(Erlang.Tests.ProcessOwnedWord504Names.OperationTest, Erlang.Tests.ProcessOwnedWord504Checks.ActiveOperation);
    Test(Erlang.Tests.BeamOwnedWord504Names.ReplayTest, Erlang.Tests.BeamOwnedWord504Checks.Replay);
    Test(Erlang.Tests.BeamOwnedWord504Names.HeldTest, Erlang.Tests.BeamOwnedWord504Checks.Held);
    Test(
        Erlang.Tests.NifMapFamily504TestNames.Mutation,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapFamily504HostEntry.MutationAndOutput
    );
    Test(
        Erlang.Tests.NifMapFamily504TestNames.Arrays,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapFamily504HostEntry.ArraysAndAliases
    );
    Test(
        Erlang.Tests.NifMapFamily504TestNames.Pending,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapFamily504HostEntry.PendingValuesAndReplacement
    );
    Test(
        Erlang.Tests.NifMapFamily504TestNames.Copy,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapFamily504HostEntry.CopyBoundariesAndOwnerLifetime
    );
    Test(
        Erlang.Tests.NifMapFamily504TestNames.Invalid,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapFamily504HostEntry.InvalidOwnersAndThreads
    );
    Test(Erlang.Tests.ProcessOwnedWord504Names.RetirementTest, Erlang.Tests.ProcessOwnedWord504Checks.Retirement);
    Test(
        Erlang.Tests.NifMapIterator504TestNames.Shapes,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapIterator504HostEntry.ShapesAndBoundaries
    );
    Test(
        Erlang.Tests.NifMapIterator504TestNames.Lifetime,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapIterator504HostEntry.HintsAndLifetime
    );
    Test(
        Erlang.Tests.NifMapIterator504TestNames.Pending,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapIterator504HostEntry.PendingPairs
    );
    Test(
        Erlang.Tests.NifMapIterator504TestNames.Containment,
        NifNativeBackend::Erlang.NifNativeBackend.NifNativeMapIterator504HostEntry.ContainmentAndThreads
    );
    Test(Erlang.Tests.ReviewFollowup498Names.ParserTest, Erlang.Tests.ReviewFollowup498Checks.ParserHostArguments);
    Test(Erlang.Tests.ReviewFollowup498Names.MapTest, Erlang.Tests.ReviewFollowup498Checks.ImportedFlatMaps);

    Test(
        Erlang.Tests.CountersCustody596Names.GuestTest,
        Erlang.Tests.CountersCustody596Checks.GuestRetirementIgnoresStaleClrEntry
    );
    Test(Erlang.Tests.CountersCustody596Names.PinTest, Erlang.Tests.CountersCustody596Checks.ActivePinOutlivesLastGuestRoot);
    Test(Erlang.Tests.CountersCustody596Names.HostTest, Erlang.Tests.CountersCustody596Checks.HostCustodyIndependentOfGuest);
    Test(Erlang.Tests.CountersMagic596Names.PhysicalTest, Erlang.Tests.CountersMagic596Checks.PhysicalHeaders);
    Test(Erlang.Tests.CountersMagic596Names.GenerationsTest, Erlang.Tests.CountersMagic596Checks.Generations);
    Test(Erlang.Tests.CountersMagic596Names.ForeignTest, Erlang.Tests.CountersMagic596Checks.ForeignBeforeStale);
    Test(Erlang.Tests.CountersStore596Names.SendCase, Erlang.Tests.CountersStore596Checks.QueueHandoff);
    Test(Erlang.Tests.CountersStore596Names.EtsCase, Erlang.Tests.CountersStore596Checks.EtsHandoff);
    Test(Erlang.Tests.CountersStore596Names.PersistentCase, Erlang.Tests.CountersStore596Checks.PersistentHandoff);
    Test(
        Erlang.Tests.CountersDictionary596Names.GetterCase,
        Erlang.Tests.CountersDictionary596Checks.GetterSurvivesEraseAndCollection
    );
    Test(
        Erlang.Tests.CountersDictionary596Names.SnapshotCase,
        Erlang.Tests.CountersDictionary596Checks.SnapshotSurvivesEraseAndCollection
    );
    Test(Erlang.Tests.CountersNative596Names.Case, Erlang.Tests.CountersNative596Checks.RealNativeFamily);
    Test(Erlang.Tests.CountersCapability596Names.Case, Erlang.Tests.CountersCapability596Checks.ExactPublicationAuthority);
    Test(
        Erlang.Tests.CountersExportRollback596Names.RollbackCase,
        Erlang.Tests.CountersExportRollback596Checks.LateFailureReleasesActualBacking
    );
    Test(
        Erlang.Tests.CountersExportRollback596Names.CommitCase,
        Erlang.Tests.CountersExportRollback596Checks.SuccessfulCopyTransfersToWrappers
    );
    Test(Erlang.Tests.CountersPrefix591Names.PrefixCase, Erlang.Tests.CountersPrefix591Checks.ActualPrefix);
    Test(Erlang.Tests.CountersPrefix591Names.GeometryCase, Erlang.Tests.CountersPrefix591Checks.ActualLaneGeometry);
    Test(Erlang.Tests.InitializerClose615Names.ClosedCase, Erlang.Tests.InitializerClose615Checks.ClosedBeforeCancellation);
    Test(Erlang.Tests.InitializerClose615Names.LiveCase, Erlang.Tests.InitializerClose615Checks.LiveCancellationRemainsVisible);
    Test(Erlang.Tests.CountersPublic611Names.InfoKeyedCase, Erlang.Tests.CountersPublic611Checks.InfoKeyedHost);
    Test(Erlang.Tests.CountersPublic611Names.ComponentCase, Erlang.Tests.CountersPublic611Checks.ComponentHost);
    Test(Erlang.Tests.CountersPublic611Names.CallbackCase, Erlang.Tests.CountersPublic611Checks.CallbackHost);
    Test(Erlang.Tests.CountersPublic611Names.ReturnedCase, Erlang.Tests.CountersPublic611Checks.ReturnedHost);
    Test(Erlang.Tests.CountersPublic611Names.SnapshotCase, Erlang.Tests.CountersPublic611Checks.SnapshotHost);
    Test(Erlang.Tests.CountersPublic611Names.InfoSingleCase, Erlang.Tests.CountersPublic611Checks.InfoSingleHost);
    Test(Erlang.Tests.CountersPublic611Names.InfoFullCase, Erlang.Tests.CountersPublic611Checks.InfoFullHost);
    Test(Erlang.Tests.CountersPublic611Names.InfoListCase, Erlang.Tests.CountersPublic611Checks.InfoListHost);
    Test(Erlang.Tests.BinarySearchAuthority621Names.Test, Erlang.Tests.BinarySearchAuthority621Checks.Run);
    Test(Erlang.Tests.TimerCallback539Names.Cancel, Erlang.Tests.TimerCallback539Checks.FireClaimBeforeCancel);
    Test(Erlang.Tests.TimerCallback539Names.Close, Erlang.Tests.TimerCallback539Checks.VmCloseDuringPublication);
    Test(Erlang.Tests.DictionaryKeys538Names.NativeCase, Erlang.Tests.DictionaryKeys538Checks.Native);
    Test(Erlang.Tests.DictionaryKeys538Names.AtomicsCase, Erlang.Tests.DictionaryKeys538Checks.Atomics);
    Test(Erlang.Tests.DictionaryKeys538Names.CountersCase, Erlang.Tests.DictionaryKeys538Checks.Counters);
    Test(Erlang.Tests.DictionaryKeys538Names.RegexCase, Erlang.Tests.DictionaryKeys538Checks.Regex);
    Test(Erlang.Tests.DictionaryKeys538Names.BinaryCase, Erlang.Tests.DictionaryKeys538Checks.Binary);
    Test(Erlang.Tests.DictionaryProjection635Names.Test, Erlang.Tests.DictionaryProjection635Checks.Scaling);
    // Opt-in component registrations; not active until owner compiles/reviews.
    Test(Erlang.Tests.MatchSaved634Names.ProtocolCase, Erlang.Tests.MatchSaved634Checks.Protocol);
    Test(Erlang.Tests.MatchSaved634Names.MutationCase, Erlang.Tests.MatchSaved634Checks.MutationAndCopy);
    Test(Erlang.Tests.MatchSaved634Names.SourceCase, Erlang.Tests.MatchSaved634Checks.SourceReleased);
    Test(Erlang.Tests.MatchSaved634Names.RollbackCase, Erlang.Tests.MatchSaved634Checks.RollbackAndCancellation);
    Test(Erlang.Tests.MatchSaved634Names.DisposeCase, Erlang.Tests.MatchSaved634Checks.DisposeWaitsReader);
    Test(Erlang.Tests.MatchSaved634Names.HaltCase, Erlang.Tests.MatchSaved634Checks.HaltWaitsReader);
    Test(Erlang.Tests.MatchSaved634Names.HeaderCase, Erlang.Tests.MatchSaved634Checks.HeaderRejection);
    Test(Erlang.Tests.DictionaryOriginal631Names.Test, Erlang.Tests.DictionaryOriginal631Checks.Run);
    Test(Erlang.Tests.DictionaryKeys605Names.NativeCase, Erlang.Tests.DictionaryKeys605Checks.NativeKeys);
    Test(Erlang.Tests.DictionaryKeys605Names.ExitCase, Erlang.Tests.DictionaryKeys605Checks.NativeOwnerExit);
    Test(Erlang.Tests.DictionaryKeys605Names.OpaqueCase, Erlang.Tests.DictionaryKeys605Checks.OpaqueReplacement);
    Test(Erlang.Tests.DictionaryKeys605Names.AtomicsCase, Erlang.Tests.DictionaryKeys605Checks.AtomicsKeys);
    Test(Erlang.Tests.DictionaryKeys627Names.CountersCase, Erlang.Tests.DictionaryKeys627Checks.Counters);
    Test(Erlang.Tests.DictionaryKeys627Names.RegexCase, Erlang.Tests.DictionaryKeys627Checks.Regex);
    Test(Erlang.Tests.DictionaryKeys627Names.BinaryCase, Erlang.Tests.DictionaryKeys627Checks.Binary);
    foreach (string family in new[]
    {
        Erlang.Tests.TimerPayload628Names.NativeFamily,
        Erlang.Tests.TimerPayload628Names.AtomicsFamily,
        Erlang.Tests.TimerPayload628Names.CountersFamily,
        Erlang.Tests.TimerPayload628Names.RegexFamily,
        Erlang.Tests.TimerPayload628Names.BinaryFamily
    })
    {
        Test(
            Erlang.Tests.TimerPayload628Names.FireCase + "/" + family,
            () => Erlang.Tests.TimerPayload628Checks.Run(Erlang.Tests.TimerPayload628Mode.Fire, family)
        );
        Test(
            Erlang.Tests.TimerPayload628Names.CancelCase + "/" + family,
            () => Erlang.Tests.TimerPayload628Checks.Run(Erlang.Tests.TimerPayload628Mode.Cancel, family)
        );
        Test(
            Erlang.Tests.TimerPayload628Names.ExitCase + "/" + family,
            () => Erlang.Tests.TimerPayload628Checks.Run(Erlang.Tests.TimerPayload628Mode.OwnerExit, family)
        );
        Test(
            Erlang.Tests.TimerPayload628Names.VmCase + "/" + family,
            () => Erlang.Tests.TimerPayload628Checks.Run(Erlang.Tests.TimerPayload628Mode.VmClose, family)
        );
    }
    Test(Erlang.Tests.TimerPayload628Names.OpaqueCase, Erlang.Tests.TimerPayload628Checks.OpaqueAlias);
    Test(Erlang.Tests.BinaryHalt629Names.Test, Erlang.Tests.BinaryHalt629Checks.Run);
    Test(Erlang.Tests.BinaryArena598Names.PhysicalCase, Erlang.Tests.BinaryArena598Checks.Physical);
    Test(Erlang.Tests.BinaryArena598Names.ActiveCase, Erlang.Tests.BinaryArena598Checks.Active);
    Test(Erlang.Tests.BinaryArena598Names.HostCase, Erlang.Tests.BinaryArena598Checks.HostReservation);
    Test(Erlang.Tests.BinaryStore536Names.BinaryEtsCase, Erlang.Tests.BinaryStore536Checks.BinaryEts);
    Test(Erlang.Tests.BinaryStore536Names.BinaryPersistentCase, Erlang.Tests.BinaryStore536Checks.BinaryPersistent);
    Test(Erlang.Tests.BinaryArena598Names.AcRootCase, Erlang.Tests.BinaryArena598Checks.AcRootMetadata);
    Test(Erlang.Tests.ProcessLiveness626Names.TerminalCase, Erlang.Tests.ProcessLiveness626Checks.ClosedBeforeToken);
    Test(Erlang.Tests.ProcessLiveness626Names.LiveCase, Erlang.Tests.ProcessLiveness626Checks.LiveCancellation);
    Test(Erlang.Tests.RegexCustody597Names.PhysicalCase, Erlang.Tests.RegexCustody597Checks.Physical);
    Test(Erlang.Tests.RegexCustody597Names.GenerationsCase, Erlang.Tests.RegexCustody597Checks.Generations);
    Test(Erlang.Tests.RegexCustody597Names.CompileImportCase, Erlang.Tests.RegexCustody597Checks.CompileImport);
    Test(Erlang.Tests.RegexLifecycle599Names.RollbackCase, Erlang.Tests.RegexLifecycle599Checks.FailedInstallRollback);
    Test(Erlang.Tests.RegexLifecycle599Names.ReaderCase, Erlang.Tests.RegexLifecycle599Checks.HostReaderHandoff);
    Test(Erlang.Tests.RegexLifecycle599Names.AdmissionCase, Erlang.Tests.RegexLifecycle599Checks.AdmissionOrders);
    Test(Erlang.Tests.RegexLifecycle599Names.PublishedCase, Erlang.Tests.RegexLifecycle599Checks.PublishedRollbackRefused);
    Test(Erlang.Tests.RegexForeignEngine605Names.Case, Erlang.Tests.RegexForeignEngine605Checks.DirectForeignActorDispose);
    Test(Erlang.Tests.RegexStore535Names.RegexEtsCase, Erlang.Tests.RegexStore535Checks.RegexEts);
    Test(Erlang.Tests.RegexStore535Names.RegexPersistentCase, Erlang.Tests.RegexStore535Checks.RegexPersistent);
    Test(Erlang.Tests.CountersExit603Names.InitializerTest, Erlang.Tests.CountersExit603Checks.InitializerDoneBeforeGlobalDrain);
    Test(Erlang.Tests.CountersExit603Names.OrdinaryTest, Erlang.Tests.CountersExit603Checks.OrdinaryExitPreservesIndependentHeap);

    var catalog = Erlang.Tests.TestRunCatalog.Create(tests.Select(test => test.Name));
    selectedTests = selection.Select(tests, catalog);
    if (selection.ListOnly)
    {
        await selection.WriteListingAsync(catalog, selectedTests.Select(test => test.Name));

        return 0;
    }
}
catch (ArgumentException failure)
{
    Console.Error.WriteLine(failure.Message);

    return Erlang.Tests.TestRunNames.InvalidArgumentsExitCode;
}

if (selection.IsPartial)
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        Partial = true,
        SelectedCount = selectedTests.Count,
        TotalCount = tests.Count
    }));

int failed = 0;
foreach (var test in selectedTests)
{
    var watch = Stopwatch.StartNew();
    try
    {
        await test.Body().WaitAsync(Erlang.Tests.TestExecutionBudgets.For(test.Name));
        Console.WriteLine("PASS " + test.Name);
        results.Add(new
        {
            test.Name,
            Status = "Passed",
            Milliseconds = watch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine("FAIL " + test.Name + ": " + ex);
        results.Add(new
        {
            test.Name,
            Status = "Failed",
            Error = ex.ToString(),
            Milliseconds = watch.ElapsedMilliseconds
        });
    }
}
Console.WriteLine($"{selectedTests.Count - failed}/{selectedTests.Count} passed");
string? report = selection.ReportPath;
if (report is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!);
    await File.WriteAllTextAsync(
        report,
        JsonSerializer.Serialize(
            selection.Report(
                selectedTests.Count - failed,
                failed,
                results,
                selectedTests.Count,
                tests.Count
            ),
            new JsonSerializerOptions { WriteIndented = true }
        )
    );
}
if (failed == 0 && !selection.IsPartial && selection.RegistryPath is not null)
    await File.WriteAllTextAsync(
        selection.RegistryPath,
        JsonSerializer.Serialize(
            exportRegistry.Exports.Select(e => new
            {
                e.Module,
                e.Function,
                e.Arity,
                Status = Erlang.Tests.MfaRegistryMetadataNames.Implemented,
                CompatibilityLedger = Erlang.Tests.MfaRegistryMetadataNames.CompatibilityLedger,
                Evidence = $"mfa/{e.Module}:{e.Function}/{e.Arity}",
                Limits = "Single direct contract case plus feature regressions; complete error/options and OTP differential verification pending"
            }),
            new JsonSerializerOptions { WriteIndented = true }
        )
    );
return failed == 0 ? 0 : 1;
