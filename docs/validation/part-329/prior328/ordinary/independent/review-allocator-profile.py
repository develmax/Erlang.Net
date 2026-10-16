"""Passive exact-head316 separate backend observation; no native evaluation."""
from pathlib import Path
import base64,hashlib,importlib.util,json,re,sys
sys.dont_write_bytecode=True
sp=importlib.util.spec_from_file_location('common',Path(__file__).with_name('passive-common.py'));m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m)
map,d,run,job,items,rows=m.load('ordinary');assert run['conclusion']==job['conclusion']=='success'
ex=d/f'artifact-{items[0]["id"]}';p=ex/'allocator-profile.json';v=m.read(p)
manifest=m.read(m.A/'allocator-public-sys-profile316-oracle1/case-manifest.json')['SeparateBackendAdaptationObservation']
assert v['Mode']=='--allocator-profile' and v['BaselineVersion']=='29.1.1' and v['PinnedCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
for k in ['VersionVerified','Complete','ContractValidationPassed','DeclaredClrAdaptation','NativeContractValidated','SdkContractValidated']:assert v[k] is True,k
assert v['RawMatchClaimed'] is False and v['RawResultsEqual'] is False and v['TimedOut'] is False and v['InfrastructureOrContractError'] is None
assert v['OriginalExpectedBackend']=='libc' and v['SdkExpectedBackend']=='dotnet_native_memory'
assert v['Name']==manifest['Name'] and v['Source']==manifest['Source'] and v['SourceSha256']==manifest['SourceUtf8Sha256']==m.sha(v['Source'].encode())
assert v['SdkOutcome']==v['SdkExpectedOutcome']==manifest['ExpectedSdk']
assert v['SdkStandardOutputHex']==v['SdkStandardErrorHex']==v['OriginalStandardErrorHex']==''
for key in ['VersionProcess','OriginalProcess']:
 assert v[key]['ExitCode']==0 and v[key]['StandardError']==''
assert v['VersionProcess']['StandardOutput'].strip()=='29.1.1'
native=v['OriginalProcess']['StandardOutput'].encode();assert native.hex().upper()==v['OriginalStandardOutputHex']
protocol=re.findall(rb'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',native);assert len(protocol)==1
path='tools/Erlang.RawOnLoadVisibilityObservations/guard/passive_etf.py';blob=m.blobs(map['Head'],[path])[path]
namespace={'__name__':'passive_exact_published316'};exec(compile(blob,path,'exec'),namespace)
etf=base64.b64decode(protocol[0],validate=True);term,consumed=namespace['decode_one'](etf);assert consumed==len(etf)
a=lambda s:('atom',s);t=lambda *x:('tuple',x)
assert term[0]=='tuple' and len(term[1])==2 and term[1][0]==a('ok');value=term[1][1];assert value[0]=='tuple' and len(value[1])==7
values=value[1];assert values[0]==a('sys_alloc_full_options') and values[1]==values[4]==t(a('e'),a('true')) and values[2]==values[5]==t(a('m'),a('libc'))
tails=[]
for tail in [values[3],values[6]]:
 assert tail[0]=='list' and len(tail[1])<=2
 keys=[];observed=[]
 for pair in tail[1]:
  assert pair[0]=='tuple' and len(pair[1])==2
  key,number=pair[1];assert key in [a('tt'),a('tp')] and number[0]=='integer' and type(number[1]) is int and number[1]>=0
  keys.append(key[1]);observed.append([key[1],number[1]])
 assert len(keys)==len(set(keys)) and (len(keys)!=2 or keys==['tt','tp']);tails.append(observed)
log=(d/f'job-{job["id"]}.log').read_text(encoding='utf-8-sig');assert 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and '--allocator-profile' in log
assert (ex/'allocator-profile.log').is_file()
report={'Status':'PASS_EXACT_HEAD328_SEPARATE_ACTUAL_ORIGINAL_LIBC_VS_SDK_DOTNET_BACKEND_PROFILE','Head':map['Head'],'Run':map['Run'],'Attempt':1,'RawReportSha256':m.sha(p.read_bytes()),'OriginalStdoutSha256':m.sha(native),'OriginalOutcome':v['OriginalOutcome'],'SdkOutcome':v['SdkOutcome'],'OriginalOptionalTailsRetained':tails,'OriginalBackend':'libc','SdkBackend':'dotnet_native_memory','RawResultsEqual':False,'DeclaredClrAdaptation':True,'RawNormalization':False,'ComparisonEqualityClaimed':False,'NativeVmSdkExecutedByVerifier':False,'CommitVerifiedByIndependentWorkflowLogNotVersionString':True,'DecoderPublishedBlobSha256':m.sha(blob)}
out=Path(__file__).parent/'allocator-profile-independent-guard.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
