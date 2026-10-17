"""Passive paired Port354 observation review. No guest/DLL/source code execution."""
from pathlib import Path
import hashlib,json
R=Path(__file__).resolve().parents[2]
P=Path(__file__).parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
HB=lambda b:hashlib.sha256(b).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
D=R/'artifacts/size-bif354-port-environment-diagnostic'
C=R/'artifacts/size-bif354-full-candidate'
freeze=D/'source-freeze.json'
assert H(freeze)=='81254D5DDB69A70A241EA80C3D780A8EAD52AF99BDA63A7978D75D1ED08A2752'
for x in J(freeze)['Files']:
    q=D/x['Path'];assert q.stat().st_size==x['Bytes'] and H(q)==x['Sha256']
t=J(D/'targets.json');names=t['Names'];assert len(names)==len(set(names))==16
full=R/'artifacts/size-bif354-full-actual1';assert H(full/'artifacts/tests.json')==t['FailedFullTestReportSha256']
fullcapture=J(full/'captured-outputs.json');assert fullcapture['TestDlls']==t['ActualFullTestDlls']
planpath=R/'artifacts/size-bif354-full-preparation/composition-plan.json';assert H(planpath)==t['FullPlanSha256']
plan=J(planpath);caserow=next(x for x in plan['Files'] if x['Path']=='tools/Erlang.Differential/PortNativeOptionCases.cs')
assert H(C/caserow['Path'])==caserow['Sha256']
assembly_rows=t['ActualFullTestDlls'];assemblymap={str((C/x['Path']).resolve()).casefold():x['Sha256'] for x in assembly_rows};assert len(assemblymap)==19
for x in assembly_rows:
    q=C/x['Path'];assert q.stat().st_size==x['Bytes'] and H(q)==x['Sha256']
build=R/'artifacts/size-bif354-port-diagnostic-build1';buildreceipt=J(build/'build.exit.json');assert buildreceipt['Settled'] and buildreceipt['ExitCode']==0 and not buildreceipt['TimedOut'] and not buildreceipt['Failures']
assert H(build/'build.stdout.log')==buildreceipt['StdoutSha256'] and H(build/'build.stderr.log')==buildreceipt['StderrSha256']
built=J(build/'built.json');dll=Path(built['HarnessDll']);assert H(dll)==built['Sha256']=='538DE453AEC26B0C572E6D1073F0031C4507AF4069F00DB31EF63E49BA10AAAB'
assert built['SourceFreezeSha256']==H(freeze)
# Independent straightforward reconstruction of the selected existing C# source literals/loop substitutions.
shell='{spawn,"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll"}'
executable='{spawn_executable,"tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe"}'
sources={}
def add(name,body):sources['erlang-port-native-option-'+name]='-module(port_native_option). -export([run/0]). run()->'+body+',done.'
for i,options in [(3,'overlapped_io'),(4,'overlapped_io,in,out')]:
    add('echo-'+str(i),'P=open_port('+shell+',[binary,'+options+']),true=erlang:port_command(P,<<1>>),receive {P,{data,<<1>>}}->ok after 5000->error(no_echo) end,true=erlang:port_close(P)')
for i,settings in enumerate(['stderr_to_stdout','stderr_to_stdout,overlapped_io','nouse_stdio,use_stdio,stderr_to_stdout'],5):
    add('shared-streams-'+str(i),'P=open_port('+executable+',[binary,{packet,1},{args,["streams"]},'+settings+']),receive {P,{data,<<11>>}}->ok after 5000->error(no_stdout_first) end,receive {P,{data,<<12>>}}->ok after 5000->error(no_stderr_first) end,receive {P,{data,<<13>>}}->ok after 5000->error(no_stdout_second) end,receive {P,{data,<<14>>}}->ok after 5000->error(no_stderr_second) end')
add('shell-shared-streams','P=open_port({spawn,"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll streams"},[binary,{packet,1},stderr_to_stdout]),receive {P,{data,<<11>>}}->ok after 5000->error(no_stdout_first) end,receive {P,{data,<<12>>}}->ok after 5000->error(no_stderr_first) end,receive {P,{data,<<13>>}}->ok after 5000->error(no_stdout_second) end,receive {P,{data,<<14>>}}->ok after 5000->error(no_stderr_second) end')
for i,value in enumerate(['custom-name','custom name',''],9):
    add('arg0-'+str(i),'P=open_port('+executable+',[binary,{packet,1},{arg0,"'+value+'"},{args,["argv0"]}]),receive {P,{data,B}}->B=list_to_binary("'+value+'") after 5000->error(no_arg0) end')
