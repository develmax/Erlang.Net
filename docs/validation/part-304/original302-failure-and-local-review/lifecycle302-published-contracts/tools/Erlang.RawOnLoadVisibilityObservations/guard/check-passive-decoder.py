import hashlib
import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('synthetic_passive_decoder', ROOT / 'passive_etf.py')
d = importlib.util.module_from_spec(spec)
spec.loader.exec_module(d)


def atom(value):
    data = value.encode()
    return bytes([119, len(data)]) + data


def integer(value):
    return bytes([97, value])


def pid(identity=1):
    return bytes([88]) + atom('n') + identity.to_bytes(4, 'big') + bytes(4) + (7).to_bytes(4, 'big')


def fun(free=(), module=None, owner=None):
    body = bytes([0]) + bytes(range(16)) + (2).to_bytes(4, 'big') + len(free).to_bytes(4, 'big') + (atom('fixture') if module is None else module) + integer(3) + integer(4) + (pid() if owner is None else owner) + b''.join(free)
    return bytes([112]) + (len(body) + 4).to_bytes(4, 'big') + body


def packet(term):
    return bytes([131]) + term


rows = []


def accepted(name, raw, verify):
    result = d.decode_exact(raw)
    assert verify(result)
    rows.append({'Name': name, 'Expected': 'ACCEPT_PASSIVE', 'Passed': True, 'Sha256': hashlib.sha256(raw).hexdigest().upper()})


def refused(name, raw):
    try:
        d.decode_exact(raw)
    except (ValueError, UnicodeDecodeError):
        rows.append({'Name': name, 'Expected': 'REFUSE', 'Passed': True, 'Sha256': hashlib.sha256(raw).hexdigest().upper()})
        return
    raise AssertionError('Corrupt ETF accepted: ' + name)


valid = fun()
accepted('zero-free-new-fun', packet(valid), lambda value: value[0] == 'new_fun' and value[-1] == ('raw_span', 1, len(valid)) and value[-2] == ())
accepted('free-variable-preserved', packet(fun([integer(42)])), lambda value: value[-2] == (('integer', 42),))
accepted('pid-identity-preserved', packet(pid(19)), lambda value: value == ('pid', ('atom', 'n'), 19, 0, 7, 88))
accepted('badfun-reason-and-stack-preserved', packet(bytes([104, 4]) + atom('exception') + atom('error') + bytes([104, 2]) + atom('badfun') + valid + bytes([106])), lambda value: value[1][2][1][1][-1][0] == 'raw_span' and value[1][3] == ('list', ()))
accepted('export-fun-passive', packet(bytes([113]) + atom('m') + atom('f') + integer(1)), lambda value: value == ('export_fun', ('atom', 'm'), ('atom', 'f'), ('integer', 1)))
size = int.from_bytes(valid[1:5], 'big')
refused('size-excludes-size-field', packet(valid[:1] + (size - 4).to_bytes(4, 'big') + valid[5:]))
refused('size-includes-tag-wrongly', packet(valid[:1] + (size + 1).to_bytes(4, 'big') + valid[5:]))
refused('size-cannot-consume-following-term', packet(valid[:1] + (size + 1).to_bytes(4, 'big') + valid[5:] + bytes([106])))
refused('truncated-fun', packet(valid[:-1]))
refused('wrong-module-type', packet(fun(module=integer(3))))
refused('wrong-pid-type', packet(fun(owner=atom('not_pid'))))
refused('oversized-free-count', packet(valid[:26] + (4294967295).to_bytes(4, 'big') + valid[30:]))
refused('extra-unaccounted-free-var', packet(valid + integer(9)))
refused('removed-legacy-fun', packet(bytes([117]) + bytes(20)))
refused('compressed-not-in-profile', packet(bytes([80]) + bytes(20)))
refused('reference-count-too-large', packet(bytes([90, 0, 6]) + atom('n') + bytes(28)))
refused('pid-node-not-atom', packet(bytes([88]) + integer(1) + bytes(12)))
refused('tuple-depth-over-bound', packet(bytes([104, 1]) * 130 + integer(0)))
refused('trailing-bytes', packet(valid) + bytes([106]))
accepted('binary-exact-bytes', packet(bytes([109]) + (3).to_bytes(4, 'big') + bytes([0, 128, 255])), lambda value: value == ('binary', bytes([0, 128, 255])))
accepted('bit-binary-padding-and-count', packet(bytes([77]) + (1).to_bytes(4, 'big') + bytes([3, 160])), lambda value: value == ('bit_binary', bytes([160]), 3))
accepted('float-raw-bits', packet(bytes([70]) + bytes.fromhex('3ff0000000000000')), lambda value: value == ('float_bits', bytes.fromhex('3ff0000000000000')))
accepted('modern-reference-full-identity', packet(bytes([90, 0, 2]) + atom('n') + (7).to_bytes(4, 'big') + (13).to_bytes(4, 'big') + (19).to_bytes(4, 'big')), lambda value: value == ('reference', ('atom', 'n'), 7, (13, 19), 90))
refused('truncated-binary', packet(bytes([109]) + (3).to_bytes(4, 'big') + bytes([0])))
refused('bit-binary-nonzero-padding', packet(bytes([77]) + (1).to_bytes(4, 'big') + bytes([3, 161])))
refused('truncated-float', packet(bytes([70]) + bytes(7)))
refused('truncated-reference', packet(bytes([90, 0, 2]) + atom('n') + bytes(8)))
nested = valid
for _ in range(40):
    nested = fun([nested])
