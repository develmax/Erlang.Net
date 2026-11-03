using Erlang.Tests;
if (args.Length > 0 && args[0] == "--gc-binary818-child")
{
    await Erlang.NifNativeBackend.GcBinary818Entry.Run(args[1..]);

    return 0;
}

if (args.Length == 5 && args[0] == AckOriginal425ReplayNames.Command)
{
    await AckOriginal425Replay.Run(
        args[1],
        args[2],
        args[3],
        args[4]
    );

    return 0;
}

if (args.Length == 5 && args[0] == AckTrace173ReplayNames.Command)
{
    await AckTrace173Replay.Run(
        args[1],
        args[2],
        args[3],
        args[4]
    );

    return 0;
}

if (args.Length != 1) throw new ArgumentException("One exact control required");
Func<Task> body = args[0] switch
{
    HostCopy827Names.Mode => HostCopy827Checks.Run,
    FunctionUnique212Names.Mode => FunctionUnique212Checks.Run,
    ControlRoot826Names.Mode => ControlRoot826Checks.Run,
    RegisterCapacity273Names.Mode => RegisterCapacity273Checks.Run,
    BeamLocation146Names.Mode => BeamLocation146Checks.Run,
    "literal800" => ModuleLiteral800Checks.Run,
    "lifetime801" => ModuleLiteral801Checks.PhysicalCustody,
    "empty803" => EmptyTuple803Checks.PhysicalHeaderAndLifetime,
    "binary805" => ModuleBinary805Checks.Run,
    "halt811" => IntrinsicHalt811Checks.ActualHaltRetiresIntrinsic,
    "otpLiteral815" => OtpLiteral815Checks.Run,
    "tokenMutable550" => Erlang.NifNativeBackend.MutableSequenceToken550Checks.ActualMutableTokenAndEnclosingRoots,
    "tokenLiteral814" => SequenceLiteral814Checks.ActualTokenLiteralCustody,
    "trace817" => Erlang.NifNativeBackend.ReadonlyTrace817Checks.RealCellsLateShareDiscardAndCollectorImport,
    "prefix817" => Erlang.NifNativeBackend.ReadonlyTracePrefix817Checks.ActualAliasProcessInfoAliasOrder,
    "priority817" => Erlang.NifNativeBackend.ReadonlyTracePriority817Checks.ActualPriorityPlacementAndTraceOrder,
    "onheap817" => Erlang.NifNativeBackend.ReadonlyTraceOnHeap817Checks.ActualOnHeapSourceGcAndSelectedShares,
    "preparedNative810" => PreparedLoader807Checks.Run,
    "preparedBuilder810" => Prepared810Entry.Builder,
    "preparedAdmission810" => Prepared810Entry.Admission,
    "sourceLiteral819" => SourceLiteral819Checks.Run,
    "sourceParser820" => ParserConstant820Checks.Run,
    "sourceDeep820" => ParserConstant820Checks.DeepPreparation,
    "suspensionRoot821" => SuspensionRootLease821Checks.ReserveCommitCancel,
    SuspensionAck286Names.Mode => SuspensionAck286Checks.RunAll,
    "resource524Guest" => NativeResource524Checks.ViewAndGuestCollection,
    "resource524Image" => NativeResource524Checks.PhysicalDestructorPinsImage,
    BinaryInfo822ControlNames.Mode => BinaryInfo822Checks.Run,
    BinaryOwner823Names.Mode => BinaryOwner823Checks.Run,
    "sourceObserver820" => ParserConstant820Checks.OriginalObserver,
    _ => throw new ArgumentException("Unknown control")
};
try
{
    await body();
    Console.WriteLine("PASS " + args[0]);

    return 0;
}
catch (Exception error)
{
    Console.WriteLine("FAIL " + args[0]);
    Console.WriteLine(error);
    if (error is Erlang.ErlangException failure) Console.WriteLine(failure.StackTraceTerm);

    return 1;
}