add('arg0-preserved-by-args','P=open_port('+executable+',[binary,{packet,1},{args,[]},{arg0,"first"},{args,["argv0"]},{arg0,"last"}]),receive {P,{data,<<108,97,115,116>>}}->ok after 5000->error(wrong_arg0) end')
for i,settings in enumerate(['nouse_stdio','use_stdio,nouse_stdio','nouse_stdio,in,out'],19):
    add('auxiliary-'+str(i),'P=open_port('+executable+',[binary,{packet,1},{args,["auxiliary"]},'+settings+']),true=erlang:port_command(P,<<1>>),receive {P,{data,<<1>>}}->ok after 5000->error(no_aux_echo) end,true=erlang:port_close(P)')
add('input-only-eof','P=open_port('+executable+',[binary,in,{packet,1},{args,["input-eof"]}]),receive {P,{data,<<17>>}}->ok after 5000->error(stdin_not_closed) end')
add('input-only-output-fails','process_flag(trap_exit,true),P=open_port({spawn,"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll slow-echo"},[binary,in,{packet,4}]),receive {P,{data,<<1>>}}->ok after 5000->error(no_ready) end,R=monitor(port,P),true=erlang:port_command(P,<<2>>),receive {\'EXIT\',P,ebadf}->ok after 5000->error(no_badfd_exit) end,receive {\'DOWN\',R,port,P,ebadf}->ok after 5000->error(no_badfd_down) end')
add('output-only-no-data','P=open_port({spawn,"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll emit"},[binary,out,eof]),receive {P,{data,_}}->error(unexpected_data);{P,eof}->error(unexpected_eof) after 200->ok end,{connected,S}=erlang:port_info(P,connected),S=self(),true=erlang:port_close(P)')
assert set(sources)==set(names)
goldenpath=R/'docs/validation/part-353/remote-completion/ordinary/artifact-12087319277/differential.json';golden=J(goldenpath);goldmap={x['Name']:x for x in golden['ModuleResults'] if x['Name'] in names};assert len(goldmap)==16
for name,source in sources.items():assert goldmap[name]['Source']==source and goldmap[name]['SourceSha256']==HB(source.encode())
profiles=[];pairs=[];evidence=[]
for folder,label,passed in [('size-bif354-port-diagnostic-default1','default-environment',False),('size-bif354-port-diagnostic-outside1','root-authorized-outside-sandbox',True)]:
    root=R/'artifacts'/folder;inputs=J(root/'inputs.json');completion=J(root/'completion.json')
    assert inputs['SourceFreezeSha256']==H(freeze) and inputs['HarnessDllSha256']==H(dll) and inputs['ActualFullDlls']==assembly_rows and inputs['ProfileLabelOnly']==label
    assert completion['Status']=='ACTUAL_NATIVEPORT16_MANAGED_DIAGNOSTIC_ACQUISITION_NOT_ENVIRONMENT_CAUSE_PROOF' and completion['HarnessDllSha256']==H(dll) and completion['ProfileLabelOnly']==label
    assert [x['Name'] for x in completion['Targets']]==names and completion['Passed']==(16 if passed else 0) and completion['Failed']==(0 if passed else 16)
    observations=[]
    for i,name in enumerate(names):
        directory=root/f'target-{i:02d}';out=directory/'outcome.json';x=J(out);record=completion['Targets'][i];receipt=J(directory/'process.exit.json');live=J(directory/'process.live.json')
        assert H(out)==record['OutcomeSha256'] and x['Name']==name and x['Passed']==record['Passed']==passed and x['Failure'] is None
        assert receipt['Settled'] and not receipt['TimedOut'] and not receipt['Failures'] and receipt['ExitCode']==(0 if passed else 1) and receipt['Pid']==live['Pid']>0
        assert receipt['Command']==live['Command'];cmd=receipt['Command'];assert Path(cmd[1]).resolve()==dll.resolve() and Path(cmd[2]).resolve()==(C/'tests/Erlang.Tests/bin/Debug/net10.0').resolve() and cmd[3]==name and Path(cmd[4]).resolve()==out.resolve()
        assert H(directory/'process.stdout.log')==receipt['StdoutSha256'] and H(directory/'process.stderr.log')==receipt['StderrSha256']
        assert (directory/'process.stdout.log').read_text().splitlines()==[('PASS ' if passed else 'FAIL ')+name] and (directory/'process.stderr.log').read_bytes()==b''
        assert 0<=x['ElapsedSeconds']<=90 and 0<=receipt['ElapsedSeconds']<=90 and x['InputAssembliesUnchanged']
        before={str(Path(a['Path']).resolve()).casefold():a['Sha256'] for a in x['BeforeAssemblies']};assert before==assemblymap
        loaded={str(Path(a['Path']).resolve()).casefold():a['Sha256'] for a in x['LoadedAssemblies']};assert len(loaded)==8 and all(assemblymap[k]==v for k,v in loaded.items())
        for a in x['LoadedAssemblies']:assert H(Path(a['Path']))==a['Sha256']
        obs=x['Observation'];gold=goldmap[name]
        assert obs['SourceSha256']==HB(sources[name].encode())==gold['SourceSha256'] and obs['CSharpSha256']==gold['CSharpSha256']
        assert obs['CallerContext']==0 and gold['CallerContext']=='UntrappedFresh' and obs['CompilerPolicy']==0 and gold['CompilerPolicy']=='Default'
        assert obs['Expected']==gold['FixtureExpected']==gold['Expected']=='{ok,done}' and obs['Actual']==('{ok,done}' if passed else '{error,error,eacces}')
        assert obs['StreamsMatchOriginalFixtureContract'] and obs['StandardOutputHex']=='' and obs['StandardErrorHex']==''
        assert not x['FullValidationExecuted'] and not x['OriginalOtpExecuted'] and not x['EnvironmentCausalityClaim']
        observations.append({'Name':name,'Observation':obs,'LoadedAssemblies':x['LoadedAssemblies'],'Environment':x['Environment'],'ExitSha256':H(directory/'process.exit.json'),'OutcomeSha256':H(out)})
        evidence.extend({'Path':q.relative_to(R).as_posix(),'Bytes':q.stat().st_size,'Sha256':H(q)} for q in [out,directory/'process.live.json',directory/'process.exit.json',directory/'process.stdout.log',directory/'process.stderr.log'])
    profiles.append({'Label':label,'Passed':16 if passed else 0,'Failed':0 if passed else 16,'InputsSha256':H(root/'inputs.json'),'CompletionSha256':H(root/'completion.json'),'Observations':observations})
