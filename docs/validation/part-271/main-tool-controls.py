from pathlib import Path
import json,hashlib,shutil,subprocess,sys
r=Path(__file__).resolve().parents[1];a=r/'artifacts';configuration=sys.argv[1] if len(sys.argv)>1 else 'Debug'
area=a/'part271-main-tool-controls';assert not area.exists(),'Never overwrite an earlier actual Main control run'
area.mkdir();sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
helper=a/'part271-main-capture-focus/bin'/configuration/'net10.0/Part271MainCaptureFocus.dll';assert helper.is_file()
rows=[]
for name,source,type_name in [('Erlang.DistributionPacketContrasts','distribution_packet_probe.erl','DistributionPacketContrasts.PacketProcess'),('Erlang.MixedMapIdentityContrasts','mixed_map_identity_probe.erl','NodeIdentityContrasts.NodeProcess')]:
 base=r/'tools'/name/'bin'/configuration/'net10.0';main=base/(name+'.dll');assert main.is_file()
 project=area/name;project.mkdir();inventory=[{'Name':p.name,'Sha256':sha(p)} for p in sorted(base.iterdir()) if p.is_file()]
 assert (base/source).read_bytes()==(r/'tools'/name/source).read_bytes()
 assert (base/'source-plan.json').read_bytes()==(r/'tools'/name/'source-plan.json').read_bytes()
 cli=[]
 for case_name in ['wrong-pin','wrong-tag','not-built','wrong-root','wrong-log-hash','wrong-version','wrong-source-hash','duplicate-artifact','wrong-scenario','wrong-plan-pin','existing-output','wrong-argument-count']:
  case=project/case_name;case.mkdir();binary=case/'tool';shutil.copytree(base,binary)
  assert all(sha(binary/row['Name'])==row['Sha256'] for row in inventory)
  otp=case/'synthetic-otp';versions=otp/'releases/29';versions.mkdir(parents=True);(versions/'OTP_VERSION').write_bytes(b'29.1.1')
  log=case/'synthetic-source-build.log';log.write_bytes(b'Synthetic CLI refusal only; NOT original OTP execution.\n')
  proof={'SourceCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','SourceTag':'OTP-29.1.1','SourceBuildCompleted':True,'InstalledOtpRoot':str(otp),'SourceBuildLog':str(log),'SourceBuildLogSha256':sha(log)}
  plan=json.loads((binary/'source-plan.json').read_text())
  if case_name=='wrong-pin':proof['SourceCommit']='wrong'
  if case_name=='wrong-tag':proof['SourceTag']='wrong'
  if case_name=='not-built':proof['SourceBuildCompleted']=False
  if case_name=='wrong-root':proof['InstalledOtpRoot']=str(case/'other-root')
  if case_name=='wrong-log-hash':proof['SourceBuildLogSha256']='0'*64
  if case_name=='wrong-version':(versions/'OTP_VERSION').write_bytes(b'29.1.0')
  if case_name=='wrong-source-hash':(binary/source).write_bytes((binary/source).read_bytes()+b'\n')
  if case_name=='duplicate-artifact':plan['RequiredArtifacts'][-1]=plan['RequiredArtifacts'][0]
  if case_name=='wrong-scenario':plan['Scenarios']=['other']
  if case_name=='wrong-plan-pin':plan['Pin']='wrong'
  if case_name in ['duplicate-artifact','wrong-scenario','wrong-plan-pin']:(binary/'source-plan.json').write_bytes(json.dumps(plan).encode())
  file=case/'evidence.json';file.write_bytes(json.dumps(proof).encode());output=case/'output'
  if case_name=='existing-output':output.mkdir()
  command=['dotnet',str(binary/(name+'.dll'))]+([] if case_name=='wrong-argument-count' else [str(otp),str(output),str(file)])
  result=subprocess.run(command,capture_output=True,timeout=20)
  (case/'stdout.raw').write_bytes(result.stdout);(case/'stderr.raw').write_bytes(result.stderr);(case/'exit').write_bytes((str(result.returncode)+'\n').encode())
  refused=result.returncode==1 and (not output.exists() if case_name!='existing-output' else not list(output.iterdir()))
  assert refused,(name,case_name)
  if case_name=='wrong-argument-count':assert ('Usage: '+name).encode() in result.stderr
  cli.append({'Name':case_name,'ExitCode':result.returncode,'RefusedBeforeWriting':refused,'SyntheticEvidence':True,'NativeExecuted':False})
 before=sha(main);raw=project/'raw-controls';result=subprocess.run(['dotnet',str(helper),str(main),type_name,str(raw)],capture_output=True,timeout=25)
 (project/'capture.stdout.raw').write_bytes(result.stdout);(project/'capture.stderr.raw').write_bytes(result.stderr);(project/'capture.exit').write_bytes((str(result.returncode)+'\n').encode())
 assert result.returncode==0,(name,result.stderr.decode(errors='replace'))
 capture=json.loads(result.stdout);assert capture['Status']=='PASS' and capture['ReflectionActualMainMethodInvoked'] and capture['ActualMainSha256']==before==sha(main) and capture['OwnedChildTerminated']
 rows.append({'Tool':name,'ActualMainAssemblySha256':before,'ActualMainAssemblyMvid':capture['ActualMainMvid'],'CopiedClosureBeforeDeliberateRefusalMutations':inventory,'CliRefusals':cli,'ActualMainProcessControls':capture,'NativeExecuted':False})
guard={'Status':'PASS_ActualMainToolCliAndProcessControls','Rows':rows,'MainTools':2,'CliRefusals':24,'ActualMainMethodsInvoked':True,'NoCopiedCaptureImplementationCompiled':True,'NativeExecuted':False}
(area/'guard.json').write_bytes((json.dumps(guard,indent=2)+'\n').encode());print('PASS24 actualMain CLI refusals and two actualMain capture methods with raw owned-child controls')
