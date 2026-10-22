"""All typed logical fields, full frames/arguments/error_info; no waivers or seeded outputs."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,traceback,zipfile
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
HEAD='76c034724be1b21da8c66ada0ec164307386cfcd';RUN=39119539009;JOB=117652131016;ARTIFACT=12133782860
ZIP='1B07705B38A66F1B0C67947A74B24820E0DF18C7217FC7D49398F2200F349E8F'
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def dump(p,x):p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')

def cohort_compare(name,plan,profile,tool,native,sdk,decoder,out):
 dlls=J(sdk.parent/'main-dll-closure.json');assert len(dlls['Files'])==5 and dlls['MainSource'] and not dlls['PrivateCandidateDllsUsed']
 dllchecks=[]
 for row in dlls['Files']:
  captured=(ROOT/row['CopiedTo']).resolve();assert captured.is_relative_to(ROOT/'artifacts') and H(captured)==row['Sha256']
  live=(ROOT/row['Path']).resolve();assert live.is_relative_to(ROOT)
  dllchecks.append({'Path':row['Path'],'CapturedSha256':row['Sha256'],'CopiedPath':row['CopiedTo'],'LiveSha256':H(live) if live.exists() else None,'MatchesLive':live.exists() and H(live)==row['Sha256']})
 receipt=J(sdk.parent/'erlang57-capture.exit.json');assert receipt['Settled'] and not receipt['TimedOut'] and receipt['ExitCode']==0 and not receipt['Failures']
 for row in J(sdk/'capture-seal.json')['Files']:
  q=(sdk/row['Path']).resolve();assert q.is_relative_to(sdk) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
 completion=J(sdk/'completion.json');assert completion['FailedGroups']==0 and len(completion['Groups'])==len(profile['Fixtures'])
 rows=[];diffs=[];nativefiles=[]
 for fixture,group in zip(profile['Fixtures'],completion['Groups']):
  assert group['Id']==fixture['Id'] and not group['ExecutionSkipped'] and group['Status']=='CAPTURED_PENDING_PASSIVE_CHECKS'
  names=[fixture['Path']]+fixture['Companions'];assert len(names)==len(group['Compile'])
  for path,compiled in zip(names,group['Compile']):assert compiled['Passed'] and compiled['DeclaredSourceFile']==Path(path).name and compiled['SourceSha256']==H(tool/path)
  directory=sdk/fixture['Id'];metadata=decoder.decode_exact((directory/'runtime-metadata.etf').read_bytes())
  assert metadata[0]=='tuple' and len(metadata[1])==3 and metadata[1][0]==('atom','sdk_runtime_provenance') and metadata[1][2][0]=='pid'
  for filename,key in [('returned.etf','ReturnedSha256'),('stack-observations.etf','StacksSha256')]:
   assert H(directory/filename)==group[key]
   q=native/fixture['Id']/filename;nativefiles.append({'Path':str(q),'Bytes':q.stat().st_size,'Sha256':H(q)})
   left=decoder.decode_exact(q.read_bytes());right=decoder.decode_exact((directory/filename).read_bytes())
   if filename=='returned.etf':batches=[('value',left,right)]
   else:
    assert left[0]==right[0]=='list' and len(left[1])==len(right[1])==5;batches=[]
    for depth,l,r in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],left[1],right[1]):
     assert l[0]==r[0]=='tuple' and len(l[1])==len(r[1])==2 and l[1][0]==r[1][0]==depth
     batches.append((repr(depth),l[1][1],r[1][1]))
   for depth,left,right in batches:
    assert left[0]==right[0]=='list' and len(left[1])==len(right[1])==len(fixture['Names'])
    for casename,l,r in zip(fixture['Names'],left[1],right[1]):
     assert l[0]==r[0]=='tuple' and len(l[1])==len(r[1])==2 and l[1][0]==r[1][0]==('atom',casename)
     equal=l[1][1]==r[1][1];rows.append({'Cohort':name,'Group':fixture['Id'],'Name':casename,'Depth':depth,'Strict':equal})
     if not equal:diffs.append({'Cohort':name,'Group':fixture['Id'],'Name':casename,'Depth':depth,'Original':repr(l[1][1]),'Sdk':repr(r[1][1])})
 assert len(rows)==6*plan['ExpectedValues'] and sum(x['Depth']=='value' for x in rows)==plan['ExpectedValues']
 out.mkdir();dump(out/'rows.json',rows);dump(out/'differences.json',diffs)
 result={'Status':'PASS_ALL_TYPED_FIELDS_STRICT' if not diffs else 'DIFFERENCES_RETAINED','Cohort':name,'Values':plan['ExpectedValues'],'FullStackProfiles':plan['ExpectedProfiles'],'Rows':len(rows),'Strict':len(rows)-len(diffs),'Differences':len(diffs),'NormalizedFields':0,'Waivers':0,'Relations':0,'NativeFiles':nativefiles,'SdkSealSha256':H(sdk/'capture-seal.json'),'SdkDlls':dllchecks,'HistoricalCapture':not all(x['MatchesLive'] for x in dllchecks),'FinalRecaptureRequired':not all(x['MatchesLive'] for x in dllchecks),'NoPromotion':True}
 dump(out/'acceptance.json',result)
 return result,rows,diffs

def main():
 p=argparse.ArgumentParser()
 for key in ['native-artifact','native-admission','sdk-root','output']:p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--native-admission-sha256',required=True);a=p.parse_args()
 out=a.output.resolve();assert out.is_relative_to(ROOT/'artifacts') and not out.exists()
 for row in J(HERE/'source-freeze.json')['Files']:assert H(HERE/row['Path'])==row['Sha256']
 reviewed=J(HERE/'reviewed-source-packet.json');packet=ROOT/reviewed['SourcePacket'];assert H(packet/'source-seal.json')==reviewed['SourceSealSha256']
 for row in J(packet/'source-seal.json')['Files']:
  q=packet/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
 artifact=a.native_artifact.resolve();remote=artifact.parent;archive=remote/('artifact-'+str(ARTIFACT)+'.zip')
 assert H(archive)==ZIP
 run=J(remote/'run.json');jobs=J(remote/'jobs.json')['jobs'];ars=J(remote/'artifacts.json')['artifacts']
 assert run['id']==RUN and run['head_sha']==HEAD and run['run_attempt']==1 and run['status']=='completed'
 assert len(jobs)==1 and jobs[0]['id']==JOB and jobs[0]['conclusion']==run['conclusion']
 assert len(ars)==1 and ars[0]['id']==ARTIFACT and ars[0]['workflow_run']['head_sha']==HEAD and ars[0]['digest'].split(':',1)[1].upper()==ZIP
 with zipfile.ZipFile(archive) as z:
  files=[x for x in z.infolist() if not x.is_dir()];assert len(files)==len({x.filename for x in files})
  assert {q.relative_to(artifact).as_posix() for q in artifact.rglob('*') if q.is_file()}=={x.filename for x in files}
  for item in files:
   q=(artifact/item.filename).resolve();assert q.is_relative_to(artifact) and q.stat().st_size==item.file_size and H(q)==hashlib.sha256(z.read(item)).hexdigest().upper()
 assert H(a.native_admission)==a.native_admission_sha256.upper();admission=J(a.native_admission)
 assert admission['Head']==HEAD and admission['Run']==RUN and admission['ParentRunConclusion']==run['conclusion'] and not admission['DownloadedCodeExecuted'] and not admission['ExpectedOutcomesSeeded']
 decoderpath=packet/'ordsets/passive_etf_family.py'
 spec=importlib.util.spec_from_file_location('reviewed_structural_etf372',decoderpath);decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
 # The reviewed decoder retains all serialized FUN fields and float bits. Its
 # validated raw byte span is transport bookkeeping, never an Erlang field.
 out.mkdir();results=[];allrows=[];alldiffs=[]
 for plan in reviewed['Cohorts']:
  name=plan['Id'];target=out/name
  try:
   report=next(x for x in admission['Collectors'] if x['Id']==name)
   if report['ExitCode']!=0:
    results.append({'Cohort':name,'Status':'BLOCKED_NATIVE_COLLECTOR_NOT_ADMITTED','ExpectedValues':plan['ExpectedValues'],'ExpectedProfiles':plan['ExpectedProfiles'],'ParentRunConclusion':run['conclusion']});continue
   child=Path(report['AcceptancePath']);assert H(child)==report['AcceptanceSha256'];childreport=J(child)
   assert childreport['Head']==HEAD and childreport['Run']==RUN and childreport['Values']==plan['ExpectedValues'] and childreport['FullStackProfiles']==plan['ExpectedProfiles']
   tool=packet/name;manifest=tool/'source-inputs.corrected.json';assert H(manifest)==plan['ManifestSha256'] and H(tool/'profile.json')==plan['ProfileSha256']
   paths=[q for q in artifact.rglob('completion.json') if J(q).get('SourceInputsSha256')==H(manifest)];assert len(paths)==1
   capture=paths[0].parent;nativecompletion=J(paths[0]);inputs=J(capture/'inputs.json')
   assert nativecompletion['Status']=='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' and nativecompletion['ProfileSha256']==inputs['ProfileSha256']==H(tool/'profile.json')
   result,rows,diffs=cohort_compare(name,plan,J(tool/'profile.json'),tool,capture,(a.sdk_root/name/'capture1/sdk').resolve(),decoder,target)
   results.append(result);allrows+=rows;alldiffs+=diffs
  except Exception as error:
   if not target.exists():target.mkdir()
   dump(target/'failure.json',{'Error':repr(error),'Traceback':traceback.format_exc(),'NoSyntheticOutcomeAdded':True})
   results.append({'Cohort':name,'Status':'PASSIVE_COMPARISON_ERROR','Error':repr(error)})
 dump(out/'rows.json',allrows);dump(out/'differences.json',alldiffs)
 completed=len(allrows)==1002 and all(r['Status']=='PASS_ALL_TYPED_FIELDS_STRICT' for r in results)
 receipt={'Status':'PASS_ALL1002_FIELDS_STRICT' if completed and run['conclusion']=='success' else 'PARTIAL_COMPARISON_OR_DIFFERENCES_RETAINED','NativeRun':RUN,'NativeHead':HEAD,'ParentRunConclusion':run['conclusion'],'Cohorts':results,'Rows':len(allrows),'Strict':len(allrows)-len(alldiffs),'Differences':len(alldiffs),'ExpectedRows':1002,'BlockedOrMissingRows':1002-len(allrows),'NormalizedFields':0,'Waivers':0,'Relations':0,'NativeAdmissionSha256':H(a.native_admission),'ArtifactZipSha256':ZIP,'DecoderSha256':H(decoderpath),'SourceFreezeSha256':H(HERE/'source-freeze.json'),'DownloadedCodeExecuted':False,'NoPromotion':True}
 dump(out/'acceptance.json',receipt);print(json.dumps({'Status':receipt['Status'],'Rows':len(allrows),'Differences':len(alldiffs),'Acceptance':str(out/'acceptance.json')}))
 if not completed or run['conclusion']!='success':raise SystemExit(1)
if __name__=='__main__':main()
