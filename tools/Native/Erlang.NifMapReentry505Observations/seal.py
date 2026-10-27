"""Bind actual source bytes; adopt a golden only from an explicit original-only receipt."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--original-only', action='store_true')
    parser.add_argument('--original-run', type=Path)
    args = parser.parse_args()
    root, tool = args.repository.resolve(), Path(__file__).resolve().parent
    assert tool == root/'tools/Native/Erlang.NifMapReentry505Observations'
    path = tool/'source-plan.json'
    plan = json.loads(path.read_bytes())
    assert plan['PinnedCommit'] == PIN
    for key in ['NativeSource', 'ErlangSource']:
        source = (root/plan[key+'Path']).resolve()
        assert source.is_relative_to(tool)
        plan[key+'Sha256'] = sha(source)
    plan['BuildScriptSha256'] = sha(tool/'build-native.py')
    plan['CollectorSha256'] = sha(tool/'collect.py')
    plan['SealerSha256'] = sha(__file__)
    for row in plan['Dependencies']:
        source = (root/row['Path']).resolve()
        assert source.is_relative_to(root)
        if args.original_only and row.get('When') == 'managed' and not source.is_file():
            continue
        raw = source.read_bytes()
        assert row['HashMode'] in ('Raw', 'TextLF')
        data = raw if row['HashMode'] == 'Raw' else raw.replace(b'\r\n', b'\n')
        row['Sha256'] = hashlib.sha256(data).hexdigest().upper()
    golden = tool/'original-values.etf'
    if args.original_run is not None:
        run = args.original_run.resolve()
        assert run.is_relative_to(root/'artifacts/native-map505')
        done = json.loads((run/'completion.json').read_bytes())
        assert done['Passed'] and done['OriginalOnly'] and done['Pin'] == PIN
        assert done['WholeRawLoaderComparisons'] == 0 and done['OriginalExecutionAttempted']
        assert done['NativeLibraryExecuted'] and done['ProviderBeforeEqual'] and done['ProviderAfterEqual']
        captured_plan = run/'build/source-plan.json'
        assert sha(captured_plan) == done['SourcePlanSha256']
        captured = json.loads(captured_plan.read_bytes())
        for key in ['PinnedCommit', 'NativeSourceSha256', 'ErlangSourceSha256', 'SerializationProfiles']:
            assert captured[key] == plan[key], key
        source = run/'original/returned.etf'
        assert source.stat().st_size == done['OriginalReturnedBytes']
        if golden.exists():
            assert golden.read_bytes() == source.read_bytes(), 'Existing golden must never be overwritten with different bytes'
        else:
            shutil.copyfile(source, golden)
        plan['OriginalGoldenSha256'] = sha(golden)
        plan['OriginalGoldenState'] = 'INDEPENDENT_ORIGINAL_REENTRY_PROTOCOL_ACQUIRED'
        plan['OriginalGoldenProvenance'] = dict(
            CompletionSha256=sha(run/'completion.json'),
            CapturedSourcePlanSha256=sha(captured_plan),
            ReturnedBytes=source.stat().st_size,
            Producer='Actual pinned original-only VM; no managed result used as reference'
        )
    elif 'OriginalGoldenSha256' in plan:
        assert golden.is_file() and sha(golden) == plan['OriginalGoldenSha256']
    else:
        assert not golden.exists(), 'Unbound golden requires explicit original-run provenance'
        plan['OriginalGoldenState'] = 'NOT_COLLECTED'
    path.write_text(json.dumps(plan, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(PlanSha256=sha(path), OriginalGoldenState=plan['OriginalGoldenState'],
        OriginalGoldenSha256=plan.get('OriginalGoldenSha256'), NativeExecutionPerformed=False)))


if __name__ == '__main__':
    main()
