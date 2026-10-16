"""Passive raw318 memory observation; validate each snapshot independently, never compare call values as an expectation."""
from pathlib import Path
import base64,hashlib,importlib.util,json,re,sys
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('m',Path(__file__).with_name('pack320-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mp,d,run,job,items,rows=m.load('ordinary');assert run['conclusion']==job['conclusion']=='success';ex=d/f'artifact-{items[0]["id"]}';v=m.read(ex/'allocator-profile.json');p=v['MemoryProfile']
assert v['Complete'] and not v['TimedOut'] and v['InfrastructureOrContractError'] is None
keys=['total','processes','processes_used','system','atom','atom_used','binary','code','ets'];assert p['KeyOrder']==keys and p['DeclaredClrAdaptation'] is True and p['RawMatchClaimed'] is False
for k in ['OriginalNineKeyNumericPartitionValidated','SdkNineKeyNumericPartitionValidated']:assert p[k] is True
assert p['OriginalObservedMemoryKeyCount']==p['SdkObservedMemoryKeyCount']==9
sourcepath='tools/Erlang.Differential/ClrMemoryCaseNames.cs';blob=m.blobs(mp['Head'],[sourcepath])[sourcepath].decode()
for field in ['Source','SdkScope','OriginalScope']:
 name='RawSnapshotSource' if field=='Source' else field;value=re.search(r'const string '+name+r' = "([^"]*)";',blob).group(1);assert p[field]==value
assert p['SourceSha256']==m.sha(p['Source'].encode())
proc=p['OriginalProcess'];assert proc['ExitCode']==0 and proc['StandardError']=='' and p['OriginalStandardErrorHex']==p['SdkStandardErrorHex']==p['SdkStandardOutputHex']==''
raw=proc['StandardOutput'].encode();assert raw.hex().upper()==p['OriginalStandardOutputHex'] and base64.b64decode(proc['StandardOutputBytes'])==raw
payload=re.findall(rb'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',raw);assert len(payload)==1
path='tools/Erlang.RawOnLoadVisibilityObservations/guard/passive_etf.py';decoder=m.blobs(mp['Head'],[path])[path];scope={'__name__':'passive_published318'};exec(compile(decoder,path,'exec'),scope)
etf=base64.b64decode(payload[0],validate=True);term,consumed=scope['decode_one'](etf);assert consumed==len(etf)
assert term[0]=='tuple' and term[1][0]==('atom','ok') and len(term[1])==2
snapshots=term[1][1];assert snapshots[0]=='tuple' and len(snapshots[1])==2
original=[]
for snapshot in snapshots[1]:
 assert snapshot[0]=='list' and len(snapshot[1])==9
 values=[]
 for key,item in zip(keys,snapshot[1]):assert item[0]=='tuple' and len(item[1])==2 and item[1][0]==('atom',key) and item[1][1][0]=='integer';values.append(item[1][1][1])
 original.append(values)
# Strict anchored numeric text profile for SDK pretty-printer result; not an Erlang evaluator.
listpattern=r'\['+','.join(r'\{'+key+r',([0-9]+)\}' for key in keys)+r'\]'
match=re.fullmatch(r'\{ok,\{'+listpattern+','+listpattern+r'\}\}',p['SdkOutcome']);assert match
values=list(map(int,match.groups()));sdk=[values[:9],values[9:]]
def validate(numbers):
 assert len(numbers)==9 and all(type(n) is int and n>=0 for n in numbers)
 total,processes,used,system,atom,atom_used,binary,code,ets=numbers;assert total==processes+system and used<=processes and atom_used<=atom and system>=atom+binary+code+ets
for values in original+sdk:validate(values)
def rendered(samples):return '{ok,{'+','.join('['+','.join('{'+key+','+str(n)+'}' for key,n in zip(keys,values))+']' for values in samples)+'}}'
assert rendered(original)==p['OriginalOutcome'] and p['RawResultsEqual']==(original==sdk)
report={'Status':'PASS_EXACT_HEAD320_ACTUAL_RAW_MEMORY_PROFILE_BOTH_BACKENDS_TWO_INDEPENDENT_NINE_KEY_SNAPSHOTS','Head':mp['Head'],'Run':mp['Run'],'Attempt':1,'RawReportSha256':m.sha((ex/'allocator-profile.json').read_bytes()),'MemorySourceSha256':p['SourceSha256'],'OriginalScope':p['OriginalScope'],'SdkScope':p['SdkScope'],'OriginalSnapshotValues':original,'SdkSnapshotValues':sdk,'EachSnapshotNumericPartitionValidated':True,'OriginalStdoutSha256':m.sha(raw),'RawResultsEqualObserved':p['RawResultsEqual'],'ComparisonEqualityRequired':False,'TwoSeparateCallsComparedAsEqual':False,'RawMatchClaimed':False,'DeclaredClrAdaptation':True,'PhysicalOrWholeGcMemoryParityClaimed':False,'NativeVmSdkExecutedByReviewer':False}
out=m.A/'pack320-memory-profile-independent-guard.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
