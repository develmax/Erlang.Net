from pathlib import Path
import json,hashlib,collections
r=Path.cwd();j=lambda p:json.loads((r/p).read_bytes());h=lambda p:hashlib.sha256((r/p).read_bytes()).hexdigest().upper()
def save(p,x):
 q=r/p;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes((json.dumps(x,indent=2,ensure_ascii=False)+'\n').encode())
t=j('artifacts/tests.json');assert t['Passed']==7688 and t['Failed']==0
old=j('docs/validation/part-371/local-completion/canonical-tests.json');oldnames=[x['Name'] for x in old['Tests']];assert [x['Name'] for x in t['Tests'] if x['Name'] in set(oldnames)]==oldnames
assert j('artifacts/binary-integration369/ordsets372-canonical-tests2/execution.exit.json')['ExitCode']==0
assert h('artifacts/part372-final-regression-review1/receipt.json')=='1689F605B33470C30CEBF3B18E6191F80EEB6A9E185ECBC025F97BF0C7B68C84'
guards=j('artifacts/part372-source-guard-review2/verified-source-files.proposed.json');assert len(guards)==508
for x in guards:assert h(x['Path'])==x['Sha256']
for x in j('artifacts/ordsets372-main-after3/ordsets/capture1/main-dll-closure.json')['Files']:assert h(x['Path'])==x['Sha256']
assert j('artifacts/part372-after-comparison3/acceptance.json')['Strict']==642
for name,count in [('erlang371',4506),('ordsets371',2616)]:assert j('artifacts/erlang372-final-baseline-comparisons1/'+name+'/acceptance.json')['Strict']==count
local='docs/validation/part-372/local-completion';save(local+'/canonical-tests.json',t)
for name in ['acceptance.json','download.json']:
 src='artifacts/erlang371-publication/'+name
 if (r/src).exists():(r/local/('prior-371-'+name)).write_bytes((r/src).read_bytes())
accept=dict(Status='SOURCE_AND_LOCAL_GATES_PASS_PUBLICATION_HEAD_CI_PENDING',TestsPassed=7688,TestsFailed=0,OldOrderedNamesRetained=7673,NewTests=15,OriginalNewHealthyRows=642,OriginalParentConclusion='failure',OriginalNumericBlockedRows=360,OldOrdsetsStrict=2616,NewErlangStrict=4506,RetainedErlangStrict=9846,RetainedQueueProplists=dict(Examined=8117,AcceptedWholeFields=8087,Unexpected=0,CorruptionControls=68,ExplicitDiagnosticsNotAccepted=30),BinarySupplement=dict(Strict=214,Rows=216,ExitCode=1,UnchangedFromPublished371=True,ReceiptSha256=h('artifacts/part372-final-regression-review1/receipt.json')),SourceGuards=508,SourceGuardsSha256=h('artifacts/part372-source-guard-review2/verified-source-files.proposed.json'),ImplementedApiDelta=0,CompleteContractDelta=4,RegisteredApis=699,CompleteContracts=118,PackageAndPublicationCiStillPending=True,NoPublicNuGetRelease=True)
save(local+'/source-local-acceptance.json',accept)
ledger=j('docs/mfa-compatibility.json');assert len(ledger['Entries'])==699 and sum(x['Status']=='Verified compatible' for x in ledger['Entries'])==114
key=lambda x:(x['Module'],x['Function'],x['Arity'])
replacements={key(x):x for x in j('artifacts/ordsets372-contract-closure-proposal1/entry-replacements.proposed.json')}
for c in j('artifacts/length372-classification-proposal1/promotion-candidates.json')['Entries']:
 mfa,arity=c['Mfa'].rsplit('/',1);module,function=mfa.split(':');k=(module,function,int(arity));x=next(x.copy() for x in ledger['Entries'] if key(x)==k);x['Status']='Verified compatible';x['Scope']=c['Scope'];x['Limits']=[];x['Evidence']=list(x['Evidence'])
 for kind,source,scope in [('CompatibilityAudit','artifacts/length372-contract-proposal1/README.md','Exhaustive finite immutable-list branch, progress, argument and source-callsite proof.'),('OriginalOtpComparison','artifacts/length372-classification-proposal1/selected-native-witnesses.json','Actual pinned original values and complete stack witnesses; final source replay retained.'),('SourceReview','artifacts/length372-classification-proposal1/promotion-candidates.json','Independent bounded source closure and explicit exclusion of map_size and erlang:size.')]:
  x['Evidence'].append(dict(Kind=kind,Path='validation/part-372/evidence/'+source.removeprefix('artifacts/'),Sha256=h(source),Scope=scope))
 replacements[k]=x
for i,x in enumerate(ledger['Entries']):
 if key(x) in replacements:ledger['Entries'][i]=replacements[key(x)]
for x in ledger['Entries']:
 if x['Status']=='Verified compatible':
  for kind,source,path,scope in [('SourceReview','artifacts/part372-source-guard-review2/source-review.json','validation/part-372/evidence/part372-source-guard-review2/source-review.json','Independent final source delta review: all496 prior paths preserved,508 exact production source guards and scoped semantic changes; actual final regressions retained.'),('LocalValidation',local+'/source-local-acceptance.json','validation/part-372/local-completion/source-local-acceptance.json','Final7688-test checkpoint and original/retained comparison gates; numeric abort and unchanged legacy diagnostics explicitly retained. Publication-head CI remains pending.')]:
   x['Evidence'].append(dict(Kind=kind,Path=path,Sha256=h(source),Scope=scope))
ledger['VerifiedSourceFiles']=guards;assert sum(x['Status']=='Verified compatible' for x in ledger['Entries'])==118
save('docs/mfa-compatibility.json',ledger)
registry=j('docs/supported-mfas.json');save(local+'/supported-mfas-raw-emitter.json',registry);baseline={key(x):x for x in j('docs/validation/part-371/local-completion/supported-mfas.json')};restored=[]
for x in registry:
 old=baseline[key(x)]
 if x.get('Limits')!=old.get('Limits'):restored.append(dict(Mfa=key(x),Before=x.get('Limits'),After=old.get('Limits')));x['Limits']=old.get('Limits')
 if key(x) in replacements:x['Limits']='Complete observable source contract verified in part372; exact original values/full stacks and final-source lifecycle/regression checks passed. Physical VM layout/timing/performance identity is not claimed.'
assert len(registry)==699;save('docs/supported-mfas.json',registry);save(local+'/metadata-restoration.json',dict(RestoredLimits=len(restored),Rows=restored,PromotedMfas=sorted(replacements),Statuses=dict(collections.Counter(x['Status'] for x in ledger['Entries']))))
scope=j('docs/module-readiness-scope.json');scope['ComparisonTestReport']='validation/part-371/local-completion/canonical-tests.json';scope['Notes'].insert(0,'Part372: unchanged699 registered APIs;118 complete contracts (+4),30 Known,551 pending. Ordsets6/21 complete;erlang21/351 complete. Final7688 tests pass; new642,oldordsets2616,newerlang4506,retainederlang9846 strict fields. Original numeric VM abort and unchanged legacy diagnostics retained.508 source guards; whole modules remain partial.');save('docs/module-readiness-scope.json',scope)
save(local+'/supported-mfas.json',registry);save(local+'/mfa-compatibility.json',ledger)
print(json.dumps(dict(Tests=7688,Apis=699,FullContracts=118,Guards=508,RestoredLimits=len(restored),OldOrderedNamesRetained=len(oldnames))))
