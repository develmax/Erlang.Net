"""Read-only exact-stage replay. Root explicitly accepts one frozen EOF-style diagnostic."""
from pathlib import Path
import hashlib,json,subprocess,time
r=Path.cwd();a=r/'artifacts';b=a/'part310-packaging-proposal1';read=lambda p:json.loads(p.read_bytes());sha=lambda data:hashlib.sha256(data).hexdigest().upper()
def git(*args,**kw):
 p=subprocess.run(['git',*args],cwd=r,capture_output=True,**kw);assert p.returncode==0,(args,p.stderr.decode(errors='replace'));return p.stdout
start=time.monotonic();c=read(b/'config-actual.json');assert git('rev-parse','HEAD').decode().strip()==c['ParentHead']=='e829c53ef4323ea8cdd0948cd77c243efa787abd'
fp=r/c['SourceFreeze']['Path'];assert sha(fp.read_bytes())==c['SourceFreeze']['Sha256']=='E16C4F41565A6D249999F1B386C0ABF10915BEF34BD3D453E423672705828F8F';f=read(fp)
out=r/'docs/validation/part-310';v=read(out/'evidence-receipt.json');assert (v['Passed'],v['Failed'],v['SourcePaths'],v['CSharpPaths'],v['Registry'],v['NativeInputs'])==(6638,0,26,20,505,1028)
assert v['SourceFreezeSha256']==c['SourceFreeze']['Sha256'] and v['FreshMainFullValidationExecuted'] and not v['ReusedFullValidation'] and v['IntegrationAndPackagesPassed'] and v['NativeTrustedWindowsProfilePassed'] and v['ActualGeneratedCSharpRoutePassed']
proofs={p.relative_to(r).as_posix() for p in out.rglob('*') if p.is_file()};assert proofs=={'docs/validation/part-310/'+x['Path'] for x in v['Files']}|{'docs/validation/part-310/evidence-receipt.json'}
for row in v['Files']:
 data=(out/row['Path']).read_bytes();assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
sources={x['Path'] for x in f['Files']};metadata=set(c['StageMetadataPaths']);changed=set(git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''};assert changed<=sources|metadata|proofs,sorted(changed-sources-metadata-proofs)
expected=sorted(sources|(metadata&changed)|proofs);pathspec=(a/'root-part310-stage-pathspec').read_bytes();assert pathspec==b'\0'.join(x.encode() for x in expected)+b'\0'
assert set(git('diff','--cached','--name-only','-z').decode().split('\0'))-{''}==set(expected)
assert len(expected)==len(set(expected));assert all('\n' not in x and '\r' not in x and '\0' not in x for x in expected)
for name in expected:
 p=(r/name).resolve();assert p.is_relative_to(r.resolve()) and not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.relative_to(r).parts) and not any(x.lower().startswith('license') for x in p.relative_to(r).parts)
attrs_raw=git('check-attr','--cached','-z','--stdin','text','binary',input=pathspec).decode().split('\0');assert attrs_raw.pop()=='' and len(attrs_raw)==len(expected)*6
attrs={name:{} for name in expected}
for offset in range(0,len(attrs_raw),3):
 name,attribute,value=attrs_raw[offset:offset+3];assert name in attrs and attribute in ['text','binary'] and attribute not in attrs[name];attrs[name][attribute]=value
assert all(set(x)=={'text','binary'} for x in attrs.values())
raw=git('cat-file','--batch',input=''.join(':'+x+'\n' for x in expected).encode());offset=0;records=[];source_rows={x['Path']:x for x in f['Files']}
for name in expected:
 end=raw.index(b'\n',offset);header=raw[offset:end].split();assert len(header)==3 and header[1]==b'blob',(name,header);size=int(header[2]);begin=end+1;staged=raw[begin:begin+size];assert len(staged)==size and raw[begin+size:begin+size+1]==b'\n';offset=begin+size+1;data=(r/name).read_bytes()
 exact=name in proofs or name.endswith(('.cs','.beam','.etf')) or attrs[name]['text']=='unset' or attrs[name]['binary']=='set'
 if name in sources and not exact:assert name in c['AllowedTextLfSources']
 mode='BinaryExact' if exact else 'TextLF';assert (staged==data if exact else staged.replace(b'\r\n',b'\n')==data.replace(b'\r\n',b'\n')),name
 if name in sources:assert (len(data),sha(data))==(source_rows[name]['Bytes'],source_rows[name]['Sha256'])
 records.append({'Path':name,'GitAttributes':attrs[name],'ComparisonMode':mode,'WorkingBytes':len(data),'WorkingSha256':sha(data),'StagedBytes':len(staged),'StagedSha256':sha(staged),'EqualAfterDeclaredMode':True})
