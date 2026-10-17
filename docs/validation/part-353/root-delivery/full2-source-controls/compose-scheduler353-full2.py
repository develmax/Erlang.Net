"""Source-only plan/apply composition. No SDK, original VM, Git mutation or network."""
import argparse
import base64
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess

PARENT = '0e739220c2d5d7afc66b6fb94c1f2b15d156bd90'
SOURCE_PARENT = '4c9cb16e7c52bc10e54ceaf652797d100def488b'
PATCH = 'artifacts/scheduler-affinity353-idle-steal-fix-proposal3'
PATCH_SHA = '16B1A5255E268A3CC89D5E673EA5FFB4AF45B6C4AFFF4491A541AFD5E4788921'
DISPATCHER = 'src/Erlang.Runtime/RuntimeActorDispatcher.cs'
DISPATCHER_SHA = '0AF57D1F9F65CE153129E6D9718B3F545F7CE83FFE409E6889F51957040C3555'
PRESERVED_DOCS = ['docs/API_COMPATIBILITY_INDEX.md', 'docs/ERLANG_COMPATIBILITY.md', 'docs/changelog/0055.md']
PERMANENT = 'artifacts/scheduler-affinity353-permanent-proposal2'
CANDIDATE = 'artifacts/scheduler-affinity353-candidate'
PERMANENT_SHA = '6D719CDE2635B96E3D95C6BC4A2F7039D80A8B6DE422667D91212E40B30EF0C3'
PROFILE = 'artifacts/scheduler-affinity-native-profile-proposal3'
PROFILE_SHA = '0ED32D791D127B18183ACAA21B6B395EF8CCFB82A518CA8251509871D1A74EDF'
PROFILE_PREFIX = 'source/'
REGISTRY = 'docs/supported-mfas.json'
PACKAGE = ['README.md', 'docs/DOTNET-LICENSE.txt', 'docs/DOTNET-ThirdPartyNotices.txt',
           'docs/OTP-LICENSE.txt', 'docs/ROSLYN-LICENSE.txt', 'docs/OTP-PORTS.md']
PLANS = [
    ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json',
     '.github/workflows/nif-pending-native.yml', 'REVIEWED_SOURCE_PLAN_SHA256', 1268),
    ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json',
     '.github/workflows/nif-reallocation-native.yml', 'REVIEWED_REALLOCATION_PLAN_SHA256', 1293),
]


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def serialize(value):
    return (json.dumps(value, indent=2) + '\n').encode()


def read(path):
    return json.loads(path.read_bytes())


def head(repository):
    return subprocess.check_output(['git', '-C', str(repository), 'rev-parse', 'HEAD'], timeout=10).decode().strip()


def safe_path(path):
    parts = Path(path).parts
    assert path and not Path(path).is_absolute() and all(part not in ('.', '..') for part in parts)
    assert not any(part.lower() in ('bin', 'obj', '.git') for part in parts)
    assert 'license.local' not in path.lower()
    return path


def checked(repository, row, path_key='Path'):
    path = repository / safe_path(row[path_key])
    data = path.read_bytes()
    assert sha(data) == row['Sha256'] and len(data) == row['Bytes'], row
    return path


