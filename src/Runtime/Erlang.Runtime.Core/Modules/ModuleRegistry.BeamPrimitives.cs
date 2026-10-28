using System.Collections.Frozen;

namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> CaptureBeamPrimitives(BeamLinkedCode code)
    {
        var primitives = new Dictionary<(string Function, int Arity), ErlangFunction>();
        var owned507 = owningRuntime?.OwnedTermsPilot506 == true
            ? new Dictionary<ErlangFunction, OwnedBindingKind507>(ReferenceEqualityComparer.Instance) : null;
        lock (gate)
        {
            foreach (BeamLinkedFunction function in code.Functions.Where(function => BeamNativePrimitives.IsStub(code, function)))
            {
                var signature = (function.Module, function.Name, function.Arity);
                if (!sourceOwners.ContainsKey(signature) && IsNativeEntry(signature.Module, signature.Name, signature.Arity))
                {
                    ErlangFunction canonical = functions[signature];
                    primitives.Add((function.Name, function.Arity), canonical);
                    OwnedBindingKind507 kind = SelectedOwnedBinding507(signature, canonical);
                    if (owned507 is not null && kind is OwnedBindingKind507.PureAliasRead or OwnedBindingKind507.LocalDictionary or OwnedBindingKind507.RuntimeRead535)
                        owned507[canonical] = kind;
                }
                else if (sourceOwners.GetValueOrDefault(signature) is PreparedBeamImage previous
                    && ReferenceEquals(codeImages.GetValueOrDefault(signature.Module), previous.ExecutionIdentity.Image)
                    && previous.ExecutionIdentity.NativePrimitives.TryGetValue((function.Name, function.Arity), out ErlangFunction? primitive))
                {
                    primitives.Add((function.Name, function.Arity), primitive);
                    OwnedBindingKind507 kind = NativeBorrowKind507(previous.ExecutionIdentity, primitive);
                    if (owned507 is not null && kind is OwnedBindingKind507.PureAliasRead or OwnedBindingKind507.LocalDictionary or OwnedBindingKind507.RuntimeRead535)
                        owned507[primitive] = kind;
                }
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && owningRuntime is not null
                    && owningRuntime.TryCaptureDdll422(
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction ddll422
                    ))
                    primitives.Add((function.Name, function.Arity), ddll422);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && UnicodeNative.TryCapture(
                        this,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction unicode
                    )
                    && code.Exports.Any(export => export.Name == UnicodeNativeNames.ToBinaryTrap && export.Arity == 2)
                    && code.Exports.Any(export => export.Name == UnicodeNativeNames.ToListTrap && export.Arity == 2))
                    primitives.Add((function.Name, function.Arity), unicode);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && MathNative.TryCapture(
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction math
                    ))
                    primitives.Add((function.Name, function.Arity), math);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && PrimFileEncodingNative410.TryCapture(
                        this,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction primFileEncoding
                    ))
                    primitives.Add((function.Name, function.Arity), primFileEncoding);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InterpreterCodeNative.TryCapture(
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction interpreterCode
                    ))
                    primitives.Add((function.Name, function.Arity), interpreterCode);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && FileNameEncodingNative.TryCapture(
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction encoding
                    ))
                    primitives.Add((function.Name, function.Arity), encoding);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && WarningMapNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction warningMap
                    ))
                    primitives.Add((function.Name, function.Arity), warningMap);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && OsNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction os
                    ))
                    primitives.Add((function.Name, function.Arity), os);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && NetKernelNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction netKernel
                    ))
                    primitives.Add((function.Name, function.Arity), netKernel);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && IoPrintableRangeNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction printableRange
                    ))
                    primitives.Add((function.Name, function.Arity), printableRange);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && ErtsInternalScalarNative413.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalScalar
                    ))
                    primitives.Add((function.Name, function.Arity), internalScalar);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalOwnerNative419.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalOwner
                    ))
                    primitives.Add((function.Name, function.Arity), internalOwner);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && BeamfileMd5Native423.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction beamfileMd5
                    ))
                    primitives.Add((function.Name, function.Arity), beamfileMd5);

                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && BeamfileChunkNative418.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction beamfileChunk
                    ))
                    primitives.Add((function.Name, function.Arity), beamfileChunk);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && PersistentTermNative.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction persistentTerm
                    ))
                    primitives.Add((function.Name, function.Arity), persistentTerm);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && TryCaptureRegex413(
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out var regex
                    ))
                    primitives.Add((function.Name, function.Arity), regex);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebugUnalignedNative421.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction debugUnaligned
                    ))
                    primitives.Add((function.Name, function.Arity), debugUnaligned);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DistDecodeNative423.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction distDecode
                    ))
                    primitives.Add((function.Name, function.Arity), distDecode);

                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DirtyQueryNative423.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction dirtyQuery
                    ))
                    primitives.Add((function.Name, function.Arity), dirtyQuery);

                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebugSameNative420.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction debugSame
                    ))
                {
                    primitives.Add((function.Name, function.Arity), debugSame);
                    if (owned507 is not null)
                        owned507[debugSame] = OwnedBindingKind507.PureAliasRead;
                }
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebugInternalNative419.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction debugInternal
                    ))
                    primitives.Add((function.Name, function.Arity), debugInternal);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebugLockNative419.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction debugLocks
                    ))
                    primitives.Add((function.Name, function.Arity), debugLocks);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebuggerDisabledNative413.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out var debugger
                    ))
                    primitives.Add((function.Name, function.Arity), debugger);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && CountersNative421.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction counters
                    ))
                    primitives.Add((function.Name, function.Arity), counters);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && AtomicsNative413.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction atomics
                    ))
                {
                    primitives.Add((function.Name, function.Arity), atomics);
                    if (owned507 is not null) owned507[atomics] = OwnedBindingKind507.RuntimeRead535;
                }
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && ManagedCollectorNative413.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction collector
                    ))
                    primitives.Add((function.Name, function.Arity), collector);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && MapKeysNative426.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction mapKeys426
                    ))
                {
                    primitives.Add((function.Name, function.Arity), mapKeys426);
                    if (owned507 is not null)
                        owned507[mapKeys426] = OwnedBindingKind507.PureAliasRead;
                }
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalProcessNative425.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalProcess425
                    ))
                    primitives.Add((function.Name, function.Arity), internalProcess425);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && ProcessAliveNative425.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction processAlive425
                    ))
                    primitives.Add((function.Name, function.Arity), processAlive425);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && CheckProcessCodeNative423.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction checkOwnCode
                    ))
                    primitives.Add((function.Name, function.Arity), checkOwnCode);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebugDispatchNative432.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction debugDispatch432
                    ))
                    primitives.Add((function.Name, function.Arity), debugDispatch432);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebugCodeNative427.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction debugCode427
                    ))
                    primitives.Add((function.Name, function.Arity), debugCode427);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebugSharingNative425.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction debugSharing425
                    ))
                {
                    primitives.Add((function.Name, function.Arity), debugSharing425);
                    if (owned507 is not null && (signature.Name is DebugSharingNames425.Flat or DebugSharingNames425.Shared))
                        owned507[debugSharing425] = OwnedBindingKind507.PureAliasRead;
                }
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DebugDirtyNative424.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction debugDirty424
                    ))
                    primitives.Add((function.Name, function.Arity), debugDirty424);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalTraceNative425.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalTrace425
                    ))
                    primitives.Add((function.Name, function.Arity), internalTrace425);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalGarbageCollectionNative428.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalGc428
                    ))
                    primitives.Add((function.Name, function.Arity), internalGc428);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalSystemTaskNative428.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalSystemTask428
                    ))
                    primitives.Add((function.Name, function.Arity), internalSystemTask428);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && HamtMapNative430.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction hamtMap430
                    ))
                {
                    primitives.Add((function.Name, function.Arity), hamtMap430);
                    if (owned507 is not null && (signature.Name == HamtNativeNames430.Children))
                        owned507[hamtMap430] = OwnedBindingKind507.PureAliasRead;
                }
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalPortLoaderNative428.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalPortLoader428
                    ))
                    primitives.Add((function.Name, function.Arity), internalPortLoader428);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalSpawnRequestNative429.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalSpawn429
                    ))
                    primitives.Add((function.Name, function.Arity), internalSpawn429);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalTextAuxiliaryNative430.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalTextAux430
                    ))
                    primitives.Add((function.Name, function.Arity), internalTextAux430);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalSystemCheckNative431.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalSystemCheck431
                    ))
                    primitives.Add((function.Name, function.Arity), internalSystemCheck431);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalDistributionProfile429Native.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction distributionProfile429
                    ))
                    primitives.Add((function.Name, function.Arity), distributionProfile429);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && InternalPurgeNative432.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction internalPurge432
                    ))
                    primitives.Add((function.Name, function.Arity), internalPurge432);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DistChannel429Native.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction distChannel429
                    ))
                    primitives.Add((function.Name, function.Arity), distChannel429);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && DistSpawn430Native.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction distSpawn430
                    ))
                    primitives.Add((function.Name, function.Arity), distSpawn430);
                else if (!functions.ContainsKey(signature) && !sourceOwners.ContainsKey(signature)
                    && AllocatorGatherNative434.TryCapture(
                        owningRuntime,
                        signature.Module,
                        signature.Name,
                        signature.Arity,
                        out ErlangFunction allocatorGather434
                    ))
                    primitives.Add((function.Name, function.Arity), allocatorGather434);
                else
                    throw new NotSupportedException(BeamRegisterNames.Unsupported);
            }
        }

        var captured = primitives.ToFrozenDictionary();
        if (owned507 is { Count: > 0 })
            preparedOwnedCapabilities507.Add(captured, new(owned507.ToFrozenDictionary<ErlangFunction, OwnedBindingKind507>(ReferenceEqualityComparer.Instance)));

        return captured;
    }
}
