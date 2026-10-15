"""Root only. All validation gates and complete copy index checked before mutation."""
import importlib.util,sys,json,re
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common',Path(__file__).with_name('part306-common.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c,f,index=m.gates(sys.argv[1]); assert not m.OUT.exists()
ready=m.read(m.pinned(c['ReadinessResult']))
assert ready['LocalPassedTests']==6627 and ready['RegisteredMfas']==504 and ready['RegisteredMfaDelta']==1 and ready['ComparisonTestReport']==c['ComparisonTestReport']
volume=m.pinned(c['ActiveChangelogBefore']); entry=m.pinned(c['ChangelogDraft']).read_text(encoding='utf-8-sig').replace('\r\n','\n').rstrip()+'\n'
assert entry.startswith('## Part 306') and b'## Part 306' not in volume.read_bytes()
combined=volume.read_bytes()+b'\r\n'+entry.replace('\n','\r\n').encode(); assert len(combined)<=32768 and len(combined.splitlines())<=400
note=m.pinned(c['MetadataNote']).read_text(encoding='utf-8-sig').strip(); assert note.startswith('Part306:')
changes={}
for relative in c['MetadataPaths']:
    p=m.safe(relative); data=p.read_bytes(); assert b'Part306:' not in data
    match=re.search(br'\r?\n\r?\n',data); assert match
    nl=b'\r\n' if b'\r\n' in data else b'\n'; changes[p]=data[:match.end()]+note.encode()+nl+nl+data[match.end():]
idx=m.pinned(c['ChangelogIndexBefore']); before=c['ChangelogIndexOldLabel'].encode(); after=c['ChangelogIndexNewLabel'].encode()
assert idx.read_bytes().count(before)==1; changes[idx]=idx.read_bytes().replace(before,after)
attrs=m.safe('.gitattributes'); assert b'docs/validation/part-306/** binary' in attrs.read_bytes()
copies=[]
for row in index:
    src=m.safe(row['SourcePath']); data=src.read_bytes(); assert m.sha(data)==row['Sha256']; copies.append((m.safe(row['Destination']),data))
for dest,data in copies: dest.parent.mkdir(parents=True,exist_ok=True); dest.write_bytes(data)
for dest,data in changes.items(): dest.write_bytes(data)
volume.write_bytes(combined)
receipt={'Status':'ACTUAL_MAIN306_FULL_NATIVE_AND_EXACT_EVIDENCE_INDEX','ParentHead':c['ParentHead'],'Passed':6627,'Failed':0,'SourcePaths':f['SourcePaths'],'CSharpPaths':f['CSharpPaths'],'SourceFreezeSha256':c['SourceFreeze']['Sha256'],'Registry':504,'RegistryDelta':1,'ErlangPresence':346,'NativeInputs':1012,'Previous305ProofFiles':259,'FreshOriginal306Verified':False,'WholeCompatibilityComplete':False,'PublicLoadNifActivated':False,'Closed0045Sha256':m.CLOSED45,'Files':[{'Path':p.relative_to(m.OUT).as_posix(),'Bytes':len(data),'Sha256':m.sha(data)} for p,data in copies]}
receipt.update({'IntegrationAndPackagesPassed':True,'NativeTrustedWindowsProfilePassed':True,'ActualGeneratedCSharpRoutePassed':True,'AddedNames':c['AddedNames'],'ReviewedSourcePlanSha256':c['NativeSourcePlan']['Sha256'],'OptionalIoExports':506,'OptionalIoEvidenceMethod':'Actual full HOST integration; no independent additional execution'})
(m.OUT/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
assert m.sha((m.ROOT/'docs/changelog/0045.md').read_bytes())==m.CLOSED45
print(json.dumps({'Passed':6627,'ProofFiles':len(copies),'VolumeBytes':len(combined)}))
