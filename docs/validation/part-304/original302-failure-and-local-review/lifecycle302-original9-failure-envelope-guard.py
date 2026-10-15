import argparse
import ast
import datetime
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
import zipfile

sys.dont_write_bytecode = True
HEAD = 'ce3636a9d17a4d1e1c979c57b4df3d0800b72134'
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
EXECUTION = '97C447A8AD78D37EA4F092963B7D4E5A7FEF5879E55DF1E4E4739F1409D5FBA9'
SNAPSHOT = 'A584FF8BFF2A6BF8C6A43C3FE9AFC27ED70DEBE2978DDE66A72AC424E5831068'
TOOL = 'tools/Erlang.RawOnLoadVisibilityObservations'


def require(value, message):
    if not value:
        raise RuntimeError(message)


def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def member(root, relative):
    path = PurePosixPath(relative)
    require(not path.is_absolute() and '..' not in path.parts and '\\' not in relative, 'Unsafe relative evidence path')
    resolved = (root / relative).resolve()
    require(resolved.is_relative_to(root) and resolved.is_file(), 'Missing or escaping evidence member')
    return resolved


def published_contracts(artifacts):
    contracts = artifacts / 'lifecycle302-published-contracts'
    snapshot_path = contracts / 'published-source-snapshot.json'
    require(sha(snapshot_path) == SNAPSHOT, 'Published source snapshot differs')
    snapshot = load(snapshot_path)
    require(snapshot['Head'] == HEAD and snapshot['ExecutionPlanSha256'] == EXECUTION, 'Published authority differs')
    for row in snapshot['Files']:
        path = member(contracts, row['Path'])
        require(path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], 'Published source bytes differ')
    require(sha(member(contracts, TOOL + '/reviewed-execution-plan.json')) == EXECUTION, 'Execution plan drift')
    plan = load(contracts / TOOL / 'reviewed-execution-plan.json')
    require(len(plan['Files']) == 18, 'Expected eighteen reviewed execution inputs')
    for row in plan['Files']:
        path = member(contracts, row['Path'])
        require(path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], 'Published execution input drift')
    return contracts


def terminal_envelope(directory, run_id, kind):
    run, jobs, artifacts = load(directory / 'run.json'), load(directory / 'jobs.json')['jobs'], load(directory / 'artifacts.json')['artifacts']
    expected_name = 'Validate CLR implementation' if kind == 'ci' else 'Pinned raw on-load visibility original observations'
    require(run['id'] == run_id and run['name'] == expected_name and run['head_sha'] == HEAD and run['run_attempt'] == 1, 'Run id/name/head/attempt differs')
    require(kind == 'original9' and run['status'] == 'completed' and run['conclusion'] == 'failure', 'Expected historical FAILED original9 workflow')
    require(len(jobs) == len(artifacts) == 1, 'Exact single job/artifact expected')
    job, artifact = jobs[0], artifacts[0]
    require(job['head_sha'] == HEAD and job['status'] == 'completed' and job['conclusion'] == 'failure', 'Expected historical failed exact-head job')
    require([step['name'] for step in job['steps'] if step['conclusion'] == 'failure'] == ['Verify original certain assertions and report separate hypothesis outcomes'], 'Failure is not the isolated typed-review step')
    require(all(step['conclusion'] in ('success', 'skipped', 'failure') for step in job['steps']), 'Unexpected cancelled/incomplete step')
    require(artifact['workflow_run']['head_sha'] == HEAD and artifact['workflow_run']['id'] == run_id and not artifact['expired'], 'Artifact run/head/expiry differs')
    require(artifact['name'] == ('validation' if kind == 'ci' else 'raw-onload-original-nine-observations'), 'Unexpected artifact name')
    archive = directory / ('artifact-' + str(artifact['id']) + '.zip')
    extracted = directory / ('artifact-' + str(artifact['id']))
    require(archive.stat().st_size == artifact['size_in_bytes'] and artifact['digest'] == 'sha256:' + sha(archive).lower(), 'Authenticated artifact length/digest differs')
    with zipfile.ZipFile(archive) as source:
        archive_members = [item for item in source.infolist() if not item.is_dir()]
        names = [item.filename for item in archive_members]
        require(len(names) == len(set(names)), 'Duplicate ZIP member')
        require(set(names) == {path.relative_to(extracted).as_posix() for path in extracted.rglob('*') if path.is_file()}, 'Exact extracted archive closure differs')
        for item in archive_members:
            path = member(extracted, item.filename)
            require(path.stat().st_size == item.file_size and path.read_bytes() == source.read(item), 'Extracted bytes differ from authenticated ZIP')
    manifest = load(directory / 'download-manifest.json')
    normalized = [row['Path'].replace('\\', '/') for row in manifest]
    require(len(normalized) == len(set(normalized)) and set(normalized) == {path.relative_to(directory).as_posix() for path in directory.rglob('*') if path.is_file() and path.name != 'download-manifest.json'}, 'Exact downloaded envelope closure differs')
    for row, name in zip(manifest, normalized):
        path = member(directory, name)
        require(path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], 'Download receipt hash/length differs')
    raw_job = member(directory, 'job-' + str(job['id']) + '.log')
    return run, job, artifact, extracted, {'Head': HEAD, 'Run': run_id, 'Job': job['id'], 'Artifact': artifact['id'], 'Attempt': 1,
                                          'RawZipSha256': sha(archive), 'RawJobSha256': sha(raw_job), 'AuthenticatedApiEnvelopeAndZipVerified': True}


