from pathlib import Path
import hashlib,json,ast
R=Path.cwd();A=R/'artifacts';F=A/'part350-ordinary-independent'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
p=A/'part348-ordinary-independent/review348.py';s=p.read_text()
s=s.replace('original348 five-fixture','original350 eighteen-fixture').replace('common348','common350')
s=s.replace("H='d1bd571fc1f655abf81bdc0336b7133f3d023fac'","H='c1f219f31726fc93e537080f400651d300c47d72'")
s=s.replace('38979399501','38983540150').replace('117236445591','117249656542').replace('12081516110','12083085671')
s=s.replace("part345-remote-new-review/ordinary/artifact-12079873039","part348-remote-new-review/ordinary/artifact-12081516110")
s=s.replace("part345-ordinary-independent/semantic-guard.json","part348-ordinary-independent/semantic-guard.json")
s=s.replace('334250AA3AC806005A4E867F5BC761FC80753424B9BF498CFCFDCF24ECE0635F','EF903275F8470BD89A17DEC16BBA90CAF9685EA32A690051651805983731BC6F')
s=s.replace('B9A111684C008BBA718F3521213C29F00389FF431ED522A1436B61A4DEA89E9B','59C15159582F24D25985F976CDB28A5108AB57D211C37332ECCAD9CC1554BE27')
s=s.replace('[(v,6295),(old,6290)]','[(v,6313),(old,6295)]').replace("('ModuleResults',5343,5)","('ModuleResults',5348,18)")
start=s.index("paths=[prefix+x")
end=s.index("for t in v['ModuleResults']+v['DiagnosticResults']")
s=s[:start]+'''families=[('AdditionalTypePredicate',6),('NumericBoundary',12)]
paths=[prefix+x+suffix+'.cs' for x,_ in families for suffix in ['CaseNames','CaseSources','Cases']]+[prefix+x for x in ['CompiledModuleCases.cs','OracleOutcomeTags.cs','TermDisplayCases.cs','CompiledStandardOutputContract.cs']]+['reference-baseline.json','.github/workflows/differential.yml']
bs=m.blobs(H,paths)
assert json.loads(bs['reference-baseline.json'])['commit']==OTP and json.loads(bs['reference-baseline.json'])['tag']=='OTP-29.1.1'
workflow=bs['.github/workflows/differential.yml'];assert ('test "$(git -C artifacts/otp-source rev-parse HEAD)" = '+OTP).encode() in workflow and b'./configure --prefix="$GITHUB_WORKSPACE/artifacts/otp-oracle"' in workflow and b'artifacts/otp-oracle/bin/erl' in workflow
log=(d/'job-117249656542.log').read_text();assert OTP in log and "Note: switching to '"+OTP+"'." in log and 'checking OTP version... 29.1.1' in log
# Closed C# constant syntax only: no code execution or SDK-derived expected values.
def constants(text):
 values={name:json.loads(value) for name,value in re.findall(r'public const string (\\w+)\\s*=\\s*("(?:[^"\\\\]|\\\\.)*")\\s*;',text)}
 for match in re.finditer(r'public const string (\\w+)\\s*=\\s*"""\\r?\\n(.*?)\\r?\\n([ \\t]*)"""\\s*;',text,re.S):
  name,body,indent=match.groups();assert not indent and name not in values;values[name]=body
 return values
contracts=[]
for family,count in families:
 names=constants(bs[prefix+family+'CaseNames.cs'].decode());sources=constants(bs[prefix+family+'CaseSources.cs'].decode());cat=bs[prefix+family+'Cases.cs'].decode()
 pairs=re.findall(r'new\\(\\s*'+family+r'CaseNames\\.(\\w+),\\s*'+family+r'CaseSources\\.(\\w+),',cat)
 assert len(pairs)==count and names['Done']=='done'
 assert len(re.findall(r'Term.Tuple\\(Term.A\\(OracleOutcomeTags.Success\\), Term.A\\('+family+r'CaseNames.Done\\)\\)',cat))==count
 assert ('.. '+family+'Cases.Modules').encode() in bs[prefix+'CompiledModuleCases.cs']
 contracts.extend(dict(Name=names[n],Source=sources[k],Family=family) for n,k in pairs)
assert constants(bs[prefix+'OracleOutcomeTags.cs'].decode())['Success']=='ok'
assert len(contracts)==18 and len({x['Name'] for x in contracts})==18
for t,c in zip(v['ModuleResults'][5348:],contracts):
 assert t['Name']==c['Name'] and t['Source']==c['Source'] and t['SourceSha256']==m.sha(c['Source'].encode())
 assert t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}' and t['CallerContext']=='UntrappedFresh' and t['CompilerPolicy']=='Default' and t['Passed'] and t['StandardStreamsMatch']
 assert all(t.get(k) in [None,''] for k in ['NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
# All previous C# oracle inputs remain raw-identical; catalog has precisely two additive spreads.
import subprocess
prior_head='d1bd571fc1f655abf81bdc0336b7133f3d023fac'
proc=subprocess.run(['git','ls-tree','-r','--name-only',prior_head,'--','tools/Erlang.Differential'],capture_output=True);assert proc.returncode==0
old_paths=[p for p in proc.stdout.decode().splitlines() if p.endswith('.cs')]
old_blobs=m.blobs(prior_head,old_paths);current_blobs=m.blobs(H,old_paths)
for p in old_paths:
 before,after=old_blobs[p],current_blobs[p]
 if p==prefix+'CompiledModuleCases.cs':
  expected=before.replace(b'.. TermOrderCases.Modules',b'.. TermOrderCases.Modules,\\n        .. AdditionalTypePredicateCases.Modules,\\n        .. NumericBoundaryCases.Modules')
  assert after==expected,p
 else:assert after==before,p
''' +s[end:]
s=s.replace("==5348 and v['CompiledModuleTrack']","==5366 and v['CompiledModuleTrack']")
s=s.replace("Passed=6295","Passed=6313").replace("NewModules=5","NewModules=18").replace('AllPrevious6290','AllPrevious6295').replace('ExactJobPassOrder6295','ExactJobPassOrder6313').replace('Prior6290ReportSha256','Prior6295ReportSha256')
s=s.replace('PUBLISHED348_ORIGINAL6295','PUBLISHED350_ORIGINAL6313').replace('PUBLISHED348_STRICT','PUBLISHED350_STRICT').replace("v['ModuleResults'][5343:]","v['ModuleResults'][5348:]")
s=s.replace('WholeTermOrderOrPerformanceVerificationClaimed=False','WholePredicateOrNumericCompatibilityClaimed=False')
s=s.replace("Disclosure='Reviewer authored managed iterative TermOrder implementation. Actual native observations were collected externally by published348 workflow; Root/other-agent independent source/focus/full reviews remain separate evidence.'","Disclosure='Passive reviewer did not execute native or managed guest code. Native observations came from the published350 workflow. Selected cases do not establish complete predicate/numeric compatibility.'")
marker="assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']"
extra='''
# FD subprocess results preserve raw marker bytes and exact output streams; no text normalization.
import base64
for row in fd['Results']:
 assert row['Passed'] and row['ExitCode']==0 and row['StandardError']=='' and row['Native']==row['Sdk']=='{ok,done}' and row['ActualOutputHex']==row['OutputHex']
 matches=re.findall(r'ERLANG_NET_ETF_BEGIN:([A-Za-z0-9+/=]+):ERLANG_NET_ETF_END',row['StandardOutput'])
 assert len(matches)==1 and base64.b64decode(matches[0],validate=True)==bytes.fromhex('83680277026F6B7704646F6E65')
assert {k:x for k,x in fd.items() if k!='Results'}=={k:x for k,x in oldfd.items() if k!='Results'}
'''
s=s.replace(marker,marker+'\n'+extra)
s=s.replace('ExistingTrustedSourcePins=m.PINS','ExistingTrustedSourcePins=m.PINS,PriorOracleSourceFilesRawPreserved=len(old_paths)')
ast.parse(s);(F/'review350.py').write_text(s,newline='\n')
helper=(A/'part350-remote-new-review/passive-common.py').read_bytes();digest=sha(helper)
common='''"""Use exact reviewed350 transport and enforce archive set/API subject identity."""
from pathlib import Path
import hashlib,json,zipfile,sys,importlib.util
sys.dont_write_bytecode=True
ROOT=Path.cwd();BASE=ROOT/'artifacts/part350-remote-new-review'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
PINS={'passive-common.py':DIGEST}
assert sha((BASE/'passive-common.py').read_bytes())==PINS['passive-common.py']
s=importlib.util.spec_from_file_location('transport350',BASE/'passive-common.py');n=importlib.util.module_from_spec(s);s.loader.exec_module(n)
read=n.read;blobs=n.blobs;A=n.A
def load(kind):
 mapping,raw,run,job,items,rows=n.load(kind)
 assert len(items)==1 and items[0]['workflow_run']['id']==run['id']==mapping['Run']
 assert items[0]['name']=={'ci':'validation','ordinary':'differential'}[kind]
 assert run['head_branch']=='main' and items[0]['digest'].startswith('sha256:')
 ex=raw/f'artifact-{items[0]["id"]}'
 with zipfile.ZipFile(raw/f'artifact-{items[0]["id"]}.zip') as z:
  assert {p.relative_to(ex).as_posix() for p in ex.rglob('*') if p.is_file()}=={p for p in z.namelist() if not p.endswith('/')}
 return mapping,raw,run,job,items,rows
'''.replace('DIGEST',repr(digest))
ast.parse(common);(F/'common350.py').write_text(common,newline='\n')
freeze=dict(Status='SOURCE_ONLY_PASSIVE350_ORDINARY_READER_PENDING_ROOT_REVIEW',ParentReaderSha256=sha(p.read_bytes()),Files=[dict(Path=str(q.relative_to(R)).replace('\\','/'),Bytes=q.stat().st_size,Sha256=sha(q.read_bytes())) for q in [F/'review350.py',F/'common350.py']],TransportSha256=digest,Executed=False,NativeCodeExecuted=False,SdkExecuted=False,MainModified=False)
(F/'source-freeze.json').write_text(json.dumps(freeze,indent=2)+'\n',newline='\n')
print(json.dumps(dict(FreezeSha256=sha((F/'source-freeze.json').read_bytes()),Files=freeze['Files'])))
