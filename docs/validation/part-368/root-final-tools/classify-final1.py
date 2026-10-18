from pathlib import Path
import collections,gzip,hashlib,json,shutil
R=Path.cwd();O=R/'artifacts/queue-integration368';D=R/'docs/validation/part-368';C=D/'local-completion';C.mkdir(exist_ok=True)
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes() if p.exists() else gzip.decompress(p.with_suffix(p.suffix+'.gz').read_bytes()))
def write(p,v):p.write_text(json.dumps(v,indent=2)+'\n')
key=lambda row:(row['Module'],row['Function'],row['Arity'])
done=J(O/'full3/completion.json');assert done['Status']=='PASS_CANONICAL_PIPELINE' and not done['SourceChanges'] and done['SourceRows']==3299
exit=J(O/'full3/canonical-full.exit.json');assert exit['Settled'] and exit['ExitCode']==0 and not exit['TimedOut'] and not exit['Failures']
for row in J(O/'full3/source-before.json')['Files']:assert H(R/row['Path'])==row['Sha256'],row['Path']
tests=J(R/'artifacts/tests.json');assert tests['Passed']==7599 and tests['Failed']==0 and len(tests['Tests'])==7599
oldtests=J(O/'baseline/tests.json');oldnames=[x['Name']for x in oldtests['Tests']];oldset=set(oldnames);names=[x['Name']for x in tests['Tests']];assert len(oldnames)==7561 and len(set(names))==7599 and [n for n in names if n in oldset]==oldnames
integration=J(R/'artifacts/integration-results.json');assert len(integration)==11 and all(v=='Passed' or v.startswith('Expected') or v=='Hello World' for v in integration.values()),integration
higher=J(D/'higher-comparison/final-capture4/acceptance.json');assert higher['Whole']==2784 and higher['UnexpectedDifferences']==0 and higher['NegativeControlsRejected']==18
assert higher['StrictEqual']==2453 and higher['ActualNamedSourceBound']==331 and higher['Values']==464 and higher['FullStacks']==2320
ends=J(D/'ends-comparison/actual1/comparison.json');assert ends['Accepted']==2574 and ends['StrictWholeEqual']==2554 and not ends['Unexpected'] and len(ends['NegativeControls'])==32
assert ends['Values']==429 and ends['FullStackProfiles']==2145 and all(x['Rejected'] for x in ends['NegativeControls'])
old=J(D/'old-core-comparison/acceptance.json');assert old['Status'].startswith('PASS') and old['Whole']==960 and old['Strict']==954 and old['ActualOpaqueBound']==6 and old['NegativeControlsRejected']==22
assert H(R/'artifacts/queue-ends368-classification-peer4/addendum.json')=='DCB3D07DF9ABD572920A60D1A6490CE970825ABE1BEA5BCF646ECB4EB95DE75A'
for name,source in [('canonical-tests.json',R/'artifacts/tests.json'),('canonical-integration.json',R/'artifacts/integration-results.json'),('canonical.exit.json',O/'full3/canonical-full.exit.json'),('canonical-completion.json',O/'full3/completion.json'),('canonical-source-before.json',O/'full3/source-before.json')]:shutil.copyfile(source,C/name)
ledgerpath=R/'docs/mfa-compatibility.json';ledger=J(ledgerpath);assert len(ledger['Entries'])==647 and len(ledger['VerifiedSourceFiles'])==382
assert collections.Counter(x['Status']for x in ledger['Entries'])=={'Verified compatible':46,'Known differences':30,'Needs verification':571}
for row in ledger['VerifiedSourceFiles']:assert H(R/row['Path'])==row['Sha256'],row['Path']
rawpath=R/'docs/supported-mfas.json';raw=J(rawpath);previous=J(O/'baseline/supported-mfas.json');previousby={key(x):x for x in previous};currentby={key(x):x for x in raw}
for name in ['ends-source-proposal/branch-proof.json','higher-source-proposal/branch-proof.json','ends-classification-source5/addendum.json','higher-classification-source5/contract-classification.json','higher-comparison/final-capture4/acceptance.json','ends-classification-final/certificate.json','source-review-load-nif/review.json','source-review-style/style5-equivalence.json']:
    assert (D/name).exists(),name
