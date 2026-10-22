"""Root-only cached own SDK observer replay. No builds, original VM or network."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,sys
R=Path(__file__).resolve().parents[2];B=Path(__file__).resolve().parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def save(p,x):p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8',newline='\n')
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,required=True);ap.add_argument('--only',nargs='+');a=ap.parse_args()
 out=a.output.resolve();assert out.is_relative_to((R/'artifacts').resolve()) and not out.exists();out.mkdir(parents=True)
 assert H(B/'inputs.json')=='D8CC20A89C523C11CEBFF70A57B906716CEBF016D3E5D831A4BE641D03BF987D'
 m=J(B/'inputs.json');helper=R/m['OwnedHelper'];assert H(helper)==m['OwnedHelperSha256']
 spec=importlib.util.spec_from_file_location('retained371_owned',helper);owned=importlib.util.module_from_spec(spec);spec.loader.exec_module(owned)
 env={k:v for k,v in os.environ.items() if k.upper() not in {'GH_TOKEN','GITHUB_TOKEN','GITHUB_API_TOKEN'}};env['DOTNET_PROCESSOR_COUNT']='1'
 closure=[]
 for n,f in [('Compiler','src'),('Runtime','src'),('Terms','src'),('Serialization','src'),('Differential','tools')]:
  p=R/f'{f}/Erlang.{n}/bin/Debug/net10.0/Erlang.{n}.dll';closure.append({'Path':p.relative_to(R).as_posix(),'Sha256':H(p)})
 save(out/'initial-main-dlls.json',closure)
 assert not a.only or set(a.only)<={x['Name'] for x in m['Stages']};results=[]
 for st in m['Stages']:
  if a.only and st['Name'] not in a.only:continue
  base=out/st['Name'];base.mkdir();cap=base/'capture1';cap.mkdir()
  try:
   tool=R/st['Tool'];assert H(tool/'source-inputs.corrected.json')==st['ManifestSha256'] and H(tool/'profile.json')==st['ProfileSha256']
   for x in J(tool/'source-inputs.corrected.json')['ToolInputs']:
    p=tool/x['Path'];assert p.stat().st_size==x['Bytes'] and H(p)==x['Sha256']
   for x in st['HarnessSources']:assert H(R/x['Path'])==x['Sha256']
   oldbin=R/st['Harness']/'bin/Debug/net10.0'
   for x in st['CachedBinFiles']:
    p=oldbin/x['Path'];assert p.stat().st_size==x['Bytes'] and H(p)==x['Sha256']
   assert H(oldbin/'MainCapture.dll')==st['ObserverSha256']
   harness=base/'main-capture';harness.mkdir()
   for x in st['HarnessSources']:shutil.copyfile(R/x['Path'],harness/Path(x['Path']).name)
   binary=harness/'bin/Debug/net10.0';shutil.copytree(oldbin,binary)
   rows=[]
   for x in closure:
    source=R/x['Path'];assert H(source)==x['Sha256'];p=binary/source.name;shutil.copyfile(source,p);assert H(p)==x['Sha256'];rows.append(dict(x,CopiedTo=p.relative_to(R).as_posix()))
   save(cap/'main-dll-closure.json',{'MainSource':True,'PrivateCandidateDllsUsed':False,'SubjectModule':st['Module'],'Files':rows})
   save(cap/'harness-source-closure.json',{'Files':[{'Path':Path(x['Path']).name,'Bytes':x['Bytes'],'Sha256':x['Sha256']} for x in st['HarnessSources']]})
   receipt=owned.owned(['C:/Program Files/dotnet/dotnet.exe',str(binary/'MainCapture.dll'),str(R),str(tool),str(cap/'sdk')],cap,st['Stage'],st['Timeout'],env,R)
   c=J(cap/'sdk/completion.json');assert c['FailedGroups']==0 and len(c['Groups'])==len(J(tool/'profile.json')['Fixtures'])
   results.append({'Name':st['Name'],'Passed':True,'Receipt':receipt,'Values':st['Values'],'Profiles':st['Profiles']})
  except Exception as e:results.append({'Name':st['Name'],'Passed':False,'Error':repr(e)})
  save(out/'settlement.json',{'Stages':results,'OriginalExecuted':False,'BuildExecuted':False,'Comparison':'PENDING'})
 unchanged=all(H(R/x['Path'])==x['Sha256'] for x in closure)
 save(out/'completion.json',{'Passed':unchanged and all(x['Passed'] for x in results),'MainDllClosureUnchanged':unchanged,'Stages':results,'OriginalExecuted':False,'BuildExecuted':False})
 return 0 if unchanged and all(x['Passed'] for x in results) else 1
if __name__=='__main__':raise SystemExit(main())
