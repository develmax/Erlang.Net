import argparse
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
parser = argparse.ArgumentParser(description='Source-only execution closure seal; run after final reviewed promotion. Executes no original or SDK code.')
parser.add_argument('--repository', type=Path, required=True)
args = parser.parse_args()
root = args.repository.resolve()
tool = root / 'tools/Erlang.RawOnLoadVisibilityObservations'
spec = importlib.util.spec_from_file_location('reviewed_raw_onload_runner', tool / 'run-reviewed.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)
target = tool / runner.PLAN_NAME
runner.require(not target.exists(), 'Refuse prior execution source seal')
runner.require(runner.sha(tool / 'original/source-plan.json') == runner.OBSERVED_PLAN and runner.sha(root / runner.BASE_HELPER) == runner.BASE_SHA, 'Frozen source parents differ')
plan = {'Status': 'REVIEWED_EXECUTION_SOURCE_ONLY', 'PinnedCommit': runner.PIN, 'ObservedPlanSha256': runner.OBSERVED_PLAN,
        'OriginalExecuted': False, 'SdkCompared': False, 'NativeFixtureUsed': False,
        'Files': [runner.record(root, name) for name in sorted(runner.reviewed_paths(root))]}
runner.write_fresh(target, plan)
print(runner.sha(target))
