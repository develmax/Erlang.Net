"""DRAFT. Root executes only after actual metadata/proof allowlist review."""
import argparse,sys,subprocess
sys.dont_write_bytecode=True
from delivery340_common import *
p=argparse.ArgumentParser();p.add_argument('--main',type=Path,required=True);p.add_argument('--plan',type=Path,required=True);p.add_argument('--plan-sha256',required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();root=a.main.resolve(strict=True)
assert sha(a.plan)==a.plan_sha256.upper() and not a.output.exists();plan=read(a.plan);paths=plan['Paths'];metadata=set(plan['Metadata'])
assert plan['ParentHead']==PARENT and len(paths)==len(set(paths)) and metadata<=set(paths)
assert not subprocess.check_output(['git','-C',str(root),'diff','--cached','--name-only']).strip()
assert all('LICENSE.local' not in p and not {'bin','obj','.cache'}.intersection(Path(p).parts) and Path(p).suffix not in {'.zip','.dll','.exe','.nupkg'} for p in paths)
assert not any(p.startswith('docs/changelog/') and p!='docs/changelog/0053.md' for p in paths)
for row in plan['Files']:
 assert row['Path'] in paths and file_at(root,row['Path']).stat().st_size==row['Bytes'] and sha(root/row['Path'])==row['Sha256']
assert len(plan['Files'])==len(paths) and {row['Path'] for row in plan['Files']}==set(paths)
# No string-built shell command; exact paths are a NUL-delimited stdin pathspec.
subprocess.run(['git','-C',str(root),'--literal-pathspecs','add','--pathspec-from-file=-','--pathspec-file-nul'],input='\0'.join(paths).encode()+b'\0',check=True,capture_output=True)
actual=subprocess.check_output(['git','-C',str(root),'diff','--cached','--name-only','-z']).decode().rstrip('\0').split('\0');assert sorted(actual)==sorted(paths)
raw=subprocess.check_output(['git','-C',str(root),'cat-file','--batch'],input=''.join(':'+p+'\n' for p in paths).encode());offset=0;verified=[]
for path in paths:
 end=raw.index(b'\n',offset);header=raw[offset:end].split();assert len(header)==3 and header[1]==b'blob';size=int(header[2]);start=end+1;value=raw[start:start+size];assert raw[start+size:start+size+1]==b'\n';offset=start+size+1
 expected=file_at(root,path).read_bytes();mode='BinaryExact'
 if path in metadata:expected=expected.replace(b'\r\n',b'\n');mode='MetadataTextLF'
 assert value==expected,path
 verified.append(dict(Path=path,Bytes=size,Sha256=hashlib.sha256(value).hexdigest().upper(),ComparisonMode=mode))
assert offset==len(raw);subprocess.run(['git','-C',str(root),'diff','--cached','--check'],check=True,capture_output=True)
save(a.output,dict(Status='EXACT340_CACHED_STAGE_PASS',ParentHead=PARENT,Paths=len(paths),Records=verified,PlanSha256=a.plan_sha256.upper()))
