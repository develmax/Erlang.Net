from pathlib import Path
import hashlib,json,re,shutil,sys
stage,root=map(Path,sys.argv[1:])
manifest=stage/'component-specs.json'
receipt=root/'receipt.json'
assert hashlib.sha256(receipt.read_bytes()).hexdigest()=='71b35578828dd6a7a7fa1e69fb86d8bcf0314b78f87d85bd939d609f09c86841'
report=json.loads(receipt.read_bytes())
assert report['nativeReleaseCases']==9
p=['production.patch','test.patch']
data=json.loads(manifest.read_bytes())
data['XmerlSourceOperandOrder']=dict(Root=root.as_posix(),Receipt='receipt.json',
    Report='html-scope.json',Hashes=['artifacts'],DigestField='sha256',Patches=p)
callbacks=stage/'synchronous-callbacks.json'
names=set(json.loads(callbacks.read_bytes()))
cls=''
for l in (root/p[1]).read_text().splitlines():
    if m:=re.match(r'^\+internal static class (\w+)',l):
        cls=m.group(1)
    if m:=re.match(r'^\+\s*internal static void (\w+)\(\)',l):
        assert cls
        names.add(f'Erlang.Tests.{cls}.{m.group(1)}')
callbacks.write_text(json.dumps(sorted(names),indent=2)+'\n',encoding='utf-8')
shutil.copyfile(root/'native-html.eval',stage/'observe-xmerl-html.eval')
(stage/'xmerl-native-plan.json').write_text(json.dumps({'Observations':9,'Receipt':str(receipt)},indent=2)+'\n')
manifest.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')

