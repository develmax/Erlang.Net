import difflib
import hashlib
import json
import pathlib
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
folder = root / 'artifacts/common-error354-capture-proposal12'
raw = (folder / 'source-freeze.json').read_bytes()
freeze = json.loads(raw)


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


diffs = {}
for row in freeze['Files']:
    data = (folder / 'source' / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
    if row['ParentSha256']:
        parent = subprocess.check_output(['git', '-C', str(root), 'show', freeze['ParentHead'] + ':' + row['Path']])
        assert sha(parent) == row['ParentSha256']
    previous = root / 'artifacts/common-error354-capture-proposal11/source' / row['Path']
    before = previous.read_bytes() if previous.exists() else subprocess.check_output(['git', '-C', str(root), 'show', freeze['ParentHead'] + ':' + row['Path']])
    if before != data:
        diffs[row['Path']] = ''.join(difflib.unified_diff(before.decode().splitlines(True), data.decode().splitlines(True)))
assert len(freeze['Files']) == 21
assert set(diffs) == {
    'src/Erlang.Runtime/ModuleRegistry.cs',
    'src/Erlang.Runtime/ModuleRegistry.ErrorHandler.cs',
    'src/Erlang.Runtime/ModuleRegistry.LocalFunctionDispatch.cs',
    'src/Erlang.Runtime/ModuleRegistry.ProcessScheduling.cs',
}
report = {
    'Status': 'PASS_SOURCE12_SHARED_SELECTED_HANDLER_BOUNDED_FOCUS_ONLY',
    'SourceFreezeSha256': sha(raw),
    'VerifiedRows': freeze['Files'],
    'DiffsFrom11OrUnmodifiedParent': diffs,
    'PriorSource11ReviewSha256': sha((out / 'source11-report.json').read_bytes()),
    'ActualBaselineBindingSha256': sha((out / 'handler-observation-binding.json').read_bytes()),
    'ClosedRouteFinding': 'InvokeUndefinedLambda now captures host/source bit under the same gate as selected body/image/code lease, and passes it to shared InvokeErrorHandler. No new function lookup or image reselection.',
    'Reviewed': [
        'CallWithDispatch removes its local handler wrapper; every handler passes shared InvokeErrorHandler exactly once.',
        'Owned admission threads frozen host bit through existing continuation without rereading current registration.',
        'Shared hook wraps selected actual body with provenance before InvokeSelected; tracing/saved calls/source code lease disposal order remains unchanged.',
        'Existing function and lambda missing-handler/badfun/publication-change failures remain before callback invocation.',
        'ModuleRegistry.ErrorHandler catches still preserve legacy selected adapter policy for uncaptured errors and idempotence for completed traces.',
        'Source11 selected-HOST history/full-args and exact selected-source frame identity logic raw unchanged; actual baseline now confirms prior source diagnosis.',
    ],
    'RequiredActual': [
        'Unchanged old handler nested/equal/source lease/shared/explicit/isolated groups; raw before-after diagnostic with no assertion change.',
        'Actual undefined_lambda/3 source and HOST callback origin controls; shared source hook does not by itself prove guest/native parity.',
        'Generic24/wrapper8/new clause+lifecycle and independent additional3 modules as Root chooses; no zero-test success.',
        'Existing deep/async/cancellation/native loading regressions and actual bound NIF errors.',
    ],
    'NotClaimed': ['Successful build or SDK execution of12', 'Whole stack/private helper/tail parity', 'Full/readiness admission', 'Performance improvement', 'Verified status promotion'],
    'SdkExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
}
target = out / 'source12-report.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'Sha256': sha(target.read_bytes())}))
