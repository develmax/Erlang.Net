from pathlib import Path
import hashlib,json,subprocess,re,ast
r=Path.cwd();out=r/'artifacts/part329-new-publication-review'
h=lambda b:hashlib.sha256(b).hexdigest().upper()
j=lambda p:json.loads((r/p).read_bytes())
g=lambda *a,**k:subprocess.check_output(['git','--no-optional-locks',*a],cwd=r,**k)
def digest(p):return h((r/p).read_bytes())
def write(n,v):(out/n).write_bytes((json.dumps(v,indent=2)+'\n').encode())
sp='artifacts/root-part329-stage-index-guard.json';fp='artifacts/root-part329-main1/source-freeze.json';rp='docs/validation/part-329/evidence-receipt.json';fg='artifacts/root-part329-main1/full-guard.json';ng='artifacts/root-part329-main-windows-native1/guard.json'
s,f,receipt,full,native=map(j,[sp,fp,rp,fg,ng]);known=j('artifacts/part329-finalization-controls-proposal2/known-paths.json')
assert g('rev-parse','HEAD').decode().strip()==f['ParentHead']=='bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612'
actual=set(g('diff','--cached','--name-only','-z').decode().split('\0'))-{''}
assert len(actual)==1170==s['StagedPaths']
assert actual==set(j('artifacts/root-part329-docs1/explicit-stage-files.json')['Files'])=={x['Path'] for x in s['Records']}
assert not any(x.startswith('artifacts/') or any(v.lower() in {'license.local.txt','license.local','bin','obj','__pycache__','.cache'} for v in Path(x).parts) for x in actual)
sources={x['Path'] for x in f['Files']};proof={x['TargetPath'] for x in receipt['Files']}|{'docs/validation/part-329/evidence-index.json',rp};metadata=set(s['ActualChangedMetadataPaths'])
assert len(sources)==12 and len(proof)==1145 and len(metadata)==13 and actual==sources|proof|metadata
assert metadata<=set(known['MetadataPaths']) and not(sources&proof or sources&metadata or proof&metadata)
assert j('docs/validation/part-329/evidence-index.json')==receipt['Files'] and receipt['EvidenceIndexSha256']==digest('docs/validation/part-329/evidence-index.json')
assert (full['Passed'],full['Failed'],full['IntegrationResults'],full['Registry'])==(6747,0,11,509)
assert native['FullPassed']==6747 and native['SourceFilesRehashed']==2397 and native['ActualFullGuard']['Sha256']==digest(fg)
assert receipt['ActualFullGuardSha256']==digest(fg) and receipt['ActualNativeGuardSha256']==digest(ng) and receipt['SourceFreezeSha256']==s['SourceFreezeSha256']==digest(fp)
assert j('docs/module-readiness.json')['LocalPassedTests']==6747 and receipt['ActualReadinessSha256']==digest('docs/module-readiness.json')
cp='artifacts/root-part329-main1/source-resource-closure.json';assert digest(cp)==f['FinalSourceClosureSha256']==full['SourceClosureSha256']==receipt['SourceClosureSha256']
rows=j(cp)['Files'];assert len(rows)==2397
for e in rows:
 b=(r/e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256'],e['Path']
extra={v for e in s['ActualStagedRawSourcePlanSeals'] for v in [e['Plan'],e['Workflow']]}|{e['Path'] for e in s['ActualStagedEmbeddedBeamAndProvenanceResources']}|{e['Path'] for e in known['ClosedVolumes']}
names=sorted(actual|extra)
raw=g('cat-file','--batch',input=''.join(':'+n+'\n' for n in names).encode());pos=0;blobs={}
for n in names:
 end=raw.index(b'\n',pos);hdr=raw[pos:end].split();assert hdr[1]==b'blob';size=int(hdr[2]);pos=end+1;blobs[n]=raw[pos:pos+size];pos+=size;assert raw[pos:pos+1]==b'\n';pos+=1
assert pos==len(raw)
for e in s['Records']:
 b=blobs[e['Path']];w=(r/e['Path']).read_bytes();assert len(b)==e['StagedBytes'] and h(b)==e['StagedSha256'] and len(w)==e['WorkingBytes'] and h(w)==e['WorkingSha256']
 assert b==(w if e['ComparisonMode']=='BinaryExact' else w.replace(b'\r\n',b'\n'))
for e in receipt['Files']:
 b=blobs[e['TargetPath']];assert len(b)==e['Bytes'] and h(b)==e['Sha256']
for e in f['Files']:assert h(blobs[e['Path']])==e['Sha256']
def attrs(names,attribute):
 q=g('check-attr','--cached','-z',attribute,'--',*names).decode().split('\0');assert q.pop()==''
 return {q[i]:q[i+2] for i in range(0,len(q),3)}
for e in s['ActualStagedRawSourcePlanSeals']:
 b=blobs[e['Plan']];d=h(b);assert b==(r/e['Plan']).read_bytes() and b'\r' not in b and not b.startswith(b'\xef\xbb\xbf')
 assert d==e['ActualStagedBlobSha256']==e['ActualStagedWorkflowPin']==e['WorktreeSha256'] and attrs([e['Plan']],'eol')[e['Plan']]=='lf'
 assert re.findall(rb'(?m)^[ \t]+'+e['EnvironmentKey'].encode()+rb':[ \t]+([A-Fa-f0-9]{64})[ \t]*\r?$',blobs[e['Workflow']])==[d.encode()]
for e in s['ActualStagedEmbeddedBeamAndProvenanceResources']:
 n=e['Path'];assert attrs([n],'binary')[n]=='set' and attrs([n],'text')[n]=='unset' and h(blobs[n])==e['StagedRawSha256'] and blobs[n]==(r/n).read_bytes()
for e in known['ClosedVolumes']:
 b=(r/e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256'];assert len(blobs[e['Path']])==e['PublishedBytes'] and h(blobs[e['Path']])==e['PublishedSha256']
assert len((r/known['ActiveVolume']).read_bytes())<=32768 and len((r/known['ActiveVolume']).read_bytes().splitlines())<=400
g('diff','--cached','--check')
write('actual-cached-stage-peer.json',dict(Status='PASS_INDEPENDENT_ACTUAL_CACHED_STAGE_1170',Passed=True,SourceFreezeSha256=digest(fp),EvidenceReceiptSha256=digest(rp),StageGuardSha256=digest(sp),FullGuardSha256=digest(fg),NativeGuardSha256=digest(ng),CachedBeamAndProvenanceBinaryAttributesAndBytesExact=True,StagedPaths=1170,SourcePaths=12,MetadataPaths=13,ProofPaths=1145,CompleteSourceRowsRehashed=2397,FourSourcePlanSealsRehashed=True,ClosedChangelogVolumesUnchanged=True,LicenseLocalAndCachesExcluded=True,DefaultGitWhitespacePassed=True,NoMutationOrNetwork=True,ReviewScriptSha256=digest('artifacts/part329-new-publication-review/review.py')))
base='artifacts/part329-publication-authority-proposal1/'
proposal=j(base+'proposal-freeze.json')
for e in proposal['Files']:assert digest(base+e['Path'])==e['Sha256'] and len((r/base/e['Path']).read_bytes())==e['Bytes']
binder=(r/base/'bind329-authority.py').read_text();dispatcher=(r/base/'dispatch329-once.ps1').read_text();ast.parse(binder)
assert dispatcher.count('Invoke-RestMethod -Method Post')==1 and 'FileMode]::CreateNew' in dispatcher and dispatcher.index('Write-ExclusiveJson ($prefix+\'-attempt.json\')')<dispatcher.index('Invoke-RestMethod -Method Post')
assert "ValidateSet('ordinary','native','reallocation','distribution')" in dispatcher and "if ($Kind -ceq 'reallocation')" in dispatcher
assert 'ExactRunMappingStillRequired=$true' in dispatcher and 'OriginalResultVerified=$false' in dispatcher and 'finally { $credentialLines=$null;$passwordLine=$null;$secret=$null;$headers=$null }' in dispatcher
assert 'with target.open(\'xb\')' in binder and 'subprocess' in binder and 'SourceFilesRehashed' in binder
write('dispatcher-source-peer.json',dict(Status='PASS_INDEPENDENT_BINDER_AND_ONCE_ONLY_DISPATCHER_SOURCE_REVIEW',DispatcherSha256=digest(base+'dispatch329-once.ps1'),BinderSha256=digest(base+'bind329-authority.py'),ProposalFreezeSha256=digest(base+'proposal-freeze.json'),Passed=True,ActualBinderSchemaSatisfiedByCachedPeer=True,SinglePostPerKind=True,AttemptRecordedExclusiveBeforePost=True,NoRetryForAmbiguousResult=True,ExactLocalAndRemoteHeadBeforePost=True,NineBoundProofRoles=True,OriginalResultsRemainPending=True,NoExecutionOrNetwork=True,Limits=['Source review does not prove future remote run success.','Root must independently verify bound authority and published head before dispatch.']))
for n in ['actual-cached-stage-peer.json','dispatcher-source-peer.json']:print(n,digest('artifacts/part329-new-publication-review/'+n))
