from pathlib import Path
import copy
import hashlib
import json
import re
import subprocess

r = Path.cwd()
old = r / 'artifacts/root-part323-composed1'
out = r / 'artifacts/root-part323-composed2'
c = out / 'candidate'
h = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_bytes())
write = lambda p, v: p.write_bytes((json.dumps(v, indent=2) + '\n').encode())
assert not out.exists()
assert h((old / 'source-freeze.json').read_bytes()) == 'C0D5852614A819F20152FFDD8894DA4B119D5E4B1898EB4933EF90519699D524'
assert h((old / 'source-resource-closure.json').read_bytes()) == 'B395DACAD8CEC82498A91B59342CC1217A66FE4F4ACA18AF1B8BC8EF63F6ABB7'
rows = read(old / 'source-resource-closure.json')['Files']
f = read(old / 'source-freeze.json')
assert len(rows) == 2349
for row in rows:
    b = (old / 'candidate' / row['Path']).read_bytes()
    assert len(b) == row['Bytes'] and h(b) == row['Sha256']
    p = c / row['Path']
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(b)
resource = 'tests/Erlang.Tests/Resources/Beam/guest_connected_bang322.provenance.json'
attrs = c / '.gitattributes'
rule = resource.encode() + b' binary'
assert rule not in attrs.read_bytes().splitlines()
attrs.write_bytes(attrs.read_bytes().rstrip(b'\n') + b'\n' + rule + b'\n')
name = 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'
before = read(c / name)
after = copy.deepcopy(before)
entries = [x for x in after['Files'] if x['Path'] == '.gitattributes']
assert len(entries) == 1 and entries[0]['HashMode'] == 'TextLF'
b = attrs.read_bytes().replace(b'\r\n', b'\n')
entries[0].update(Bytes=len(b), Sha256=h(b))
assert len(after['Files']) == 1133
assert {k: v for k, v in after.items() if k != 'Files'} == {k: v for k, v in before.items() if k != 'Files'}
write(c / name, after)
new_plan = h((c / name).read_bytes())
workflow = c / '.github/workflows/nif-reallocation-native.yml'
b, count = re.subn(rb'(?m)^([ \t]*REVIEWED_REALLOCATION_PLAN_SHA256: )[A-F0-9]{64}[ \t]*\r?$',
                  lambda m: m[1] + new_plan.encode(), workflow.read_bytes())
assert count == 1
workflow.write_bytes(b)
changed = {'.gitattributes', name, '.github/workflows/nif-reallocation-native.yml'}
actual = []
for row in rows:
    b = (c / row['Path']).read_bytes()
    if row['Path'] not in changed:
        assert len(b) == row['Bytes'] and h(b) == row['Sha256']
    actual.append(dict(Path=row['Path'], Bytes=len(b), Sha256=h(b)))
for plan in f['Plans']:
    if plan['Inputs'] == 1133:
        plan['Sha256'] = new_plan
    for item in read(c / plan['Path'])['Files']:
        b = (c / item['Path']).read_bytes()
        if item['HashMode'] == 'TextLF':
            b = b.replace(b'\r\n', b'\n')
        assert len(b) == item['Bytes'] and h(b) == item['Sha256'], item['Path']
write(out / 'source-resource-closure.json', dict(Files=actual))
mapping = {x['Path']: x for x in actual}
for row in f['Files']:
    row.update(mapping[row['Path']])
f.update(Status='SOURCE_ONLY323_EMBEDDED_PROVENANCE_CHECKOUT_ATTRIBUTE_REPAIR_PENDING_NEW_FULL',
         CandidateDirectory=str(c), FinalSourceClosureSha256=h((out / 'source-resource-closure.json').read_bytes()),
         PreviousSourceFreezeSha256=h((old / 'source-freeze.json').read_bytes()),
         PreviousSourceClosureSha256='B395DACAD8CEC82498A91B59342CC1217A66FE4F4ACA18AF1B8BC8EF63F6ABB7',
         MetadataOnlyChangedPaths=sorted(changed), AllThirteenCompiledCSharpAndBothResourcesUnchanged=True,
         PreviousSuccessfulFullAndNativePreserved=True,
         CheckoutHazardProofSha256='87C5EB2424ECB634C32EA1812DE59DCC5F1A9F0713B1BB872583BA5AB07CF112',
         OriginalComposerSha256=f['ComposerSha256'], ComposerSha256=h(Path(__file__).read_bytes()))
write(out / 'source-freeze.json', f)
blob = subprocess.check_output(['git', 'hash-object', '-w', '--no-filters', str(c / resource)]).decode().strip()
gitargs = ['git', '-c', 'core.autocrlf=true', '-c', 'core.attributesFile=' + str(attrs)]
checkout = subprocess.check_output(gitargs + ['cat-file', '--filters', '--path=' + resource, blob])
assert checkout == (c / resource).read_bytes()
attributes = subprocess.check_output(gitargs + ['check-attr', '-a', '--', resource]).decode()
assert 'text: unset' in attributes and 'binary: set' in attributes
write(out / 'repaired-checkout-proof.json', dict(Status='PASS_ACTUAL_GIT_AUTOCRLF_TRUE_RESOURCE_BYTES_RETAINED_WITH_REPAIRED_ATTRIBUTES',
      SourceFreezeSha256=h((out / 'source-freeze.json').read_bytes()), Resource=resource,
      CheckoutBytes=len(checkout), CheckoutSha256=h(checkout), EffectiveAttributes=attributes,
      Scope='Actual Git smudge using candidate rules via core.attributesFile; staged/published rule verification remains required.'))
print(json.dumps(dict(FreezeSha256=h((out / 'source-freeze.json').read_bytes()),
      ClosureSha256=f['FinalSourceClosureSha256'], OwnPlanSha256=new_plan,
      LegacyPlanSha256=f['Plans'][0]['Sha256'], CheckoutProofSha256=h((out / 'repaired-checkout-proof.json').read_bytes()))))
