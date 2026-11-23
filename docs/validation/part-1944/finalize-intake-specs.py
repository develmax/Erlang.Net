from pathlib import Path
import hashlib,json,re,sys
stage,asn=map(Path,sys.argv[1:])
root=asn/'binary-choice-wire-unit'
receipt=root/'final-receipt.json'
data=json.loads(receipt.read_text(encoding='utf-8-sig'))
patches=['binary-choice-wire-unit.patch','binary-choice-wire-unit-tests.patch']
assert len(patches)==2
binding=data['StrictReplay']['Patches']
assert all(hashlib.sha256((root/name).read_bytes()).hexdigest()==binding[name].lower() for name in patches)
specs=stage/'component-specs.json'
parts=json.loads(specs.read_bytes())
parts['ForwardedBinaryMeasurementUnit']=dict(Root=root.as_posix(),Receipt=receipt.name,
    Report='candidate.json',Hashes=['StrictReplay','Patches'],Patches=patches)
specs.write_text(json.dumps(parts,indent=2)+'\n',encoding='utf-8')
sync=set()
for part in parts.values():
    patch=Path(part['Root'])/part['Patches'][1]
    section=patch.read_text(encoding='utf-8-sig')
    if re.search(r'^\+\s*internal static void Run\(\)',section,re.MULTILINE):
        classes=re.findall(r'^\+internal static class (\w+)',section,re.MULTILINE)
        assert classes,patch
        sync.update(f'Erlang.Tests.{name}.Run' for name in classes)
(stage/'synchronous-callbacks.json').write_text(json.dumps(sorted(sync),indent=2)+'\n',encoding='utf-8')