def policy(data, mode):
    assert mode in ('TextLF', 'BinaryExact')
    return data.replace(b'\r\n', b'\n') if mode == 'TextLF' else data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=['plan', 'apply'])
    parser.add_argument('--repository', required=True)
    parser.add_argument('--plan-directory', required=True)
    parser.add_argument('--metadata-plan')
    parser.add_argument('--metadata-plan-sha256')
    parser.add_argument('--destination')
    parser.add_argument('--approved-plan-sha256')
    args = parser.parse_args()
    repository = Path(args.repository).resolve()
    assert head(repository) == PARENT
    plan_directory = Path(args.plan_directory).resolve()
    assert plan_directory.is_relative_to(repository / 'artifacts')
    if args.mode == 'apply':
        assert args.approved_plan_sha256
        plan_path = plan_directory / 'composition-plan.json'
        assert sha(plan_path.read_bytes()) == args.approved_plan_sha256.upper()
        plan = read(plan_path)
        assert plan['ComposerSha256'] == sha(Path(__file__).read_bytes())
        assert plan['ParentHead'] == PARENT
        destination = Path(plan['Destination']).resolve()
        assert destination.is_relative_to(repository / 'artifacts') and not destination.exists()
        # Verify every pinned input and all bytes before creating any candidate output.
        for row in plan['InputPins']:
            checked(repository, row)
        for row in plan['Files']:
            checked(repository, row, 'SourcePath')
        destination.mkdir()
        for row in plan['Files']:
            target = destination / safe_path(row['Path'])
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(repository / row['SourcePath'], target)
        for row in plan['Files']:
            assert sha((destination / row['Path']).read_bytes()) == row['Sha256'], row['Path']
        output = {'Status': 'ACTUAL_SOURCE_COMPOSITION_ONLY_NO_SDK', 'PlanSha256': args.approved_plan_sha256.upper(),
                  'Destination': str(destination), 'Rows': len(plan['Files']), 'SdkExecuted': False,
                  'NativeExecuted': False, 'MainChanged': False}
        (plan_directory / 'application-receipt.json').write_bytes(serialize(output))
        print(json.dumps(output))
        return
    assert args.metadata_plan and args.metadata_plan_sha256 and args.destination
    destination = Path(args.destination).resolve()
    assert destination.is_relative_to(repository / 'artifacts') and not destination.exists()
    assert not plan_directory.exists(), 'fresh plan directory required'
    permanent_path = repository / PERMANENT / 'source-freeze.json'
    profile_path = repository / PROFILE / 'source-freeze.json'
    metadata_path = repository / safe_path(args.metadata_plan)
    assert sha(permanent_path.read_bytes()) == PERMANENT_SHA
    assert sha(profile_path.read_bytes()) == PROFILE_SHA
    assert sha(metadata_path.read_bytes()) == args.metadata_plan_sha256.upper()
    permanent = read(permanent_path)
    profile = read(profile_path)
    metadata = read(metadata_path)
    assert permanent['ParentHead'] == SOURCE_PARENT and metadata['ParentHead'] == PARENT
    patch_path = repository / PATCH / 'source-freeze.json'
    assert sha(patch_path.read_bytes()) == PATCH_SHA
    patch = read(patch_path)
    assert metadata['PatchFreezeSha256'] == PATCH_SHA
    assert metadata['DispatcherSha256'] == DISPATCHER_SHA
    assert metadata['DispatcherSourcePath'] == PATCH + '/source/' + DISPATCHER
    changed = subprocess.check_output(['git', '-C', str(repository), 'diff', '--name-only', SOURCE_PARENT, PARENT], timeout=10).decode().splitlines()
    assert sorted(changed) == sorted(PRESERVED_DOCS)
    closure_path = repository / PERMANENT / 'source-resource-closure.json'
    assert sha(closure_path.read_bytes()) == permanent['SourceClosureSha256']
    closure = read(closure_path)['Files']
    mapping = {}
    kinds = {}
    owned = {row['Path'] for row in permanent['Files']}
    input_pins = []

    def pin(path):
        data = path.read_bytes()
        input_pins.append({'Path': path.relative_to(repository).as_posix(), 'Bytes': len(data), 'Sha256': sha(data)})

    def add(target, source, kind, expected=None):
        safe_path(target)
        data = source.read_bytes()
        if expected:
            assert sha(data) == expected['Sha256'] and len(data) == expected['Bytes'], target
        mapping[target] = source
        kinds[target] = kind

    for path in [permanent_path, profile_path, metadata_path, patch_path, closure_path, Path(__file__).resolve()]:
        pin(path)
    for row in closure:
        add(row['Path'], repository / CANDIDATE / row['Path'], 'FrozenPermanentInput', row)
    for row in permanent['Files']:
        checked(repository / CANDIDATE, row)
    for target in PRESERVED_DOCS:
        source = repository / target
        add(target, source, 'PreservedCurrentPublishedReportingDocs')
        pin(source)
    for row in patch['Files']:
        checked(repository / PATCH, row)
    patch_source = repository / metadata['DispatcherSourcePath']
    assert sha(patch_source.read_bytes()) == DISPATCHER_SHA
    add(DISPATCHER, patch_source, 'IndependentlyReviewedIdleStealFix')
    pin(patch_source)
    for row in profile['Files']:
        checked(repository / PROFILE, row)
        if row['Path'].startswith(PROFILE_PREFIX):
            target = row['Path'][len(PROFILE_PREFIX):]
            assert target not in mapping, target
            add(target, repository / PROFILE / row['Path'], 'ReviewedNativeProfile', row)
            owned.add(target)
    assert len(metadata['Files']) == len({row['TargetPath'] for row in metadata['Files']})
    for row in metadata['Files']:
        parent = repository / row['TargetPath']
        assert (sha(parent.read_bytes()) if parent.exists() else None) == row['ParentSha256'], row['TargetPath']
        add(row['TargetPath'], checked(repository, row, 'SourcePath'), 'ReviewedMetadataOverlay', row)
        owned.add(row['TargetPath'])
    assert metadata['GuardCount'] == 89 and metadata['Counts'] == {'Verified': 16, 'Known': 24, 'Needs': 547}
    generated = {}

    def content(path):
        return generated[path] if path in generated else mapping[path].read_bytes()

    attributes = content('.gitattributes')
    for rule in [b'tools/Erlang.SchedulerAffinityOriginalObservations/** -text whitespace=trailing-space,space-before-tab,cr-at-eol',
                 b'.github/workflows/scheduler-affinity-original.yml text eol=lf']:
        assert attributes.splitlines().count(rule) == 1
    # Compare against the immediately preceding actual runtime, not the earlier reporting delta.
    scope = json.loads(content('docs/module-readiness-scope.json'))
    scope['ComparisonTestReport'] = 'validation/part-351/full/tests.json'
    generated['docs/module-readiness-scope.json'] = serialize(scope)
    owned.add('docs/module-readiness-scope.json')
    for path, workflow, seal, count in PLANS:
        plan = json.loads(content(path))
        assert len(plan['Files']) == count
        for row in plan['Files']:
            data = policy(content(row['Path']), row['HashMode'])
            assert row['Bytes'] == len(data) and row['Sha256'] == sha(data), row['Path']
        workflow_bytes = content(workflow)
        assert b'\r' not in workflow_bytes and not workflow_bytes.startswith(b'\xef\xbb\xbf')
        seals = re.findall(rb'(' + seal.encode() + rb': )([A-F0-9]{64})', workflow_bytes)
        assert len(seals) == 1 and seals[0][1].decode() == sha(content(path))
    for path, workflow, seal, count in PLANS:
        for row in json.loads(content(path))['Files']:
            data = policy(content(row['Path']), row['HashMode'])
            assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
    ledger = json.loads(content('docs/mfa-compatibility.json'))
    assert len(ledger['Entries']) == 587 and len(ledger['VerifiedSourceFiles']) == 89
    counts = {name: sum(row['Status'] == name for row in ledger['Entries'])
              for name in ['Verified compatible', 'Known differences', 'Needs verification']}
    assert counts == {'Verified compatible': 16, 'Known differences': 24, 'Needs verification': 547}
    for row in ledger['VerifiedSourceFiles']:
        assert sha(content(row['Path'])) == row['Sha256'], row['Path']
    # Copy every direct ledger evidence input and both prior reports needed by readiness.
    required_docs = {'docs/validation/part-351/full/tests.json', 'docs/validation/part-350/full/tests.json'}
    for entry in ledger['Entries']:
        for evidence in entry['Evidence']:
            target = 'docs/' + evidence['Path']
            if target not in mapping:
                add(target, repository / safe_path(target), 'InheritedReadinessEvidence')
            assert sha(content(target)) == evidence['Sha256'], target
            required_docs.add(target)
    for target in sorted(required_docs):
        if target not in mapping:
            add(target, repository / target, 'InheritedReadinessEvidence')
    prior = json.loads(content('docs/validation/part-351/full/tests.json'))
    assert prior['Passed'] == 7297 and prior['Failed'] == 0 and len(prior['Tests']) == 7297
    registry = json.loads(content(REGISTRY))
    assert len(registry) == 587
    assert len({(row['Module'], row['Function'], row['Arity']) for row in registry}) == 587
    # Preserve 41 original permanent bytes; only independently reviewed Dispatcher changes.
    for row in permanent['Files']:
        expected = DISPATCHER_SHA if row['Path'] == DISPATCHER else row['Sha256']
        assert sha(content(row['Path'])) == expected, row['Path']
    for target in PRESERVED_DOCS:
        assert content(target) == (repository / target).read_bytes(), target
    plan_directory.mkdir()
    for path, data in generated.items():
        output = plan_directory / 'generated' / path
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_bytes(data)
        add(path, output, 'FinalAttributeScopeAndPlanSeals')
        owned.add(path)
    rows = [{'Path': target, 'SourcePath': source.relative_to(repository).as_posix(),
             'Bytes': source.stat().st_size, 'Sha256': sha(source.read_bytes()), 'Kind': kinds[target]}
            for target, source in sorted(mapping.items())]
    stable = [row for row in rows if row['Path'] != REGISTRY]
    packages = [next(row for row in rows if row['Path'] == target) for target in PACKAGE]
    assert len(packages) == 6
    plan = {'Status': 'REVIEWABLE_SOURCE_COMPOSITION_PLAN_NOT_APPLIED', 'ParentHead': PARENT,
            'Destination': str(destination), 'ComposerSha256': sha(Path(__file__).read_bytes()),
            'InputPins': input_pins, 'Files': rows, 'StableClosure': stable,
            'OwnedPaths': sorted(owned), 'PackageResources': packages,
            'RegistryBefore': next(row for row in rows if row['Path'] == REGISTRY),
            'ReadinessInputs': sorted(required_docs), 'VerifiedSourceGuardCount': 89,
            'CompatibilityCounts': counts, 'ExpectedOldOrderedPrefix': 7297, 'ExpectedNewTests': 16,
            'ExpectedFullTestsIfPasses': 7313, 'ExpectedIntegrationOutcomes': 11,
            'PermanentSourceParent': SOURCE_PARENT, 'PatchFreezeSha256': PATCH_SHA,
            'DispatcherSha256': DISPATCHER_SHA, 'PreservedCurrentMainDocs': PRESERVED_DOCS, 'ExpectedPackageOutputs': 2,
            'RegistryExcludedFromStableClosure': True, 'SdkExecuted': False, 'NativeExecuted': False,
            'MainChanged': False, 'OrdinaryCorpusDefaultFlagsUnchanged': True}
    (plan_directory / 'composition-plan.json').write_bytes(serialize(plan))
    print(json.dumps({'PlanSha256': sha((plan_directory / 'composition-plan.json').read_bytes()),
                      'Rows': len(rows), 'StableRows': len(stable), 'Owned': len(owned), 'Applied': False}))


if __name__ == '__main__':
    main()
