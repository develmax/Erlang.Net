import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess
import sys

sys.dont_write_bytecode = True
PACKAGE = Path(__file__).resolve().parent
PARENT = 'ca33514ba537688aaa8a265200b8d270a01f98fb'
EXECUTION = '97C447A8AD78D37EA4F092963B7D4E5A7FEF5879E55DF1E4E4739F1409D5FBA9'
EVIDENCE = 'docs/validation/part-302/'


def require(value, message):
    if not value:
        raise RuntimeError(message)


def sha_bytes(data):
    return hashlib.sha256(data).hexdigest().upper()


def sha(path):
    return sha_bytes(path.read_bytes())


def encoded(value):
    return (json.dumps(value, indent=2, ensure_ascii=False) + '\n').encode()


def member(root, relative, exists=True):
    parts = PurePosixPath(relative)
    require(not parts.is_absolute() and '..' not in parts.parts and '\\' not in relative, 'Unsafe relative path')
    result = (root / relative).resolve()
    require(result.is_relative_to(root) and (not exists or result.is_file()), 'Missing or escaping file: ' + relative)
    return result


def git(root, *argv, input_data=None):
    return subprocess.check_output(['git', '-C', str(root), *argv], input=input_data, timeout=60)


def row_for(relative, data):
    return {'Path': relative, 'Bytes': len(data), 'Sha256': sha_bytes(data)}


def verify_row(root, row):
    path = member(root, row['Path'])
    require(path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], 'Bound input bytes differ: ' + row['Path'])


def package_gate(approved):
    freeze_path = PACKAGE / 'source-freeze.json'
    require(sha(freeze_path) == approved.upper(), 'Explicit reviewed finalizer freeze mismatch')
    freeze = json.loads(freeze_path.read_bytes())
    for package_record in freeze['Files']:
        verify_row(PACKAGE, package_record)
    return json.loads((PACKAGE / 'input-gates.json').read_bytes())


