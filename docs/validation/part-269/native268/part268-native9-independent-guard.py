from pathlib import Path
import hashlib,json,zipfile,importlib.util,re

a=Path(__file__).parent;root=a/'part-268-pinned-apply-optimizer';n=root/'apply-optimizer';source=a/'part268-committed-optimizer-source'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
spec=importlib.util.spec_from_file_location('bounded_etf',a/'part264-committed-iodata-source/verify-iodata-contrasts.py');etf=importlib.util.module_from_spec(spec);spec.loader.exec_module(etf)
decode=lambda p:etf.decode(p.read_bytes())
atom=lambda value:('atom',value)
head='3a7ab4a6ac3adeee2242545a25ad2ace27efa8e7';pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
m=json.loads((a/'part-268-pinned-apply-optimizer-metadata.json').read_text(encoding='utf-8-sig'));p=json.loads((a/'part-268-native9-latest-provenance.json').read_text(encoding='utf-8-sig'))
assert m['Run']['head_sha']==p['Run']['head_sha']==head and m['Run']['id']==38570628234 and p['Job']['id']==115874566807
assert m['Run']['conclusion']==p['Job']['conclusion']=='success' and all(s['conclusion']=='success' for s in p['Job']['steps'])
archive=a/'part-268-pinned-apply-optimizer.zip';assert sha(archive)==m['ZipSHA256']==m['Artifact']['digest'][7:].upper()
with zipfile.ZipFile(archive) as z:
    files=[i for i in z.infolist() if not i.is_dir()]
    assert {i.filename for i in files}=={f.relative_to(root).as_posix() for f in root.rglob('*') if f.is_file()}
    for i in files:assert z.read(i)==(root/i.filename).read_bytes()
report=json.loads((n/'apply-optimizer.json').read_text());plan=json.loads((source/'predeclared-contracts.json').read_text())
assert report['Complete'] and report['ExpectedExecutions']==report['ActualExecutions']==report['Captured']==9
assert not report['SdkExpectedUsed'] and not report['SemanticCompatibilityClaim']
assert report['SourceCommit']==pin and report['SourceTag']=='OTP-29.1.1' and report['DeclarationSha256']==sha(source/'predeclared-contracts.json')
for filename in ['apply_optimizer_probe.erl','apply_optimizer_local.erl','predeclared-contracts.json']:assert (n/filename).read_bytes()==(source/filename).read_bytes()
for row in plan['CanonicalSourceBinding']['Sources']:assert sha(n/row['Path'])==row['Sha256']
build=report['BuildEvidence'];assert build==json.loads((root/'beam-source-build-evidence.json').read_text()) and build['HarnessCommit']==head and build['SourceCommit']==pin and build['SourceBuildCompleted'] and build['RunId']==str(m['Run']['id'])
assert build['SourceBuildLogSha256']==sha(root/'beam-otp-build.log')==sha(n/'source-build.log')
assert sha(n/'input.beam')=='59C8A07B362337A38E76DB3661B6B91BF3650D235631FA7D47B20488365BC20D'
for prefix in ['runtime','compile','apply_optimizer_probe.disassemble','apply_optimizer_local.disassemble']:
    assert (n/(prefix+'.exit')).read_text().strip()=='0' and (n/(prefix+'.raw-complete')).read_text().strip()=='true'
runtime=decode(n/'runtime.etf');assert runtime[0]=='tuple' and runtime[1][2]==atom('jit')
string=lambda t:bytes(x[1] for x in t[1]).decode()
assert string(runtime[1][0])=='29' and string(runtime[1][1])=='17.1' and string(runtime[1][3])==build['InstalledOtpRoot']
outer={row['Name']:row for row in report['Files']};assert len(outer)==len(report['Files'])
assert set(outer)=={f.name for f in n.iterdir() if f.is_file()}
inventory=[]
for name,row in outer.items():
    actual={'Length':(n/name).stat().st_size,'Sha256':sha(n/name)}
    if actual['Length']!=row['Length'] or actual['Sha256']!=row['Sha256']:inventory.append({'Name':name,'Declared':row,'Actual':actual})
