from pathlib import Path
import hashlib,json,importlib.util,gc
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
T=R/'artifacts/queue-original367-original2/tool';C=R/'artifacts/queue-integration367/capture3';S=C/'long-sdk'
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def load(p,name):
 spec=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
D=load(T/'passive_etf_family.py','long_trusted_decoder');P=load(R/'artifacts/queue367-longreverse-policy2/reverse_relation.py','predeclared_reverse_relation')
def named(t,names):
 assert t[0]=='list' and len(t[1])==len(names);rows={}
 for name,row in zip(names,t[1]):
  assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==name and name not in rows;rows[name]=row[1][1]
 return rows
profile=J(T/'profile.json');assert profile['PinnedOtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and len(profile['Fixtures'])==4
completion=J(S/'completion.json');assert completion['FailedGroups']==0 and len(completion['Groups'])==4 and not completion['OriginalExecuted']
receipts=[]
for lane in ['build','long-capture']:
 q=J(C/(lane+'.exit.json'));assert q['Settled'] and not q['TimedOut'] and q['ExitCode']==0 and not q['Failures']
 for stream in ['stdout','stderr']:assert H(C/(lane+'.'+stream+'.log'))==q[stream.title()+'Sha256']
 receipts.append(dict(Lane=lane,Pid=q['Pid'],Normal=True,Seconds=q['ElapsedSeconds'],ReceiptSha256=H(C/(lane+'.exit.json'))))
sealed={}
for row in J(S/'capture-seal.json')['Files']:
 p=(S/row['Path']).resolve();assert p.is_relative_to(S.resolve()) and p.stat().st_size==row['Bytes'] and H(p)==row['Sha256'] and row['Path'] not in sealed;sealed[row['Path']]=row['Sha256']
closure=J(C/'main-dll-closure.json');assert len(closure['Files'])==5 and closure['MainSource'] and not closure['PrivateCandidateDllsUsed']
for row in closure['Files']:assert H(R/row['Path'])==H(R/row['CopiedTo'])==row['Sha256']
review=R/'artifacts/queue-integration367-source-review2';ledger=J(review/'proposed-mfa-compatibility.json');assert len(ledger['VerifiedSourceFiles'])==357
for row in ledger['VerifiedSourceFiles']:assert H(R/row['Path'])==row['Sha256']
for row in J(R/'artifacts/queue-integration367-host-review3/source-bindings.json'):assert H(R/row['Path'])==row['Sha256']
budget=J(C/'observer-budget-successor.json');assert len(budget['Changes'])==1 and budget['RuntimeSourceChanges']==0 and not budget['SourceFunctionTraversalSkipped']
# This artifact-only observer is now the output directory referenced by closure.
observer=R/'artifacts/queue-integration367/main-capture3/SourceFunctionObservationNames.cs'
if not observer.exists():
 candidates=[p for p in (R/'artifacts/queue-integration367').glob('*/SourceFunctionObservationNames.cs') if H(p)==budget['Changes'][0]['After']];assert len(candidates)==1;observer=candidates[0]
assert H(observer)==budget['Changes'][0]['After']
output=[];groups=[];valuecount=stackcount=0
for fixture in profile['Fixtures']:
 directory=S/fixture['Id'];g=next(x for x in completion['Groups'] if x['Id']==fixture['Id']);assert g['Status']=='CAPTURED_PENDING_PASSIVE_CHECKS' and not g['ExecutionSkipped']
 expected=[(T/fixture['Path']).resolve()]+[(T/x).resolve() for x in fixture['Companions']]
 assert len(g['Compile'])==2
 for row,p in zip(g['Compile'],expected):assert row['Passed'] and Path(row['Path']).resolve()==p and row['DeclaredSourceFile']==p.name and row['SourceSha256']==H(p)
 names=[('atom',x) for x in fixture['Names']];returned=D.decode_exact((directory/'returned.etf').read_bytes());values=named(returned,names);valuecount+=len(values)
 assert H(directory/'returned.etf')==g['ReturnedSha256'] and H(directory/'stack-observations.etf')==g['StacksSha256']
 assert all(v==('tuple',(('atom','error'),('atom','error'),('atom','badarg'))) for v in values.values())
 bindings=J(directory/'source-function-bindings.json');assert not bindings['Bindings'] and not bindings['SourceResolverInvoked'] and not bindings['GuestCodeExecutedByDiagnostic'] and bindings['VisitedTerms']<8_000_000
 raw=D.decode_exact((directory/'stack-observations.etf').read_bytes());depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)];depthrows=named(raw,depths)
 for depth,node in depthrows.items():
  rows=named(node,names);stackcount+=len(rows)
  for name,result in rows.items():
   assert result[0]=='tuple' and len(result[1])==4 and result[1][:3]==(('atom','error'),('atom','error'),('atom','badarg'))
   assert result[1][3][0]=='list' and result[1][3][1]
   for frame in result[1][3][1]:assert frame[0]=='tuple' and len(frame[1])==4
   if name[1].startswith(('to_list_rear_','out_rear_')):
    count=int(name[1].rsplit('_',1)[1]);first=P.frame(result);state=P.snapshot(first[1][2],count);assert state['Consumed']%40==0
    output.append(dict(Group=fixture['Id'],Depth=repr(depth),Case=name[1],State=state,OwnAllocation40CellMultiple=True))
 groups.append(dict(Id=fixture['Id'],Values=len(values),FullObservations=len(values)*5,ObserverVisitedTerms=bindings['VisitedTerms'],SourceFunctionsAbsent=True,ReturnedSha256=H(directory/'returned.etf'),StacksSha256=H(directory/'stack-observations.etf')))
 print(fixture['Id'],len(values),len(values)*5,flush=True)
 del returned,values,raw,depthrows,rows;gc.collect()
assert valuecount==11 and stackcount==55 and len(output)==20
report=dict(Status='PASS_FINAL_LONG_SDK_CAPTURE_ADMISSION_ORIGINAL2_PENDING',Values=11,FullObservations=55,Groups=groups,QueueReverseSnapshots=output,SdkSealSha256=H(S/'capture-seal.json'),ProfileSha256=H(T/'profile.json'),DecoderSha256=H(T/'passive_etf_family.py'),Receipts=receipts,MainDllClosureSha256=H(C/'main-dll-closure.json'),ObserverSuccessorSha256=H(C/'observer-budget-successor.json'),OwnSourceProtocolReviewSha256=H(review/'source-protocol-proof.json'),Production357GuardsCurrent=True,FinalHostReviewSha256=H(R/'artifacts/queue-integration367-host-review3/review.json'),FailedCapture2Preserved=True,OwnStateInterpretation='Allocation-path40cells per actual own remaining reduction; two genuine entry snapshot terms; lookahead error before copy; native heap branch/quantum is not asserted identical.',SourceProtocolConformanceClaim='Reviewed actual own allocation branch, not arbitrary valid-prefix selection. No original partition is loaded into Main.',Original2Compared=False,CompatibilityPromoted=False,RuntimeOrNativeExecutedByReview=False,Projection=False,Normalization=False)
(O/'admission.json').write_text(json.dumps(report,indent=2)+'\n');print('PASS11/55;20queue snapshot decompositions')
