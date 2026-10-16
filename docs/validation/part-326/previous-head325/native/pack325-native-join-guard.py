import sys
sys.dont_write_bytecode=True
from pathlib import Path
import hashlib,json,importlib.util
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
s=importlib.util.spec_from_file_location('audit',a/'pack325-audit-common.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mapping=j(a/'pack325-native-run-map.json');head=mapping['Head'];rid=mapping['Run']
sdkpath=a/f'pack325-native-{rid}-independent-envelope-and-sdk-guard.json';sdk=j(sdkpath);localpath=a/'pack325-original13-independent-local-report.json';local=j(localpath);capture=j(a/'pack325-original13-independent-local-capture.json')
assert sdk['Head']==capture['Head']==head and sdk['WorkflowConclusion']==sdk['JobConclusion']=='success' and sdk['SdkSixArgumentPassed'] and sdk['StrictEightLineSdkContractPassed'] and sdk['Reviewed1109SourceBindingsExact']
assert local['Status']=='ORIGINAL_ONLY_THIRTEEN_TYPED_SEMANTIC_PASS' and len(local['Cases'])==13 and all(x['Passed'] for x in local['Cases'].values()) and not local['SdkCompared'] and not local['SdkPass'] and not local['GenericNifSupport']
assert capture['ExitCode']==0 and not capture['Stderr'] and not capture['NativeExecuted'] and capture['ReportSha256']==h(localpath)
d=a/f'pack325-watch-{rid}-terminal';remote_path=next(d.rglob('nif-pending-original-typed-review.json'));remote=j(remote_path)
assert {k:v for k,v in local.items() if k!='RawEvidenceDirectory'}=={k:v for k,v in remote.items() if k!='RawEvidenceDirectory'}
winpath='docs/validation/part-325/final-native/guard.json';stdoutpath='docs/validation/part-325/final-native/legacy.stdout';readerpath='tools/Erlang.NifPendingVarargsObservations/reviewed_sdk_stdout.py';sources=m.blobs(head,[winpath,stdoutpath,readerpath]);raw=sources[winpath];win=json.loads(raw)
assert win['Status']=='PASS_ACTUAL_MAIN325_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE' and win['FullPassed']==6693 and win['SourceFilesRehashed']==2366 and win['OriginalExecuted'] is False and win['PhysicalMemoryCompared'] is False
import types
reader=types.ModuleType('passive_published315');exec(compile(sources[readerpath],readerpath,'exec'),reader.__dict__);parsed=reader.parse_sdk_stdout(sources[stdoutpath],b'',require_abi=False)
assert parsed['NativePayloadDomainRolesValidated'] and parsed['LoadedOwnerSnapshotMatchesRuntime']
assert parsed['GeneratedCSharpSha256']==sdk['GeneratedCSharpSha256']=='6E28E8B093F042C22ACAFD2C0213A55C7DCEFC2D162B486AC8443FC7C4E5F77A'
report={'Status':'PASS_EXACT_HEAD325_LINUX_SDK_EIGHT_LINES_THREE_DOMAIN_ROLES_AND_SEPARATE_ORIGINAL13','Head':head,'Run':rid,'Job':mapping['Job'],'Artifact':sdk['Artifact'],'Attempt':1,'SourcePlanInputs':1109,'SourcePlanSha256':'A8575369F4D9B3325D7711E614561C80793C4E11BE18B1F01268680028D1880E','StrictSdkEightLinesPassed':True,'NativePayloadDomainRolesPassed':3,'IndependentOriginal13TypedPassed':True,'OriginalCases':13,'OriginalAndSdkThirteenCasesCompared':False,'WindowsLinuxGeneratedCSharpSha256Exact':True,'GeneratedCSharpSha256':sdk['GeneratedCSharpSha256'],'WindowsAndLinuxDomainRolesBothPassed':True,'WindowsPublishedGuardPath':winpath,'WindowsPublishedGuardSha256':m.sha(raw),'LinuxSdkGuardSha256':h(sdkpath),'LocalOriginal13ReportSha256':h(localpath),'RemoteOriginal13ReportSha256':h(remote_path),'GeneralNifAbiComplete':False,'DefaultNativeLoadInstructionHandlerInstalled':False,'NativeExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
out=a/'pack325-native-combined-independent-guard.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
