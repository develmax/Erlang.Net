"""Parent-only passive comparison launcher. No VM/SDK subject or build execution."""
from pathlib import Path
import argparse,hashlib,json,subprocess,sys
O=Path(__file__).resolve().parent;R=O.parents[1]
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def save(p,x):p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
def main():
 p=argparse.ArgumentParser()
 for key in ['capture','guards','output']:p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--guards-sha256',required=True);p.add_argument('--only',nargs='+');a=p.parse_args()
 for row in J(O/'comparison-freeze.json')['Files']:assert H(O/row['Path'])==row['Sha256']
 out=a.output.resolve();assert not out.exists() and out.is_relative_to(R/'artifacts')
 assert H(a.guards)==a.guards_sha256.upper();guards=J(a.guards);assert len(guards)>=499
 for row in guards:assert H(R/row['Path'])==row['Sha256'],row['Path']
 plan=J(O/'comparison-inputs.json');assert not a.only or set(a.only)<={x['Name'] for x in plan['Stages']}
 out.mkdir();results=[]
 for stage in plan['Stages']:
  name=stage['Name']
  if a.only and name not in a.only:continue
  target=out/name;sdk=a.capture/name/'capture1/sdk'
  try:
   assert sdk.exists() and H(R/stage['Script'])==stage['ScriptSha256']
   closure=J(sdk.parent/'main-dll-closure.json');assert closure['MainSource'] and not closure['PrivateCandidateDllsUsed'] and len(closure['Files'])==5
   for row in closure['Files']:assert H(R/row['Path'])==H(R/row['CopiedTo'])==row['Sha256'],row['Path']
   argv=[sys.executable,'-B',stage['Script'],*stage['Arguments'],'--sdk',str(sdk),'--output',str(target)]
   result=subprocess.run(argv,cwd=R,capture_output=True)
   (out/(name+'.stdout')).write_bytes(result.stdout);(out/(name+'.stderr')).write_bytes(result.stderr)
   results.append({'Name':name,'ExitCode':result.returncode,'Command':argv,'Acceptance':str(target/'acceptance.json') if (target/'acceptance.json').exists() else None})
  except Exception as error:results.append({'Name':name,'Error':repr(error),'NoSyntheticOutcomeAdded':True})
  save(out/'settlement.json',{'Stages':results,'NoSubjectExecuted':True,'SourceGuardsSha256':H(a.guards)})
 unchanged=all(H(R/row['Path'])==row['Sha256'] for row in guards)
 save(out/'completion.json',{'Stages':results,'AllRequestedAttempted':len(results)==sum(not a.only or x['Name'] in a.only for x in plan['Stages']),'SourceGuardsUnchanged':unchanged,'AllComparisonsPassed':unchanged and all(x.get('ExitCode')==0 for x in results),'NoSubjectExecuted':True})
 if not unchanged or any(x.get('ExitCode')!=0 for x in results):raise SystemExit(1)
if __name__=='__main__':main()