assert offset==len(raw)
assert (r/'docs/supported-mfas.json').read_bytes()==(out/'supported-mfas.json').read_bytes();assert (r/'docs/module-readiness.json').read_bytes()==(out/'module-readiness.json').read_bytes()
assert len(sources)==26 and sum(x.endswith('.cs') for x in sources)==20 and all(x['ComparisonMode']=='BinaryExact' for x in records if x['Path'] in proofs or x['Path'].endswith(('.cs','.beam','.etf')))
closed=sha((r/'docs/changelog/0045.md').read_bytes());assert closed=='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6';active=(r/'docs/changelog/0046.md').read_bytes();assert len(active)<=32768 and len(active.splitlines())<=400
default=subprocess.run(['git','diff','--cached','--check'],cwd=r,capture_output=True);diagnostic='tests/Erlang.Tests/SourceLoadNifHostChecks.cs:406: new blank line at EOF.'
default_record={'Command':['git','diff','--cached','--check'],'ExitCode':default.returncode,'Stdout':default.stdout.decode('utf-8-sig'),'Stderr':default.stderr.decode('utf-8-sig')}
(a/'root-part310-default-whitespace-check-review2.json').write_text(json.dumps(default_record,indent=2)+'\n');assert default.returncode!=0 and default.stdout.decode('utf-8-sig').strip()==diagnostic and default.stderr==b'',default_record
adjusted=subprocess.run(['git','-c','core.whitespace=-blank-at-eof','diff','--cached','--check'],cwd=r,capture_output=True);adjusted_record={'Command':['git','-c','core.whitespace=-blank-at-eof','diff','--cached','--check'],'ExitCode':adjusted.returncode,'Stdout':adjusted.stdout.decode('utf-8-sig'),'Stderr':adjusted.stderr.decode('utf-8-sig')}
(a/'root-part310-adjusted-whitespace-check-review2.json').write_text(json.dumps(adjusted_record,indent=2)+'\n');assert adjusted.returncode==0 and adjusted.stdout==adjusted.stderr==b'',adjusted_record
guard={'Status':'PASS_EXACT_STAGE_PATHS_EXPLICIT_GIT_ATTRIBUTE_HASH_MODES','ReviewRevision':2,'ReviewMethod':'Read-only bulk cat-file/check-attr; no source or index mutation','SourceFreezeSha256':c['SourceFreeze']['Sha256'],'SourcePaths':26,'CSharpPaths':20,'StagePaths':len(expected),'ProofFiles':len(proofs),'ProofsBinaryExact':True,'WorkingSourceFreezePreserved':True,'Closed0045Sha256':closed,'DefaultGitWhitespaceCheckPassed':False,'KnownFrozenStyleWarning':diagnostic,'KnownWarningIsSemanticFailure':False,'BlankAtEofDisabledExplicitly':True,'OtherGitWhitespaceChecksPassed':True,'DefaultWhitespaceCheckEvidence':'artifacts/root-part310-default-whitespace-check-review2.json','DefaultWhitespaceCheckEvidenceSha256':sha((a/'root-part310-default-whitespace-check-review2.json').read_bytes()),'AdjustedWhitespaceCheckEvidence':'artifacts/root-part310-adjusted-whitespace-check-review2.json','AdjustedWhitespaceCheckEvidenceSha256':sha((a/'root-part310-adjusted-whitespace-check-review2.json').read_bytes()),'OriginalFrozenStageGuardScriptPreserved':True,'ControlScriptSha256':sha(Path(__file__).read_bytes()),'ConfigSha256':sha((b/'config-actual.json').read_bytes()),'ElapsedSeconds':time.monotonic()-start,'MainEdited':False,'IndexEdited':False,'PostExecuted':False,'Records':records}
target=a/'root-part310-stage-index-guard.json';assert not target.exists();target.write_text(json.dumps(guard,indent=2)+'\n');print(json.dumps({key:guard[key] for key in ['Status','ReviewRevision','StagePaths','ProofFiles','ElapsedSeconds','DefaultGitWhitespaceCheckPassed','KnownFrozenStyleWarning','OtherGitWhitespaceChecksPassed','MainEdited','IndexEdited']}))
