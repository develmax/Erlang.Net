import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess
import sys

sys.dont_write_bytecode = True
PACKAGE = Path(__file__).resolve().parent
EVIDENCE = 'docs/validation/part-304/'
TOOL = 'tools/Erlang.RawOnLoadVisibilityObservations'


def require(value, message):
    if not value:
        raise RuntimeError(message)


def encoded(value):
    return (json.dumps(value, indent=2, ensure_ascii=False) + '\n').encode()


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def file(root, name, exists=True):
    relative = PurePosixPath(name)
    require(not relative.is_absolute() and '..' not in relative.parts and '\\' not in name, 'Unsafe path')
    target = (root / name).resolve()
    require(target.is_relative_to(root) and (not exists or target.is_file()), 'Missing or escaping path: ' + name)
    return target


def checked(root, record, key='Path'):
    data = file(root, record[key]).read_bytes()
    require(len(data) == record['Bytes'] and sha(data) == record['Sha256'], 'Bound bytes differ: ' + record[key])
    return data


def row(name, data):
    return {'Path': name, 'Bytes': len(data), 'Sha256': sha(data)}


def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], timeout=60)


def gate(root, approved):
    require(sha((PACKAGE / 'source-freeze.json').read_bytes()) == approved.upper(), 'Explicit finalizer freeze differs')
    for record in json.loads((PACKAGE / 'source-freeze.json').read_bytes())['Files']:
        checked(PACKAGE, record)
    config = json.loads((PACKAGE / 'input-gates.json').read_bytes())
    require(git(root, 'rev-parse', 'HEAD').decode().strip() == config['Parent'], 'HEAD must remain explicitly published303')
    for record in config['Protected'] + config['Reuse303']:
        checked(root, record)
    return config


def execution_gate(root, config):
    plan_file = file(root, TOOL + '/reviewed-execution-plan.json')
    require(sha(plan_file.read_bytes()) == config['ExecutionPlanSha256'], 'New execution seal differs')
    plan = json.loads(plan_file.read_bytes())
    actual = {path.relative_to(root).as_posix() for path in (root / TOOL).rglob('*') if path.is_file() and path != plan_file}
    actual |= {'.github/workflows/raw-onload-original-observations.yml', 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py'}
    require(len(plan['Files']) == 19 and actual == {record['Path'] for record in plan['Files']}, 'Exact19 execution input set differs')
    for record in plan['Files']:
        checked(root, record)


def write_all(root, writes):
    for name, data in writes.items():
        require(type(data) is bytes, 'Precomputed byte output required')
        file(root, name, exists=False)
    for name, data in writes.items():
        destination = file(root, name, exists=False)
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)
        require(sha(destination.read_bytes()) == sha(data), 'Written bytes differ')


