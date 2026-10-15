import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys

sys.dont_write_bytecode = True
HEAD302 = 'ce3636a9d17a4d1e1c979c57b4df3d0800b72134'
EXECUTION = '4D3BECB18843F6519D10BCC326979B19D350EA2D0D8A3232BF9A1BE013624273'
TOOL = 'tools/Erlang.RawOnLoadVisibilityObservations'


def require(value, message):
    if not value:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser(description='Root-only local304 guard checks on SAME failed302 archive; no original/native/workflow execution.')
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--expected-current-head', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root, output = args.repository.resolve(), args.output.resolve()
    require(sys.flags.optimize == 0 and re.fullmatch('[0-9a-f]{40}', args.expected_current_head), 'Explicit local head and enabled assertions required')
    actual_head = lambda: subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD'], text=True, timeout=30).strip()
    require(actual_head() == args.expected_current_head and not output.exists() and not output.is_relative_to(root / TOOL), 'Local head/fresh output mismatch')
    tool = root / TOOL
    plan_path = tool / 'reviewed-execution-plan.json'
    require(sha(plan_path) == EXECUTION, 'Reviewed304 execution seal differs')
    plan = json.loads(plan_path.read_bytes())
    expected = {record['Path'] for record in plan['Files']}
    actual = {path.relative_to(root).as_posix() for path in tool.rglob('*') if path.is_file() and path != plan_path} | {'.github/workflows/raw-onload-original-observations.yml', 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py'}
    require(len(plan['Files']) == len(expected) == 19 and actual == expected, 'Exact19 execution input set differs')
    for record in plan['Files']:
        path = (root / record['Path']).resolve()
        require(path.is_relative_to(root) and path.is_file() and path.stat().st_size == record['Bytes'] and sha(path) == record['Sha256'], 'Installed304 input differs')
    raw = root / 'artifacts/lifecycle302-watch-38734860285-terminal/artifact-11973920550'
    failure = json.loads((raw / 'original-nine-report.json').read_bytes())
    require(failure['Status'] == 'ORIGINAL_NINE_GUARD_REFUSED_OR_FAILED' and failure['Error'] == 'Stack frame module/function', 'Historical302 failure must remain unchanged')
    output.mkdir(parents=True)
    environment = dict(os.environ)
    environment['PYTHONDONTWRITEBYTECODE'] = '1'
    environment.pop('PYTHONOPTIMIZE', None)

    def execute(name, argv):
        result = subprocess.run(argv, cwd=root, env=environment, capture_output=True, timeout=120)
        (output / (name + '.stdout')).write_bytes(result.stdout)
        (output / (name + '.stderr')).write_bytes(result.stderr)
        (output / (name + '.process.json')).write_bytes((json.dumps({'Arguments': argv, 'ExitCode': result.returncode, 'OriginalExecuted': False, 'PassiveOrSyntheticOnly': True}, indent=2) + '\n').encode())
        require(result.returncode == 0, 'Passive/synthetic guard failed: ' + name)

    execute('stack16', [sys.executable, str(tool / 'guard/check-stack-frames.py'), str(output / 'stack16.json')])
    execute('decoder35', [sys.executable, str(tool / 'guard/check-passive-decoder.py'), str(output / 'decoder35.json')])
    execute('report-budget', [sys.executable, str(tool / 'guard/check-report-budget.py'), str(output / 'report-budget')])
    reviewed = output / 'same9-reviewed.json'
    execute('same9-review', [sys.executable, str(tool / 'guard/verify-original9.py'), '--observations', str(raw / 'observations'), '--proposal', str(tool / 'original'),
                           '--build-provenance', str(raw / 'build-provenance.json'), '--base-guard', str(root / 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py'),
                           '--decoder', str(tool / 'guard/passive_etf.py'), '--controls', str(raw / 'capture-controls'), '--report', str(reviewed),
                           '--expected-head', HEAD302, '--expected-run', '38734860285', '--expected-attempt', '1', '--expected-job', 'original-raw-onload-visibility'])
    local = json.loads(reviewed.read_bytes())
    canonical = json.loads((Path(__file__).resolve().parent / 'local-review-same9.json').read_bytes())
    require(local['CertainAssertionsPassed'] is True and local['HypothesisContradicted'] is False and len(local['Cases']) == 9, 'Same9 certain/hypothesis outcome changed')
    for actual_case, canonical_case in zip(local['Cases'], canonical['Cases']):
        for key in ('Name', 'CertainAssertions', 'CertainPassed', 'Hypothesis', 'RawEtfSha256', 'FullPassiveTypedObservation', 'RawStackAndFunIdentityRetained'):
            require(actual_case[key] == canonical_case[key], 'Full original term/stack/fun result differs')
    require(actual_head() == args.expected_current_head, 'Local head changed during passive checks')
    report = {'Status': 'LOCAL304_STACK16_DECODER35_REPORT_AND_SAME_RAW9_TYPED_REVIEW_PASS', 'LocalHead': args.expected_current_head,
              'OriginalObservationHead': HEAD302, 'ExecutionPlanSha256': EXECUTION, 'ExecutionInputs': 19, 'StackControls': 16, 'DecoderControls': 35,
              'CertainAssertionsPassed': True, 'HypothesisContradicted': False, 'Workflow302ConclusionStillFailure': True,
              'RawFailureReportSha256': sha(raw / 'original-nine-report.json'), 'LocalPassiveReportSha256': sha(reviewed),
              'NoTermStackFunNormalization': True, 'AbsoluteProvenancePathsMayDiffer': True,
              'OriginalReexecuted': False, 'NativeExecuted': False, 'SdkCompared': False, 'RuntimeFullRetested': False, 'RemoteCiIdentityVerifiedByThisScript': False,
              'GitMutated': False, 'WorkflowRetried': False, 'PostExecuted': False}
    (output / 'local-validation.json').write_bytes((json.dumps(report, indent=2) + '\n').encode())
    print(json.dumps(report))


if __name__ == '__main__':
    main()
