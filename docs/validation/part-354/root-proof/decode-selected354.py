from pathlib import Path
import hashlib
import json

folder = Path('artifacts/size-bif354-selected-handler-actual1')
data = (folder / 'selected-handler.etf').read_bytes()
assert hashlib.sha256(data).hexdigest().upper() == '703BD5337A6545A091C3961149D407548C9A89AC807AFD7E7F78147EDB35B33A'
assert data[0] == 131
offset = 1

def take(size):
    global offset
    assert 0 <= size <= len(data) - offset
    value = data[offset:offset + size]
    offset += size
    return value

def integer(size):
    return int.from_bytes(take(size), 'big')

def term(depth=0):
    assert depth < 64
    tag = integer(1)
    if tag == 97:
        return integer(1)
    if tag == 98:
        return int.from_bytes(take(4), 'big', signed=True)
    if tag in (100, 118, 119):
        size = integer(1 if tag == 119 else 2)
        return {'Atom': take(size).decode('latin1' if tag == 100 else 'utf-8')}
    if tag in (104, 105):
        size = integer(1 if tag == 104 else 4)
        assert size < 1024
        return {'Tuple': [term(depth + 1) for _ in range(size)]}
    if tag == 106:
        return []
    if tag == 108:
        size = integer(4)
        assert size < 1024
        elements = [term(depth + 1) for _ in range(size)]
        tail = term(depth + 1)
        return elements if tail == [] else {'List': elements, 'Tail': tail}
    raise AssertionError('Unexpected diagnostic ETF tag: ' + str(tag))

observed = term()
assert offset == len(data)
report = {'Status': 'PASSIVE_TYPED_DECODE_OF_ACTUAL_RAW_OBSERVATION',
          'EtfSha256': hashlib.sha256(data).hexdigest().upper(),
          'Observed': observed, 'ExpectedTraceInjected': False}
output = folder / 'typed-observation.json'
assert not output.exists()
output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(observed))
