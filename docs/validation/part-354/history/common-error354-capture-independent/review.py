import hashlib
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = pathlib.Path(__file__).resolve().parent


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def git(repo, *args):
    return subprocess.check_output(['git', '-C', str(repo), *args])


freezes = {}
rows = []
for revision in (3, 4, 5):
    folder = ROOT / f'artifacts/common-error354-capture-proposal{revision}'
    raw = (folder / 'source-freeze.json').read_bytes()
    freeze = json.loads(raw)
    freezes[str(revision)] = sha(raw)
    for row in freeze['Files']:
        data = (folder / 'source' / row['Path']).read_bytes()
        assert len(data) == row['Bytes'] and sha(data) == row['Sha256'], row['Path']
        if row['ParentSha256']:
            assert sha(git(ROOT, 'show', freeze['ParentHead'] + ':' + row['Path'])) == row['ParentSha256']
        if revision == 5:
            rows.append(row)
    assert len(freeze['Files']) == 9

changed = []
diffs = {}
for row in rows:
    path = row['Path']
    before = (ROOT / 'artifacts/common-error354-capture-proposal4/source' / path).read_bytes()
    after = (ROOT / 'artifacts/common-error354-capture-proposal5/source' / path).read_bytes()
    if before != after:
        import difflib
        changed.append(path)
        diffs[path] = ''.join(difflib.unified_diff(before.decode().splitlines(True), after.decode().splitlines(True)))
assert changed == ['src/Erlang.Compiler/Execution.cs']

pin = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
pinned = []
for path, anchors in [
    ('erts/emulator/beam/beam_common.c', ['handle_error', 'save_stacktrace', 'EXF_HAS_EXT_INFO']),
    ('erts/emulator/beam/emu/bif_instrs.tab', ['nif_bif.call_nif', 'c_p->current = codemfa']),
    ('erts/emulator/beam/erl_nfunc_sched.h', ['erts_nfunc_restore_error']),
    ('erts/emulator/beam/erl_nif.c', ['enif_make_badarg', 'enif_raise_exception']),
]:
    data = git(ROOT.parent / 'otp', 'show', pin + ':' + path)
    lines = data.decode().splitlines()
    pinned.append({'Path': path, 'Sha256': sha(data), 'Anchors': {a: [i + 1 for i, line in enumerate(lines) if a in line] for a in anchors}})

report = {
    'Status': 'PASS_SOURCE5_CONDITIONAL_BOUNDED_DIAGNOSTIC_FOCUS_ONLY',
    'SourceFreezes': freezes,
    'SourceFiles': rows,
    'Source4To5ChangedPaths': changed,
    'Source4To5Diff': diffs,
    'PinnedCommit': pin,
    'PinnedSources': pinned,
    'PreservedSourceFinding': {
        'Revisions': [3, 4],
        'Finding': 'Source-bound NIF admission/invoke was outside the source-origin catch. NifImageCall has only finally disposal and does not capture Erlang errors, so unwind could remove the NIF logical frame before the outer capture.',
        'Correction': 'Revision5 encloses existing NIF branch in the catch while EnterFunction is live; NIF lease disposal and function-selection order remain intact.',
        'NifImageCallSha256': sha((ROOT / 'src/Erlang.Runtime/NifImageCall.cs').read_bytes()),
    },
    'ReviewedContracts': [
        'Selected actual delegate identity and source-owner classification under registry gate; wrapping occurs after selection without a fresh lookup.',
        'Native binding identity map participates in recursive Apply rollback.',
        'Synchronous and asynchronous native failures use saved caller chain/argument copy and read depth at failure capture.',
        'Explicit traces including empty/custom error_info traces are returned unchanged; automatic capture clones shared exception rather than mutating it.',
        'Evaluate catches before expression-local try/catch consumes an error; named/anonymous source catches run before logical scope disposal.',
        'Only ErlangException is captured; tail-call and cancellation control exceptions retain existing propagation.',
        'Selected HOST handler retains live logical scope; public WithStackFrame selected-origin behavior is deliberately preserved.',
        'ProcLib/Erpc avoid concatenating or replacing already captured complete traces.',
    ],
    'RequiredBeforeFullAdmission': [
        'Resolve or measure non-native preloaded Erlang wrapper frame omission: these wrappers have neither source-owner scope nor native-boundary scope, and inner automatic capture makes outer unwind framing idempotent. Do not classify arbitrary image-null HOST delegates as native.',
        'Run unchanged custom selected-handler, explicit/shared exception, rollback and failure-time-depth controls; do not migrate assertions without pinned source basis.',
        'Run source-bound NIF failure control with existing real native path; generic arity/arguments and dirty-restored NIF trace contracts are not proved by this source correction alone.',
        'Run meaningful deep recursion/async/cancellation regressions because Evaluate now adds an async catch boundary.',
        'Compare typed native witnesses before metadata compatibility or whole backtrace-depth claims.',
    ],
    'SdkExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'CompatibilityPromotion': False,
    'ReviewerAuthorship': 'Independent reviewer of these nine authored-by-publication sources; previously authored related proc_lib/rpc code, disclosed.',
}
(OUT / 'report.json').write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'ReportSha256': sha((OUT / 'report.json').read_bytes()), 'Source5Sha256': freezes['5']}))