assert len(previousby)==617 and len(currentby)==len(raw)==647
newkeys=set(currentby)-set(previousby);assert len(newkeys)==30 and all(k[0]=='queue' for k in newkeys) and not set(previousby)-set(currentby)
profile=J(R/'artifacts/queue-original368-original1/tool/profile.json')
assert {f'{k[0]}:{k[1]}/{k[2]}' for k in newkeys}==set(profile['Mfas']) and len(profile['Mfas'])==30
notes=[]
for k,before in previousby.items():
    after=currentby[k];assert {n:v for n,v in before.items()if n!='Limits'}=={n:v for n,v in after.items()if n!='Limits'},k
    if before['Limits']!=after['Limits']:notes.append(dict(Mfa=f'{k[0]}:{k[1]}/{k[2]}',Generated=after['Limits'],Preserved=before['Limits']))
shutil.copyfile(rawpath,C/'raw-generated-supported-mfas.json')
restored=[dict(previousby.get(key(row),row))for row in raw]
corrections=[]
for row in restored:
    if row['Module']=='queue':
        row['Limits']='Complete observable source-protocol contract audited against OTP-29.1.1, including exact errors/callers and genuine own member/reverse entry state; see mfa-compatibility.json and validation/part-368. All38 queue exports are implemented and classified.'
        if key(row)in previousby:corrections.append(dict(Module=row['Module'],Function=row['Function'],Arity=row['Arity'],Limits=row['Limits']))
write(rawpath,restored)
assert len(corrections)==8 and {key(x) for x in corrections}=={key(x) for x in previous if x['Module']=='queue'}
write(C/'registry-restoration.json',dict(PreviousRows=617,FinalRows=647,NewQueueOnly=30,Removed=0,AllPreviousNonLimitsFieldsPreserved=True,AllPreviousNonQueueCustomFieldsPreserved=True,RestoredNotes=notes,OldRegistryLimitsCorrections=corrections,Reason='Canonical emitter resets custom notes; restore by exactMFA, explicitly update stale oldqueue8 statement that other30 are absent.'))
acceptance=dict(Status='PASS_QUEUE_REMAINING30_COMPLETE_SOURCE_CONTRACT_GATES',PreviousApis=617,Apis=647,ApiDelta=30,PreviousComplete=46,CompleteDelta=30,Complete=76,Known=30,Needs=541,QueueApis=38,QueueComplete=38,Tests=7599,TestDelta=38,PreviousOrderedNamesPreserved=7561,IntegrationOutcomes=11,LocalPackages=2,SourceGuards=382,SourceInputs=3299,CanonicalSeconds=exit['ElapsedSeconds'],CanonicalSourceUnchanged=True,OriginalRun=39078144460,OriginalHead='29be5d7c68c5ad04784a57ee399cc99d14a655a4',OriginalValues=893,OriginalFullStackProfiles=4465,Compared=5358,StrictWhole=5007,ActualNamedCodeRelations=331,OwnEntryRelations=20,OldCoreCompared=960,NegativeControls=72,Capture4BeforeOneLineNifExclusion=True,Source5SubsetReuse='Exact21fixture scan excludes load_nif; finalone-line decline preserves their execution path. CRLF-to-LF equivalence separately proved. Capture4 retains actual original source/DLL identity; full3 is finalsource execution.',Full1Passed=7598,Full1Failed=1,Full2PreflightOnly=True,Full3Passed=7599,OriginalPlatform='Linux GitHub CI',MainPlatform='Windows',ImplementationPublished=False,LicenseCommitted=False,NoFrameProjection=True,Basis='Complete perMFA source clause/domain/order/error/callback/metadata arguments plus independent original wholefields, genuine ownentry/code bindings, negativecontrols, actual normal/cancel/unwind and current-source full regression; finite observations alone do not establish completeness.')
write(C/'acceptance-before-classification.json',acceptance)
def evidence(kind,path,scope):
    p=D/path;assert p.exists(),p
    return dict(Kind=kind,Path=p.relative_to(R/'docs').as_posix(),Sha256=H(p),Scope=scope)
