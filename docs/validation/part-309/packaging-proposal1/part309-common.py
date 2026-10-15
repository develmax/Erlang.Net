"""Standalone tooling gates with explicit published308 full/native reuse."""
from pathlib import Path
import hashlib,json,re,subprocess
ROOT=Path.cwd().resolve();OUT=ROOT/'docs/validation/part-309';CLOSED45='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(Path(p).read_bytes());git=lambda *x:subprocess.check_output(['git',*x],cwd=ROOT)
def safe(name):
 p=(ROOT/name).resolve();assert p.is_relative_to(ROOT) and not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.relative_to(ROOT).parts)
 assert not any(x.lower().startswith('license') for x in p.relative_to(ROOT).parts);return p
def pinned(pin):
 p=safe(pin['Path']);data=p.read_bytes();assert len(data)==pin['Bytes'] and sha(data)==pin['Sha256'],str(p);return p
def render(text,c):
 text=text.replace('{{Remote308Summary}}',c['Remote308Summary']);assert '{{' not in text;return text
def noted_doc(data,note):
 match=re.search(br'\r?\n\r?\n',data);assert match;nl=b'\r\n' if b'\r\n' in data else b'\n';return data[:match.end()]+note.encode()+nl+nl+data[match.end():]
def table_gate(c,after=False):
 audit=read(pinned(c['TableAudit']));candidate=pinned(c['TableCandidate']).read_bytes();old=git('show',c['ParentHead']+':docs/ERLANG_COMPATIBILITY.md')
 assert sha(candidate)==audit['ResultSha256'] and audit['ParentHead']==c['ParentHead'] and not audit['RuntimeChanged'] and not audit['TestsExecuted']
 parentraw=candidate.decode()
 for name,note in audit['CorrectedRows'].items():
  corrected=f'| `{name}` | {note} |';assert parentraw.count(corrected)==1;parentraw=parentraw.replace(corrected,f'| `{name}` | Missing; real implementation/runtime prerequisites required |')
 assert sha(parentraw.encode())==audit['ParentSha256'] and parentraw.encode().replace(b'\r\n',b'\n')==old.replace(b'\r\n',b'\n')
 text=parentraw
 assert set(audit['CorrectedRows'])=={'dist_get_stat/1','load_nif/2','process_info/1'}
 for name,note in audit['CorrectedRows'].items():
  line=f'| `{name}` | Missing; real implementation/runtime prerequisites required |';assert text.count(line)==1;text=text.replace(line,f'| `{name}` | {note} |')
 assert text.encode()==candidate
 missing={name for name,status in re.findall(r'^\| `([^`]+)` \| ([^\n]+)',text,re.M) if status.startswith('Missing;')}
 assert missing==set(audit['RemainingUnregistered'])=={'alloc_info/1','alloc_sizes/1','memory/0','memory/1'}
 expected=noted_doc(candidate,render(pinned(c['MetadataNote']).read_text(encoding='utf-8-sig').strip(),c)) if after else candidate
 assert (ROOT/'docs/ERLANG_COMPATIBILITY.md').read_bytes()==expected
