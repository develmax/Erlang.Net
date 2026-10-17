import hashlib
import json
import pathlib
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
pin = 'ad05823719d77c8faee87348ea39513d4e2f99c5'


def digest(value):
    return hashlib.sha256(value).hexdigest().upper()


paths = [
    'src/Erlang.Otp.Behaviours/OtpModules.cs',
    'src/Erlang.Otp.Behaviours/ProcLib.cs',
    'src/Erlang.Otp.Behaviours/ProcLibIntrospection.cs',
    'src/Erlang.Otp.Behaviours/ProcLibOptionStartup.cs',
    'src/Erlang.Runtime/ProcessRuntime.cs',
    'src/Erlang.Runtime/RuntimeInitializer.cs',
    'src/Erlang.Runtime/NativeBuiltinCatalog.cs',
    'src/Erlang.Runtime/ErlangErrorRegistrations.cs',
]
bindings = [{'Path': path, 'Sha256': digest((root / path).read_bytes())} for path in paths]
otp = subprocess.check_output(['git', '-C', str(root.parent / 'otp'), 'show', pin + ':lib/stdlib/src/proc_lib.erl'])
report = {
    'Status': 'SOURCE5_REACHABLE_WRAPPER_GAP_AND_INITIAL_CALL_FALLBACK_RISK_NO_RUNTIME_EXECUTION',
    'Source5FreezeSha256': digest((root / 'artifacts/common-error354-capture-proposal5/source-freeze.json').read_bytes()),
    'Bindings': bindings,
    'PinnedProcLib': {'Commit': pin, 'Path': 'lib/stdlib/src/proc_lib.erl', 'Sha256': digest(otp), 'Lines': '818-831'},
    'ConcreteRoute': {
        'Call': "proc_lib:initial_call([{{dictionary,'$initial_call'},{m,f,not_integer}}])",
        'SourceSequence': ['RawInitialCall accepts stored triple', 'MakeDummyArguments enters noninteger branch', 'erlang:integer_to_list(not_integer) fails', 'native boundary freezes trace using guest executionFrame only', 'outer RuntimeCallFrames sees automatic marker and cannot append proc_lib frame'],
        'NativeContract': 'Pinned initial_call invokes make_dummy_args non-tail inside returned tuple; make_dummy_args invokes integer_to_list inside list concatenation, so proc_lib source frames are relevant.',
        'Unproven': 'No VM/SDK observation yet; precise full native stack including private helper depth requires actual witness. Adding only public wrapper scope does not prove exact private helper frame parity.',
    },
    'ExplicitRoute': {
        'Call': 'proc_lib:start_link(M,F,A,Timeout,[monitor])',
        'SourceSequence': 'VerifyNoMonitor -> RejectMonitor -> erlang:error/2 explicit factory, with no proc_lib entry logical scope.',
        'Classification': 'Reachable pre-existing explicit-factory limitation; not advertised as a new runtime RED.'
    },
    'TrustedRegistrationRequirement': 'OtpModules/ProcLib use ordinary Register through initializer Apply, not Core InstallPreloaded. Record trusted Erlang-source-wrapper identity explicitly; never classify all image-null or arbitrary HOST registrations as native/source.',
    'InitialCallFallback': {
        'SourceFact': 'SpawnTraced initializes InitialCall once from spawn description. Hibernate transfer replaces current callback with another selected MFA without updating InitialCall.',
        'Conclusion': 'InitialCall is historical process metadata, not unconditional current stack frame authority. Prefer actual selected live invocation scope; test absent-context behavior explicitly.',
        'AutomaticEmptyRisk': 'Source(null) marks Nil automatically captured; Erpc.Stack then suppresses its previous fallback. This requires real selected-target/bare-context control, not a guessed stale InitialCall frame.'
    },
    'HotPathRisk': 'InvokeNative allocates an argument array and dispatch wrapper before successful calls, including total predicates/self/node; no performance improvement claimed. Measure after correctness; a proven nonthrowing exact binding exclusion may be reviewed later.',
    'ProposedControls': [
        'Real proc_lib initial_call stored noninteger arity at depth1/2/8, normal/apply/external-fun entry; typed top and relevant wrapper/helper frames.',
        'Real proc_lib start_link forbidden-monitor error/2 preserves supplied Args and wrapper top without erlang:error frame.',
        'Same-named ordinary HOST replacement remains unclassified and shared/explicit exception identity unchanged.',
        'Real process starts M1:F1 then hibernates into M2:F2; failure must not fabricate historical M1:F1 fallback.',
        'Real erpc local target nested error/1 with absent versus present logical source scope; captured-empty distinction source-reviewed.',
    ],
    'SdkExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'StatusPromotion': False,
}
path = out / 'wrapper-followup.json'
path.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(digest(path.read_bytes()))