def find_one(root, filename):
    paths = [path for path in root.rglob(filename) if path.is_file()]
    require(len(paths) == 1, 'Expected exactly one artifact member: ' + filename)
    return paths[0]


def verify_ci(extracted, contracts, result):
    tests = load(find_one(extracted, 'tests.json'))
    expected = load(contracts / 'docs/validation/part-301/tests.json')
    require(tests['Passed'] == 6621 and tests['Failed'] == 0 and len(tests['Tests']) == 6621 and all(test['Status'] == 'Passed' for test in tests['Tests']), 'CI full test result differs')
    names = [test['Name'] for test in tests['Tests']]
    require(len(set(names)) == 6621 and names == [test['Name'] for test in expected['Tests']] and sum(name.startswith('mfa/') for name in names) == 503, 'Historical301 ordered names/MFA count differ')
    integration = load(find_one(extracted, 'integration-results.json'))
    require(integration == load(contracts / 'docs/validation/part-301/integration-results.json') and len(integration) == 11, 'Eleven integration/package outcomes differ')
    result.update(Status='PASS_EXACT_HEAD302_CI6621_FULL_UNCHANGED_RUNTIME', Passed=6621, Failed=0, OrderedNamesExactTo301=True, RegisteredMfas=503,
                  Integration=integration, OriginalNineVerified=False, SdkOriginalCompared=False, MainEdited=False, DispatchOrRerun=False)


