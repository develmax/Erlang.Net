"""Pure local admission checks; never starts a VM, compiler or child process."""
import argparse
import ast
import importlib.util
import json
from pathlib import Path
from unittest.mock import patch
from source_inputs import digest, parse_index_inventory, require_normal_receipt, sanitized_environment, source_file_identity

parser = argparse.ArgumentParser()
parser.add_argument('--repository', required=True)
args = parser.parse_args()
tool = Path(__file__).resolve().parent
repository = Path(args.repository).resolve()
manifest = json.loads((tool / 'manifest.json').read_bytes())
assert len(manifest['Cases']) == 5
assert len({case['Module'] for case in manifest['Cases']}) == 5
for row in manifest['Files']:
    assert digest(tool / row['Path']) == row['Sha256']
for path in tool.glob('*.py'):
    ast.parse(path.read_bytes(), filename=str(path))
normal = {'ExitCode': 0, 'ParentReaped': True, 'StreamsClosed': True,
          'TerminationRequested': False, 'RemainingGroupKillRequested': False, 'CleanupFailure': None}
require_normal_receipt(normal)
refusals = [('ExitCode', 1), ('ExitCode', None), ('ParentReaped', False), ('StreamsClosed', False),
            ('TerminationRequested', True), ('RemainingGroupKillRequested', True), ('CleanupFailure', 'failure')]
for name, value in refusals:
    altered = dict(normal)
    altered[name] = value
    try:
        require_normal_receipt(altered)
    except AssertionError:
        pass
    else:
        raise AssertionError('invalid receipt accepted: ' + name)
decoder_row = manifest['Dependencies'][1]
decoder_path = repository / decoder_row['Path']
assert digest(decoder_path) == decoder_row['Sha256']
spec = importlib.util.spec_from_file_location('scheduler_passive_etf', decoder_path)
decoder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(decoder)
expected = {
    'profile.etf': ('tuple', (('atom', 'scheduler_profile'), ('integer', 2), ('integer', 2))),
    'returned.etf': ('atom', 'done'),
    'compiled-protocol.etf': ('tuple', (('atom', 'ok'), ('atom', 'done'))),
    'outcome.etf': ('tuple', (('atom', 'returned'), ('atom', 'done'))),
}
for name, value in expected.items():
    assert decoder.decode_exact((tool / 'golden' / name).read_bytes()) == value
class Link:
    def is_symlink(self):
        return True

    def read_bytes(self):
        raise AssertionError('symlink target must never be read')


class Root:
    def __truediv__(self, name):
        return Link()


for name, payload in [('erts/preloaded/doc', b'../../system/doc/erts'),
                      ('lib/mnesia/examples/DATA', b'../missing-directory'), ('dangling', b'no-target')]:
    with patch('source_inputs.os.readlink', return_value=payload):
        row = source_file_identity(Root(), name, '120000')
    assert row['Mode'] == '120000' and row['Bytes'] == len(payload)
    import hashlib
    assert row['Sha256'] == hashlib.sha256(payload).hexdigest().upper()
class Regular:
    def is_file(self):
        return True

    def is_symlink(self):
        return False

    def read_bytes(self):
        return b'raw\r\nbytes\x00'


class RegularRoot:
    def __truediv__(self, name):
        return Regular()


for mode in ['100644', '100755']:
    row = source_file_identity(RegularRoot(), 'regular', mode)
    assert row['Mode'] == mode and row['Sha256'] == hashlib.sha256(Regular().read_bytes()).hexdigest().upper()
for mode, stage in [('160000', '0'), ('040000', '0'), ('100644', '2')]:
    try:
        parse_index_inventory((mode + ' ' + 'a' * 40 + ' ' + stage + '\tpath\0').encode())
    except AssertionError:
        pass
    else:
        raise AssertionError('unexpected git mode/stage accepted')
poisoned = {name: 'untrusted-root-or-flags' for name in ['ERL_ROOTDIR', 'ERL_FLAGS', 'ERL_AFLAGS',
            'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS']}
poisoned['PATH'] = 'kept-path'
assert sanitized_environment(poisoned) == {'PATH': 'kept-path'}
assert poisoned['ERL_ROOTDIR'] == 'untrusted-root-or-flags'
print('PASS source AST, five inputs, seven cleanup refusals, four ETF goldens, three link payloads, two regular modes, three mode/stage refusals and six cleared Erlang overrides; no VM/SDK')
