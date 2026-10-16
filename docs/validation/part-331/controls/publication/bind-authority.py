"""Root-only post-publication binder. Reads Git/proofs; no POST or Git mutation."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser()
for n in ['published-head','verified-remote-head','bindings','bindings-sha256']:p.add_argument('--'+n,required=True)
a=p.parse_args();r=Path.cwd().resolve();h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());g=lambda *x:subprocess.check_output(['git','--no-optional-locks',*x],cwd=r)
assert re.fullmatch('[0-9a-f]{40}',a.published_head) and a.published_head==a.verified_remote_head==g('rev-parse','HEAD').decode().strip()
assert g('rev-parse','HEAD^').decode().strip()=='b7484cb0dacc13c04ac818a3c58e4ff4f6afe344'
assert not g('diff','--name-only').strip() and not g('diff','--cached','--name-only').strip()
def safe(name):
 q=Path(name);assert not q.is_absolute() and '..' not in q.parts and not any(c in name for c in '\0\r\n');v=(r/q).resolve();assert v.is_relative_to(r);return v
manifest=Path(a.bindings).resolve();assert manifest.is_relative_to(r/'artifacts') and h(manifest.read_bytes())==a.bindings_sha256.upper()
bindings=j(manifest)['RequiredBindings'];roles={x['Role']:x for x in bindings}
assert len(bindings)==len(roles)==9 and set(roles)=={'SourceFreeze','FullGuard','NativeGuard','IndependentActualPeer','StageGuard','IndependentStagePeer','EvidenceReceipt','Readiness','DispatcherSourcePeer'}
values={}
for e in bindings:
 q=safe(e['Path']);assert re.fullmatch('[A-F0-9]{64}',e['Sha256']) and h(q.read_bytes())==e['Sha256'];values[e['Role']]=j(q)
assert roles['SourceFreeze']['Sha256']=='559184A858FEDF503164F7D303B9E17DEFAD80292533EE4AB7D9B7C221EBCA5E'
assert roles['FullGuard']['Sha256']=='8914E21E6485FB04504DE9B654E818524FD3C44CD009EC730B1F98E6E2E6C05A'
f,full,native,stage,receipt,ready=[values[k] for k in ['SourceFreeze','FullGuard','NativeGuard','StageGuard','EvidenceReceipt','Readiness']]
assert (f['SourcePaths'],f['CSharpPaths'],f['SourceClosurePaths'],f['ExpectedTests'],f['ExpectedRegistry'])==(19,15,2417,6775,515)
assert f['ParentHead']=='b7484cb0dacc13c04ac818a3c58e4ff4f6afe344'
assert (full['Passed'],full['Failed'],full['IntegrationResults'],full['Registry'],full['ErlangMfas'],full['ProcLibMfas'])==(6775,0,11,515,351,6)
closure='9E6119DDB243964E57FB9AC2A35B550B03B4B4495DE54AD6505BE6CB5A76FF34'
assert f['SourceClosureSha256']==full['SourceClosureSha256']==closure
assert native['Status']=='PASS_ACTUAL_MAIN331_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE' and native['FullPassed']==6775 and native['SourceFilesRehashed']==2417
assert native['SourceFreezeSha256']==full['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and native['ActualFullGuard']['Sha256']==roles['FullGuard']['Sha256']
assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'])==(6775,515,509,6)
assert receipt['Files'] and receipt['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and receipt['ActualFullGuardSha256']==roles['FullGuard']['Sha256'] and receipt['ActualNativeGuardSha256']==roles['NativeGuard']['Sha256'] and receipt['ActualReadinessSha256']==roles['Readiness']['Sha256']
assert receipt['SourceClosureSha256']==closure
assert stage['Status'].startswith('PASS') and stage['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and stage['EvidenceReceiptSha256']==roles['EvidenceReceipt']['Sha256']
assert (stage['SourcePaths'],stage['CSharpPaths'])==(19,15)
for key in ['ProofsBinaryExact','AllCSharpBinaryExact','DefaultGitWhitespaceCheckPassed']:assert stage[key] is True
assert stage['LicenseLocalStaged'] is False and stage['CacheStaged'] is False
for k in ['IndependentActualPeer','IndependentStagePeer']:
 peer=values[k];assert peer.get('Passed') is True or str(peer.get('Status','')).startswith('PASS')
 for pk,rk in [('SourceFreezeSha256','SourceFreeze'),('EvidenceReceiptSha256','EvidenceReceipt'),('StageGuardSha256','StageGuard'),('FullGuardSha256','FullGuard'),('NativeGuardSha256','NativeGuard')]:assert peer[pk]==roles[rk]['Sha256']
 assert peer['CachedBeamAndProvenanceBinaryAttributesAndBytesExact'] is True
dispatcher=Path(__file__).with_name('dispatch-once.ps1');dp=values['DispatcherSourcePeer'];assert str(dp['Status']).startswith('PASS') and dp['DispatcherSha256']==h(dispatcher.read_bytes())
records=stage['Records'];names={x['Path'] for x in records};assert len(records)==len(names)==stage['StagedPaths']
assert names==set(g('diff','HEAD^','HEAD','--name-only','-z').decode().split('\0'))-{''}
sources={x['Path'] for x in f['Files']};proofs={x['TargetPath'] for x in receipt['Files']}|{'docs/validation/part-331/evidence-index.json','docs/validation/part-331/evidence-receipt.json'};metadata=names-sources-proofs
assert len(sources)==19 and sources<=names and proofs<=names and not sources&proofs
assert all(n.startswith('docs/') or n in ['AGENTS.md','CHANGELOG.md','README.md'] for n in metadata)
assert all(not n.startswith('artifacts/') and not any(v.lower() in {'license.local.txt','bin','obj','__pycache__','.cache'} for v in Path(n).parts) for n in names)
index=safe('docs/validation/part-331/evidence-index.json');assert j(index)==receipt['Files'] and h(index.read_bytes())==receipt['EvidenceIndexSha256']
seals=stage['ActualStagedRawSourcePlanSeals'];resources=stage['ActualStagedEmbeddedBeamAndProvenanceResources'];assert len(seals)==4 and len(resources)==2
allnames=sorted(names|{x[k] for x in seals for k in ['Plan','Workflow']}|{x['Path'] for x in resources})
for n in allnames:safe(n)
raw=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],cwd=r,input=''.join(a.published_head+':'+n+'\n' for n in allnames).encode());off=0;blobs={}
for n in allnames:
 end=raw.index(b'\n',off);header=raw[off:end].split();assert len(header)==3 and header[1]==b'blob';size=int(header[2]);off=end+1;blobs[n]=raw[off:off+size];off+=size;assert raw[off:off+1]==b'\n';off+=1
assert off==len(raw)
for e in records:assert len(blobs[e['Path']])==e['StagedBytes'] and h(blobs[e['Path']])==e['StagedSha256']
for e in receipt['Files']:assert len(blobs[e['TargetPath']])==e['Bytes'] and h(blobs[e['TargetPath']])==e['Sha256']
for e in f['Files']:assert len(blobs[e['Path']])==e['Bytes'] and h(blobs[e['Path']])==e['Sha256']
for e in seals:
 b=blobs[e['Plan']];digest=h(b);assert b'\r' not in b and not b.startswith(b'\xef\xbb\xbf') and digest==e['ActualStagedBlobSha256']==e['ActualStagedWorkflowPin']==e['WorktreeSha256']
 assert e['GitEolAttribute']=='lf' and re.findall(rb'(?m)^[ \t]+'+e['EnvironmentKey'].encode()+rb':[ \t]+([A-F0-9]{64})[ \t]*\r?$',blobs[e['Workflow']])==[digest.encode()]
for e in resources:assert e['BinarySetTextUnset'] and e['WorktreeEqualsStagedBytes'] and h(blobs[e['Path']])==e['StagedRawSha256']
cp=safe('artifacts/root-part331-main/source-resource-closure.json');assert h(cp.read_bytes())==closure;rows=j(cp)['Files'];assert len(rows)==2417
for e in rows:
 b=safe(e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256']
assert {x['Inputs']:x['Sha256'] for x in f['Plans']}=={1123:'D5959E69FADBEC0FB6D48DF356965F2212050454EA1B3B35927CBF73BEA09A5E',1148:'E3221E2A0B12DA57598BF67B00285538AD964396EB0747F254F1FE2FAAB6C6A1'}
authority=dict(Status='ROOT_REVIEWED_PART331_POST_PUBLICATION_DISPATCH_AUTHORITY',Part=331,RootExecutionApproved=True,PublishedHead=a.published_head,RemoteHeadReportedVerifiedByRoot=a.verified_remote_head,DispatcherSourceSha256=h(dispatcher.read_bytes()),RootBinderSha256=h(Path(__file__).read_bytes()),BindingManifestSha256=h(manifest.read_bytes()),ReviewedPlans=[{k:x[k] for k in ['Path','Inputs','Sha256']} for x in f['Plans']],PublishedCommitBlobsRehashed=len(records),ActualChangedMetadataPaths=sorted(metadata),ActualProofPaths=len(proofs),RequiredBindings=bindings)
for key in ['FreshFullPassed','IntegrationAndPackagesPassed','FreshPostFullWindowsLegacyPassed','FreshPostFullWindowsReallocationPassed','FreshPostFullWindowsProviderFivePhasesPassed','ActualStagedCanonicalSourcePlansPassed','DefaultGitWhitespaceCheckPassed']:authority[key]=True
target=r/'artifacts/root-part331-post-publication-dispatch-authority.json'
with target.open('xb') as stream:stream.write((json.dumps(authority,indent=2)+'\n').encode())
print(json.dumps(dict(AuthoritySha256=h(target.read_bytes()),PublishedHead=a.published_head,NoNetworkOrPost=True)))
