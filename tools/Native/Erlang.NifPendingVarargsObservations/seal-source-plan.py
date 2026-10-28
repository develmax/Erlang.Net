import argparse
import hashlib
import json
import pathlib
import subprocess

EXPLICIT_REVIEWED_INPUTS = frozenset((
    'src/Runtime/Erlang.Runtime.Core/Native/Nif/native/windows_varargs526.c',
    'src/Runtime/Erlang.Runtime.Core/runtimes/win-x64/native/Erlang.Nif.WindowsVariadic.dll',
    'tests/Erlang.Tests/Compiler/Beam/BeamPureReceive396Checks.cs',
    'tests/Erlang.Tests/Compiler/Beam/BeamPureReceive396Names.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplyBoundary520Checks.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplyBoundary520Imports.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplyBoundary520Names.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplyNativeMetadata527Admission.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplyNativeMetadata527Names.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/DynamicApplySourceAdmission520Checks.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/NativeMetadataImage527Checks.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/NativeMetadataImage527Names.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/NativeMetadataWitness527Checks.cs',
    'tests/Erlang.Tests/Compiler/Beam/Calls/NativeMetadataWitness527Names.cs',
    'tests/Erlang.Tests/Compiler/Beam/Receive/BeamOrdinaryBinaryReceive525Checks.cs',
    'tests/Erlang.Tests/Compiler/Beam/Receive/BeamOrdinaryBinaryReceive525Names.cs',
    'tests/Erlang.Tests/Compiler/Beam/Receive/BeamReceiveBinary398Checks.cs',
    'tests/Erlang.Tests/Compiler/Beam/Receive/BeamReceiveBinary398Names.cs',
    'tests/Erlang.Tests/Compiler/Beam/Receive/BeamReceiveMap397Checks.cs',
    'tests/Erlang.Tests/Compiler/Beam/Receive/BeamReceiveMap397Names.cs',
    'tests/Erlang.Tests/Compiler/Beam/Receive/BeamReceiveSelectionHostNames.cs',
    'tests/Erlang.Tests/Harness/Program.cs',
    'tests/Erlang.Tests/Resources/Beam/dirty_flags521_driver525.beam',
    'tests/Erlang.Tests/Resources/Beam/dirty_flags521_driver525.compile.etf',
    'tests/Erlang.Tests/Resources/Beam/dirty_flags521_driver525.erl',
    'tests/Erlang.Tests/Resources/Beam/native_metadata527_erlang.beam',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/observed-deterministic.etf',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/observed.etf',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/original-acquisition.json',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/owned_guest506.erl',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/owned_observer506.beam',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/owned_observer506.erl',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/owned_probe506.erl',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/paired_worker506.beam',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/paired_worker506.erl',
    'tests/Erlang.Tests/Runtime/Memory/AllocatorFirstFit519Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/AllocatorFirstFit519Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamConstructedCommit548Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamConstructedCommit548TestNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamGuestReturn507Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamGuestReturn507Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamListCurrentRoot550Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamListCurrentRoot550Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamOwnedReceive507Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamOwnedReceive507Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamOwnedRoots507Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamOwnedRoots507Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamOwnedWord504Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BeamOwnedWord504Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BinaryOwned512Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BinaryOwned512Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BoxedFloat556Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BoxedFloat556Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/BoxedInteger559Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/BoxedInteger559TestNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridAst507Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridAst507Observation.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridAst507Probe.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridAsyncPattern507.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridBoundary507Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridBoundary507Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridKeyScope507Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridKeyScope507Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/HybridRetainingPattern507.cs',
    'tests/Erlang.Tests/Runtime/Memory/LinearConstruction514Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/LinearConstruction514Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/NativeMapComposition510Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/NativeMapComposition510Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/NativeMapOwned510Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/NativeMapOwned510Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/NifPhysicalCustody521Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/NifPhysicalCustody521Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/NifPhysicalCustody521TestLease.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556Admission.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556AdmissionReader.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556Metadata.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalFloat556Replay.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalInteger559Admission.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalInteger559AdmissionReader.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalInteger559Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OriginalInteger559Replay.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuest506Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuest506Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuest506RuntimeFactory.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuestOriginal506Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuestOriginal506Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedShutdown506Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedShutdown506Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedTrampoline506Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedTrampoline506Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/PrivateSelfGc519Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/PrivateSelfGc519Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/ProcessOwnedTerm504Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/ProcessOwnedTerm504Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/ProcessOwnedWord504Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/ProcessOwnedWord504Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/ResourcePolicy508Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/ResourcePolicy508Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/RuntimeQueueStatistics504TestNames.cs',
    'tests/Erlang.Tests/Runtime/Memory/SourceBinaryComposition511Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/SourceBinaryComposition511Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/SourceLiveness511Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/SourceLiveness511Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/TrustedCapabilities507Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/TrustedCapabilities507Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/TrustedGuest507Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/TrustedGuest507Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/UnknownSourceLiveness511Expr.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Maps/NifMapFamily504TestNames.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Maps/NifMapIterator504TestNames.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Maps/NifMapMaterialized505TestNames.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Maps/NifMapReview504TestNames.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Maps/NifNativeFlatDuplicate505TestNames.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Memory/NifAppendOnly516Checks.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Memory/NifAppendOnly516Names.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Memory/NifEnvironmentRoots516Checks.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Memory/NifEnvironmentRoots516Names.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/Memory/UnknownNifGuest516Term.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/WindowsVariadic526Checks.cs',
    'tests/Erlang.Tests/Runtime/Native/Nif/WindowsVariadic526Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource540Checks.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource540Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource541Comparison.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource541ComparisonChecks.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource541Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource541Replay.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource542Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnResource542OriginalAdmission.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546Admission.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546Checks.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546FunctionScope.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546Names.cs',
    'tests/Erlang.Tests/Runtime/Processes/Spawn/SpawnWorker546WitnessLaw.cs',
    'tests/Erlang.Tests/Runtime/Scheduling/DirtyIoConcurrency522Checks.cs',
    'tests/Erlang.Tests/Runtime/Scheduling/DirtyIoConcurrency522Names.cs',
    'tests/Erlang.Tests/Runtime/Scheduling/DirtyNative419Access.cs',
    'tests/Erlang.Tests/Runtime/Scheduling/Native521RuntimeFactory.cs',
    'tests/Erlang.Tests/Runtime/Statistics/RuntimeStatisticsHostChecks.cs',
    'tests/Erlang.Tests/Runtime/Statistics/RuntimeStatisticsHostParameters.cs',
    'tools/Native/Erlang.NifDirty521Observations/.gitattributes',
    'tools/Native/Erlang.NifDirty521Observations/build-native.py',
    'tools/Native/Erlang.NifDirty521Observations/collect.py',
    'tools/Native/Erlang.NifDirty521Observations/fixtures/dirty_flags521.c',
    'tools/Native/Erlang.NifDirty521Observations/fixtures/dirty_flags521.erl',
    'tools/Native/Erlang.NifDirty521Observations/fixtures/dirty_flags521_driver.erl',
    'tools/Native/Erlang.NifDirty521Observations/seal.py',
    'tools/Native/Erlang.NifMapFamily504Observations/collect.py',
    'tools/Native/Erlang.NifMapFamily504Observations/source-plan.json',
    'tools/Native/Erlang.NifMapMaterialized505Observations/collect.py',
    'tools/Native/Erlang.NifMapMaterialized505Observations/seal-plan.py',
    'tools/Native/Erlang.NifMapMaterialized505Observations/source-plan.json',
    'tools/Native/Erlang.NifMapReentry505Observations/collect.py',
    'tools/Native/Erlang.NifMapReentry505Observations/source-plan.json',
    'tools/Native/Erlang.NifVariadic526Observations/.gitattributes',
    'tools/Native/Erlang.NifVariadic526Observations/build-native.py',
    'tools/Native/Erlang.NifVariadic526Observations/collect.py',
    'tools/Native/Erlang.NifVariadic526Observations/fixtures/variadic_family526.c',
    'tools/Native/Erlang.NifVariadic526Observations/fixtures/variadic_family526.erl',
    'tools/Native/Erlang.NifVariadic526Observations/fixtures/variadic_family526_driver.erl',
    'tools/Native/Erlang.NifVariadic526Observations/seal-source-plan.py',
))