def apply(root, state, config, approved):
    require(not state.exists() and not git(root, 'diff', '--cached', '--name-only', '-z'), 'Fresh state and empty index required')
    source_names = {record['Path'] for record in config['Plans'] if record['Source']}
    tracked = set(git(root, 'diff', '--name-only', '-z').decode().split('\0')) - {''}
    require(tracked <= source_names, 'Unrelated tracked change before tooling-only apply')
    writes = {}
    for record in config['Plans']:
        target = file(root, record['Path'], exists=False)
        current_sha = sha(target.read_bytes()) if target.exists() else None
        permitted = {record['ParentSha256'], record['Sha256']} if record['Source'] else {record['ParentSha256']}
        require(current_sha in permitted, 'Parent/new source or document hash differs: ' + record['Path'])
        writes[record['Path']] = checked(PACKAGE / 'planned', record)
    for manifest_key, hash_key, count in [('ProofManifest', 'ProofManifestSha256', 170), ('CiManifest', 'CiManifestSha256', 12), ('NativeManifest', 'NativeManifestSha256', 226)]:
        manifest_path = file(root, config[manifest_key])
        require(sha(manifest_path.read_bytes()) == config[hash_key], 'Detached proof manifest differs')
        manifest_records = json.loads(manifest_path.read_bytes())
        require(len(manifest_records) == count, 'Detached proof count differs')
        for proof_record in manifest_records:
            require(proof_record['Destination'].startswith(EVIDENCE), 'Proof destination outside part304')
            proof_bytes = checked(root, proof_record, 'SourcePath')
            require(proof_record['Destination'] not in writes, 'Duplicate proof destination')
            writes[proof_record['Destination']] = proof_bytes
        writes[EVIDENCE + 'detached-manifests/' + manifest_path.name] = manifest_path.read_bytes()
    for historical_record in config['Reuse303']:
        suffix = historical_record['Path'].removeprefix('docs/validation/part-303/')
        writes[EVIDENCE + 'reused303/' + suffix] = checked(root, historical_record)
    parent_receipt = json.loads(file(root, 'docs/validation/part-303/evidence-receipt.json').read_bytes())
    parent_tests = json.loads(file(root, 'docs/validation/part-303/tests.json').read_bytes())
    require(parent_receipt['Passed'] == 6622 and parent_receipt['Failed'] == 0 and parent_receipt['ActualPostFullMainNativeWindowsPassed'] is True and parent_receipt['Registry'] == 503 and parent_receipt['ErlangPresence'] == 345, 'Actual303 runtime/native proof differs')
    require(parent_tests['Passed'] == 6622 and parent_tests['Failed'] == 0 and len(parent_tests['Tests']) == 6622 and all(test['Status'] == 'Passed' for test in parent_tests['Tests']), 'Actual303 full report differs')
    ci_guard = json.loads(file(root, 'artifacts/pack303-ci-38736661986-independent-guard.json').read_bytes())
    require(ci_guard['Head'] == config['Parent'] and ci_guard['Status'] == 'PASS_EXACT_HEAD303_CI6622_FULL' and ci_guard['Passed'] == 6622 and ci_guard['Failed'] == 0 and ci_guard['OrderedNamesExact'] is True and ci_guard['RegisteredMfas'] == 503, 'Independent published303 CI proof differs')
    linux_guard = json.loads(file(root, 'artifacts/pack303-native-combined-independent-guard.json').read_bytes())
    require(linux_guard['Head'] == config['Parent'] and linux_guard['Status'] == 'PASS_EXACT_HEAD303_LINUX_TRUSTED_SDK_AND_SEPARATE_ORIGINAL13' and linux_guard['SdkSixArgumentNativePassed'] is True and linux_guard['IndependentOriginal13TypedPassed'] is True and linux_guard['SourceInputsVerified'] == 1007 and linux_guard['OriginalAndSdkThirteenCasesCompared'] is False and linux_guard['GeneralNifAbiComplete'] is False, 'Independent303 Linux SDK/separate original13 proof differs')
    native_guard = json.loads(file(root, 'docs/validation/part-303/root-artifacts/root-part303-main-native-guard.json').read_bytes())
    require(native_guard['ExitCode'] == 0 and native_guard['SourceHashesExact'] is True and native_guard['NativeTrustedWindowsProfilePassed'] is True and native_guard['ActualGeneratedCSharpRoutePassed'] is True, 'Actual303 reused native proof differs')
    guard_package = root / config['GuardPackage']
    require(sha((guard_package / 'source-freeze.json').read_bytes()) == config['GuardPackageFreezeSha256'], 'Guard candidate freeze differs')
    author_report = json.loads((guard_package / 'local-review-same9.json').read_bytes())
    require(author_report['Status'] == 'ORIGINAL_NINE_CERTAIN_PASS_HYPOTHESIS_MATCHED' and author_report['CertainAssertionsPassed'] is True and author_report['HypothesisContradicted'] is False and author_report['SdkCompared'] is False, 'Same-raw author review differs')
    for delivery_name in ('finalize304.py', 'Run-Checks304.ps1', 'README.md', 'input-gates.json', 'source-freeze6.json', 'source-freeze.json'):
        writes[EVIDENCE + 'root-delivery/' + delivery_name] = (PACKAGE / delivery_name).read_bytes()
    receipt = {'Status': 'GUARD_REPAIR_PREPARED_ROOT_INSTALLED_CHECKS_PENDING', 'PublishedParent303Head': config['Parent'],
               'SourcePathsIncludingAttributes': 6, 'CSharpPaths': 0, 'NewExecutionInputs': 19, 'ExecutionPlanSha256': config['ExecutionPlanSha256'],
               'ObservationPlanUnchanged': '36F3A580DAAB83675014C835F5537861C894440D761479B839384B722EDD5319', 'PolicyUnchanged': '5FBDB2F091A0A1D3F55C6673B7A6B140D23A1CA2823BDE3F6865AAC8028ED957',
               'Original302WorkflowConclusion': 'failure', 'Original302RawCollected': 9, 'Original302GuardError': 'Stack frame module/function',
               'Original302SuccessfulWorkflowClaimed': False, 'AuthorAndPeerSameRawCertainPassed': True, 'AuthorAndPeerHypothesesMatched': True,
               'AuthorPeerFullReportSha256': '70FA85303FE66218A01A6CBB73B3F5553F73CED05B83DEEF4CDAE8E990D360B2',
               'InstalledLocalChecksPassed': False, 'ReadinessRegistryQueryPassed': False, 'StackControlsExpected': 16, 'DecoderControlsExpected': 35,
               'Reused303FullPassed': 6622, 'Reused303PostFullWindowsNativePassed': True, 'Reused303Ci6622Verified': True,
               'Historical303LinuxSdkNativeVerified': True, 'Historical303IndependentOriginal13Verified': True, 'Historical303NativeSourceInputs': 1007, 'Historical303Original13SdkComparisonClaimed': False,
               'Registry': 503, 'PreviousRegistry': 503, 'RegistryDelta': 0, 'ErlangMfas': 345, 'PreviousErlangMfas': 345, 'ErlangDelta': 0,
               'ReadinessBaseline': 'reused303/module-readiness.json', 'Closed0045Sha256': next(record['Sha256'] for record in config['Protected'] if record['Path'] == 'docs/changelog/0045.md'),
               'DatedChangelogVolume': '0046', 'RuntimeFullRetestedFor304': False, 'NativeRetestedFor304': False, 'OriginalReexecuted': False,
               'SdkCompared': False, 'SdkPass': False, 'GenericRuntimeParity': False, 'SourceProtocolImplementationChanged': False,
               'WorkflowRetried': False, 'PostExecuted': False, 'SameHead304CiVerified': False, 'Future304HeadInferred': False,
               'FinalizerFreezeSha256': approved.upper()}
    receipt['Files'] = [row(name.removeprefix(EVIDENCE), data) for name, data in sorted(writes.items()) if name.startswith(EVIDENCE)]
    writes[EVIDENCE + 'evidence-receipt.json'] = encoded(receipt)
    packet = root / EVIDENCE
    if packet.exists():
        for existing in packet.rglob('*'):
            if existing.is_file():
                name = existing.relative_to(root).as_posix()
                require(name in writes and existing.read_bytes() == writes[name], 'Conflicting or unreviewed existing part304 file')
    state_value = {'PackageSha256': approved.upper(), 'Writes': [row(name, data) for name, data in sorted(writes.items())], 'LocalPassed': False, 'ReadinessPassed': False, 'StageVerified': False}
    # All typed receipt, source, document and proof bytes are calculated before writes.
    write_all(root, writes)
    state.mkdir(parents=True)
    (state / 'state.json').write_bytes(encoded(state_value))
    execution_gate(root, config)
    print('Applied guarded precomputed tooling/docs/proof only; no build/test/native/original/stage/POST.')