for left,right in zip(profiles[0]['Observations'],profiles[1]['Observations'],strict=True):
    assert left['Name']==right['Name'] and left['LoadedAssemblies']==right['LoadedAssemblies'] and left['Environment']==right['Environment']
    a=left['Observation'];b=right['Observation'];assert {k:v for k,v in a.items() if k!='Actual'}=={k:v for k,v in b.items() if k!='Actual'}
    pairs.append({'Name':left['Name'],'SourceSha256':a['SourceSha256'],'RecordedCSharpSha256':a['CSharpSha256'],'CallerContext':'UntrappedFresh','CompilerPolicy':'Default','Expected':a['Expected'],'DefaultActual':a['Actual'],'OutsideActual':b['Actual'],'StreamsEqual':True,'LoadedAssemblyIdentitiesEqual':True,'RecordedEnvironment':left['Environment']})
report={'Status':'PASS_INDEPENDENT_PAIRED_SAME_DLL_PORT16_CONTEXT_OBSERVATIONS','SourceFreezeSha256':H(freeze),'ReaderSha256':H(Path(__file__)),'HarnessDllSha256':H(dll),'FailedFullReportSha256':H(full/'artifacts/tests.json'),'PriorPublishedGoldenReportSha256':H(goldenpath),'SourceCatalogSha256':caserow['Sha256'],'OriginalFullDllRows':19,'LoadedAssemblyRowsPerCase':8,'ActualOwnedTargets':32,'Default':{'Passed':0,'Failed':16},'Outside':{'Passed':16,'Failed':0},'Pairs':pairs,'Profiles':[{k:v for k,v in x.items() if k!='Observations'} for x in profiles],'RawEvidence':evidence,'Interpretation':'All sixteen unchanged selected contracts fail with eacces in the recorded default context and pass in the Root-authorized outside context using identical retained full1 assemblies, harness, fixture sources, expected terms, generated-code hashes, policies and streams. This corroborates a context-sensitive execution restriction for these observations; it does not identify a specific Win32 denial API/security mechanism or prove universal port compatibility.','Limits':['No native original OTP or SDK execution by this passive reader. Prior published source/golden rows corroborate fixture identity only, not fresh native acquisition.','Erlang sources independently reconstructed and raw source hashes verified; generated CSharp bytes were not retained by harness, so recorded CSharp hashes are bound/compared, not independently regenerated.','Actual nineteen test-directory DLLs rehashed and eight loaded DLLs checked. Probe child executable/DLL initial hashes were not emitted by harness; no invented before/after identity of every descendant binary.','Profile label is recorded by Root invocation; OS/framework/architecture/cwd fields are identical, but Windows token/job/policy details were not recorded. No specific causal mechanism or whole full-run pass follows.'],'NoMainSdkNetworkMutation':True}
q=P/'report.json';assert not q.exists();q.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(H(q))