def reviewed_inputs(root, plan_path):
    paths = set(subprocess.check_output(['git', 'ls-files', '-z', '--', 'src', 'global.json', 'Directory.*', 'NuGet.Config', 'nuget.config'], cwd=root).decode().split('\0')) - {''}
    paths = {path for path in paths if pathlib.Path(path).suffix in ('.cs', '.csproj', '.props', '.targets', '.json', '.Config', '.config')}
    paths.update(path.name for pattern in ('Directory.*', 'global.json', 'NuGet.Config', 'nuget.config') for path in root.glob(pattern) if path.is_file() and path.suffix in ('.props', '.targets', '.json', '.Config', '.config'))
    for directory in ('src', 'tools/Native/Erlang.NifNativeBackend', 'tools/Native/Erlang.NifPendingVarargsObservations'):
        paths.update(str(path.relative_to(root)).replace('\\', '/') for path in (root / directory).rglob('*') if path.is_file() and not {'bin', 'obj', '__pycache__'}.intersection(path.parts) and path.resolve() != plan_path.resolve() and (directory != 'src' or path.suffix in ('.cs', '.csproj', '.props', '.targets')))
    paths.add('tools/Native/Erlang.NifFixtureHost/native/fixture.c')
    paths.add('tools/Compiler/Erlang.BeamFixtures/verify-metadata-mutations.py')
    for name in ('GeneratedModuleCompiler.cs', 'GeneratedModuleArtifact.cs', 'GeneratedModuleReferences.cs', 'GeneratedModuleMetadata.cs', 'OracleDiagnostics.cs'):
        paths.add('tools/Validation/Erlang.Differential/' + name)
    paths.update(EXPLICIT_REVIEWED_INPUTS)
    return paths