def verify_original(extracted, contracts, artifacts, run, job, result, report_path):
    require(run['event'] == 'workflow_dispatch' and run['head_branch'] == 'main', 'Original observation run must be controlled-main manual dispatch')
    attempt_path, accepted_path = artifacts / 'root-part302-original9-dispatch-attempt.json', artifacts / 'root-part302-original9-dispatch-result.json'
    attempt, accepted = load(attempt_path), load(accepted_path)
    require(attempt['Stage'] == 'BEFORE_SINGLE_POST' and accepted['Stage'] == 'SINGLE_POST_ACCEPTED' and all(row['Head'] == HEAD and row['Workflow'] == run['workflow_id'] and row['ApprovedExecutionPlanSha256'] == EXECUTION for row in (attempt, accepted)), 'Recorded single dispatch/head/workflow/input differs')
    attempted_at = datetime.datetime.fromisoformat(attempt['Time'].replace('Z', '+00:00'))
    created_at = datetime.datetime.fromisoformat(run['created_at'].replace('Z', '+00:00'))
    require(datetime.timedelta(seconds=-15) <= created_at - attempted_at <= datetime.timedelta(minutes=5), 'Actual run not bound to recorded single dispatch time')
    remote_report_path = find_one(extracted, 'original-nine-report.json')
    observation_root = remote_report_path.parent
    remote_report = load(remote_report_path)
    preflight = load(member(observation_root, 'reviewed-inputs.json'))
    provenance_path = member(observation_root, 'build-provenance.json')
    provenance = load(provenance_path)
    require(preflight['ExecutionPlanSha256'] == EXECUTION and preflight['ObservedPlanSha256'] == '36F3A580DAAB83675014C835F5537861C894440D761479B839384B722EDD5319' and preflight['HarnessCommit'] == HEAD, 'Executed input preflight binding differs')
    require(provenance['SourceHead'] == PIN and provenance['ExecutionPlanSha256'] == EXECUTION and provenance['HarnessCommit'] == HEAD and str(provenance['RunId']) == str(run['id']) and str(provenance['RunAttempt']) == '1' and provenance['Job'] == 'original-raw-onload-visibility', 'Source-built tool/CI identity differs')
    require(provenance['OriginalCasesExecuted'] is False and provenance['NativeFixtureUsed'] is False and provenance['CiIdentityNetworkAuthenticated'] is False, 'Build provenance scope differs')
    expected_tools = ['observer-install/bin/erl', 'observer-install/bin/erlc', 'observer-install/lib/erlang/erts-17.1/bin/beam.smp', 'observer-install/lib/erlang/erts-17.1/bin/erlexec']
    require([row['Path'] for row in provenance['Files']] == expected_tools and all(row['Bytes'] > 0 and re.fullmatch('[0-9A-F]{64}', row['Sha256']) for row in provenance['Files']), 'Four actual installed tool identities required')
    observed_plan = load(contracts / TOOL / 'original/source-plan.json')
    require(provenance['PinnedReferenceSources'] == observed_plan['PinnedSources'], 'Pinned original source blob identities differ')
    syntax = ast.parse((contracts / TOOL / 'run-reviewed.py').read_bytes())
    headers = next(ast.literal_eval(node.value) for node in syntax.body if isinstance(node, ast.Assign) and any(isinstance(target, ast.Name) and target.id == 'HEADERS' for target in node.targets))
    require([(row['Name'], row['GitPath'], row['Sha256']) for row in provenance['InstalledPinnedPublicHeaders']] == headers and all(row['Bytes'] > 0 for row in provenance['InstalledPinnedPublicHeaders']), 'Installed pinned public header identities differ')
    configured = provenance['ConfiguredSizeHeader']
    require(configured['Path'] == 'observer-install/lib/erlang/usr/include/erl_int_sizes_config.h' and 0 < configured['Bytes'] < 65536 and re.fullmatch('[0-9A-F]{64}', configured['Sha256']), 'Actual configured size header receipt absent')
    require(PIN in member(observation_root, 'original-build.log').read_text(encoding='utf-8', errors='replace'), 'Raw source-build pin log absent')
    synthetic = load(member(observation_root, 'synthetic-decoder.json'))
    require(synthetic['Count'] == 35 and all(row['Passed'] is True for row in synthetic['Cases']) and synthetic['OriginalExecuted'] is False and synthetic['SdkCompared'] is False, 'Separate passive synthetic controls not complete')
    require(load(member(observation_root, 'synthetic-report/report.json'))['Status'] == 'SYNTHETIC_REPORT_SERIALIZATION_BUDGET_PASS', 'Synthetic report-budget controls absent')
    typed_path = Path(str(report_path) + '.typed-rerun.json')
    require(not typed_path.exists(), 'Fresh independent typed rerun output required')
    guard = contracts / TOOL / 'guard/verify-original9.py'
    argv = [sys.executable, str(guard), '--observations', str(observation_root / 'observations'), '--proposal', str(contracts / TOOL / 'original'), '--build-provenance', str(provenance_path),
            '--base-guard', str(contracts / 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py'), '--decoder', str(contracts / TOOL / 'guard/passive_etf.py'), '--controls', str(observation_root / 'capture-controls'),
            '--report', str(typed_path), '--expected-head', HEAD, '--expected-run', str(run['id']), '--expected-attempt', '1', '--expected-job', 'original-raw-onload-visibility']
    environment = dict(os.environ)
    environment['PYTHONDONTWRITEBYTECODE'] = '1'
    environment.pop('PYTHONOPTIMIZE', None)
    completed = subprocess.run(argv, env=environment, capture_output=True, timeout=120)
    Path(str(report_path) + '.typed-rerun.stdout').write_bytes(completed.stdout)
    Path(str(report_path) + '.typed-rerun.stderr').write_bytes(completed.stderr)
    Path(str(report_path) + '.typed-rerun.process.json').write_bytes((json.dumps({'Arguments': argv, 'ExitCode': completed.returncode, 'PassiveTypedReviewOnly': True, 'OriginalExecutedLocally': False}, indent=2) + '\n').encode())
    require(completed.returncode == 0, 'Independent typed certain assertions refused/failed; retain raw/typed evidence')
    independent = load(typed_path)
    keys = ('Status', 'CertainAssertionsPassed', 'HypothesisContradicted', 'PinnedCommit', 'PlanSha256', 'PolicySha256', 'RawReceiptSha256', 'BuildProvenanceSha256', 'ExplicitExpectedCiIdentity', 'CaptureControlsPassed')
    require(all(remote_report[key] == independent[key] for key in keys), 'Remote and independent typed result differ')
    require(len(remote_report['Cases']) == len(independent['Cases']) == 9, 'Complete nine typed cases required')
    for remote_case, independent_case in zip(remote_report['Cases'], independent['Cases']):
        term_keys = ('Name', 'CertainAssertions', 'CertainPassed', 'Hypothesis', 'RawEtfSha256', 'FullPassiveTypedObservation', 'RawStackAndFunIdentityRetained')
        require(all(remote_case[key] == independent_case[key] for key in term_keys), 'Full raw term/stack/fun projections differ across passive rerun')
    require(independent['CertainAssertionsPassed'] is True and all(independent[key] is False for key in ('SdkCompared', 'SdkPass', 'GenericRuntimeParity', 'OriginalExecutedByGuard', 'CiIdentityNetworkAuthenticated')), 'No SDK/general parity or local original execution claim allowed')
    result.update(Status='PASS_ORIGINAL302_NINE_CERTAIN_HYPOTHESIS_CONTRADICTED' if independent['HypothesisContradicted'] else 'PASS_ORIGINAL302_NINE_CERTAIN_HYPOTHESIS_MATCHED',
                  CertainAssertionsPassed=True, HypothesisContradicted=independent['HypothesisContradicted'], Cases=[{'Name': row['Name'], 'CertainPassed': row['CertainPassed'], 'Hypothesis': row['Hypothesis']} for row in independent['Cases']],
                  SourceBuiltPinnedOtp=PIN, InstalledToolHeaderSourceReceiptsVerified=True, ApprovedExecutionPlanSha256=EXECUTION, PublishedExecutionInputsVerified=18,
                  SingleDispatchAttemptSha256=sha(attempt_path), SingleDispatchAcceptedSha256=sha(accepted_path), RemoteTypedReportSha256=sha(remote_report_path), IndependentTypedReportSha256=sha(typed_path),
                  FullPassiveTermStackFunValuesExact=True, RawEtfBytesPreserved=True, ProvenanceAbsolutePathsMayDiffer=True, NoTermNormalization=True,
                  SourceAlgorithmCorrectionClaimed=False, SdkCompared=False, SdkPass=False, GenericRuntimeParity=False, OriginalExecutedLocally=False, NativeExecutedLocally=False,
                  MainEdited=False, GitMutated=False, DispatchOrRerun=False)


def verify_failed_original(extracted, contracts, artifacts, run, job, result, report_path):
    require(run['event'] == 'workflow_dispatch' and run['head_branch'] == 'main', 'Controlled-main manual dispatch required')
    attempt_path = artifacts / 'root-part302-original9-dispatch-attempt.json'
    accepted_path = artifacts / 'root-part302-original9-dispatch-result.json'
    attempt, accepted = load(attempt_path), load(accepted_path)
    require(attempt['Stage'] == 'BEFORE_SINGLE_POST' and accepted['Stage'] == 'SINGLE_POST_ACCEPTED' and all(row['Head'] == HEAD and row['Workflow'] == run['workflow_id'] and row['ApprovedExecutionPlanSha256'] == EXECUTION for row in (attempt, accepted)), 'Single dispatch identity/input mismatch')
    attempted_at = datetime.datetime.fromisoformat(attempt['Time'].replace('Z', '+00:00'))
    created_at = datetime.datetime.fromisoformat(run['created_at'].replace('Z', '+00:00'))
    require(datetime.timedelta(seconds=-15) <= created_at - attempted_at <= datetime.timedelta(minutes=5), 'Recorded dispatch time mismatch')
    remote_report_path = find_one(extracted, 'original-nine-report.json')
    remote_report, observation_root = load(remote_report_path), remote_report_path.parent
    require(remote_report['Status'] == 'ORIGINAL_NINE_GUARD_REFUSED_OR_FAILED' and remote_report['ErrorType'] == 'ValueError' and remote_report['Error'] == 'Stack frame module/function' and remote_report['SdkCompared'] is False, 'Historical schema failure changed')
    preflight = load(member(observation_root, 'reviewed-inputs.json'))
    require(preflight['ExecutionPlanSha256'] == EXECUTION and preflight['ObservedPlanSha256'] == '36F3A580DAAB83675014C835F5537861C894440D761479B839384B722EDD5319' and preflight['HarnessCommit'] == HEAD, 'Execution seal/source preflight drift')
    provenance = load(member(observation_root, 'build-provenance.json'))
    require(provenance['SourceHead'] == PIN and provenance['ExecutionPlanSha256'] == EXECUTION and provenance['HarnessCommit'] == HEAD and str(provenance['RunId']) == str(run['id']) and str(provenance['RunAttempt']) == '1' and provenance['Job'] == 'original-raw-onload-visibility', 'Original pin/build identity differs')
    expected_tools = ['observer-install/bin/erl', 'observer-install/bin/erlc', 'observer-install/lib/erlang/erts-17.1/bin/beam.smp', 'observer-install/lib/erlang/erts-17.1/bin/erlexec']
    require([row['Path'] for row in provenance['Files']] == expected_tools and all(row['Bytes'] > 0 and re.fullmatch('[0-9A-F]{64}', row['Sha256']) for row in provenance['Files']), 'Four installed tool identities required')
    plan = load(contracts / TOOL / 'original/source-plan.json')
    require(provenance['PinnedReferenceSources'] == plan['PinnedSources'], 'Pinned source receipts drift')
    syntax = ast.parse((contracts / TOOL / 'run-reviewed.py').read_bytes())
    headers = next(ast.literal_eval(node.value) for node in syntax.body if isinstance(node, ast.Assign) and any(isinstance(target, ast.Name) and target.id == 'HEADERS' for target in node.targets))
    require([(row['Name'], row['GitPath'], row['Sha256']) for row in provenance['InstalledPinnedPublicHeaders']] == headers, 'Installed pinned header receipts drift')
    configured = provenance['ConfiguredSizeHeader']
    require(configured['Path'] == 'observer-install/lib/erlang/usr/include/erl_int_sizes_config.h' and 0 < configured['Bytes'] < 65536 and re.fullmatch('[0-9A-F]{64}', configured['Sha256']), 'Configured header receipt missing')
    import importlib.util
    helper = contracts / 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py'
    spec = importlib.util.spec_from_file_location('failed302_verified_helpers', helper)
    base = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(base)
    observations = observation_root / 'observations'
    receipt = load(member(observations, 'receipt.json'))
    require(receipt['Complete'] is True and receipt['SemanticAssertionsExecuted'] is False and receipt['SdkExecuted'] is False and receipt['SdkCompared'] is False and receipt['NativeFixtureUsed'] is False and receipt['ExpectedFreshVmCases'] == 9 and receipt['OrderedFreshVmCases'] == [row['Name'] for row in plan['Cases']], 'Raw collection scope/count differs')
    members = base.verify_members(observations, receipt['Files'])
    require({path.relative_to(observations).as_posix() for path in observations.rglob('*') if path.is_file()} == members | {'receipt.json'}, 'Exact original raw closure differs')
    erl = receipt['Tools'][0]['Path']
    for tool_record, built in zip(receipt['Tools'], provenance['Files'][:2]):
        require(tool_record['Sha256'] == built['Sha256'] and tool_record['Path'].endswith('/' + built['Path']), 'Collector tool not bound to actual source build')
    pids = [base.process(observations, row['Name'] + '/observation', erl) for row in plan['Cases']]
    require(len(set(pids)) == 9, 'Nine distinct successful fresh VM processes required')
    result.update(Status='VERIFIED_FAILED302_WORKFLOW_ENVELOPE_AND_COMPLETE_RAW9_NO_TYPED_PASS', WorkflowConclusion='failure', TypedReviewConclusion='failure',
                  GuardError='Stack frame module/function', OriginalRawCasesCollected=9, CertainAssertionsVerifiedByThisFailureEnvelope=False,
                  OriginalNineWorkflowPassed=False, PublishedExecutionInputsVerified=18, SourceBuiltPinnedOtp=PIN, InstalledToolHeaderSourceReceiptsVerified=True,
                  SingleDispatchAttemptSha256=sha(attempt_path), SingleDispatchAcceptedSha256=sha(accepted_path), RemoteFailureReportSha256=sha(remote_report_path), RawReceiptSha256=sha(observations / 'receipt.json'),
                  SdkCompared=False, SdkPass=False, GenericRuntimeParity=False, OriginalExecutedLocally=False, NativeExecutedLocally=False, MainEdited=False, GitMutated=False, DispatchOrRerun=False)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('kind', choices=['original9'])
    parser.add_argument('run_id', type=int)
    parser.add_argument('--artifacts', type=Path, default=Path('artifacts'))
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    require(sys.flags.optimize == 0 and not args.report.exists(), 'Enabled assertions/fresh report required')
    artifacts = args.artifacts.resolve()
    report_path = args.report.resolve()
    result = {'Status': 'TERMINAL_ENVELOPE_OR_TYPED_REVIEW_REFUSED', 'Head': HEAD, 'Kind': args.kind, 'Run': args.run_id, 'SdkCompared': False, 'DispatchOrRerun': False}
    try:
        contracts = published_contracts(artifacts)
        run, job, item, extracted, envelope = terminal_envelope(artifacts / ('lifecycle302-watch-' + str(args.run_id) + '-terminal'), args.run_id, args.kind)
        result.update(envelope)
        if args.kind == 'ci':
            verify_ci(extracted, contracts, result)
        else:
            verify_failed_original(extracted, contracts, artifacts, run, job, result, report_path)
    except Exception as error:
        result.update(ErrorType=type(error).__name__, Error=str(error))
        report_path.write_bytes((json.dumps(result, indent=2) + '\n').encode())
        raise
    report_path.write_bytes((json.dumps(result, indent=2) + '\n').encode())
    print(json.dumps(result))


if __name__ == '__main__':
    main()