def state_gate(root, state, config, approved):
    state_value = json.loads(file(state, 'state.json').read_bytes())
    require(state_value['PackageSha256'] == approved.upper(), 'State freeze differs')
    for record in state_value['Writes']:
        checked(root, record)
    execution_gate(root, config)
    return state_value


def result(root, state, config, approved, kind, output):
    state_value = state_gate(root, state, config, approved)
    flag = 'LocalPassed' if kind == 'local-result' else 'ReadinessPassed'
    require(not state_value[flag], 'Result already recorded')
    if kind == 'local-result':
        actual = json.loads(file(output, 'local-validation.json').read_bytes())
        require(actual['Status'] == 'LOCAL304_STACK16_DECODER35_REPORT_AND_SAME_RAW9_TYPED_REVIEW_PASS' and actual['LocalHead'] == config['Parent'] and actual['ExecutionPlanSha256'] == config['ExecutionPlanSha256'] and actual['Workflow302ConclusionStillFailure'] is True and actual['CertainAssertionsPassed'] is True and actual['HypothesisContradicted'] is False and actual['OriginalReexecuted'] is False and actual['SdkCompared'] is False, 'Actual installed local304 result differs')
        folder = 'local-tooling'
    else:
        actual = json.loads(file(output, 'module-readiness.json').read_bytes())
        require(actual == json.loads(file(root, 'docs/module-readiness.json').read_bytes()), 'Actual readiness CLI JSON differs')
        require(file(output, 'MODULE_READINESS.md').read_bytes() == file(root, 'docs/MODULE_READINESS.md').read_bytes(), 'Actual readiness Markdown differs')
        require(json.loads(file(output, 'command-exit.json').read_bytes())['ExitCode'] == 0 and actual['RegisteredMfas'] == 503 and actual['PreviousRegisteredMfas'] == 503 and all(module['Delta'] == 0 for module in actual['Modules']), 'Actual503 delta0 registry query required')
        folder = 'readiness-cli'
    writes = {EVIDENCE + folder + '/' + path.relative_to(output).as_posix(): path.read_bytes() for path in sorted(output.rglob('*')) if path.is_file()}
    write_all(root, writes)
    state_value['Writes'] += [row(name, data) for name, data in sorted(writes.items())]
    state_value[flag] = True
    (state / 'state.json').write_bytes(encoded(state_value))
    print('Actual ' + folder + ' result preserved; no runtime/native/original rerun claimed.')


