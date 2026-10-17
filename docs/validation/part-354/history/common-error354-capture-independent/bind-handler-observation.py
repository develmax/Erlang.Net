import hashlib
import json
import pathlib

root = pathlib.Path(__file__).resolve().parents[2]
folder = root / 'artifacts/size-bif354-selected-handler-actual1'


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


typed_raw = (folder / 'typed-observation.json').read_bytes()
typed = json.loads(typed_raw)
assert typed['EtfSha256'] == sha((folder / 'selected-handler.etf').read_bytes())
repeated, bridge = [item['Tuple'] for item in typed['Observed']['Tuple']]
assert repeated[0] == {'Atom': 'repeated_host'} and repeated[1] == 2
frames = repeated[3]
assert isinstance(frames[0]['Tuple'][2], list) and frames[1]['Tuple'][2] == 3
assert bridge[0] == {'Atom': 'source_handler_host_bridge'}
assert bridge[1] == {'Atom': 'undefined_function'}
assert bridge[3][0]['Tuple'][0] == {'Atom': 'handler_host_bridge'}
report = {
    'Status': 'PASS_PASSIVE_ACTUAL_OBSERVATION_CONFIRMS_PRIOR_SOURCE_DIAGNOSIS',
    'Bindings': [{'Path': str((folder / name).relative_to(root)).replace('\\', '/'), 'Sha256': sha((folder / name).read_bytes())} for name in ('typed-observation.json', 'selected-handler.etf', 'completion.json', 'diagnostic.exit.json', 'observation.json')],
    'PreviousSourceDiagnosis': 'handler-failure-diagnosis.json remains immutable and truthfully unmeasured at its creation time.',
    'ActualConfirmed': [
        'Repeated handler invoked twice: inner first frame carries argument list, outer second frame carries integer arity3.',
        'Source handler live CurrentFunction is undefined_function, but saved error stack starts handler_host_bridge MFA.',
    ],
    'ExpectedValuesNotSeeded': 'These actual terms confirm diagnosis only; original assertions and source-derived contract remain unchanged.',
    'SdkExecutedByReviewer': False,
    'NetworkExecuted': False,
    'MainMutation': False,
}
target = pathlib.Path(__file__).resolve().parent / 'handler-observation-binding.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(sha(target.read_bytes()))
