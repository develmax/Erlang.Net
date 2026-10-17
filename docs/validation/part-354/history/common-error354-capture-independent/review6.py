import hashlib
import json
import pathlib
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
folder = root / 'artifacts/common-error354-capture-proposal6'
raw = (folder / 'source-freeze.json').read_bytes()
freeze = json.loads(raw)


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


for row in freeze['Files']:
    data = (folder / 'source' / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
    if row['ParentSha256']:
        parent = subprocess.check_output(['git', '-C', str(root), 'show', freeze['ParentHead'] + ':' + row['Path']])
        assert sha(parent) == row['ParentSha256']
assert len(freeze['Files']) == 14
report = {
    'Status': 'REFUSED_SOURCE6_PENDING_NARROW_CORRECTIONS_NO_RUNTIME_RED_CLAIM',
    'SourceFreezeSha256': sha(raw),
    'VerifiedRows': freeze['Files'],
    'AddressedPriorFindings': [
        'Selected wrapper/native classification uses original delegate and code-image identity under gate and participates in Apply rollback.',
        'OtpModules has an explicit source-wrapper installation boundary; Source(null) no longer falsely completes empty trace or guesses InitialCall.',
        'Source-bound NIF inner catch retains actual current source frame and argument list before admission disposal.',
        'make_dummy_args/2 gets an explicit source-guided private helper scope.',
    ],
    'BlockingSourceFindings': [
        {
            'Id': 'WRAPPER_FUNCTION_CLAUSE_ARGUMENTS',
            'Origin': 'Independent reviewer',
            'Path': 'src/Erlang.Runtime/RuntimeExceptionCapture.cs',
            'Source': 'InvokeWrapper catch calls Source(context,error,frame) without arguments even for function_clause.',
            'ReachableRoute': 'proc_lib:initial_call(bad_input) -> RawInitialCall function_clause; source6 freezes top arity1 rather than argument list [bad_input].',
            'RequiredCorrection': 'Preserve entry arguments and supply them for function_clause only, matching existing Execution.InvokeFrame policy; add real wrapper guard-failure control.',
        },
        {
            'Id': 'PRELOADED_EXISTING_HOST_AUTHORITY',
            'Origin': 'Root finding independently confirmed',
            'Path': 'src/Erlang.Runtime/ModuleRegistry.cs',
            'Source': 'InstallPreloaded enumerates and seals every existing !sourceOwners function after configure, including unrelated public HOST registrations which configure did not install.',
            'RequiredCorrection': 'Seal only exact trusted installation declarations/changes; existing HOST body before an unrelated InstallPreloaded configure must remain unclassified and preserve identity.',
        },
        {
            'Id': 'PROC_LIB_DUPLICATE_LIFECYCLE_SCOPE',
            'Origin': 'Root finding independently confirmed',
            'Path': 'src/Erlang.Otp.Behaviours/ProcLibLifecycle.cs',
            'Source': 'Wrapper admission adds init_p/3 or wake_up/3; Execute already enters same MFA. Real nested callback failure can capture duplicate identical lifecycle frames.',
            'RequiredCorrection': 'Source-guided same-entry ownership policy, without collapsing genuine recursive equal-MFA calls; retain lifecycle/crash-report assertions and add real callback failure control.',
        },
    ],
    'NotBlockedBy': 'Internal installer access: Runtime friend assembly already includes OtpBehaviours.',
    'KnownLimits': [
        'Adding C# wrapper scopes alone does not prove native private-helper/tail-call stack parity.',
        'NIF dirty/ordinary error frame and formatter parity still needs actual source-bound native control.',
        'Entry argument copy/delegate wrapper allocations require later measurement; no performance claim.',
    ],
    'SdkExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'CompatibilityPromotion': False,
}
target = pathlib.Path(__file__).resolve().parent / 'source6-report.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(sha(target.read_bytes()))