def stage(root, state, config, approved):
    state_value = state_gate(root, state, config, approved)
    require(state_value['LocalPassed'] and state_value['ReadinessPassed'] and not state_value['StageVerified'], 'Actual local/readiness checks required before one stage')
    require(not git(root, 'diff', '--cached', '--name-only', '-z'), 'Existing index entries forbidden')
    intended = {record['Path'] for record in state_value['Writes']}
    changed = set(git(root, 'diff', '--name-only', '-z').decode().split('\0')) - {''}
    require(changed <= intended, 'Unexpected tracked changes')
    require(not any(name.startswith(('src/', 'tests/', 'artifacts/')) or 'LICENSE.local' in name or '__pycache__' in name for name in intended), 'Forbidden staging path')
    require('docs/changelog/0045.md' not in intended and 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py' not in intended, 'Immutable volume/helper must not be staged')
    require(not (state / 'stage-attempt.json').exists(), 'Existing stage attempt; inspect rather than retry')
    receipt_path = file(root, EVIDENCE + 'evidence-receipt.json')
    receipt = json.loads(receipt_path.read_bytes())
    packet_members = [path for path in (root / EVIDENCE).rglob('*') if path.is_file() and path != receipt_path]
    require({path.relative_to(root).as_posix() for path in packet_members} == {name for name in intended if name.startswith(EVIDENCE) and name != EVIDENCE + 'evidence-receipt.json'}, 'Unexpected proof member')
    receipt.update(Status='GUARD_REPAIR_INSTALLED_LOCAL16_35_REPORT_SAME9_AND_READINESS_PASS_WORKFLOW302_STILL_FAILED', InstalledLocalChecksPassed=True, ReadinessRegistryQueryPassed=True)
    receipt['Files'] = [row(path.relative_to(root / EVIDENCE).as_posix(), path.read_bytes()) for path in sorted(packet_members)]
    receipt_bytes = encoded(receipt)
    stage_records = [row(name, receipt_bytes if name == EVIDENCE + 'evidence-receipt.json' else file(root, name).read_bytes()) for name in sorted(intended)]
    nul = b''.join(record['Path'].encode() + b'\0' for record in stage_records)
    # Final receipt, complete NUL list and stage manifest are precomputed before writes.
    receipt_path.write_bytes(receipt_bytes)
    (state / 'stage-paths.nul').write_bytes(nul)
    (state / 'stage-manifest.json').write_bytes(encoded({'Files': stage_records, 'CommitPushPostRequested': False}))
    (state / 'stage-attempt.json').write_bytes(encoded({'Parent': config['Parent'], 'Paths': len(stage_records)}))
    git(root, '--literal-pathspecs', 'add', '--force', '--pathspec-from-file=' + str(state / 'stage-paths.nul'), '--pathspec-file-nul')
    actual = set(git(root, 'diff', '--cached', '--name-only', '-z').decode().split('\0')) - {''}
    require(actual == intended, 'Actual staged paths differ')
    blobs = []
    for stage_record in stage_records:
        name = stage_record['Path']
        data = file(root, name).read_bytes()
        binary = name.startswith((EVIDENCE, TOOL + '/'))
        expected = data if binary else data.replace(b'\r\n', b'\n')
        staged = git(root, 'show', ':' + name)
        require(staged == expected, 'Actual staged blob differs: ' + name)
        blobs.append({'Path': name, 'Bytes': len(staged), 'Sha256': sha(staged), 'Mode': 'BinaryExact' if binary else 'TextLF'})
    state_value['Writes'] = [row(record['Path'], receipt_bytes) if record['Path'] == EVIDENCE + 'evidence-receipt.json' else record for record in state_value['Writes']]
    state_value['StageVerified'] = True
    (state / 'state.json').write_bytes(encoded(state_value))
    (state / 'stage-result.json').write_bytes(encoded({'Status': 'EXACT_NUL_FORCE_STAGE_ALL_BLOBS_PASS', 'Paths': len(blobs), 'Blobs': blobs, 'Committed': False, 'Pushed': False, 'PostExecuted': False}))
    print('Exact NUL staging and every blob verified; no commit/push/POST.')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('phase', choices=['apply', 'local-result', 'readiness-result', 'stage'])
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--state-directory', type=Path, required=True)
    parser.add_argument('--approved-package-sha256', required=True)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    require(sys.flags.optimize == 0, 'Enabled Python assertions required')
    root, state = args.repository.resolve(), args.state_directory.resolve()
    require(not state.is_relative_to(PACKAGE) and not state.is_relative_to(root / 'docs'), 'Mutable state outside frozen package/docs required')
    config = gate(root, args.approved_package_sha256)
    if args.phase == 'apply':
        apply(root, state, config, args.approved_package_sha256)
    elif args.phase == 'stage':
        stage(root, state, config, args.approved_package_sha256)
    else:
        require(args.output is not None, 'Actual output directory required')
        result(root, state, config, args.approved_package_sha256, args.phase, args.output.resolve())


if __name__ == '__main__':
    main()
