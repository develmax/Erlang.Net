from pathlib import Path
import json,hashlib,subprocess,shutil
r=Path.cwd();a=r/'artifacts';root=a/'process-info-logical-prototype';p=a/'process-info-logical-source-freeze-revision7.json';x=json.loads(p.read_text());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
for f in x['Files']:assert sha((root/f['Path']).read_bytes())==f['Sha256']
focus=json.loads((a/'process-info-logical-focus-revision7.json').read_text(encoding='utf-8-sig'));assert focus['Host'] and focus['Failures']==0 and len(focus['Rows'])==7 and all(row['Pass'] for row in focus['Rows'])
for path in ['tools/Erlang.Differential/CompiledModuleCases.cs','tests/Erlang.Tests/Program.cs']:
 f=next(v for v in x['Files'] if v['Path']==path);s=f['SourceContent'];s=s.replace(', .. ProcessInfoLogicalCases.Modules','') if path.endswith('CompiledModuleCases.cs') else s.replace('\nTest(Erlang.Tests.ProcessInfoLogicalHostNames.Test, Erlang.Tests.ProcessInfoLogicalHostChecks.Run);','');assert s==f['BeforeSource']
seed=[];owned={f['Path'] for f in x['Files']}
for b in subprocess.check_output(['git','ls-tree','-r','--name-only','477d037','--','src','tools/Erlang.Differential','tools/Erlang.PortProbe']).decode().splitlines():
 if b in owned:continue
 pp=root/b
 if pp.exists():
  old=subprocess.check_output(['git','show','477d037:'+b]);assert old.decode().replace('\r\n','\n')==pp.read_text().replace('\r\n','\n'),b;seed.append(b)
x['Stage']='ScopedLogicalProcessQueriesPassedOriginalOtpPending';x['PreBuildFreezeSha256']=sha(p.read_bytes());x['ActualChecks']={'BuildErrors':0,'BuildWarnings':0,'StyleChanges':0,'HostPassed':True,'CompiledPassed':7,'NativeExecuted':False,'SourceUnchangedSinceLatestPreBuild':True,'OtherSeedSourceFilesExact':len(seed),'ExistingCaseAndTestDeclarationsPreserved':True};x['HistoricalFailuresPreserved']=['MSB9008 missing existing seed dependency','Unsupported ?MODULE fixture macro','Undefined unqualified resume_process/1 fixture'];x['FinalRuntimeAssemblySha256']=sha((root/'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes());out=a/'process-info-logical-source-freeze-final.json';out.write_bytes((json.dumps(x,indent=2)+'\n').encode());print('FINAL',sha(out.read_bytes()),'other seed',len(seed))
stage=a/'process-info-logical-evidence-staging';stage.mkdir(exist_ok=True)
for p in a.glob('process-info-logical-*'):
 if p.is_file():d=stage/'docs/validation/part-273/process-info-logical'/p.name;d.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,d)
for p in a.glob('*process-info*py'):
 if p.is_file():d=stage/'docs/validation/part-273/process-info-logical'/p.name;d.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,d)
for f in x['Files']:
 p=root/f['Path'];d=stage/'source'/f['Path'];d.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,d)
rows=[{'Path':str(p.relative_to(stage)).replace('\\','/'),'Sha256':sha(p.read_bytes()),'Length':p.stat().st_size} for p in sorted(stage.rglob('*')) if p.is_file() and p.name!='receipt.json'];v={'Status':'STAGED_ONLY','Files':rows,'SourcePaths':12,'SourceFreezeSha256':sha(out.read_bytes()),'NativeExecuted':False,'ProcessInfo1Registered':False,'Target0Warnings0Errors':True,'Style0':True,'HostAndSevenCompiledPass':True,'PriorFailuresPreserved':True};p=stage/'receipt.json';p.write_bytes((json.dumps(v,indent=2)+'\n').encode());print('RECEIPT',sha(p.read_bytes()),len(rows))
