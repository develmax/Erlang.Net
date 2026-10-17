import difflib
import hashlib
import json
import pathlib
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
folder = root / 'artifacts/common-error354-capture-proposal13'
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
    previous = root / 'artifacts/common-error354-capture-proposal12/source' / row['Path']
    before = previous.read_bytes()
    if before != data:
        diffs[row['Path']] = ''.join(difflib.unified_diff(before.decode().splitlines(True), data.decode().splitlines(True)))
assert len(freeze['Files']) == 21
assert list(diffs) == ['src/Erlang.Runtime/ModuleRegistry.cs']
controls = []
for name in ('common-error354-reinstall-controls', 'common-error354-lambda-handler-controls'):
    directory = root / 'artifacts' / name
    raw_control = (directory / 'source-freeze.json').read_bytes()
    control = json.loads(raw_control)
    for row in control['Files']:
        data = (directory / row['Path']).read_bytes()
        assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
    controls.append({'Path': str((directory / 'source-freeze.json').relative_to(root)).replace('\\', '/'), 'Sha256': sha(raw_control), 'Files': control['Files']})
report = {
    'Status': 'PASS_SOURCE13_NARROW_REINSTALL_AND_SOURCE_PRESERVATION_FOR_FOCUSED_EXECUTION',
    'SourceFreezeSha256': sha(raw),
    'VerifiedRows': freeze['Files'],
    'DiffsFrom12': diffs,
    'PreviousReviewSha256': sha((out / 'source12-report.json').read_bytes()),
    'ReviewedRuntimeDelta': [
        'Trusted actually-changed installation now assigns current runtimeErrorBindings entry rather than retaining stale TryAdd identity.',
        'DeleteModule removes current metadata alongside the current function, intrinsic and source owner; oldCode snapshot construction remains before removal and unchanged.',
        'OldCode retains exact entry delegates, image, descriptor and SourceOwners; purge-state storage/leases/current code admission untouched.',
        'Existing recursive Apply snapshots/restores runtimeErrorBindings alongside functions/codeImages/oldCode/sourceOwners/preloaded state; no partial metadata commit on nested failure.',
        'Selected body and image identity checks unchanged; unrelated HOST installation authority correction retained.',
    ],
    'Controls': controls,
    'ControlReview': [
        'Reinstall control uses actual public InstallPreloaded/DeleteModule/Apply/Call routes; reflection reads metadata/snapshot identity only.',
        'Initial trusted failure, nested rollback, old snapshot retention and new trusted dispatch capture are asserted; shared exception objects remain unchanged.',
        'Reflection property names match actual ModuleCodeSnapshot/RuntimeErrorBinding; Entries element Item2 is actual ValueTuple delegate field.',
        'Lambda controls use genuine compiled named/anonymous functions, ETF restore and real delete+purge before dispatch; four HOST/source callback combinations retain actual original function reference and argument list.',
        'No direct reflection handler dispatch or old assertion change; source-specific one-frame versus two repeated HOST frames remains explicitly synthetic embedding policy.',
    ],
    'ExistingSixteenVerifiedScopePreservation': {
        'Scope': 'Narrow source12-to13 delta only, not a blanket re-audit or permission to refresh all source bindings.',
        'Conclusion': 'No new observable branch difference for successful total predicate/self/node handler contracts: selected original delegate, argument validation, handler bodies, returned terms and successful dispatch path unchanged. Only error-boundary metadata replacement/removal lifetime changes.',
        'Evidence': 'Only ModuleRegistry differs; exact unified diff retained. All other20 source12 files raw exact.',
        'Limits': 'Current final styled/composed source hashes and complete original16 dependency extension still require final binding review. No compatibility status promotion.',
    },
    'RequiredActual': [
        'Compile/run new reinstall target and four lambda mechanisms, unchanged old handler groups, depth/source controls; preserve all previous failure receipts.',
        'Rehash final styled/runtime composition and required current verified-source bindings before readiness/full admission.',
        'No full compatibility or performance claim from focused success alone.',
    ],
    'UserConventionsBeforePermanent': 'Reinstall reflection field/property names and lambda semantic literals still need normal constants/style extraction without semantic changes; current source admission is artifact-only bounded focus.',
    'SdkExecutedByReviewer': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'CompatibilityPromotion': False,
}
target = out / 'source13-report.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'Sha256': sha(target.read_bytes()), 'Controls': [c['Sha256'] for c in controls]}))
