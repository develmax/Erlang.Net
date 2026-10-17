from pathlib import Path
import hashlib
import json

def decode(data):
    assert data[0] == 131
    offset = 1

    def take(size):
        nonlocal offset
        assert 0 <= size <= len(data) - offset
        value = data[offset:offset + size]
        offset += size
        return value

    def number(size):
        return int.from_bytes(take(size), 'big')

    def term(depth=0):
        assert depth < 64
        tag = number(1)
        if tag == 97:
            return number(1)
        if tag == 98:
            return int.from_bytes(take(4), 'big', signed=True)
        if tag in (100, 118, 119):
            size = number(1 if tag == 119 else 2)
            return {'Atom': take(size).decode('latin1' if tag == 100 else 'utf-8')}
        if tag in (104, 105):
            size = number(1 if tag == 104 else 4)
            assert size < 1024
            return {'Tuple': [term(depth + 1) for _ in range(size)]}
        if tag == 106:
            return []
        if tag == 108:
            size = number(4)
            assert size < 1024
            values = [term(depth + 1) for _ in range(size)]
            tail = term(depth + 1)
            return values if tail == [] else {'List': values, 'Tail': tail}
        raise AssertionError('Unsupported diagnostic ETF tag ' + str(tag))

    result = term()
    assert offset == len(data)
    return result

files = [Path('artifacts/size-bif354-selected-handler-actual1/selected-handler.etf'),
         Path('artifacts/size-bif354-focused-actual6/target-32/selected-handler.etf')]
hashes = ['703BD5337A6545A091C3961149D407548C9A89AC807AFD7E7F78147EDB35B33A',
          'DC3236F48105D660E6403C2FE64C81C52D324BCC84CF5D18BEAF84A82E5400A4']
values = []
for path, expected in zip(files, hashes):
    data = path.read_bytes()
    assert hashlib.sha256(data).hexdigest().upper() == expected
    values.append(decode(data))
before, after = (value['Tuple'] for value in values)
old_host, new_host = before[0]['Tuple'], after[0]['Tuple']
assert old_host[:3] == new_host[:3]
assert len(old_host[3]) == len(new_host[3]) == 2
assert old_host[3][0] == new_host[3][0] == new_host[3][1]
assert old_host[3][1]['Tuple'][2] == 3
old_source, new_source = before[1]['Tuple'], after[1]['Tuple']
assert old_source[:3] == new_source[:3]
assert old_source[3][0]['Tuple'][1] != old_source[1]
assert new_source[3][0]['Tuple'][1] == new_source[1]
assert new_source[3][0]['Tuple'][2] == old_source[3][0]['Tuple'][2]
output = Path('artifacts/root-selected354-before-after.json')
assert not output.exists()
output.write_text(json.dumps({
    'Status': 'PASS_ACTUAL_HOST_SELECTED_HANDLER_BEFORE_AFTER_RELATIONS',
    'Sources': [{'Path': str(path), 'Sha256': digest} for path, digest in zip(files, hashes)],
    'Before': values[0], 'After': values[1],
    'OldAssertionsChanged': False, 'NativeParityClaimed': False,
    'Scope': 'Typed observations plus unchanged HOST contract assertions, not original VM output.'
}, indent=2) + '\n', encoding='utf-8')
print(hashlib.sha256(output.read_bytes()).hexdigest().upper())
