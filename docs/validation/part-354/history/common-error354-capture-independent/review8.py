import difflib
import hashlib
import json
import pathlib
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
folder = root / 'artifacts/common-error354-capture-proposal8'
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
    prior = root / 'artifacts/common-error354-capture-proposal6/source' / row['Path']
    before = prior.read_bytes() if prior.exists() else b''
    if before != data:
        diffs[row['Path']] = ''.join(difflib.unified_diff(before.decode().splitlines(True), data.decode().splitlines(True)))
assert len(freeze['Files']) == 15
fixture = root / 'artifacts/common-error354-independent-fixtures2'
fixture_raw = (fixture / 'source-freeze.json').read_bytes()
fixture_freeze = json.loads(fixture_raw)
for row in fixture_freeze['Files']:
    data = (fixture / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256']

report = {
    'Status': 'PASS_SOURCE8_BOUNDED_DIAGNOSTIC_FOCUS_ONLY_NO_FULL_ADMISSION',
    'SourceFreezeSha256': sha(raw),
    'ParentHead': freeze['ParentHead'],
    'VerifiedRows': freeze['Files'],
    'Source6To8Diffs': diffs,
    'PreservedRefusalSha256': sha((out / 'source6-report.json').read_bytes()),
    'PreservedControlPrecisionSha256': sha((out / 'source6-control-correction.json').read_bytes()),
    'ClosedFindings': [
        {
            'Finding': 'WRAPPER_FUNCTION_CLAUSE_ARGUMENTS',
            'Correction': 'InvokeWrapper copies entry arguments and passes them for function_clause only; private RawInitialCall has its own live scope/catch using original input.',
            'ControlPrecision': 'Public start_link/5 invalid module tests public guard; initial_call invalid input tests private raw_initial_call, not public initial_call top.',
        },
        {
            'Finding': 'PRELOADED_EXISTING_HOST_AUTHORITY',
            'Correction': 'InstallPreloaded now compares pre-configure delegate dictionary and seals only new/changed bindings, leaving unrelated preexisting HOST registrations unclassified.',
            'Identity': 'Selected original body AND original code image must match under registry gate; metadata rollback remains included.',
        },
        {
            'Finding': 'PROC_LIB_DUPLICATE_LIFECYCLE_SCOPE',
            'Correction': 'Execute uses explicit EnterSourceWrapperBody; it replaces only the immediate marked same-module wrapper slot and restores prior chain on disposal. Ordinary EnterFunction and recursive source frame handling are unchanged.',
            'NativePolicy': 'init_p/5 tail transfers to init_p_do_apply/3; init_p/3 and wake_up/3 retain a single protected-body scope. This explicit helper is not a global equal-MFA deduplication rule.',
        },
        {
            'Finding': 'LOAD_NIF_CAPTURE_CALLSITE_COMPILE_MIGRATION',
            'Correction': 'Execution.LoadNif.cs now supplies context to RuntimeCallFrames.Capture. All three known calls pass current context.',
            'History': 'Source8 preserves reference to actual3 build failure; no successful source8 compilation is inferred.',
        },
    ],
    'OtherAcceptedCorrections': [
        'Source-bound NIF invocation is caught before NIF admission/source scope disposal with actual argument list.',
        'Source(null) remains uncaptured; no historical InitialCall fallback or fake post-hibernate frame.',
        'Explicit/custom traces preserve identity; automatic capture is clone-once/idempotent and selected HOST handler legacy append behavior remains unchanged.',
        'Saved asynchronous native caller chain and copied arguments retain failure-time depth sampling.',
    ],
    'Fixture2Sha256': sha(fixture_raw),
    'Fixture2Rows': fixture_freeze['Files'],
    'Fixture2SourceReview': 'Twenty-four generic depth combinations plus eight proc_lib invalid-arity/monitor-rejection combinations. Qualified non-tail guest call edges and inner leaf catch retain meaningful depth/ordering assertions; no native observation yet.',
    'RequiredFurtherControls': [
        'Public start_link/5 invalid module guard exact function_clause args and private raw_initial_call invalid input exact private top.',
        'Earlier unrelated HOST registration before trusted preload remains unclassified; recursive rollback and same-body/new-image lifecycle identity.',
        'Lifecycle callback failure must show single init_p/3 or wake_up/3 current frame and init_p5-to-do_apply3 tail replacement, preserving crash report semantics.',
        'Existing selected-handler/shared/explicit identity controls, actual source-bound NIF failure and deep/async/cancellation regressions unchanged.',
    ],
    'RemainingLimits': [
        'C# wrapper/private-helper scopes and tail transfers remain selectively source-guided; no complete OTP internal stack parity.',
        'Repeated trusted native delete/reinstallation uses TryAdd metadata; original-boundary retention versus resealing needs explicit lifecycle coverage before broad module lifecycle claims.',
        'Capture wrapper and unconditional argument-copy hot-path allocations are unmeasured; no performance claim.',
    ],
    'SdkExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'CompatibilityPromotion': False,
}
target = out / 'source8-report.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'Sha256': sha(target.read_bytes()), 'ChangedPathsFrom6': list(diffs)}))
