from pathlib import Path
import json,hashlib,shutil,subprocess,sys
r=Path(__file__).resolve().parents[1];a=r/'artifacts';configuration=sys.argv[1] if len(sys.argv)>1 else 'Debug'
area=a/'part272-main-packet-controls';assert not area.exists(),'Never overwrite an earlier actual Main control run'
area.mkdir();sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
helper=a/'part271-main-capture-focus/bin'/configuration/'net10.0/Part271MainCaptureFocus.dll';# Reflection helper is not executed for unchanged PacketProcess source.
# Previous actual Main raw-process evidence remains immutable.
frozen=json.loads((a/'packet-pending-correction-source-freeze-final.json').read_text())
for f in frozen['Files']:
 assert sha(r/f['Path'])==f['Sha256'],f['Path']
process=r/'tools/Erlang.DistributionPacketContrasts/PacketProcess.cs'
assert process.read_bytes()==subprocess.check_output(['git','show','2c1f6dadcf97b7301b60ea153dd6895a6a1a4687:tools/Erlang.DistributionPacketContrasts/PacketProcess.cs'])
rows=[]
for name,source,type_name in [('Erlang.DistributionPacketContrasts','distribution_packet_probe.erl','DistributionPacketContrasts.PacketProcess')]:
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
 rows.append({'Tool':name,'ActualMainAssemblySha256':sha(main),'CopiedClosureBeforeDeliberateRefusalMutations':inventory,'CliRefusals':cli,'NativeExecuted':False})

guard={'Status':'PASS_ActualMainCorrectedPacketCliSourceHashControls','Rows':rows,'MainTools':1,'CliRefusals':12,'RawProcessHelperRerun':False,'UnchangedHelperSourceVerified':True,'NativeExecuted':False}
(area/'guard.json').write_bytes((json.dumps(guard,indent=2)+'\n').encode());print('PASS12 actual Main corrected packet CLI refusals; raw process source unchanged from271')
