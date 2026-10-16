from pathlib import Path
import hashlib
import json
import subprocess

r = Path.cwd()
p = 'tests/Erlang.Tests/Resources/Beam/guest_connected_bang322.provenance.json'
raw = (r / p).read_bytes()
h = lambda b: hashlib.sha256(b).hexdigest().upper()
git = lambda *args, **kw: subprocess.check_output(['git', *args], cwd=r, **kw)
blob = git('hash-object', '-w', '--no-filters', p).decode().strip()
out = r / 'artifacts/root323-provenance-checkout-probe1'
assert not out.exists()
rows = []
for value in ['false', 'true']:
    checkout = git('-c', 'core.autocrlf=' + value, 'cat-file', '--filters', '--path=' + p, blob)
    rows.append({'CoreAutocrlf': value, 'Bytes': len(checkout), 'Sha256': h(checkout),
                 'RawResourceBytesEqual': checkout == raw, 'CarriageReturns': checkout.count(b'\r')})
assert rows[0]['RawResourceBytesEqual'] and not rows[1]['RawResourceBytesEqual']
attrs = git('check-attr', '-a', '--', p)
assert not attrs
out.mkdir()
report = {'Status': 'CONFIRMED_PREPUBLICATION_RESOURCE_CHECKOUT_HASH_HAZARD_NOT_RUNTIME_FAILURE',
          'Path': p, 'GitRawBlob': blob, 'WorkingBytes': len(raw), 'WorkingSha256': h(raw),
          'ActualAttributes': [], 'Profiles': rows, 'Published': False,
          'ExistingMain1Full6685AndNativeRemainPassed': True,
          'RequiredRepair': 'Declare the embedded provenance resource binary, then rebind source and validation authority.'}
(out / 'report.json').write_bytes((json.dumps(report, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'ReportSha256': h((out / 'report.json').read_bytes()), 'Profiles': rows}))
