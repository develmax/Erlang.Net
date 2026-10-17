import difflib
import hashlib
import json
import pathlib
import subprocess

folder = pathlib.Path(__file__).resolve().parent
root = folder.parents[1]
candidate = root / 'artifacts/size-bif354-full-candidate'
path = 'src/Erlang.Runtime/ErlangApplyDispatch.cs'
before = (candidate / path).read_bytes()
text = before.decode()
old = 'throw exception.WithStackFrame(module, function, arguments);'
new = '''throw RuntimeExceptionCapture.Call(
                context,
                exception,
                context.CaptureExecutionChain(),
                module,
                function,
                arguments,
                false
            );'''
newline = '\r\n' if b'\r\n' in before else '\n'
new = new.replace('\n', newline)
assert text.count(old) == 1
after = text.replace(old, new).encode()
destination = folder / 'source' / path
destination.parent.mkdir(parents=True, exist_ok=True)
destination.write_bytes(after)


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


bound = []
for item in [path, 'src/Erlang.Runtime/RuntimeExceptionCapture.cs', 'src/Erlang.Runtime/ProcessContext.ErrorHandlers.cs', 'src/Erlang.Compiler/RuntimeCallFrames.cs', 'tools/Erlang.Differential/ApplyFrameCases.cs', 'tools/Erlang.Differential/ApplyFrameCaseNames.cs']:
    data = (candidate / item).read_bytes()
    bound.append({'Path': item, 'Sha256': sha(data)})
controls = []
for item in sorted((folder / 'controls').glob('*.cs')):
    data = item.read_bytes()
    controls.append({'Path': item.relative_to(folder).as_posix(), 'Bytes': len(data), 'Sha256': sha(data)})
pin = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
pinned = []
for item, anchors in [
    ('erts/emulator/beam/beam_common.c', ['apply_bif_error_adjustment', 'apply(Process* p', 'create a stub entry which in turn calls the error handler']),
    ('lib/kernel/src/error_handler.erl', ['undefined_function(Module, Func, Args)', 'raise_undef_exception(Module, Func, Args)', 'crash(Tuple)', 'Stk = [Tuple|tl(Stacktrace)]']),
]:
    data = subprocess.check_output(['git', '-C', str(root.parent / 'otp'), 'show', pin + ':' + item])
    lines = data.decode().splitlines()
    pinned.append({'Path': item, 'Sha256': sha(data), 'Anchors': {anchor: [index + 1 for index, line in enumerate(lines) if anchor in line] for anchor in anchors}})
graph = json.loads((root / 'artifacts/common-error354-capture-independent/final-styled3-source-guards.json').read_bytes())['VerifiedSourceFiles']
report = {
    'Status': 'SOURCE_ONLY_APPLY_FORWARDING_ORIGIN_CORRECTION_REQUIRES_PEER_AND_EXECUTION',
    'ParentCandidate': str(candidate.relative_to(root)).replace('\\', '/'),
    'ParentFullInputsSha256': sha((root / 'artifacts/size-bif354-full-actual1/inputs.json').read_bytes()),
    'ParentSourceSha256': sha(before),
    'Files': [{'Path': path, 'Bytes': len(after), 'Sha256': sha(after)}],
    'UnchangedParentBindings': bound,
    'Controls': controls,
    'Diff': ''.join(difflib.unified_diff(before.decode().splitlines(True), after.decode().splitlines(True))),
    'PinnedCommit': pin,
    'PinnedSources': pinned,
    'ActualFailure': 'Full354 old erlang-apply-forwarded-frame-undef-direct-and-dynamic expected target missing_apply_frame_module:absent/[17] for both paths, dynamic path instead had erlang:apply/[M,F,Args]. Assertions remain unchanged.',
    'SourceCause': 'Validated direct target failure receives legacy partial WithStackFrame (auto=false). Outer native apply origin then replaces partial trace with apply frame before compiler RuntimeCallFrames Cons guard executes.',
    'Correction': 'Freeze actual validated forwarded target using existing RuntimeExceptionCapture.Call(native:false) and current caller chain at target catch. Native=false avoids inventing native formatter metadata. Automatic/explicit already captured traces return unchanged.',
    'WhyNotCaptureCallFailureAdapter': 'That context adapter includes selected-source HOST-bridge compatibility rewriting. Forwarded apply target is the actual validated target and must preserve its target origin even inside a selected handler; direct central Call intentionally bypasses that embedding-specific override.',
    'Preserved': [
        'Argument/module/function/1023limit validation before InvokeTarget unchanged; true apply argument errors retain apply origin.',
        'Existing catch is still direct dispatch plus !explicit and empty trace only; selected handler dispatch/fresh native/source captured errors are not rewritten.',
        'Explicit raise custom/empty traces unchanged; no blanket arbitrary Cons is treated as complete.',
        'No source function/lease, process scheduling, argument parsing, tracing, registry selection or assertion changes.',
        'Common RuntimeExceptionCapture remains byte-identical to failed full, avoiding shared RPC repair overlap.',
    ],
    'FocusedPlan': {
        'Target': 'Erlang.Tests.ApplyForwardingRegressionHostChecks.Run',
        'Cases': ['erlang-apply-forwarded-frame-badarg-direct-and-dynamic', 'erlang-apply-forwarded-frame-undef-direct-and-dynamic', 'erlang-apply-forwarded-frame-invalid-module-keeps-apply-frame', 'erlang-apply-forwarded-frame-invalid-function-keeps-apply-frame', 'erlang-apply-forwarded-frame-improper-arguments-keeps-apply-frame', 'erlang-apply-forwarded-frame-explicit-raise-preserves-all-classes-and-empty-stack'],
        'ExpectedAuthority': 'Exact existing ApplyFrameCases.Modules expected terms, compiled with actual original source/caller context. No copied or SDK-seeded fixtures.',
        'Runner': 'Root existing bounded owned child helper; link only controls2, execute group after rebuilding fresh corrected candidate. Retain DLL/source hashes and raw failure receipts. No execution by proposal author.',
        'AdditionalUnchangedRegressions': ['Shared apply256/1023/1024 boundaries', 'Existing handler groups and corrected RPC old case', 'Current354 generic depth/explicit/forwarded controls'],
    },
    'SourceGuardRequirement': {'Path': path, 'AlreadyIn114': any(row['Path'] == path for row in graph), 'NeedFinalBindingReview': True},
    'SdkExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'AssertionsChanged': False,
    'CompatibilityPromotion': False,
}
target = folder / 'source-freeze.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(json.dumps({'Sha256': sha(target.read_bytes()), 'SourceSha256': sha(after), 'AlreadyIn114': report['SourceGuardRequirement']['AlreadyIn114']}))