parser = argparse.ArgumentParser(description='Source-only sealing after final reviewed source promotion; does not execute native code.')
parser.add_argument('--repository', type=pathlib.Path, required=True)
parser.add_argument('--output', type=pathlib.Path, required=True)
args = parser.parse_args()
root = args.repository.resolve()
if args.output.exists():
    raise RuntimeError('Fresh seal path required.')
paths = reviewed_inputs(root, args.output)
if not any(path.startswith('tools/Native/Erlang.NifNativeBackend/') for path in paths):
    raise RuntimeError('Final native backend must be promoted before source sealing.')
records = []
for name in sorted(paths):
    raw = '/original/' in name or '/native/' in name or name == 'tools/Compiler/Erlang.BeamFixtures/verify-metadata-mutations.py'
    data = (root / name).read_bytes()
    mode = 'BinaryExact' if raw else 'TextLF'
    if not raw:
        data = data.replace(b'\r\n', b'\n')
    records.append({'Path': name, 'Bytes': len(data), 'Sha256': hashlib.sha256(data).hexdigest().upper(), 'HashMode': mode})
receipt = {'Status': 'REVIEWED_SOURCE_ONLY_NATIVE_NOT_EXECUTED', 'PinnedOtpCommit': 'ad05823719d77c8faee87348ea39513d4e2f99c5', 'NativeOrSdkExecuted': False, 'Files': records}
args.output.write_bytes((json.dumps(receipt, indent=2) + '\n').encode('utf-8'))
print(hashlib.sha256(args.output.read_bytes()).hexdigest().upper())
