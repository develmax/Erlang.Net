import hashlib
import json
import pathlib
import subprocess

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
actual = root / 'artifacts/size-bif354-focused-actual5'


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


completion_raw = (actual / 'completion.json').read_bytes()
completion = json.loads(completion_raw)
failed = []
for name in ('target-22', 'target-23'):
    data = (actual / name / 'outcome.json').read_bytes()
    outcome = json.loads(data)
    assert outcome['Passed'] is False
    failed.append({'Path': str((actual / name / 'outcome.json').relative_to(root)).replace('\\', '/'), 'Sha256': sha(data), 'Name': outcome['Name'], 'Failure': outcome['Failure']})
pin = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
native = subprocess.check_output(['git', '-C', str(root.parent / 'otp'), 'show', pin + ':erts/emulator/beam/beam_common.c'])
bound = []
for path in [
    'tests/Erlang.Tests/ProcessErrorHandlerHostChecks.cs',
    'tests/Erlang.Tests/ProcessErrorHandlerHostNames.cs',
    'src/Erlang.Runtime/ModuleRegistry.ErrorHandler.cs',
]:
    data = (root / path).read_bytes()
    bound.append({'Path': path, 'Sha256': sha(data)})
report = {
    'Status': 'ACTUAL_TWO_FAILURES_SOURCE_CONSISTENT_DIAGNOSIS_NO_TRACE_TERM_OBSERVATION',
    'CompletionSha256': sha(completion_raw),
    'FailureBindings': failed,
    'CurrentUnmodifiedHostSources': bound,
    'RuntimeSource8FreezeSha256': sha((root / 'artifacts/common-error354-capture-proposal8/source-freeze.json').read_bytes()),
    'Findings': [
        {
            'Actual': 'RepeatedFrames line206: two calls and two frames must be identical full-argument handler frames.',
            'SourceCause': 'InvokeHandler captures inner selected handler with savedArguments, but immutable parent ErlangExecutionFrame has only Arity. Source renders only first arguments; outer repeated handler renders integer3. Outer WithSelectedHandlerFrame sees completed automatic trace and cannot restore legacy argument-bearing selected frame.',
            'Confidence': 'Source-consistent explanation of exact failed assertion. Actual error term not dumped by failed harness; do not label actual observed frame contents.',
        },
        {
            'Actual': 'SourceLease line545 expects top selected source handler undefined_function, not bridge.',
            'SourceCause': 'Registered intrinsic HOST bridge throws fresh error under generated source handler. Compiler RuntimeCallFrames.Capture freezes synthetic bridge call top using Call(native:false), then source-origin and selected-handler unwind adapters are idempotent and cannot preserve prior selected-handler top policy.',
            'Confidence': 'Source-consistent route. Actual error term not dumped; no independent measured stack shape yet.',
        },
    ],
    'PinnedNative': {
        'Commit': pin,
        'Path': 'erts/emulator/beam/beam_common.c',
        'Sha256': sha(native),
        'Anchors': ['call_error_handler selects actual process handler export and registers M/F/Args', 'save_stacktrace distinguishes current argument-list errors from ordinary source caller arities'],
        'Conclusion': 'Two identical argument-bearing recursive HOST frames are a preserved synthetic embedding contract, not proof ordinary native caller frames contain Args. Keep distinction explicit.',
    },
    'CorrectionConstraints': [
        'Preserve original assertions, selected body identity/image/code lease and cleanup.',
        'No blanket all-caller argument snapshots or equal-MFA collapsing.',
        'Do not reframe explicit traces or mutate shared exceptions.',
        'Keep generic native origin freeze before leaf catch and failure-time depth intact.',
        'Distinguish selected-HOST or HOST-intrinsic compatibility provenance from actual native/source origin; no synthetic bridge claim as native BIF.',
        'Run both unchanged old groups plus nested source intermediate/equal handler/shared identity and new generic depth controls after source peer.',
    ],
    'SdkExecuted': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'CompatibilityPromotion': False,
}
target = out / 'handler-failure-diagnosis.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(sha(target.read_bytes()))
