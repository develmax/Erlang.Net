from pathlib import Path
import json,hashlib,importlib.util,sys
R=Path(__file__).resolve().parents[2];O=R/'artifacts/proplists-integration365-replay3-peer';X=R/'artifacts/proplists-integration365/main-replay3';H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();J=lambda p:json.loads(p.read_bytes())
s=importlib.util.spec_from_file_location('passive_final_v',R/'artifacts/proplists-stack-relational-peer1/validate.py');v=importlib.util.module_from_spec(s);sys.modules[s.name]=v;s.loader.exec_module(v)
cohorts=[('strict','lc_strict_singleton',R/'artifacts/proplists-stack-close-original1/download/run-39056890376/artifact-12109847910/capture',R/'artifacts/proplists-stack-close-original1/tool/profile.json',X/'strict-sdk'),('iterator','proplists_iterator_contract',R/'artifacts/proplists-integration365-original1/download/run-39061299103/artifact-12111562130/capture',R/'artifacts/proplists-integration365-original1/tool/profile.json',X/'iterator-sdk'),('flat','flat_iterators',R/'artifacts/proplists-integration365-original2/download/run-39062211544/artifact-12112450983/capture',R/'artifacts/proplists-integration365-original2/tool/profile.json',X/'raw-sdk'),('raw','raw_cursors',R/'artifacts/proplists-integration365-original2/download/run-39062211544/artifact-12112450983/capture',R/'artifacts/proplists-integration365-original2/tool/profile.json',X/'raw-sdk')]
reports=[];admissions={}
for cohort,group,original,profile,sdk in cohorts:
 key=str(original)
 if key not in admissions:admissions[key]=v.admit_original(original,profile)
 seal,files=v.admit_sdk(sdk)
 fixture=next(x for x in J(profile)['Fixtures'] if x['Id']==group);names=[v.atom(x) for x in fixture['Names']]
 for filename in ['returned.etf','stack-observations.etf']:
  op=original/group/filename;sp=sdk/group/filename;assert group+'/'+filename in files
  left=v.decode(op);right=v.decode(sp);pairs=[]
  if filename=='returned.etf':
   ll=v.named(left);rr=v.named(right);assert list(ll)==list(rr)==names;pairs=[('values',n,ll[n],rr[n]) for n in names]
  else:
   ll=v.named(left);rr=v.named(right);depths=[v.atom('default'),('integer',0),('integer',1),('integer',2),('integer',8)];assert list(ll)==list(rr)==depths
   for depth in depths:
    ln=v.named(ll[depth]);rn=v.named(rr[depth]);assert list(ln)==list(rn)==names;pairs.extend((repr(depth),n,ln[n],rn[n]) for n in names)
  rows=[dict(Depth=d,Name=n[1],WholeDecodedExact=a==b,Original=repr(a),Sdk=repr(b)) for d,n,a,b in pairs]
  reports.append(dict(Cohort=cohort,Group=group,File=filename,Total=len(rows),WholeExact=sum(x['WholeDecodedExact'] for x in rows),Rows=rows,RawBytesExact=op.read_bytes()==sp.read_bytes(),OriginalSha256=H(op),SdkSha256=H(sp),SdkSealSha256=seal))
for report in reports:
 differences=[x for x in report['Rows'] if not x['WholeDecodedExact']]
 if report['Cohort']!='raw':assert not differences,(report['Cohort'],differences)
 else:assert {x['Name'] for x in differences}=={'hamt33_tuple_keys','hamt33_atom_keys'}
closure=J(X/'main-dll-closure.json');assert closure['MainSource'] and not closure['PrivateCandidateDllsUsed']
for row in closure['Files']:assert H(R/row['Path'])==H(R/row['CopiedTo'])==row['Sha256']
processes=[]
for name in ['base','extra','strict','binding','iterator','raw']:
 p=X/(name+'.exit.json');receipt=J(p);assert receipt['Settled'] and not receipt['TimedOut'] and receipt['ExitCode']==0 and not receipt['Failures']
 for stream in ['Stdout','Stderr']:assert H(X/(name+'.'+stream.lower()+'.log'))==receipt[stream+'Sha256']
 processes.append(dict(Name=name,ReceiptSha256=H(p),Pid=receipt['Pid'],Seconds=receipt['ElapsedSeconds'],NormalForeground=True))
base=J(O/'base-main-replay3/report.json');extra=J(O/'extra-main-replay3.json');binding=J(O/'binding-main-replay3.json');negative=J(O/'binding-frame-review.json')
controls=len(base['NegativeControls'])+len(extra['NegativeControls'])+len(negative['NegativeControls']);assert controls==37
combined={}
for filename in ['returned.etf','stack-observations.etf']:
 bs=base['Stats'][filename];es=extra['Stats'][filename];bind=binding['Summary']['after_guard'][filename];simple=[x for x in reports if x['File']==filename]
 combined[filename]=dict(Total=bs['Total']+es['Total']+bind['Total']+sum(x['Total'] for x in simple),StrictEqual=bs['StrictEqual']+es['StrictEqual']+bind['WholeExact']+sum(x['WholeExact'] for x in simple),RelationalAllFields=bs['RelationalAllFields']+es['RelationalAllFields']+bind['WholeExact']+sum(x['WholeExact'] for x in simple))
 combined[filename]['MandatoryRemaining']=combined[filename]['Total']-combined[filename]['RelationalAllFields']
rows=J(R/'artifacts/proplists-integration365-source-review3/verified-source-files.proposed.json')['VerifiedSourceFiles'];assert all(H(R/x['Path'])==x['Sha256'] for x in rows)
result=dict(Status='PASS_BOUNDED_PASSIVE_REPLAY_ADMISSION_WITH_MANDATORY_RESIDUALS',Combined=combined,Reports=reports,OriginalAdmissions=admissions,NormalSdkProcesses=processes,MainDllClosure=closure,MainDllClosureSha256=H(X/'main-dll-closure.json'),CurrentReviewedSourceGuardRows=len(rows),CurrentReviewedSourceGuardsExact=True,NegativeControlsRerun=controls,ControlReports={name:H(O/name) for name in ['base-main-replay3/report.json','extra-main-replay3.json','binding-main-replay3.json','binding-frame-review.json']},PriorOriginalAcceptances={str(p):H(p) for p in [R/'artifacts/proplists-integration365-hash-peer1/original/acceptance.json',R/'artifacts/proplists-integration365-peer1/original8/acceptance.json']},DownloadedCodeExecuted=False,MainExecutedByReviewer=False,Normalization=False,FieldProjection=False,CompatibilityPromotions=[],KnownResiduals=['get_keys unordered returned-list ordering:1 value+5stack observations','own atom table changes physical HAMT topology:2raw values+10raw stack observations'],OriginalCodeImageStacksNotPairedByThisReplay=True,NoComplete22Claim=True)
(O/'replay3-comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(dict(Combined=combined,NegativeControls=controls,Simple=[dict(Cohort=x['Cohort'],File=x['File'],Total=x['Total'],Exact=x['WholeExact']) for x in reports],Sha256=H(O/'replay3-comparison.json')),indent=2))
