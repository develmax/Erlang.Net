"""Root-only post-publication binder. Reads Git/proofs; no POST or Git mutation."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser()
for n in ['published-head','verified-remote-head','bindings','bindings-sha256','promotion-receipt','promotion-receipt-sha256']:p.add_argument('--'+n,required=True)
a=p.parse_args();r=Path.cwd().resolve();h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());g=lambda *x:subprocess.check_output(['git','--no-optional-locks',*x],cwd=r)
assert re.fullmatch('[0-9a-f]{40}',a.published_head) and a.published_head==a.verified_remote_head==g('rev-parse','HEAD').decode().strip()
assert g('rev-parse','HEAD^').decode().strip()=='aaf48552bee429b0447d3e28e8274d34ad590d73'
assert not g('diff','--name-only').strip() and not g('diff','--cached','--name-only').strip()
def safe(name):
 q=Path(name);assert not q.is_absolute() and '..' not in q.parts and not any(c in name for c in '\0\r\n');v=(r/q).resolve();assert v.is_relative_to(r);return v
manifest=Path(a.bindings).resolve();assert manifest.is_relative_to(r/'artifacts') and h(manifest.read_bytes())==a.bindings_sha256.upper()
bindings=j(manifest)['RequiredBindings'];roles={x['Role']:x for x in bindings}
assert len(bindings)==len(roles)==9 and set(roles)=={'SourceFreeze','FullGuard','NativeGuard','IndependentActualPeer','StageGuard','IndependentStagePeer','EvidenceReceipt','Readiness','DispatcherSourcePeer'}
values={}
for e in bindings:
 q=safe(e['Path']);assert re.fullmatch('[A-F0-9]{64}',e['Sha256']) and h(q.read_bytes())==e['Sha256'];values[e['Role']]=j(q)
assert roles['SourceFreeze']['Sha256']=='AA5249A3F81F74F825626EAAF1DACB1E44564105B6BD0589DB5A0DD9CA4046C4'
# Future full/native hashes come only from the actual independently reviewed binding manifest.
f,full,native,stage,receipt,ready=[values[k] for k in ['SourceFreeze','FullGuard','NativeGuard','StageGuard','EvidenceReceipt','Readiness']]
assert (f['OwnedPaths'],f['OwnedCSharpFiles'],f['SourceClosurePaths'])==(18,14,2439)
assert f['ParentHead']=='aaf48552bee429b0447d3e28e8274d34ad590d73'
promotion_path=Path(a.promotion_receipt).resolve();assert promotion_path.is_relative_to(r/'artifacts') and re.fullmatch('[A-Fa-f0-9]{64}',a.promotion_receipt_sha256) and h(promotion_path.read_bytes())==a.promotion_receipt_sha256.upper();promotion=j(promotion_path)
assert promotion['Status']=='EXACT_TESTED_CANDIDATE_SOURCE_PROMOTED' and promotion['OwnedPaths']==18 and promotion['SourceClosurePaths']==2439 and promotion['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and promotion['FullGuardSha256']==roles['FullGuard']['Sha256']
assert (full['Passed'],full['Failed'],full['Registry'],full['ErlangMfas'],full['ProcLibMfas'])==(6871,0,528,351,19)
assert full['Status']=='PASS_ACTUAL_ISOLATED333_FULL' and full['PreviousOrderedNames']==6822 and full['PreviousOrderedNamesUnchanged'] is True and full['All521PreviousRegistryRowsRestoredExact'] is True
full_exit_path=safe('artifacts/proc-lib333-full-source/full-exit.json');assert h(full_exit_path.read_bytes())==full['FullExitSha256'];full_exit=j(full_exit_path)
assert full_exit['ExitCode']==0 and full_exit['TimedOut'] is False and full_exit['Settled'] is True
expected_integrations={'HybridOutput':'Hello World','CleanRebuild':'Passed','PackagedReadiness':'Passed','Build':'Passed','PackageConsumer':'Passed','OracleUnavailableReport':'Expected failure recorded','Incremental':'Passed','Tests':'Passed','LocalTool':'Passed','DisabledPreprocessing':'Expected failure','Readiness':'Passed'}
assert full['IntegrationResults']==expected_integrations
closure='BBECB7B32C2807BB7DC74F06AD500C5517283468864F0203FE35C54F084DC2CE'
assert f['SourceClosureSha256']==full['SourceClosureSha256']==closure
assert native['Status']=='PASS_ACTUAL_ISOLATED333_POST_FULL_WINDOWS_NATIVE_THREE_ROLES' and native['FullPassed']==6871 and native['SourceRowsRehashed']==2439
assert native['SourceFreezeSha256']==full['SourceFreezeSha256']==roles['SourceFreeze']['Sha256'] and native['ActualFullGuard']['Sha256']==roles['FullGuard']['Sha256']
assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'])==(6871,528,521,7)
assert (receipt['Passed'],receipt['Failed'],receipt['IntegrationOutcomes'],receipt['RegisteredMfas'],receipt['ProcLibMfas'],receipt['ErlangMfas'],receipt['OwnedPaths'],receipt['SourceClosurePaths'])==(6871,0,11,528,19,351,18,2439)
index=safe('docs/validation/part-333/evidence-index.json');proofrows=j(index)
assert h(index.read_bytes())==receipt['EvidenceIndexSha256'] and len(proofrows)==receipt['CopiedFiles'] and len(proofrows)>0
assert sum(e['Bytes'] for e in proofrows)==receipt['CopiedBytes']
assert isinstance(stage,dict) and isinstance(stage.get('Status'),str)
assert (stage['Status'].startswith('PASS_') or stage['Status'].endswith('_PASS')) and not any(x in stage['Status'] for x in ['FAIL','PENDING','UNEXECUTED'])
for k in ['IndependentActualPeer','IndependentStagePeer']:
 peer=values[k];assert peer.get('Passed') is True or str(peer.get('Status','')).startswith('PASS')
 for pk,rk in [('SourceFreezeSha256','SourceFreeze'),('EvidenceReceiptSha256','EvidenceReceipt'),('StageGuardSha256','StageGuard'),('FullGuardSha256','FullGuard'),('NativeGuardSha256','NativeGuard')]:assert peer[pk]==roles[rk]['Sha256']
 assert peer['CachedBeamAndProvenanceBinaryAttributesAndBytesExact'] is True
stagepeer=values['IndependentStagePeer'];records=stagepeer['Records'];names={e['Path'] for e in records}
assert records and len(records)==len(names)
assert names==set(g('diff','HEAD^','HEAD','--name-only','-z').decode().split('\0'))-{''}
sources={x['Path'] for x in f['Files']};proofs={p.relative_to(r).as_posix() for p in safe('docs/validation/part-333').rglob('*') if p.is_file()};metadata=names-sources-proofs
assert len(sources)==18 and sources<=names and proofs<=names and names==sources|proofs|metadata and not sources&proofs and metadata
assert {e['TargetPath'] for e in proofrows}<proofs and len(proofrows)==len({e['TargetPath'] for e in proofrows})
extras=proofs-{e['TargetPath'] for e in proofrows};assert {'docs/validation/part-333/evidence-index.json','docs/validation/part-333/evidence-receipt.json'}<=extras
assert all(n in {'docs/validation/part-333/evidence-index.json','docs/validation/part-333/evidence-receipt.json','docs/validation/part-333/.gitattributes'} for n in extras)
assert all(n.startswith('docs/') or n in ['AGENTS.md','CHANGELOG.md','README.md'] for n in metadata)
assert all(not n.startswith('artifacts/') and not any(v.lower() in {'license.local.txt','bin','obj','__pycache__','.cache'} for v in Path(n).parts) for n in names)
dispatcher=Path(__file__).with_name('dispatch-once.ps1');dp=values['DispatcherSourcePeer'];assert str(dp['Status']).startswith('PASS') and dp['DispatcherSha256']==h(dispatcher.read_bytes())
seals=stagepeer['ActualStagedRawSourcePlanSeals'];resources=stagepeer['ActualStagedEmbeddedBeamAndProvenanceResources'];assert len(seals)==4 and len(resources)==2
allnames=sorted(names|{x[k] for x in seals for k in ['Plan','Workflow']}|{x['Path'] for x in resources})
for n in allnames:safe(n)
raw=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],cwd=r,input=''.join(a.published_head+':'+n+'\n' for n in allnames).encode());off=0;blobs={}
for n in allnames:
 end=raw.index(b'\n',off);header=raw[off:end].split();assert len(header)==3 and header[1]==b'blob';size=int(header[2]);off=end+1;blobs[n]=raw[off:off+size];off+=size;assert raw[off:off+1]==b'\n';off+=1
assert off==len(raw)
for e in records:
 b=blobs[e['Path']];working=safe(e['Path']).read_bytes();assert len(b)==e['StagedBytes'] and h(b)==e['StagedSha256']
 assert b==(working if e['Path'] in sources|proofs else working.replace(b'\r\n',b'\n'))
for e in proofrows:assert len(blobs[e['TargetPath']])==e['Bytes'] and h(blobs[e['TargetPath']])==e['Sha256']
for e in f['Files']:assert len(blobs[e['Path']])==e['Bytes'] and h(blobs[e['Path']])==e['Sha256']
for e in seals:
 b=blobs[e['Plan']];digest=h(b);assert b'\r' not in b and not b.startswith(b'\xef\xbb\xbf') and digest==e['ActualStagedBlobSha256']==e['ActualStagedWorkflowPin']==e['WorktreeSha256']
 assert e['GitEolAttribute']=='lf' and re.findall(rb'(?m)^[ \t]+'+e['EnvironmentKey'].encode()+rb':[ \t]+([A-F0-9]{64})[ \t]*\r?$',blobs[e['Workflow']])==[digest.encode()]
for e in resources:assert e['BinarySetTextUnset'] and e['WorktreeEqualsStagedBytes'] and h(blobs[e['Path']])==e['StagedRawSha256']
g('diff','HEAD^','HEAD','--check')
cp=safe('artifacts/proc-lib333-full-source/source-resource-closure.json');assert h(cp.read_bytes())==closure;rows=j(cp)['Files'];assert len(rows)==2439
for e in rows:
 b=safe(e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256']
package_path=safe('artifacts/proc-lib333-full-source/package-resources.json');assert h(package_path.read_bytes())==f['PackageResourceManifestSha256']==full['PackageResourceManifestSha256']
package_rows=j(package_path)['Files'];assert len(package_rows)==full['PackageResourceRows']==6
for e in package_rows:
 b=safe(e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256']
assert {x['Inputs']:x['Sha256'] for x in f['Plans']}=={1130:'FEE73B2B6794CDFEB55A6F90A943756BD64B829D989500C4F70029EA476D876D',1155:'57DDC44995189476F1366F9C4CEE945A9C00F4D589915CB5E7314401294DA8CB'}
authority=dict(Status='ROOT_REVIEWED_PART333_POST_PUBLICATION_DISPATCH_AUTHORITY',Part=333,RootExecutionApproved=True,PublishedHead=a.published_head,RemoteHeadReportedVerifiedByRoot=a.verified_remote_head,DispatcherSourceSha256=h(dispatcher.read_bytes()),RootBinderSha256=h(Path(__file__).read_bytes()),BindingManifestSha256=h(manifest.read_bytes()),ReviewedPlans=[{k:x[k] for k in ['Path','Inputs','Sha256']} for x in f['Plans']],PublishedCommitBlobsRehashed=len(records),ActualChangedMetadataPaths=sorted(metadata),ActualProofPaths=len(proofs),RequiredBindings=bindings)
authority.update(ExpectedFullPassed=6871,ExpectedRegistry=528,PromotionReceiptSha256=h(promotion_path.read_bytes()),ValidationMode='Single complete source-bound validation pipeline exit0 plus six package resources',InitialPipelineCompleted=True,PackagingResumed=False,ActualBoundStageStatus=stage['Status'])
for key in ['SingleFullValidationPassed','IntegrationAndPackagesPassed','FreshPostFullWindowsLegacyPassed','FreshPostFullWindowsReallocationPassed','FreshPostFullWindowsProviderFivePhasesPassed','ActualStagedCanonicalSourcePlansPassed','DefaultGitWhitespaceCheckPassed']:authority[key]=True
target=r/'artifacts/root-part333-post-publication-dispatch-authority.json'
with target.open('xb') as stream:stream.write((json.dumps(authority,indent=2)+'\n').encode())
print(json.dumps(dict(AuthoritySha256=h(target.read_bytes()),PublishedHead=a.published_head,NoNetworkOrPost=True)))
