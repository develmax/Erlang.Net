"""Atomic fresh source plan; never starts a VM or rewrites evidence."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Runtime/Erlang.OwnedGuest506Observations'
WORKFLOWS = ['.github/workflows/owned-guest506-original.yml', '.github/workflows/validate.yml']
DEPENDENCIES = [
    'src/Runtime/Erlang.Runtime.Core/Diagnostics/Debug/DebugDirtyOperations424.cs',
    'src/Runtime/Erlang.Runtime.Core/Diagnostics/Debug/DebugDirtyTermCopy424.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Resources/ResourceRootTraversal595.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/DebugDirtyAtomics595Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/DebugDirtyAtomics595Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsHostAggregate594.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentTermArenaSnapshot417.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentTermReadSnapshot.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/RuntimeEtsReadLease.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/RuntimeEtsStoredObject.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsAggregate594Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsAggregate594Names.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsEtsAggregate594Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsEtsAggregate594Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.ExternalTerms.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsHostCopyTransaction591.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentArenaCodec416.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentArenaRead416.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentArenaRoot416.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentArenaRootBuilder416.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentArenaSelection591.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentArenaSelection591Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentArenaStore416.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentIdentifierRoot416.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/PersistentTableRead416.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/RuntimeEtsStoredPayload.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588CaseNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588MetadataNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics590Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics590Identity.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics590Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics590Result.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics590Role.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics590SyntheticNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics590Wire.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics590WireNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalReference563Replay.Atomics590.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsExportRollback593Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsExportRollback593Names.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsPersistentSelection591Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsPersistentSelection591Names.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsReservation591Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsReservation591Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedTermInvocation.Native524.cs',
    'src/Runtime/Erlang.Runtime.Core/Services/RuntimeExternalTermReservation.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsRetirement590Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsRetirement590Destructor.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsRetirement590Names.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsStore590Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsStore590Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeCallEnvironment.Native524.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeCallEnvironment.OwnedRoots516.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Resources/NativeResourceRoots524.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Resources/NativeTermHostScope524.cs',
    'src/Runtime/Erlang.Runtime.Core/Primitives/References/ResourceReferenceTerm.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessContext.Atomics586.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedTermDetachedCopy506.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedTermInvocation.Atomics586.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessBinaryRegistry512.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Atomics586.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Native524.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/ProcessDictionary.cs',
    'src/Runtime/Erlang.Runtime.Core/Services/RuntimeReferenceResources.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsRoots586.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/RuntimeAtomicsOwner413.Retirement586.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsDictionaryHost589Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsDictionaryHost589Names.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsHost413Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsHost413Names.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsMagic586Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsMagic586Names.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsP2a586Checks.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsP2a586Fixture.cs',
    'tests/Erlang.Tests/Runtime/Storage/Atomics/AtomicsP2a586Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsResourceLease586.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsOwnership586Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsNative413.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsMagicHeader413.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsLeaseKind586.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsHostCustody586.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsGuestRoot413.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsConstructorBinding413.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsBackingLease586.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsArray413.Ownership586.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/AtomicsArray413.cs',
    'src/Runtime/Erlang.Runtime.Core/Services/RuntimeReferenceResources.Atomics413.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Magic576.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/NativeOffheapHeader576.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.Atomics413.cs',
    'src/Runtime/Erlang.Runtime.Core/Storage/RuntimeAtomicsOwner413.cs',
    'src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.BeamPrimitives.cs',
    'src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.BootstrapAtomics413.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalReference563Replay.Atomics588.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588Stock.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588Images.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588Evidence.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588Cases.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588CaseChecks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588AdmissionReader.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalAtomics588Admission.cs',
    'src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamLinkedCode.LoaderMapKeys565.cs',
    'src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamLinkedCode.cs',
    'src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamLoaderMapKeys565.cs',
    'src/Runtime/Erlang.Runtime.Core/Modules/ModuleRegistry.PreparedBeam.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.LoaderMapKeys565.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/NativeMapOwnedRoots510.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedMapRow517.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermAllocationSnapshot567.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermCollection508.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Allocation567.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Generations508.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.LoaderMapKeys565.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.MapPayload517.cs',
    'tests/Erlang.Tests/Runtime/Memory/Allocation567Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/Allocation567TestNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamLoaderMapKeys565Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamLoaderMapKeys565Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalLoaderMap528Names.cs',
    'src/Runtime/Erlang.Terms/Storage/IOwnedReferenceStorage562.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/OrdinaryReference562Names.cs',
    'src/Runtime/Erlang.Terms/Identifiers/ReferenceTerm.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Reference562.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermSequence506.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap506.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedTermTransfer506.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermFrame.Guest507.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Guest507.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermFrame.Source511.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.DebugWords526.cs',
    'tests/Erlang.Tests/Runtime/Memory/OrdinaryReference562Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OrdinaryReference562TestNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalReference563Admission.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalReference563AdmissionReader.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalReference563Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalReference563Replay.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/BoxedInteger559Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Integer559.cs',
    'src/Runtime/Erlang.Terms/Storage/IOwnedIntegerStorage559.cs',
    'src/Runtime/Erlang.Terms/Values/Integer.cs',
    'tests/Erlang.Tests/Runtime/Memory/BoxedInteger559Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BoxedInteger559TestNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalInteger559Admission.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalInteger559AdmissionReader.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalInteger559Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalInteger559Replay.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeLibraryOwner.LoadPrivateData557.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeCallEnvironment.LoadPrivateData557.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeCallEnvironment.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Nif/NifNativeLibraryOwner.cs',
    'tests/Erlang.Tests/Runtime/Memory/BoxedFloat556Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BoxedFloat556Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/BoxedFloat556Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/CellStorage526Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/NativeTermCellBuffer526.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/OwnedPhysicalWordSnapshot526.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.Float556.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Memory/ProcessTermHeap.NativeWords526.cs',
    'src/Runtime/Erlang.Terms/Storage/IOwnedFloatStorage556.cs',
    'src/Runtime/Erlang.Terms/Values/FloatTerm.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556Admission.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556AdmissionReader.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556Metadata.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556Replay.cs',
    'src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamRegisterInterpreter.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamListCurrentRoot550Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamListCurrentRoot550Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamLocalFrame.Owned507.cs',
    'src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamOwnedRoots507.cs',
    'src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamRegisterState.Owned507.cs',
    'src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamConstructedCommit548Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamConstructedCommit548Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamConstructedCommit548TestNames.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/ProcessSpawnRequestOptions.cs',
    'src/Runtime/Erlang.Runtime.Core/Primitives/RuntimeNativeMetadataImage.cs',
    'src/Runtime/Erlang.Runtime.Core/Primitives/RuntimeNativeErrorMetadata545.cs',
    'src/Runtime/Erlang.Runtime.Core/Primitives/RuntimeNativeErrorMetadata545Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/ProcessSpawnRequestRegistrations.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/ProcessSpawnRequestErrorMetadata547.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplyBoundary520Imports.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546Admission.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546Checks.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546FunctionScope.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546WitnessLaw.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource542Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource542OriginalAdmission.cs',
    'tests/Erlang.Tests/Runtime/Diagnostics/Debug/DebugDirtyNative424Access.cs',
    'tests/Erlang.Tests/Runtime/Modules/CodeLifecycle/OwnedFunctionOriginal407Terms.cs',
    'tests/Erlang.Tests/Runtime/Modules/CodeLifecycle/OwnedFunctionOriginal407Names.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Internal/InternalSpawnRequestNative429.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/ProcessSpawnOptionRegistrations.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/ProcessSpawnOptions.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/SpawnValidation/ProcessSpawnResourceNames540.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/SpawnValidation/ProcessSpawnResourceOptions540.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/SpawnValidation/ProcessSpawnResourceValidation540.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplyNativeMetadata527Admission.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplyNativeMetadata527Names.cs',
    'tests/Erlang.Tests/Harness/Program.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalMap517Admission.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalMap517AdmissionReader.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource540Checks.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource540Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource541Comparison.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource541ComparisonChecks.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource541Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource541Replay.cs',
    'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py',
    'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/bounded_inventory.py',
    'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py',
    'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py',
    'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/build-original.py',
    'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/manifest.json',
    'tools/prepare-regex-provider.ps1',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuestOriginal506Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuestOriginal506Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuest506RuntimeFactory.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedShutdown506Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedShutdown506Names.cs',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/observed.etf',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/observed-deterministic.etf',
]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--staged-tree', type=Path)
    parser.add_argument('--replace-plan-sha256')
    args = parser.parse_args()
    repo = args.repository.resolve()
    tree = args.staged_tree.resolve() if args.staged_tree else repo
    tool = Path(__file__).resolve().parent
    if tool != tree/TOOL:
        raise RuntimeError('Sealer must be under the declared tracked tool path')
    plan_path = tool/'source-plan.json'
    if plan_path.exists():
        actual = hashlib.sha256(plan_path.read_bytes()).hexdigest().upper()
        if not args.replace_plan_sha256 or actual != args.replace_plan_sha256.upper():
            raise RuntimeError('Replacing a plan requires its current SHA; immutable history belongs outside the tool')
    rows = []
    allowed_root = {'.gitattributes', 'collect.py', 'seal.py', 'jobs.json', 'README.md', 'adopt-original.py', 'adopt-float556.py', 'adopt-integer559.py', 'adopt-reference563.py', 'stock-owners588.py', 'adopt-atomics588.py', 'capture-atomics588.py', 'atomics-closure592.py', 'capture-float556.py', 'capture-integer559.py', 'capture-reference563.py', 'admitted_receipts.py', 'adopt-spawn541.py', 'capture-spawn541.py'}
    for path in sorted(tool.rglob('*')):
        if not path.is_file() or path == plan_path:
            continue
        relative = path.relative_to(tool)
        if relative.parts[0] not in {'fixtures', 'golden'} and relative.as_posix() not in allowed_root:
            raise RuntimeError('Live output/unlisted source cannot enter plan: '+str(relative))
        data = path.read_bytes()
        rows.append(dict(Scope='Tool', Path=relative.as_posix(), HashMode='Raw',
                         Sha256=hashlib.sha256(data).hexdigest().upper(), Bytes=len(data)))
    for name in [*DEPENDENCIES, *WORKFLOWS]:
        path = (tree if name in WORKFLOWS else repo)/name
        mode = 'Raw' if path.suffix in {'.etf', '.beam', '.cs'} else 'TextLF'
        data = path.read_bytes()
        if mode == 'TextLF':
            data = data.replace(b'\r\n', b'\n')
        rows.append(dict(Scope='Repository', Path=name, HashMode=mode,
                         Sha256=hashlib.sha256(data).hexdigest().upper(), SealedBytes=len(data)))
    plan = dict(State='SEALED_SOURCE_NOT_EXECUTION_RECEIPT', PinnedCommit=PIN, ToolPath=TOOL,
                Inputs=rows, Excludes=['source-plan.json', 'all live artifact output'],
                WholeContractsClosed=0, GoldenProfile='Historical genuinely acquired paired335 raw whole protocol',
                ProviderProfiles=['official Windows library376 admission', 'existing pinned Linux build receipt'])
    data = (json.dumps(plan, indent=2)+'\n').encode('utf-8')
    descriptor, temporary = tempfile.mkstemp(prefix='.source-plan-', suffix='.tmp', dir=tool)
    try:
        with os.fdopen(descriptor, 'wb') as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, plan_path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)
    print(json.dumps(dict(SourcePlanSha256=hashlib.sha256(data).hexdigest().upper(), Inputs=len(rows),
                          Executed=False, WholeContractsClosed=0)))


if __name__ == '__main__':
    main()