value = d.decode_exact(packet(nested))
span_count = 0
while value[0] == 'new_fun':
    tag, start, length = value[-1]
    assert tag == 'raw_span' and packet(nested)[start] == 112 and length > 0 and start + length <= len(packet(nested))
    assert not isinstance(value[-1], bytes)
    span_count += 1
    value = value[-2][0] if value[-2] else ('end',)
assert span_count == 41
rows.append({'Name': 'nested-fun-zero-raw-copy-spans', 'Expected': 'ACCEPT_PASSIVE_SPANS', 'Passed': True, 'NestedFuns': span_count})
old_budget = d.MAX_TERMS
try:
    d.MAX_TERMS = 100
    refused('nested-fun-aggregate-term-budget', packet(nested))
    refused('string-ext-preallocation-budget', packet(bytes([107, 0, 101]) + bytes(101)))
    refused('tuple-container-preallocation-budget', packet(bytes([104, 100]) + integer(0) * 100))
finally:
    d.MAX_TERMS = old_budget
refused('fun-map-key-direct', packet(bytes([116]) + (1).to_bytes(4, 'big') + valid + integer(1)))
refused('fun-map-key-nested-tuple', packet(bytes([116]) + (1).to_bytes(4, 'big') + bytes([104, 1]) + valid + integer(1)))
refused('pid-map-key-profile-refusal', packet(bytes([116]) + (1).to_bytes(4, 'big') + pid() + integer(1)))
accepted('atom-map-key-fun-value-spans', packet(bytes([116]) + (1).to_bytes(4, 'big') + atom('key') + valid), lambda value: value[0] == 'map' and next(iter(value[1]))[1][-1][0] == 'raw_span')
assert d.decode_exact(packet(pid(1))) != d.decode_exact(packet(pid(2)))
output = Path(sys.argv[1])
assert not output.exists()
report = {'Status': 'SYNTHETIC_PASSIVE_DECODER_CONTROLS_PASS', 'Cases': rows, 'Count': len(rows), 'OriginalExecuted': False, 'NativeExecuted': False, 'SdkCompared': False, 'FunExecuted': False, 'DecoderSha256': hashlib.sha256((ROOT / 'passive_etf.py').read_bytes()).hexdigest().upper()}
output.write_bytes((json.dumps(report, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'Count': len(rows)}))