def source_gate(root, before=False):
    source_plan = json.loads((PACKAGE / 'source-freeze19.json').read_bytes())
    for source_record in source_plan['Files']:
        source_path = member(root, source_record['Path'])
        expected = source_record.get('ParentSha256') if before and source_record['Path'] == '.gitattributes' else source_record['Sha256']
        require(sha(source_path) == expected, 'Exact source19 differs: ' + source_record['Path'])
    require(sha(member(root, 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py')) == source_plan['ExistingHelperUnchangedSha256'], 'Reused helper changed')
    tool = root / 'tools/Erlang.RawOnLoadVisibilityObservations'
    execution_path = tool / 'reviewed-execution-plan.json'
    require(sha(execution_path) == EXECUTION, 'Execution seal changed')
    execution_plan = json.loads(execution_path.read_bytes())
    actual_paths = {path.relative_to(root).as_posix() for path in tool.rglob('*') if path.is_file() and path != execution_path}
    actual_paths |= {'.github/workflows/raw-onload-original-observations.yml', 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py'}
    require(len(execution_plan['Files']) == 18 and actual_paths == {record['Path'] for record in execution_plan['Files']}, 'Exact execution closure changed')
    for execution_record in execution_plan['Files']:
        verify_row(root, execution_record)
    return source_plan


def head_gate(root):
    require(git(root, 'rev-parse', 'HEAD').decode().strip() == PARENT, 'Current local head must still be published301')


def write_precomputed(root, writes):
    # All bytes and JSON values are already calculated and checked by the caller.
    for relative, content in writes.items():
        require(type(content) is bytes, 'Every planned write must be precomputed bytes')
        member(root, relative, exists=False)
    for relative, content in writes.items():
        destination = member(root, relative, exists=False)
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(content)
        require(sha(destination) == sha_bytes(content), 'Write verification failed: ' + relative)


def apply(root, state, gates, approved):
    head_gate(root)
    require(not state.exists(), 'Fresh finalization state directory required')
    require(not git(root, 'diff', '--cached', '--name-only', '-z'), 'Index must be empty before finalization')
    changed = set(git(root, 'diff', '--name-only', '-z').decode().split('\0')) - {''}
    require(changed <= {'.gitattributes'}, 'Unexpected tracked work before tooling-only finalization')
    source_gate(root, before=True)
    for protected_record in gates['ProtectedInputs'] + gates['Reuse301'] + gates['LocalTooling']:
        verify_row(root, protected_record)
    writes = {}
    for document_record in gates['PlannedDocs']:
        target = member(root, document_record['Path'], exists=False)
        parent_sha = document_record['ParentSha256']
        require((not target.exists()) if parent_sha is None else target.is_file() and sha(target) == parent_sha, 'Document parent changed: ' + document_record['Path'])
        document_bytes = member(PACKAGE / 'planned-docs', document_record['Path']).read_bytes()
        require(len(document_bytes) == document_record['Bytes'] and sha_bytes(document_bytes) == document_record['Sha256'], 'Planned document differs')
        writes[document_record['Path']] = document_bytes
    # Parse all typed facts before any workspace write; never reuse output variables.
    publication = json.loads(member(root, 'artifacts/root-part302-parent301-publication.json').read_bytes())
    previous = json.loads(member(root, 'docs/validation/part-301/evidence-receipt.json').read_bytes())
    tests = json.loads(member(root, 'docs/validation/part-301/tests.json').read_bytes())
    native = json.loads(member(root, 'docs/validation/part-301/root-artifacts/root-part301-main-native-guard.json').read_bytes())
    local = json.loads(member(root, 'artifacts/root-part302-local-tooling/local-validation.json').read_bytes())
    require(publication['Head'] == PARENT and publication['Status'] == 'PUBLISHED_PARENT301_REMOTE_AND_LOCAL_HEAD_EXACT' and publication['FullPassed'] == 6621 and publication['Original301Verified'] is False, 'Actual publication receipt differs')
    require(previous['Passed'] == 6621 and previous['Failed'] == 0 and previous['ActualPostFullMainNativeWindowsPassed'] is True and tests['Passed'] == 6621 and tests['Failed'] == 0 and len(tests['Tests']) == 6621 and all(test['Status'] == 'Passed' for test in tests['Tests']), 'Actual successful301 full/native receipt required')
    require(native['ExitCode'] == 0 and native['NativeTrustedWindowsProfilePassed'] is True and native['ActualGeneratedCSharpRoutePassed'] is True, 'Post-full native proof required')
    require(local['Status'] == 'LOCAL_REVIEWED_SOURCE_AND_SYNTHETIC_CONTROLS_PASS' and local['LocalHead'] == PARENT and local['ExecutionPlanSha256'] == EXECUTION and local['ExactSourceRecords'] == 18 and local['DecoderControlsPassed'] == 35 and local['ReportBudgetControlsPassed'] is True and local['OriginalExecuted'] is False and local['RuntimeRetested'] is False, 'Actual local302 proof required')
    raw_path = member(root, gates['RawManifestPath'])
    require(sha(raw_path) == gates['RawManifestSha256'], 'Raw provenance manifest differs')
    raw_manifest = json.loads(raw_path.read_bytes())
    require(len(raw_manifest['Files']) == 95, 'Expected95 raw provenance files')
    for raw_record in raw_manifest['Files']:
        raw_bytes = member(root / 'artifacts', raw_record['ArtifactPath']).read_bytes()
        require(len(raw_bytes) == raw_record['Bytes'] and sha_bytes(raw_bytes) == raw_record['Sha256'], 'Raw provenance bytes differ')
        writes[EVIDENCE + 'source-proposals/' + raw_record['ArchivePath']] = raw_bytes
    writes[EVIDENCE + 'raw-provenance-manifest.json'] = raw_path.read_bytes()
    for local_record in gates['LocalTooling']:
        local_relative = local_record['Path'].removeprefix('artifacts/root-part302-local-tooling/')
        writes[EVIDENCE + 'local-tooling/' + local_relative] = member(root, local_record['Path']).read_bytes()
    for historical_record in gates['Reuse301']:
        historical_relative = historical_record['Path']
        if historical_relative.startswith('docs/validation/part-301/'):
            archive_relative = 'reused301/' + historical_relative.removeprefix('docs/validation/part-301/')
        else:
            archive_relative = 'parent301-and-dispatch/' + historical_relative.removeprefix('artifacts/')
        writes[EVIDENCE + archive_relative] = member(root, historical_relative).read_bytes()
    ci_binding = gates['Historical301Ci']
    require(sha(member(root, ci_binding['ManifestPath'])) == ci_binding['ManifestSha256'] and len(ci_binding['Files']) == 12, 'Historical301 CI manifest differs')
    for ci_record in ci_binding['Files']:
        ci_bytes = member(root, ci_record['SourcePath']).read_bytes()
        require(len(ci_bytes) == ci_record['Bytes'] and sha_bytes(ci_bytes) == ci_record['Sha256'], 'Historical301 CI bytes differ')
        require(ci_record['Destination'].startswith(EVIDENCE + 'previous-head301-ci/'), 'Historical CI destination escapes packet')
        writes[ci_record['Destination']] = ci_bytes
    ci_guard_record = next(record for record in ci_binding['Files'] if record['SourcePath'].endswith('pack301-ci-38732620778-independent-guard.json'))
    ci_guard = json.loads(member(root, ci_guard_record['SourcePath']).read_bytes())
    require(ci_guard['Head'] == PARENT and ci_guard['Status'] == 'PASS_EXACT_HEAD301_CI6621_FULL' and ci_guard['Passed'] == 6621 and ci_guard['Failed'] == 0 and ci_guard['RegisteredMfas'] == 503, 'Historical exact-head301 CI guard differs')
    writes[EVIDENCE + 'previous-head301-ci/detached-staging-manifest.json'] = member(root, ci_binding['ManifestPath']).read_bytes()
    for delivery_name in ('finalize302.py', 'Run-Readiness302.ps1', 'README.md', 'input-gates.json', 'source-freeze19.json', 'source-freeze.json'):
        writes[EVIDENCE + 'root-delivery/' + delivery_name] = (PACKAGE / delivery_name).read_bytes()
    receipt = {'Status': 'TOOLING_ONLY_LOCAL_CLOSURE35_REPORT_PASS_ORIGINAL9_PENDING_READINESS_CLI_PENDING',
               'PublishedParent301Head': PARENT, 'ParentPublicationReceiptLoaded': True, 'RemoteVerifiedByThisScript': False,
               'Reused301FullPassed': 6621, 'Reused301PostFullTrustedNativePassed': True, 'RuntimeRetestedFor302': False, 'NativeRetestedFor302': False,
               'SourcePathsIncludingAttributes': 19, 'NewToolingWorkflowFiles': 18, 'ExactExecutionInputs': 18, 'ExecutionPlanSha256': EXECUTION,
               'ObservedPlanSha256': '36F3A580DAAB83675014C835F5537861C894440D761479B839384B722EDD5319',
               'LocalDecoderControlsPassed': 35, 'LocalReportBudgetControlsPassed': True, 'LocalCiIdentityVerified': False,
               'RegisteredMfas': 503, 'PreviousRegisteredMfas': 503, 'RegisteredMfaDelta': 0, 'ErlangMfas': 345, 'PreviousErlangMfas': 345, 'ErlangMfaDelta': 0,
               'ReadinessBaseline': 'reused301/module-readiness.json', 'ReadinessComparisonTestReport': '../part-301/tests.json',
               'ReadinessRegistryQueryVerified': False, 'Closed0045Sha256': gates['ClosedVolume0045Sha256'], 'ActiveChangelogVolume': '0046',
               'FinalizerPackageFreezeSha256': approved.upper(), 'OriginalNineStatus': 'PENDING_NOT_EXECUTED_OR_NOT_YET_VERIFIED',
               'Original301Verified': False, 'CI301TerminalIndependentlyVerified': True, 'CI301ReusedPassed': 6621, 'SameHead302CiVerified': False,
               'Ordinary301RemoteTerminalVerified': False, 'Native301RemoteTerminalVerifiedByParent': True,
               'Native301RemoteProofArchivedHere': False, 'Native301RemoteArchiveDeferredTo': 303, 'Dispatch301ReceiptsAloneAreAcknowledgementOnly': True,
               'HistoricalGenerated9CompiledLoadedOnly': True, 'HistoricalGenerated9BodiesExecuted': False,
               'SdkCompared': False, 'SdkPass': False, 'GenericRuntimeParity': False, 'SourceProtocolCorrectionClaimed': False,
               'WorkflowDispatched': False, 'Future302HeadInferred': False}
    receipt['Files'] = [row_for(relative.removeprefix(EVIDENCE), data) for relative, data in sorted(writes.items()) if relative.startswith(EVIDENCE)]
    writes[EVIDENCE + 'evidence-receipt.json'] = encoded(receipt)
    expected_packet = {relative for relative in writes if relative.startswith(EVIDENCE)}
    existing_packet = root / EVIDENCE
    if existing_packet.exists():
        for existing_path in existing_packet.rglob('*'):
            if existing_path.is_file():
                existing_relative = existing_path.relative_to(root).as_posix()
                if existing_relative == EVIDENCE + 'archive-receipt.json':
                    archive_receipt = json.loads(existing_path.read_bytes())
                    require(archive_receipt['SourceFiles'] == 95 and archive_receipt['LocalToolingHead'] == PARENT and archive_receipt['OriginalExecuted'] is False, 'Existing archive receipt differs')
                    writes[existing_relative] = existing_path.read_bytes()
                else:
                    require(existing_relative in expected_packet and existing_path.read_bytes() == writes[existing_relative], 'Conflicting existing part302 evidence')
    state_value = {'PackageSha256': approved.upper(), 'PublishedParent': PARENT, 'Writes': [row_for(relative, content) for relative, content in sorted(writes.items())], 'ReadinessVerified': False}
    # Every copy/document/receipt byte has now been precomputed and validated.
    write_precomputed(root, writes)
    state.mkdir(parents=True)
    (state / 'state.json').write_bytes(encoded(state_value))
    source_gate(root)
    require(sha(member(root, 'docs/changelog/0045.md')) == gates['ClosedVolume0045Sha256'], 'Closed0045 changed')
    print('Applied precomputed tooling/docs evidence only; no build, test, native execution, staging or POST.')


def verify_state(root, state, approved):
    state_value = json.loads(member(state, 'state.json').read_bytes())
    require(state_value['PackageSha256'] == approved.upper() and state_value['PublishedParent'] == PARENT, 'Finalization state binding differs')
    head_gate(root)
    source_gate(root)
    for expected_write in state_value['Writes']:
        verify_row(root, expected_write)
    return state_value


def readiness(root, state, gates, approved, cli_output):
    state_value = verify_state(root, state, approved)
    require(not state_value['ReadinessVerified'], 'Readiness evidence already recorded')
    result_json = json.loads(member(cli_output, 'module-readiness.json').read_bytes())
    expected_json = json.loads(member(root, 'docs/module-readiness.json').read_bytes())
    require(result_json == expected_json, 'Actual readiness CLI JSON does not match precomputed503/345/delta0 baseline')
    require(member(cli_output, 'MODULE_READINESS.md').read_bytes() == member(root, 'docs/MODULE_READINESS.md').read_bytes(), 'Actual readiness CLI Markdown differs')
    cli_exit = json.loads(member(cli_output, 'command-exit.json').read_bytes())
    require(cli_exit['ExitCode'] == 0 and cli_exit['BuildOrTestsRun'] is False, 'Successful actual registry-only CLI query required')
    for module_row in result_json['Modules']:
        require(module_row['Delta'] == 0 and module_row['RegisteredMfas'] == module_row['PreviousRegisteredMfas'], 'Readiness registry delta changed')
    writes = {EVIDENCE + 'readiness-cli/' + path.name: path.read_bytes() for path in sorted(cli_output.iterdir()) if path.is_file()}
    guard = {'Status': 'ACTUAL_READINESS_CLI503_ERLANG345_IMMUTABLE301_BASELINE_DELTA0_PASS', 'ExitCode': 0, 'PublishedParent': PARENT,
             'RegisteredMfas': 503, 'ErlangMfas': 345, 'AllModuleDeltas': 0, 'ImmutableBaselineSha256': sha(member(root, 'docs/validation/part-301/module-readiness.json')),
             'OriginalExecuted': False, 'NativeExecuted': False, 'RuntimeFullRetested': False, 'RegistryQueryExecuted': True}
    writes[EVIDENCE + 'readiness-cli/independent-guard.json'] = encoded(guard)
    write_precomputed(root, writes)
    state_value['Writes'] += [row_for(relative, content) for relative, content in sorted(writes.items())]
    state_value['ReadinessVerified'] = True
    (state / 'state.json').write_bytes(encoded(state_value))
    print('Actual readiness CLI output independently matched; no runtime/native test rerun.')


def stage(root, state, gates, approved):
    state_value = verify_state(root, state, approved)
    require(state_value['ReadinessVerified'], 'Actual readiness CLI guard required before stage')
    require(not git(root, 'diff', '--cached', '--name-only', '-z'), 'Refuse mixing an existing index')
    receipt_path = member(root, EVIDENCE + 'evidence-receipt.json')
    final_receipt = json.loads(receipt_path.read_bytes())
    final_receipt['Status'] = 'TOOLING_ONLY_LOCAL_CLOSURE35_REPORT_READINESS_PASS_ORIGINAL9_PENDING'
    final_receipt['ReadinessRegistryQueryVerified'] = True
    packet_files = sorted(path for path in (root / EVIDENCE).rglob('*') if path.is_file() and path != receipt_path)
    expected_packet = {record['Path'] for record in state_value['Writes'] if record['Path'].startswith(EVIDENCE) and record['Path'] != EVIDENCE + 'evidence-receipt.json'}
    require({path.relative_to(root).as_posix() for path in packet_files} == expected_packet, 'Unexpected part302 evidence file before stage')
    final_receipt['Files'] = [row_for(path.relative_to(root / EVIDENCE).as_posix(), path.read_bytes()) for path in packet_files]
    receipt_bytes = encoded(final_receipt)
    source_plan = json.loads((PACKAGE / 'source-freeze19.json').read_bytes())
    intended = {record['Path'] for record in source_plan['Files']} | {record['Path'] for record in gates['PlannedDocs']} | {record['Path'] for record in state_value['Writes'] if record['Path'].startswith(EVIDENCE)}
    tracked_changes = set(git(root, 'diff', '--name-only', '-z').decode().split('\0')) - {''}
    require(tracked_changes <= intended, 'Unexpected tracked changes; no staging performed')
    require(not any(path.startswith(('src/', 'tests/')) or 'LICENSE.local' in path or '__pycache__' in path or path.startswith('artifacts/') for path in intended), 'Forbidden staging path')
    require('docs/changelog/0045.md' not in intended and sha(member(root, 'docs/changelog/0045.md')) == gates['ClosedVolume0045Sha256'], 'Closed-volume staging forbidden')
    require('tools/Erlang.NifPendingVarargsObservations/verify-original13.py' not in intended, 'Unchanged reused helper must not be staged')
    for intended_path in intended:
        member(root, intended_path)
    stage_records = []
    for intended_path in sorted(intended):
        intended_bytes = receipt_bytes if intended_path == EVIDENCE + 'evidence-receipt.json' else member(root, intended_path).read_bytes()
        stage_records.append(row_for(intended_path, intended_bytes))
    nul_bytes = b''.join(record['Path'].encode() + b'\0' for record in stage_records)
    require(not (state / 'stage-attempt.json').exists(), 'A stage attempt is already recorded; inspect index rather than retry blindly')
    # Final receipt, NUL path list and exact stage manifest are precomputed before writes.
    receipt_path.write_bytes(receipt_bytes)
    (state / 'stage-paths.nul').write_bytes(nul_bytes)
    (state / 'stage-manifest.json').write_bytes(encoded({'Status': 'EXACT_TOOLING_DOCS_STAGE_PLAN', 'Files': stage_records, 'NoCommitPushOrPost': True}))
    (state / 'stage-attempt.json').write_bytes(encoded({'Head': PARENT, 'Files': len(stage_records), 'CommitPushOrPostRequested': False}))
    git(root, '--literal-pathspecs', 'add', '--force', '--pathspec-from-file=' + str(state / 'stage-paths.nul'), '--pathspec-file-nul')
    actual_index = set(git(root, 'diff', '--cached', '--name-only', '-z').decode().split('\0')) - {''}
    require(actual_index == intended, 'Actual index differs from exact intended path set; inspect without commit')
    blob_results = []
    for staged_record in stage_records:
        staged_path = staged_record['Path']
        staged_blob = git(root, 'show', ':' + staged_path)
        working_bytes = member(root, staged_path).read_bytes()
        binary_exact = staged_path.startswith((EVIDENCE, 'tools/Erlang.RawOnLoadVisibilityObservations/')) or staged_path == '.github/workflows/raw-onload-original-observations.yml'
        expected_blob = working_bytes if binary_exact else working_bytes.replace(b'\r\n', b'\n')
        require(staged_blob == expected_blob, 'Staged blob bytes differ: ' + staged_path)
        blob_results.append({'Path': staged_path, 'BlobSha256': sha_bytes(staged_blob), 'Bytes': len(staged_blob), 'HashMode': 'BinaryExact' if binary_exact else 'TextLF'})
    state_value['Writes'] = [row_for(record['Path'], receipt_bytes) if record['Path'] == EVIDENCE + 'evidence-receipt.json' else record for record in state_value['Writes']]
    state_value['StageVerified'] = True
    (state / 'state.json').write_bytes(encoded(state_value))
    (state / 'stage-result.json').write_bytes(encoded({'Status': 'EXACT_NUL_STAGE_AND_ALL_BLOB_BYTES_PASS', 'Paths': len(actual_index), 'Blobs': blob_results, 'PublishedParent': PARENT, 'Committed': False, 'Pushed': False, 'WorkflowDispatched': False}))
    print('Exact tooling/docs paths staged; no commit, push, build or workflow dispatch.')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('phase', choices=['apply', 'readiness-result', 'stage'])
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--state-directory', type=Path, required=True)
    parser.add_argument('--approved-package-sha256', required=True)
    parser.add_argument('--cli-output', type=Path)
    args = parser.parse_args()
    require(sys.flags.optimize == 0, 'Python assertion optimization must remain disabled')
    root, state = args.repository.resolve(), args.state_directory.resolve()
    require(not state.is_relative_to(root / 'docs') and not state.is_relative_to(PACKAGE), 'Keep mutable execution state outside source package/docs')
    gates = package_gate(args.approved_package_sha256)
    if args.phase == 'apply':
        apply(root, state, gates, args.approved_package_sha256)
    elif args.phase == 'readiness-result':
        require(args.cli_output is not None, 'Actual fresh CLI output required')
        readiness(root, state, gates, args.approved_package_sha256, args.cli_output.resolve())
    else:
        stage(root, state, gates, args.approved_package_sha256)


if __name__ == '__main__':
    main()
