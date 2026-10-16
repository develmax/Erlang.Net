"""Root-only exact pathspec staging after reviewed promotion/docs/readiness; never commits/pushes."""
import argparse,sys
sys.dont_write_bytecode=True
from common348 import *
p=argparse.ArgumentParser()
for n in ('candidate','main','full-dir','independent-guard','docs-plan','readiness-plan','output'):p.add_argument('--'+n,type=Path,required=True)
for n in ('independent-guard-sha256','docs-plan-sha256','readiness-plan-sha256'):p.add_argument('--'+n,required=True)
a=p.parse_args();main=a.main.resolve(strict=True);full=a.full_dir.resolve(strict=True)
assert not a.output.exists() and sha(a.docs_plan)==a.docs_plan_sha256.upper() and sha(a.readiness_plan)==a.readiness_plan_sha256.upper()
authority(a.candidate.resolve(strict=True),full,a.independent_guard,a.independent_guard_sha256)
assert subprocess.check_output(['git','-C',str(main),'rev-parse','HEAD']).decode().strip()==PARENT and not subprocess.check_output(['git','-C',str(main),'diff','--cached','--name-only']).strip()
assert sha(full/'source-freeze.json')==SOURCE;owned=exact(main,full/'source-freeze.json')
expected=set(owned)
for plan in [read(a.docs_plan),read(a.readiness_plan)]:
 for row in plan['Files']:
  name=row['TargetPath'];assert not name.startswith('artifacts/') and 'LICENSE.local.txt' not in name
  p=checked(main,name);assert p.stat().st_size==row['Bytes'] and sha(p)==row['Sha256'];expected.add(name)
modified=set(subprocess.check_output(['git','-C',str(main),'diff','--name-only','-z']).decode().rstrip('\0').split('\0'))-{''};assert modified<=expected,modified-expected
pathspec=a.output.with_name(a.output.stem+'-pathspec.nul');assert not pathspec.exists();pathspec.write_bytes(b''.join(n.encode()+b'\0' for n in sorted(expected)))
subprocess.run(['git','-C',str(main),'add','--pathspec-from-file='+str(pathspec.resolve()),'--pathspec-file-nul'],check=True,capture_output=True)
staged=set(subprocess.check_output(['git','-C',str(main),'diff','--cached','--name-only','-z']).decode().rstrip('\0').split('\0'))-{''};assert staged==expected,(staged-expected,expected-staged)
names=sorted(staged);raw=subprocess.check_output(['git','-C',str(main),'cat-file','--batch'],input=''.join(':'+n+'\n' for n in names).encode());offset=0;rows=[]
for name in names:
 end=raw.index(b'\n',offset);header=raw[offset:end].split();assert len(header)==3 and header[1]==b'blob';size=int(header[2]);data=raw[end+1:end+1+size];assert raw[end+1+size:end+2+size]==b'\n';offset=end+2+size
 working=checked(main,name).read_bytes();same=data==working
 if not same:assert not name.endswith('.cs') and not name.startswith('docs/validation/part-348/') and data==working.replace(b'\r\n',b'\n'),name
 rows.append(dict(Path=name,Bytes=len(data),Sha256=hashlib.sha256(data).hexdigest().upper(),WorkingSha256=sha(main/name),RawExact=same))
assert offset==len(raw);subprocess.run(['git','-C',str(main),'diff','--cached','--check'],check=True,capture_output=True)
owned_variants=[r['Path'] for r in rows if not r['RawExact'] and r['Path'] in owned]
save(a.output,dict(Status='EXACT348_CACHED_STAGE_PASS',ParentHead=PARENT,Files=rows,Owned25WorkingRawExact=True,OwnedPublishedRawExact=not owned_variants,OwnedMetadataLfVariants=owned_variants,CSharpRawExact=True,ProofRawExact=True,ExplicitTextEolNormalizations=[r['Path'] for r in rows if not r['RawExact']],LocalTests=7268,Integrations=11,Original348Pending=True))
