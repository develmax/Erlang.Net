import hashlib
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


guard = load_module('synthetic_stack_guard', ROOT / 'verify-original9.py')
decoder = load_module('synthetic_stack_decoder', ROOT / 'passive_etf.py')


def atom(value):
    return ('atom', value)


def atom_bytes(value):
    raw = value.encode()
    return bytes([119, len(raw)]) + raw


pid = bytes([88]) + atom_bytes('n') + (1).to_bytes(4, 'big') + bytes(8)
body = bytes([0]) + bytes(range(16)) + bytes(8) + atom_bytes('m') + bytes([97, 0, 97, 0]) + pid
local_fun = decoder.decode_exact(bytes([131, 112]) + (len(body) + 4).to_bytes(4, 'big') + body)
export_fun = decoder.decode_exact(bytes([131, 113]) + atom_bytes('m') + atom_bytes('f') + bytes([97, 0]))
empty = ('list', ())
rows = []


def capture(frame):
    return ('tuple', (atom('exception'), atom('error'), atom('undef'), ('list', (('tuple', tuple(frame)),))))


def accepted(name, frame):
    value = capture(frame)
    result = guard.captured(value)
    if result is not value[1] or result[3] is not value[1][3] or result[3][1][0][1] != tuple(frame):
        raise RuntimeError('Passive frame/stack identity changed: ' + name)
    rows.append({'Name': name, 'Expected': 'ACCEPT_AND_RETAIN_FULL_FRAME', 'Passed': True})


def refused(name, frame):
    try:
        guard.captured(capture(frame))
    except ValueError:
        rows.append({'Name': name, 'Expected': 'REFUSE_MALFORMED_FRAME', 'Passed': True})
        return
    raise RuntimeError('Malformed function stack frame accepted: ' + name)


accepted('local-fun-empty-arguments-location', [local_fun, empty, empty])
accepted('local-fun-zero-arity', [local_fun, ('integer', 0), empty])
accepted('local-fun-maximum-arity', [local_fun, ('integer', 255), empty])
accepted('export-fun-proper-arguments', [export_fun, ('list', (('integer', 42),)), empty])
accepted('existing-mfa-three-fields', [atom('m'), atom('f'), ('integer', 0)])
accepted('existing-mfa-four-fields', [atom('m'), atom('f'), empty, empty])
refused('fun-arity-over-bound', [local_fun, ('integer', 256), empty])
refused('fun-negative-arity', [local_fun, ('integer', -1), empty])
refused('fun-improper-arguments', [local_fun, ('improper', (), atom('tail')), empty])
refused('fun-tuple-arguments', [local_fun, ('tuple', ()), empty])
refused('fun-improper-location', [local_fun, empty, ('improper', (), atom('tail'))])
refused('fun-atom-location', [local_fun, empty, atom('bad_location')])
refused('fun-two-fields', [local_fun, empty])
refused('fun-four-fields', [local_fun, empty, empty, empty])
refused('nonfunction-first-field', [('binary', b'x'), empty, empty])
refused('function-as-mfa-name', [atom('m'), local_fun, ('integer', 0)])
if len(rows) != 16:
    raise RuntimeError('Expected sixteen independent stack-shape controls')
output = Path(sys.argv[1])
if output.exists():
    raise RuntimeError('Fresh stack-control output required')
report = {'Status': 'SYNTHETIC_PASSIVE_STACK_FRAME_CONTROLS_PASS', 'Count': len(rows), 'Cases': rows,
          'FullStackAndFunIdentityPreserved': True, 'GuardSha256': hashlib.sha256((ROOT / 'verify-original9.py').read_bytes()).hexdigest().upper(),
          'DecoderSha256': hashlib.sha256((ROOT / 'passive_etf.py').read_bytes()).hexdigest().upper(),
          'OriginalExecuted': False, 'NativeExecuted': False, 'SdkCompared': False, 'FunExecuted': False}
output.write_bytes((json.dumps(report, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'Count': len(rows)}))