assert len(inventory)==1 and inventory[0]['Name']=='apply-optimizer.json'
(a/'part268-native9-self-inventory-discrepancy.json').write_text(json.dumps({'Status':'DeclaredSelfReportDigestMismatchPreserved','Differences':inventory,'Cause':'ApplyExporter.Report evaluates Files(output) while the previous report exists, then writes and replaces that report. Its own entry therefore describes the previous snapshot.','RawReportRewritten':False},indent=2)+'\n')
disassembly=(n/'apply_optimizer_probe.disassembly.txt').read_text();blocks=re.split(r'(?=\{function,)',disassembly)
rows=[]
for row,binding in zip(report['Results'],plan['ScenarioBindings']):
    name=row['Name'];folder=n/name;assert name==binding['Name'] and row['Captured'] and row['Stage']=='completed'
    assert row['Process']=={'ExitCode':0,'TimedOut':False,'RawComplete':True}
    listed={x['Name']:x for x in row['Files']};assert set(listed)=={f.name for f in folder.iterdir() if f.is_file()}
    for filename,item in listed.items():assert sha(folder/filename)==item['Sha256'] and (folder/filename).stat().st_size==item['Length']
    assert decode(folder/'prepared.etf')[0]=='reference'
    actual=decode(folder/'actual.etf');messages=decode(folder/'messages.etf');assert actual[0]=='tuple'
    values=actual[1]
    if values[0]==atom('exception'):assert values[1]==atom('error') and values[3][0]=='list' and values[3][1]
    reason=values[2] if values[0]==atom('exception') else None
    first=values[3][1][0] if reason else None
    text=(folder/'actual.txt').read_text()
    if name=='local-override':
        assert values[0]==atom('value') and values[1]==('tuple',(atom('local'),atom('erlang'),atom('call_on_load_function'),('list',(atom('beam_on_load_fresh_success'),))))
    if name=='item-effect':assert messages==('list',(atom('item_evaluated'),))
    else:assert messages==('list',())
    module=binding['Module'];function=binding['NativeFunctionName']
    if module=='apply_optimizer_probe':block=next(b for b in blocks if b.startswith('{function,'+function+','))
    else:block=next(b for b in re.split(r'(?=\{function,)',(n/'apply_optimizer_local.disassembly.txt').read_text()) if b.startswith('{function,run,'))
    rows.append({'Name':name,'ActualEtfSha256':sha(folder/'actual.etf'),'ActualRawText':text,'MessagesEtfSha256':sha(folder/'messages.etf'),'RawFunctionDisassembly':block,'ObservedReasonDecoded':repr(reason),'FirstRawFrameDecoded':repr(first),'HypothesisAcceptedAsExpected':False})
digest=json.loads((a/'part-268-native9-job.log.digest.json').read_text(encoding='utf-8-sig'));assert sha(a/'part-268-native9-job.log')==digest['SHA256']
assert pin in (a/'part-268-native9-job.log').read_text()
result={'Status':'NineNativeObservationsProvenanceAndRawTermsVerified','Head':head,'RunId':m['Run']['id'],'JobId':p['Job']['id'],'ArtifactId':m['Artifact']['id'],'ZipSha256':sha(archive),'AllExtractedRawZipBytesExact':True,'RawFileCount':len(files),'AllDeclaredInventoryHashesExact':False,'SelfInventoryMismatchPreserved':True,'ScenarioFileInventoriesExact':True,'CapturedNativeVms':9,'SdkExpectedUsed':False,'SemanticCompatibilityClaim':False,'PredeclaredHypothesesUnchanged':True,'Rows':rows}
(a/'part268-native9-independent-guard.json').write_text(json.dumps(result,indent=2)+'\n')
print(result['Status'],len(files),'rawfiles; selfinventory mismatch retained')