audit=[evidence('CompatibilityAudit','ends-source-proposal/branch-proof.json','Exhaustive pinned end/alias clause and improper-term partitions; source5 applicability addendum retains full17ends plusmember proof.'),evidence('CompatibilityAudit','higher-source-proposal/branch-proof.json','Source-complete structural/callback clauses and private dependency paths; original-discovered BIF caller and staticapply corrections explicitly supersede initial inferences.'),evidence('CompatibilityAudit','ends-classification-source5/addendum.json','Current382guards, complete18 eligibility, cap/zerocharge/source5path applicability and exactLF equivalence; capture4 is explicit source-subset reuse.'),evidence('CompatibilityAudit','higher-classification-source5/contract-classification.json','Complete13 source arguments with actual namedcode/error/callback fields and source5 subset reuse; member long proof supplied independently.'),evidence('OriginalOtpComparison','higher-comparison/final-capture4/acceptance.json','2784full observations;2453strict+331 genuine namedcode bindings,18controls; source5 retained by explicit unchanged-path proof.'),evidence('OriginalOtpComparison','ends-classification-final/certificate.json','Final42574full observations;2554strict+20 ownentry relations,32controls; every other argument/error/source/caller field exact.'),evidence('SourceReview','source-review-load-nif/review.json','Narrow original-source load_nif indirect-call exclusion preserves prior46/source5 queue paths; finalLF line endings bound by Root equality receipt.'),evidence('LocalValidation','local-completion/acceptance-before-classification.json','Finalcurrent7599/7599+11integration+2localpackages;382guards/3299inputs, actual8prerequisites and late-phase cancellation/unwind, no implementation publication.')]
audit.insert(-1,evidence('SourceReview','source-review-style/style5-equivalence.json','Mechanical source5 CRLF-to-LF equality; language tokens unchanged and actual final source guard refreshed to090353. Full3 executes the exact formatted sources.'))
for row in ledger['Entries']:
    if row['Status']=='Verified compatible':row['Evidence'].append(audit[-1])
    if key(row)in newkeys:
        assert row['Status']=='Needs verification';row['Status']='Verified compatible';row['Limits']=[]
        row['Scope']=f'Complete observable OTP-29.1.1 queue:{row["Function"]}/{row["Arity"]} source contract on supported finite Erlang terms: exact persistent two-list representation, clause-specific shallow/improper guards, lazy evaluation, exact equality, callback order and exceptions, full effective arguments/error_info/source/caller metadata at configured depths. Native member/reverse saved-entry protocol and opaque code identities use genuine ownVM state; physical heap/scheduler/timing/performance identity is not claimed. Source-complete perMFA arguments and original wholefield witnesses are linked.'
        row['Evidence']=audit
write(ledgerpath,ledger)
assert collections.Counter(x['Status']for x in ledger['Entries'])=={'Verified compatible':76,'Known differences':30,'Needs verification':541}
scopepath=R/'docs/module-readiness-scope.json';scope=J(scopepath)
assert scope['ComparisonTestReport']=='validation/part-367/local-completion/canonical-tests-before-reverse-delta.json'
delta=J(R/'artifacts/queue368-docs-accounting-proposal3/scope-component-delta.json')
for item in delta['Components']:
    component=next(x for x in scope['Components']if x['Id']==item['Id']);assert component['Status']==item['PreserveStatus'];component['Implemented'].append(item['AppendImplemented'])
next(x for x in scope['Components']if x['Id']=='Compiler')['Implemented'].append('Part368 finalsource: indirect load_nif stays on original apply validation and never gains a direct-source caller capability; actualexisting boundary HOST plus final7599 regression pass.')
for module in ['queue','proplists']:
    if module not in scope['FocusModules']:scope['FocusModules'].append(module)
write(scopepath,scope)
write(C/'classification.json',dict(Status='THIRTY_NEW_QUEUE_APIS_AND_COMPLETE_CONTRACTS',Counts=dict(collections.Counter(x['Status']for x in ledger['Entries'])),Queue=dict(Registered=38,Reference=38,Complete=38),ApiDelta=30,ContractDelta=30,TestDelta=38,ImplementationPublished=False,LedgerSha256=H(ledgerpath)))
print(json.dumps(dict(Apis=647,Complete=76,Queue='38/38',Tests=7599,Published=False)))
