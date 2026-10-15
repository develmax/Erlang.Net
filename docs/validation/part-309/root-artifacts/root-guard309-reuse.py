from pathlib import Path
import hashlib,json,runpy,subprocess
r=Path.cwd();a=r/'artifacts';read=lambda p:json.loads(p.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
freeze=a/'root-part309-final-source-freeze.json';assert sha(freeze.read_bytes())=='EDB97892181DA1CA458A9ADEFCD60163AF255DF416E0388FDB18E74217832659';f=read(freeze)
assert not f['NewFullValidationExecuted'] and not f['RuntimeCompilerAndTestSourcesChanged']
for row in f['Files']:
 data=(r/row['Path']).read_bytes();assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
assert not subprocess.check_output(['git','diff','HEAD','--name-only','-z','--','src','tests','build','examples','benchmarks','tools/Erlang.Differential','tools/Erlang.NifNativeBackend','tools/Erlang.NifPendingVarargsObservations','Directory.Build.props','Directory.Build.targets','global.json','Erlang.Net.slnx'])
old=r/'docs/validation/part-308';receipt=read(old/'evidence-receipt.json');tests=read(old/'tests.json');assert (tests['Passed'],tests['Failed'])==(6637,0)
assert len(read(old/'integration-results.json'))==11 and receipt['IntegrationAndPackagesPassed'] and receipt['NativeTrustedWindowsProfilePassed']
assert read(a/'root-part309-main-tool-target-exit.json')['ExitCode']==0
log=(a/'root-part309-main-tool-target.log').read_text(encoding='utf-8-sig');assert 'Предупреждений: 0' in log and 'Ошибок: 0' in log
tooling=a/'beam-load-nif-opcodes313-validated1';tf=tooling/'validation-freeze.json';assert sha(tf.read_bytes())=='DE5FBAE0024DB7ADB89489DF595C31E10DF8F9EB84A20D349112B48035839F65'
for row in read(tf)['Files']:
 data=(tooling/row['Path']).read_bytes();assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';helper=runpy.run_path(str(plan.parent/'run-reviewed.py'));pv=read(plan);assert sha(plan.read_bytes())==f['ReviewedSourcePlanSha256'] and len(pv['Files'])==1020
assert helper['reviewed_inputs'](r,plan)=={x['Path'] for x in pv['Files']}
for row in pv['Files']:
 actual=helper['record'](r/row['Path'],r,row['HashMode']);actual['Path']=row['Path'];assert actual==row,row['Path']
registry=(r/'docs/supported-mfas.json').read_bytes();assert registry==(old/'supported-mfas.json').read_bytes()
runtime=r/'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll';native=read(a/'root-part308-main-native-guard.json');assert sha(runtime.read_bytes())==native['RuntimeDllSha256']
doc=a/'root-part309-erlang-table-candidate';audit=read(doc/'audit.json');assert sha((r/'docs/ERLANG_COMPATIBILITY.md').read_bytes())==audit['ResultSha256']
result={'Status':'REUSE_PUBLISHED308_RUNTIME_FULL6637_INTEGRATION11_NATIVE1020_AND_NEW309_TOOL_TARGET_PASS','SourceFreezeSha256':sha(freeze.read_bytes()),'SourceHashesExact':True,'SourcePaths':8,'CSharpPaths':3,'ReusedFullHead':f['ReusedFullHead'],'ReusedFullPassed':6637,'ReusedIntegrationResults':11,'NewFullValidationExecuted':False,'NewNativeExecuted':False,'NewOriginalExecuted':False,'RuntimeCompilerAndTestSourcesChanged':False,'NativeSourceInputsVerifiedUnchanged':1020,'ReviewedSourcePlanSha256':sha(plan.read_bytes()),'RuntimeDllSha256':sha(runtime.read_bytes()),'StandaloneMainToolBuildPassed':True,'IsolatedToolValidationFreezeSha256':sha(tf.read_bytes()),'AllFrozenToolingMembersVerified':len(read(tf)['Files']),'Registry':505,'ErlangPresence':347,'RawAndDerivedRegistryUnchangedFrom308':True,'CorrectedHumanTableRows':3,'OriginalOpcodeShapesVerified':False,'GeneralNifAbiComplete':False}
out=a/'root-part309-reuse-and-tool-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
