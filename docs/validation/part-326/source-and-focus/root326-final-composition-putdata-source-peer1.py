from pathlib import Path
import hashlib,json,re,subprocess,zipfile
R=Path.cwd();D=R/'artifacts/root-part326-composed1';C=D/'candidate';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
F=D/'source-freeze.json';CL=D/'source-resource-closure.json';assert h(F.read_bytes())=='98F62E3123C7E205F90485735727E619B7094DB9C9B8C609E230B4EBCA1FF02D';assert h(CL.read_bytes())=='098DD51D477CBD68C004AAF7D45115B06A0F7566370B56A05B75CBCCB68D989F'
f=read(F);rows=read(CL)['Files'];assert (f['SourcePaths'],f['CSharpPaths'],len(rows))==(13,8,2370)
owned={x['Path']:x for x in f['Files']};assert len(owned)==13
base=read(R/'artifacts/root-part325-main1/source-resource-closure.json')['Files'];bm={x['Path']:x for x in base};assert len(bm)==2366
for x in rows:
 raw=(C/x['Path']).read_bytes();assert len(raw)==x['Bytes'] and h(raw)==x['Sha256']
 if x['Path'] not in owned:assert x==bm[x['Path']]
assert sum(x['Path'] not in owned for x in rows)==2357
for name,x in owned.items():
 raw=(C/name).read_bytes();assert h(raw)==x['Sha256'] and len(raw)==x['Bytes'] and x['Added']==(name not in bm)
 if name in bm:assert x['ParentSha256']==bm[name]['Sha256']
G=R/'artifacts/spawn-async-dist326-focus1';green=read(G/'formatted-source-freeze.json');assert len(green['Files'])==8
for x in green['Files']:assert (C/x['Path']).read_bytes()==(G/'candidate'/x['Path']).read_bytes() and h((C/x['Path']).read_bytes())==x['Sha256']
assert (G/'tokens-before.json').read_bytes()==(G/'tokens-after.json').read_bytes()
Z=R/'artifacts/spawn-async-dist326-candidate2/baseline-source-resources.zip'
with zipfile.ZipFile(Z) as z:
 assert set(z.namelist())==set(bm)
 for name,x in bm.items():assert len(z.read(name))==x['Bytes'] and h(z.read(name))==x['Sha256']
 oldattrs=z.read('.gitattributes');assert (C/'.gitattributes').read_bytes()==oldattrs+b'docs/validation/part-326/** binary\n'
 for p in f['Plans']:
  name=p['Path'];value=read(C/name);prior=json.loads(z.read(name));raw=(C/name).read_bytes();assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf') and h(raw)==p['Sha256']
  assert len(value['Files'])==p['Inputs'] and [x['Path'] for x in value['Files']]==[x['Path'] for x in prior['Files']]
  assert {k:v for k,v in value.items() if k!='Files'}=={k:v for k,v in prior.items() if k!='Files'}
  assert [x['HashMode'] for x in value['Files']]==[x['HashMode'] for x in prior['Files']]
  for x in value['Files']:
   b=(C/x['Path']).read_bytes();assert x['HashMode'] in ['TextLF','BinaryExact'];b=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b
   assert len(b)==x['Bytes'] and h(b)==x['Sha256']
  workflow=(C/p['Workflow']).read_bytes();assert re.findall(rb'^\s*'+p['Pin'].encode()+rb': ([A-F0-9]{64})$',workflow,re.M)==[p['Sha256'].encode()]
  oldpin=h(z.read(name));expected,count=re.subn(rb'(?m)^([ \t]*'+p['Pin'].encode()+rb': )'+oldpin.encode()+rb'[ \t]*\r?$',lambda m:m[1]+p['Sha256'].encode(),z.read(p['Workflow']));assert count==1 and expected==workflow
assert sorted((p['Inputs'],p['Sha256']) for p in f['Plans'])==[(1109,'A673C88561EC5E31E1F69BE5795519A3AA658E013B6000910B03E192A520E6A5'),(1134,'53500EFC145705B70364DE7BC19E60FBCE55C62C7C4C00CB66712B1B8F01F9E4')]
# Independent published-parent source batching, exact working bytes or canonicalLF only.
paths=list(bm);blob=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],input=''.join(f['ParentHead']+':'+x+'\n' for x in paths).encode());offset=0;canonical=0
with zipfile.ZipFile(Z) as z:
 for name in paths:
  end=blob.index(b'\n',offset);header=blob[offset:end].split();assert header[1]==b'blob';n=int(header[2]);start=end+1;b=blob[start:start+n];offset=start+n+1;assert blob[offset-1:offset]==b'\n'
  if b!=z.read(name):assert b==z.read(name).replace(b'\r\n',b'\n');canonical+=1
assert offset==len(blob)
for p in f['ActualGreenAndHistoricalRedGuards']:
 path=R/p['Path'];assert h(path.read_bytes())==p['Sha256'] and read(path)['Passed'] is True
 for x in read(path)['Bindings']:assert h((R/x['Path']).read_bytes())==x['Sha256']
assert f['ExpectedFullPassed']==6708 and f['PublicMfaDelta']==0 and f['Original326Executed'] is False
report={'Status':'PASS_INDEPENDENT_FINAL326_SOURCE_COMPOSITION_ACTUAL_GREEN_HISTORICAL_RED_BOUND','Passed':True,'SourceFreezeSha256':h(F.read_bytes()),'SourceClosureSha256':h(CL.read_bytes()),'PublishedParent':f['ParentHead'],'SourcePaths':13,'CSharpPaths':8,'SourceClosurePaths':2370,'All2370SourceRowsRehashed':True,'All2366ParentRawAndPublishedRowsVerified':True,'CanonicalLfPublicationDifferences':canonical,'AllOutsideOwned2357ParentRowsRawExact':True,'AllEightActualCompiledFormattedCSharpRawExact':True,'TwoNativePlansInputPathSetsHashModesNonFilesPoliciesExactParent':True,'BothCanonicalLfRawPlanHashesEqualWorkflowPins':True,'Docs326BinaryAttributeOnlyAppend':True,'Plans':f['Plans'],'ActualGreenAndHistoricalRedGuards':f['ActualGreenAndHistoricalRedGuards'],'ActualFullNativeReadinessStillFuture':True,'ExpectedFullPassedNotObserved':6708,'Original326Executed':False,'Findings':[],'NoReviewerMainGitMutationBuildSdkNativeVmNetworkExecution':True,'ReviewerScriptSha256':h(Path(__file__).read_bytes())}
out=R/'artifacts/root326-final-composition-putdata-source-peer1.json';assert not out.exists();out.write_bytes((json.dumps(report,indent=2)+'\n').encode());print(h(out.read_bytes()))
