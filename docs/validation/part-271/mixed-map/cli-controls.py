import pathlib,subprocess,hashlib,json,shutil,copy,sys
root=pathlib.Path(__file__).resolve().parent
candidate=root.parent/'mixed-map-collector-prototype'
base=candidate/'tools/Erlang.MixedMapIdentityContrasts/bin/Release/net10.0'
area=root/'cli-controls';area.mkdir(exist_ok=True)
original_source=(candidate/'tools/Erlang.MixedMapIdentityContrasts/mixed_map_identity_probe.erl').read_bytes()
rows=[]
for name in ['wrong-pin','wrong-tag','not-built','wrong-root','wrong-log-hash','wrong-version','wrong-source-hash','duplicate-artifact','wrong-scenario','wrong-plan-pin','existing-output']:
 case=area/name;case.mkdir(exist_ok=True); binary=case/'tool';shutil.copytree(base,binary,dirs_exist_ok=True)
 otp=case/'fake-otp';versions=otp/'releases/29';versions.mkdir(parents=True,exist_ok=True);(versions/'OTP_VERSION').write_text('29.1.1')
 log=case/'fake-source-build.log';log.write_bytes(b'SYNTHETIC CLI refusal control; NOT original OTP execution.\n')
 evidence={'SourceCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','SourceTag':'OTP-29.1.1','SourceBuildCompleted':True,'InstalledOtpRoot':str(otp.resolve()),'SourceBuildLog':str(log.resolve()),'SourceBuildLogSha256':hashlib.sha256(log.read_bytes()).hexdigest().upper()}
 plan=json.loads((binary/'source-plan.json').read_text())
 if name=='wrong-pin':evidence['SourceCommit']='wrong'
 if name=='wrong-tag':evidence['SourceTag']='OTP-29.1.0'
 if name=='not-built':evidence['SourceBuildCompleted']=False
 if name=='wrong-root':evidence['InstalledOtpRoot']=str(case/'other-root')
 if name=='wrong-log-hash':evidence['SourceBuildLogSha256']='0'*64
 if name=='wrong-version':(versions/'OTP_VERSION').write_text('29.1.0')
 if name=='wrong-source-hash':(binary/'mixed_map_identity_probe.erl').write_bytes(original_source+b'\n')
 if name=='duplicate-artifact':plan['RequiredArtifacts'][-1]=plan['RequiredArtifacts'][0]
 if name=='wrong-scenario':plan['Scenarios']=['other']
 if name=='wrong-plan-pin':plan['Pin']='wrong'
 if name in ['duplicate-artifact','wrong-scenario','wrong-plan-pin']:(binary/'source-plan.json').write_text(json.dumps(plan))
 proof=case/'evidence.json';proof.write_text(json.dumps(evidence));output=case/'output'
 if name=='existing-output':output.mkdir(exist_ok=True)
 args=['dotnet',str(binary/'Erlang.MixedMapIdentityContrasts.dll'),str(otp),str(output),str(proof)]
 p=subprocess.run(args,capture_output=True,timeout=20)
 (case/'stdout.raw').write_bytes(p.stdout);(case/'stderr.raw').write_bytes(p.stderr);(case/'exit').write_text(str(p.returncode)+'\n')
 refused=p.returncode!=0 and (not output.exists() if name!='existing-output' else not list(output.iterdir()))
 rows.append({'Name':name,'ExitCode':p.returncode,'OutputCreated':output.exists(),'RefusedBeforeWriting':refused,'SyntheticEvidence':True,'NativeExecuted':False})
(root/'cli-controls.json').write_text(json.dumps({'Status':'PASS' if all(x['RefusedBeforeWriting'] for x in rows) else 'FAIL','Rows':rows},indent=2))
print(json.dumps(rows,indent=2))
sys.exit(0 if all(x['RefusedBeforeWriting'] for x in rows) else 1)
