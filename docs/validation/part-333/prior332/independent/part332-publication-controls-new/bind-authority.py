"""Root-only post-publication binder. Reads Git/proofs; no POST or Git mutation."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser()
for n in ['published-head','verified-remote-head','bindings','bindings-sha256']:p.add_argument('--'+n,required=True)
a=p.parse_args();r=Path.cwd().resolve();h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());g=lambda *x:subprocess.check_output(['git','--no-optional-locks',*x],cwd=r)
assert re.fullmatch('[0-9a-f]{40}',a.published_head) and a.published_head==a.verified_remote_head==g('rev-parse','HEAD').decode().strip()
assert g('rev-parse','HEAD^').decode().strip()=='6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d'
assert not g('diff','--name-only').strip() and not g('diff','--cached','--name-only').strip()
def safe(name):
 q=Path(name);assert not q.is_absolute() and '..' not in q.parts and not any(c in name for c in '\0\r\n');v=(r/q).resolve();assert v.is_relative_to(r);return v
manifest=Path(a.bindings).resolve();assert manifest.is_relative_to(r/'artifacts') and h(manifest.read_bytes())==a.bindings_sha256.upper()
bindings=j(manifest)['RequiredBindings'];roles={x['Role']:x for x in bindings}
assert len(bindings)==len(roles)==9 and set(roles)=={'SourceFreeze','FullGuard','NativeGuard','IndependentActualPeer','StageGuard','IndependentStagePeer','EvidenceReceipt','Readiness','DispatcherSourcePeer'}
values={}
for e in bindings:
 q=safe(e['Path']);assert re.fullmatch('[A-F0-9]{64}',e['Sha256']) and h(q.read_bytes())==e['Sha256'];values[e['Role']]=j(q)
assert roles['SourceFreeze']['Sha256']=='8CA193FB17ED3418F6FD79127FE3CBB64AAFCC423AE32EB564A1B8E82D417E71'
assert roles['FullGuard']['Sha256']=='FBB9A95750DD49DA34C8FF76B4CE10D2D1EC640C98A32555D9D4667BFDC899E7'
f,full,native,stage,receipt,ready=[values[k] for k in ['SourceFreeze','FullGuard','NativeGuard','StageGuard','EvidenceReceipt','Readiness']]
assert (f['OwnedPaths'],f['OwnedCSharpFiles'],f['SourceClosurePaths'])==(25,21,2431)
promotion_path=safe('artifacts/root-part332-promotion.json');assert h(promotion_path.read_bytes())=='7DB2439F85BC5159A2C72D2CF71A29324C953F79C5F64A5426407BCA41675705';promotion=j(promotion_path)
assert promotion['Status']=='EXACT_TESTED_CANDIDATE_SOURCE_PROMOTED' and promotion['OwnedPaths']==25 and promotion['SourceClosurePaths']==2431 and promotion['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and promotion['FullGuardSha256']==roles['FullGuard']['Sha256']
assert promotion['RepeatedLocalFull'] is False and promotion['InitialPipelineExitCode']==1 and promotion['PackageSuffixExitCode']==0
assert (full['Passed'],full['Failed'],full['Registry'],full['ErlangMfas'],full['ProcLibMfas'])==(6822,0,521,351,12)
assert full['Status']=='PASS_ACTUAL_ISOLATED332_COMPOSITE_VALIDATION' and full['InitialPipelineCompleted'] is False and full['PackagingResumed'] is True and full['CompletedTestsRerun'] is False
expected_integrations={'HybridOutput':'Hello World','CleanRebuild':'Passed','PackagedReadiness':'Passed','Build':'Passed','PackageConsumer':'Passed','OracleUnavailableReport':'Expected failure recorded','Incremental':'Passed','Tests':'Passed','LocalTool':'Passed','DisabledPreprocessing':'Expected failure','Readiness':'Passed'}
assert full['IntegrationResults']==expected_integrations
closure='56908A656DEA730DE49C960EB82B4C5656A46E5B94B0E77E75F98BAA07A12F95'
assert f['SourceClosureSha256']==full['SourceClosureSha256']==closure
assert native['Status']=='PASS_ACTUAL_ISOLATED332_POST_FULL_WINDOWS_NATIVE_THREE_ROLES' and native['FullPassed']==6822 and native['SourceRowsRehashed']==2431
assert native['SourceFreezeSha256']==full['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and native['ActualFullGuard']['Sha256']==roles['FullGuard']['Sha256']
assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'])==(6822,521,515,6)
assert receipt['Files'] and receipt['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and receipt['ActualFullGuardSha256']==roles['FullGuard']['Sha256'] and receipt['ActualNativeGuardSha256']==roles['NativeGuard']['Sha256'] and receipt['ActualReadinessSha256']==roles['Readiness']['Sha256']
assert receipt['SourceClosureSha256']==closure
assert stage['Status'].startswith('PASS') and stage['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and stage['EvidenceReceiptSha256']==roles['EvidenceReceipt']['Sha256']
assert (stage['SourcePaths'],stage['CSharpPaths'])==(25,21)
for key in ['ProofsBinaryExact','AllCSharpBinaryExact','DefaultGitWhitespaceCheckPassed']:assert stage[key] is True
assert stage['LicenseLocalStaged'] is False and stage['CacheStaged'] is False
for k in ['IndependentActualPeer','IndependentStagePeer']:
 peer=values[k];assert peer.get('Passed') is True or str(peer.get('Status','')).startswith('PASS')
 for pk,rk in [('SourceFreezeSha256','SourceFreeze'),('EvidenceReceiptSha256','EvidenceReceipt'),('StageGuardSha256','StageGuard'),('FullGuardSha256','FullGuard'),('NativeGuardSha256','NativeGuard')]:assert peer[pk]==roles[rk]['Sha256']
 assert peer['CachedBeamAndProvenanceBinaryAttributesAndBytesExact'] is True
dispatcher=Path(__file__).with_name('dispatch-once.ps1');dp=values['DispatcherSourcePeer'];assert str(dp['Status']).startswith('PASS') and dp['DispatcherSha256']==h(dispatcher.read_bytes())
records=stage['Records'];names={x['Path'] for x in records};assert len(records)==len(names)==stage['StagedPaths']
assert names==set(g('diff','HEAD^','HEAD','--name-only','-z').decode().split('\0'))-{''}
sources={x['Path'] for x in f['Files']};proofs={x['TargetPath'] for x in receipt['Files']}|{'docs/validation/part-332/evidence-index.json','docs/validation/part-332/evidence-receipt.json'};metadata=names-sources-proofs
assert len(sources)==25 and sources<=names and proofs<=names and not sources&proofs
assert all(n.startswith('docs/') or n in ['AGENTS.md','CHANGELOG.md','README.md'] for n in metadata)
assert all(not n.startswith('artifacts/') and not any(v.lower() in {'license.local.txt','bin','obj','__pycache__','.cache'} for v in Path(n).parts) for n in names)
index=safe('docs/validation/part-332/evidence-index.json');assert j(index)==receipt['Files'] and h(index.read_bytes())==receipt['EvidenceIndexSha256']
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
cp=safe('artifacts/proc-lib332-full-source/source-resource-closure.json');assert h(cp.read_bytes())==closure;rows=j(cp)['Files'];assert len(rows)==2431
for e in rows:
 b=safe(e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256']
assert {x['Inputs']:x['Sha256'] for x in f['Plans']}=={1128:'1DED3D93FDFD0FE001D017B9E340C0D0D9619C0FFEE1E5CFE255F5735DAFF182',1153:'8E72D62549B86793F9819B6C1D432C38F91B36D204007BEC9BD27CFE1C1EC155'}
authority=dict(Status='ROOT_REVIEWED_PART332_POST_PUBLICATION_DISPATCH_AUTHORITY',Part=332,RootExecutionApproved=True,PublishedHead=a.published_head,RemoteHeadReportedVerifiedByRoot=a.verified_remote_head,DispatcherSourceSha256=h(dispatcher.read_bytes()),RootBinderSha256=h(Path(__file__).read_bytes()),BindingManifestSha256=h(manifest.read_bytes()),ReviewedPlans=[{k:x[k] for k in ['Path','Inputs','Sha256']} for x in f['Plans']],PublishedCommitBlobsRehashed=len(records),ActualChangedMetadataPaths=sorted(metadata),ActualProofPaths=len(proofs),RequiredBindings=bindings)
authority.update(ExpectedFullPassed=6822,ExpectedRegistry=521,PromotionReceiptSha256=h(promotion_path.read_bytes()),ValidationMode='Exact promoted source with initial tests and integration prefix plus successful resumed package suffix',InitialPipelineCompleted=False,PackagingResumed=True)
for key in ['CompositeFullValidationPassed','IntegrationAndPackagesPassed','FreshPostFullWindowsLegacyPassed','FreshPostFullWindowsReallocationPassed','FreshPostFullWindowsProviderFivePhasesPassed','ActualStagedCanonicalSourcePlansPassed','DefaultGitWhitespaceCheckPassed']:authority[key]=True
target=r/'artifacts/root-part332-post-publication-dispatch-authority.json'
with target.open('xb') as stream:stream.write((json.dumps(authority,indent=2)+'\n').encode())
print(json.dumps(dict(AuthoritySha256=h(target.read_bytes()),PublishedHead=a.published_head,NoNetworkOrPost=True)))
