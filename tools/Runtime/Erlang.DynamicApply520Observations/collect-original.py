"""Source-bound fixture acquisition using existing provider/process helpers."""
import argparse, hashlib, importlib.util, json, os, shutil, subprocess, sys
from pathlib import Path
sys.dont_write_bytecode=True
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(name,path):
 s=importlib.util.spec_from_file_location(name,path);v=importlib.util.module_from_spec(s);s.loader.exec_module(v);return v
def save(p,v):Path(p).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def require(v,s):
 if not v:raise ValueError(s)
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--repository',type=Path,required=True);parser.add_argument('--plan-sha256',required=True);parser.add_argument('--output',type=Path,required=True);parser.add_argument('--execute-original',action='store_true');parser.add_argument('--expected-head',required=True)
 group=parser.add_mutually_exclusive_group(required=True);group.add_argument('--official-admission',type=Path);group.add_argument('--build-receipt',type=Path);parser.add_argument('--admission-sha256',required=True);parser.add_argument('--otp-source',type=Path)
 a=parser.parse_args();repo=a.repository.resolve(strict=True);tool=Path(__file__).resolve().parent;out=a.output.resolve();plan=tool/'source-plan.json'
 require(sha(plan)==a.plan_sha256.upper(),'Reviewed source plan SHA differs');v=json.loads(plan.read_bytes());require(v['Pin']==PIN,'Pin differs')
 for row in v['Inputs']:
  f=(repo/row['Path']).resolve(strict=True);require(f.is_relative_to(repo) and sha(f)==row['Sha256'],'Source input differs')
 require(subprocess.check_output(['git','-C',str(repo),'rev-parse','HEAD'],text=True).strip()==a.expected_head,'Actual collector head differs')
 require(out.is_relative_to(repo/'artifacts') and not out.exists(),'Fresh owned artifact output required')
 require(a.execute_original,'No execution without explicit --execute-original')
 shared=load('dynamic520_shared',repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py')
 installed,erl,provider,after=shared.provider(a,repo);out.mkdir(parents=True);save(out/'provider-before.json',provider);phases={};errors=[];images=[];passed=False
 for f in sorted((tool/'fixtures').glob('*.erl')):shutil.copyfile(f,out/f.name)
 odbc=repo/'tests/Erlang.Tests/Resources/ApplyOriginal477/odbc.beam';require(sha(odbc)=='7830D857641C35AD0491C89212AB124003281C27103D280FD151BA9460EEA8BC','Existing genuine477 ODBC image differs');shutil.copyfile(odbc,out/'odbc.beam')
 env=dict(os.environ,ERL_CRASH_DUMP=str(out/'erl_crash.dump'),ERL_CRASH_DUMP_SECONDS='15')
 try:
  c=out/'compile';c.mkdir()
  for f in out.glob('*.erl'):shutil.copyfile(f,c/f.name)
  expression='Names=[dynamic_apply_caller520,dynamic_apply_target520,acquire520],lists:foreach(fun(M)->{ok,M}=compile:file(atom_to_list(M)++".erl",[debug_info,return_errors]),ok end,Names),I=[begin {ok,{M,[{imports,Rows}]}}=beam_lib:chunks(atom_to_list(M)++".beam",[imports]),{M,Rows} end||M<-[dynamic_apply_caller520,dynamic_apply_target520]],ok=file:write_file("static-imports.etf",term_to_binary(I)),halt().'
  phases['Compile']=shared.execute(repo,[str(erl),'+S','2:2','-noshell','-noinput','-eval',expression],c,env)
  decoder=load('dynamic520_etf',repo/'tools/Native/Erlang.NifMapFamily504Observations/diagnostic_etf.py');tree=decoder.decode_exact((c/'static-imports.etf').read_bytes());require(tree[0]=='list','Import list')
  for entry in tree[1]:
   require(entry[0]=='tuple' and len(entry[1])==2,'Import module row');mod,imports=entry[1];require(mod[0]=='atom' and imports[0]=='list','Module imports shape');modules=set()
   for imp in imports[1]:require(imp[0]=='tuple' and len(imp[1])==3,'Import MFA');modules.add(imp[1][0][1])
   require(modules=={'erlang'},'Caller/callee must have only genuine erlang static imports, never dynamic target')
   b=c/(mod[1]+'.beam');shutil.copyfile(b,out/b.name);images.append(dict(Module=mod[1],Name=b.name,Sha256=sha(b),StaticModules=sorted(modules)))
  shutil.copyfile(c/'acquire520.beam',out/'acquire520.beam')
  phases['Observer']=shared.execute(repo,[str(erl),'+S','2:2','-noshell','-noinput','-pa',str(out),'-eval','ok=acquire520:run(),halt().'],out,env)
  require(all((out/n).is_file() for n in ['observed.etf','observed-deterministic.etf','worker-witness.etf','owners.etf']),'Full original outputs missing');passed=True
 except Exception as error:errors.append(repr(error))
 finally:
  try:
   current=after();require(current==provider['Inventory'],'Provider closure changed');save(out/'provider-after.json',dict(ProviderEqual=True,Inventory=current))
  except Exception as error:errors.append('postflight: '+repr(error));passed=False
  inventory=[dict(Path=f.relative_to(out).as_posix(),Bytes=f.stat().st_size,Sha256=sha(f)) for f in sorted(out.rglob('*')) if f.is_file() and f!=out/'completion.json']
  save(out/'completion.json',dict(Pin=PIN,OriginalOnly=True,AcquisitionPassed=passed and not errors,SourcePlanSha256=sha(plan),ProviderAdmissionSha256=a.admission_sha256.upper(),Files=inventory,Images=images,LoaderImage=dict(Module='odbc',Name='odbc.beam',Sha256=sha(out/'odbc.beam')),Phases=phases,Errors=errors,WholeContractsClosed=0,SourceInputs=v['Inputs']))
 require(passed and not errors,'Original acquisition failed; retain all errors/cleanup: '+repr(errors))
if __name__=='__main__':main()