def gates(path):
 c=read(path);assert c['Part']==309 and c['ParentHead']=='e1d8a999faac113d98ebf2d4471f80ef30daf84a' and git('rev-parse','HEAD').decode().strip()==c['ParentHead']
 assert c['SourceFreeze']['Sha256']=='EDB97892181DA1CA458A9ADEFCD60163AF255DF416E0388FDB18E74217832659';f=read(pinned(c['SourceFreeze']))
 assert f['SourcePaths']==len(f['Files'])==8 and f['CSharpPaths']==sum(x['Path'].endswith('.cs') for x in f['Files'])==3 and f['TokensExactForCSharp']
 assert f['ReusedFullHead']==c['ParentHead'] and f['ReusedFullPassed']==6637 and not f['NewFullValidationExecuted'] and not f['RuntimeCompilerAndTestSourcesChanged'] and not f['NewNativeExecuted'] and not f['NewOriginalCompilerExecuted'] and not f['NewOpcodeShapesVerified'] and not f['NewPublicMfa']
 for row in f['Files']:pinned(row)
 assert not git('diff','HEAD','--name-only','--','src','tests','tools/Erlang.Differential','tools/Erlang.NifNativeBackend').strip()
 guard=read(pinned(c['ReuseGuard']));assert guard['Status']=='REUSE_PUBLISHED308_RUNTIME_FULL6637_INTEGRATION11_NATIVE1020_AND_NEW309_TOOL_TARGET_PASS'
 assert guard['SourceFreezeSha256']==c['SourceFreeze']['Sha256'] and guard['SourceHashesExact'] and guard['ReusedFullPassed']==6637 and guard['ReusedIntegrationResults']==11 and guard['NativeSourceInputsVerifiedUnchanged']==1020 and guard['StandaloneMainToolBuildPassed'] and guard['AllFrozenToolingMembersVerified']==103
 assert not guard['NewFullValidationExecuted'] and not guard['NewNativeExecuted'] and not guard['NewOriginalExecuted'] and not guard['OriginalOpcodeShapesVerified']
 target=read(pinned(c['ToolTargetExit']));assert target['ExitCode']==0 and target['StandaloneToolTarget'] and not target['NewFullValidationExecuted']
 log=pinned(c['ToolTargetLog']).read_text(encoding='utf-8-sig');assert re.search(r'(?:0 Warning\(s\)|Предупреждений:\s*0)',log) and re.search(r'(?:0 Error\(s\)|Ошибок:\s*0)',log)
 receipt=read(pinned(c['ToolReceipt']));assert receipt['Status']=='VALIDATED_TOOLING_ONLY_READY_FOR_PUBLICATION_THEN_ORIGINAL_COLLECTION' and receipt['BuildExitCode']==receipt['BuildWarnings']==receipt['BuildErrors']==0 and receipt['ManagedCliRefusalsPassed']==6 and receipt['VerifierRefusalsPassed']==2 and receipt['NewExporterAppendRefusalPassed'] and receipt['CSharpTokenExact']
 assert not receipt['OriginalCompilationPerformed'] and not receipt['OriginalBodyExecutionPerformed'] and not receipt['NativeLoadingExecutionPerformed'] and not receipt['OriginalShapesObserved']
 plan=read(pinned(c['NativeSourcePlan']));assert len(plan['Files'])==1020 and c['NativeSourcePlan']['Sha256']==f['ReviewedSourcePlanSha256']==guard['ReviewedSourcePlanSha256']=='29FC1D27A9A11D03C9928C50DDB6A900D5F235ADE731FE3E3934F66181E7CFA0'
 for row in plan['Files']:
  data=safe(row['Path']).read_bytes()
  if row['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
  else:assert row['HashMode']=='BinaryExact'
  assert len(data)==row['Bytes'] and sha(data)==row['Sha256']
 for label,published in c['ReusedPublishedPaths'].items():assert pinned(c[label]).read_bytes()==git('show',c['ParentHead']+':'+published)
 tests=read(pinned(c['Tests']));assert (tests['Passed'],tests['Failed'])==(6637,0) and len(tests['Tests'])==6637 and all(x['Status']=='Passed' for x in tests['Tests'])
 assert len(read(pinned(c['Integration'])))==11 and read(pinned(c['ReusedNativeGuard']))['NativeTrustedWindowsProfilePassed'] and read(pinned(c['ReusedNativeGuard']))['ActualGeneratedCSharpRoutePassed']
 registry=read(pinned(c['Registry']));assert (ROOT/'docs/supported-mfas.json').read_bytes()==pinned(c['Registry']).read_bytes() and len(registry)==505 and sum(x['Module']=='erlang' for x in registry)==347
 index=read(pinned(c['EvidenceIndex']));destinations=set()
 for row in index:
  pinned({'Path':row['SourcePath'],'Bytes':row['Bytes'],'Sha256':row['Sha256']});dest=safe(row['Destination']);assert dest.is_relative_to(OUT) and dest not in destinations;destinations.add(dest)
 tf=read(pinned(c['ToolFreeze']));assert c['ToolFreeze']['Sha256']=='DE5FBAE0024DB7ADB89489DF595C31E10DF8F9EB84A20D349112B48035839F65' and len(tf['Files'])==103
 for row in tf['Files']:assert any(x['SourcePath']=='artifacts/beam-load-nif-opcodes313-validated1/'+row['Path'] and x['Bytes']==row['Bytes'] and x['Sha256']==row['Sha256'] for x in index)
 states=read(pinned(c['RemoteStates']));assert states['Head']==c['ParentHead'] and set(states['Runs'])=={'ci','native','original'}
 for state in states['Runs'].values():assert state['Attempt']==1 and state['Head']==c['ParentHead']
 table_gate(c)
 assert sha((ROOT/'docs/changelog/0045.md').read_bytes())==CLOSED45 and c['ComparisonTestReport']=='validation/part-308/tests.json'
 volume=pinned(c['ActiveChangelogBefore']).read_bytes();assert len(volume)<=32768 and len(volume.splitlines())<=400 and b'docs/validation/part-309/** binary' in (ROOT/'.gitattributes').read_bytes()
 return c,f,index
