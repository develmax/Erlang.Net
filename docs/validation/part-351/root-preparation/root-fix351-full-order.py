from pathlib import Path
import json,hashlib,shutil
r=Path.cwd();old=r/'artifacts/spawn-priority351-full-source1';new=r/'artifacts/spawn-priority351-full-source2';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert not new.exists();shutil.copytree(old,new)
p=new/'added-tests.json';v=json.loads(p.read_bytes());assert len(v['Names'])==11 and all(x.startswith('runtime/priority/') for x in v['Names'][:8]) and all(x.startswith('oracle/compiled-module/') for x in v['Names'][8:])
v['Names']=v['Names'][8:]+v['Names'][:8];p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
runner=new/'run-full.py';text=runner.read_text(encoding='utf-8');anchor="assert sha(seed/'registry-before.json')==config['RegistryBeforeSha256']";assert text.count(anchor)==1
text=text.replace(anchor,anchor+"\nassert sha(seed/'added-tests.json')==config['AddedTestsSha256']")
runner.write_text(text,encoding='utf-8',newline='\n')
p=new/'configuration.json';config=json.loads(p.read_bytes());config.update(SeedRelativePath=new.relative_to(r).as_posix(),AddedTestsSha256=sha(new/'added-tests.json'));p.write_text(json.dumps(config,indent=2)+'\n',encoding='utf-8',newline='\n')
receipt=dict(Status='PRE_SDK_NEW_TEST_ORDER_METADATA_CORRECTION',OldSeed=old.relative_to(r).as_posix(),NewSeed=new.relative_to(r).as_posix(),Finding='Program registers compiled-module catalog before appended HOST checks. Change new-name expectation to compiled3 then HOST8. No guest runtime or fixture change.',SourceFreezeSha256=sha(new/'source-freeze.json'),SourceClosureSha256=sha(new/'source-resource-closure.json'),AddedTestsSha256=sha(new/'added-tests.json'),RunnerSha256=sha(runner),ConfigurationSha256=sha(p),SdkExecuted=False,MainModified=False)
(new/'order-correction.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(receipt))
