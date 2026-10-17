import difflib
import hashlib
import json
import pathlib
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
folder = root / 'artifacts/common-error354-capture-proposal11'
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
    previous = root / 'artifacts/common-error354-capture-proposal10/source' / row['Path']
    before = previous.read_bytes() if previous.exists() else b''
    if before != data:
        diffs[row['Path']] = ''.join(difflib.unified_diff(before.decode().splitlines(True), data.decode().splitlines(True)))
assert len(freeze['Files']) == 18
fixture = root / 'artifacts/common-error354-independent-fixtures3'
fixture_raw = (fixture / 'source-freeze.json').read_bytes()
fixture_freeze = json.loads(fixture_raw)
for row in fixture_freeze['Files']:
    data = (fixture / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
report = {
    'Status': 'PASS_SOURCE11_BOUNDED_UNDEFINED_FUNCTION_HANDLER_FOCUS_WITH_EXPLICIT_UNDEFINED_LAMBDA_GAP',
    'SourceFreezeSha256': sha(raw),
    'ParentHead': freeze['ParentHead'],
    'VerifiedRows': freeze['Files'],
    'DiffsFrom10': diffs,
    'FailureDiagnosisSha256': sha((out / 'handler-failure-diagnosis.json').read_bytes()),
    'ReviewedContracts': [
        'CallWithDispatch selects original body/image/handler under gate, then carries selected provenance into actual admitted callback; no new lookup or lease ownership change.',
        'SelectedErrorHandlerInvocation carries copied args; AsyncLocal scopes restore previous invocation across nested/async calls.',
        'Only selected HOST synthetic frames carry HostHandlerArguments. Ordinary source/native caller frames continue to render arity.',
        'Selected source first entered logical frame is associated once, then untrusted call origin checks actual frame object identity, not arbitrary same signature.',
        'Fresh HOST bridge failure at the selected source entry captures actual handler frame+entry args. A different source intermediate frame or completed native/explicit trace is not replaced.',
        'Shared exceptions still clone once; explicit traces/automatic native captures and public selected-origin WithStackFrame contract unchanged.',
        'No image mutation, new code admission or new lock added in capture helpers; cancellation/control exceptions not caught.',
    ],
    'ObservedFailureRepairScope': 'Source correction is consistent with actual RepeatedFrames/SourceLease failures; successful execution and raw trace confirmation remain pending Root SDK.',
    'UncoveredRoute': {
        'Path': 'src/Erlang.Runtime/ModuleRegistry.LocalFunctionDispatch.cs',
        'Sha256': sha((root / 'src/Erlang.Runtime/ModuleRegistry.LocalFunctionDispatch.cs').read_bytes()),
        'Method': 'InvokeUndefinedLambda',
        'Finding': 'It selects undefined_lambda/3 then directly invokes InvokeErrorHandler, bypassing the new provenance wrapper currently placed only in CallWithDispatch. No new HostHandlerArguments/selected source admission metadata is installed for this route.',
        'AdmissionEffect': 'Not blocking the narrow observed undefined_function focus. Blocks any claim that all selected handler routes are corrected; add shared provenance hook and actual lambda-path controls before broad/full source admission.',
    },
    'Fixture3Sha256': sha(fixture_raw),
    'Fixture3Rows': fixture_freeze['Files'],
    'Fixture3Caveat': 'Lifecycle fixture removes default logger without restoring it. Admit only fresh isolated per-target process; do not silently promote to shared permanent catalog. Independent additional-controls lifecycle fixture catches error inside leaf and restores dictionary, avoiding global logger mutation.',
    'RequiredActualChecks': [
        'Unchanged RunNestedFrames including RepeatedFrames/IntermediateFrames, RunSourceLease including active-code refs/cancel/disposal, RunFrames/State/Isolation.',
        'Raw selected handler trace before/after using diagnostic; retain no-assertion-change history.',
        'Existing generic depth, wrappers, explicit/shared identity, async snapshot, null fallback and real native origin controls.',
        'Separate undefined_lambda source/HOST callbacks before broad selected-handler completion.',
    ],
    'SdkExecuted': False,
    'NativeExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'CompatibilityPromotion': False,
}
target = out / 'source11-report.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'Sha256': sha(target.read_bytes()), 'Changed': list(diffs)}))
