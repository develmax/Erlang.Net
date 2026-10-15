import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys

sys.dont_write_bytecode = True
EXECUTION_SHA = '97C447A8AD78D37EA4F092963B7D4E5A7FEF5879E55DF1E4E4739F1409D5FBA9'
FREEZE_SHA = 'A5D7FBD058265DCDFF948E04485612D35FE68F838F1FC7FDB9D5085D6FEFA3E2'
TOOL = 'tools/Erlang.RawOnLoadVisibilityObservations'


def require(value, message):
    if not value:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def local_head(root):
    return subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD'], text=True, timeout=30).strip()


def main():
    parser = argparse.ArgumentParser(description='Local tooling source closure and synthetic checks only; no CI/original/native/SDK claim.')
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    require(sys.flags.optimize == 0, 'Synthetic assertions require Python optimization disabled')
    root, output = args.repository.resolve(), args.output.resolve()
    require(re.fullmatch('[0-9a-f]{40}', args.expected_head) and local_head(root) == args.expected_head, 'Explicit current local head mismatch')
    require(not output.exists() and not output.is_relative_to(root / TOOL), 'Fresh evidence outside source closure required')
    proposal = Path(__file__).resolve().parent.parent / 'raw-onload-combined303-execution-proposal2'
    freeze_path = proposal / 'source-freeze.json'
    require(sha(freeze_path) == FREEZE_SHA, 'Frozen proposal2 manifest differs')
    freeze = json.loads(freeze_path.read_bytes())
    overlay_records = [row for row in freeze['Files'] if row['Path'].startswith('overlay/')]
    require(len(overlay_records) == 19, 'Coherent19-file source set required')
    for row in overlay_records:
        relative = row['Path'][len('overlay/'):]
        path = (root / relative).resolve()
        require(path.is_relative_to(root) and path.is_file() and path.stat().st_size == row['Bytes'] and sha(path) == row['Sha256'], 'Installed frozen source differs: ' + relative)
    tool = root / TOOL
    plan_path = tool / 'reviewed-execution-plan.json'
    require(sha(plan_path) == EXECUTION_SHA, 'Installed reviewed execution seal differs')
    # Verify the runner bytes BEFORE importing trusted pure source helpers.
    spec = importlib.util.spec_from_file_location('local_reviewed_source_functions', tool / 'run-reviewed.py')
    runner = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(runner)
    plan = json.loads(plan_path.read_bytes())
    require(len(plan['Files']) == 18 and {row['Path'] for row in plan['Files']} == runner.reviewed_paths(root), 'Exact local source closure differs')
    require(all(runner.record(root, row['Path']) == row for row in plan['Files']), 'Local closure hash/length differs')
    output.mkdir(parents=True)
    environment = dict(os.environ)
    environment['PYTHONDONTWRITEBYTECODE'] = '1'
    environment.pop('PYTHONOPTIMIZE', None)
    commands = [
        ('decoder35', [sys.executable, str(tool / 'guard/check-passive-decoder.py'), str(output / 'decoder35.json')]),
        ('report-budget', [sys.executable, str(tool / 'guard/check-report-budget.py'), str(output / 'report-budget')]),
    ]
    for name, argv in commands:
        result = subprocess.run(argv, cwd=root, env=environment, capture_output=True, timeout=120)
        (output / (name + '.stdout')).write_bytes(result.stdout)
        (output / (name + '.stderr')).write_bytes(result.stderr)
        (output / (name + '.process.json')).write_bytes((json.dumps({'Arguments': argv, 'ExitCode': result.returncode, 'TimedOut': False, 'SyntheticOnly': True}, indent=2) + '\n').encode())
        require(result.returncode == 0, 'Synthetic source control failed: ' + name)
    decoded = json.loads((output / 'decoder35.json').read_bytes())
    budget = json.loads((output / 'report-budget/report.json').read_bytes())
    require(decoded['Count'] == 35 and all(row['Passed'] is True for row in decoded['Cases']) and decoded['OriginalExecuted'] is False and decoded['SdkCompared'] is False, 'Expected35 synthetic decoder results')
    require(budget['Status'] == 'SYNTHETIC_REPORT_SERIALIZATION_BUDGET_PASS' and budget['OriginalExecuted'] is False and budget['SdkCompared'] is False, 'Expected synthetic report-budget result')
    require(local_head(root) == args.expected_head and runner.reviewed_paths(root) == {row['Path'] for row in plan['Files']} and all(runner.record(root, row['Path']) == row for row in plan['Files']), 'Source/head changed during local checks')
    report = {'Status': 'LOCAL_REVIEWED_SOURCE_AND_SYNTHETIC_CONTROLS_PASS', 'LocalHead': args.expected_head,
              'ExecutionPlanSha256': EXECUTION_SHA, 'FrozenProposal2Sha256': FREEZE_SHA, 'ExactSourceRecords': 18, 'DecoderControlsPassed': 35,
              'ReportBudgetControlsPassed': True, 'CiIdentityVerified': False, 'RemoteHeadVerified': False, 'OriginalExecuted': False,
              'RawCaptureControlsExecuted': False, 'SdkExecuted': False, 'SdkCompared': False, 'NativeExecuted': False,
              'RuntimeRetested': False, 'WorkflowDispatched': False}
    (output / 'local-validation.json').write_bytes((json.dumps(report, indent=2) + '\n').encode())
    print(json.dumps(report))


if __name__ == '__main__':
    main()
