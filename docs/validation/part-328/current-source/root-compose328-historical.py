"""Compose published327 production with identical final328 nonproduction helpers."""
from pathlib import Path, PurePosixPath
import argparse, hashlib, importlib.util, json, zipfile
p=argparse.ArgumentParser();p.add_argument('--focus-directory',required=True,type=Path);p.add_argument('--expected-freeze',required=True);p.add_argument('--output-name',required=True);args=p.parse_args()
r=Path.cwd().resolve();a=r/'artifacts';green=args.focus_directory.resolve();assert green.is_relative_to(a)
sha=lambda q:hashlib.sha256(q.read_bytes()).hexdigest().upper()
j=lambda q:json.loads(q.read_bytes())
fp=green/'formatted-source-freeze.json';assert sha(fp)==args.expected_freeze.upper();f=j(fp)
cl=green/'formatted-source-resource-closure.json';assert sha(cl)==f['SourceClosureSha256']
current=j(cl)['Files'];owned=f['Files'];assert all(x['Path'].endswith('.cs') for x in owned)
for row in current:
 q=green/'candidate'/row['Path'];assert q.stat().st_size==row['Bytes'] and sha(q)==row['Sha256']
baseline=a/'undefined-lambda328-candidate1';bf=baseline/'baseline-source-resource-closure.json';bz=baseline/'baseline-source-resources.zip'
assert sha(bf)=='4B3542580F10D6448650177D8261CC759CA6D0E5DADCD6F92414E3D1DC629956'
assert sha(bz)=='F4E18B91C93903112B9DA76C0CFD9CC55C8C5C4AE040688120C66C0E89A562'
b=j(bf);assert b['ParentHead']=='4bc4c00b713c1cc0bbf32a1c74add45174bbd10a' and len(b['Files'])==2379
modulepath=a/'root326-audit-common.py';spec=importlib.util.spec_from_file_location('historical328passive',modulepath);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
published=m.blobs(b['ParentHead'],[x['Path'] for x in b['Files']])
with zipfile.ZipFile(bz) as z:
 assert set(z.namelist())=={x['Path'] for x in b['Files']}
 raw={}
 for row in b['Files']:
  name=row['Path'];assert not PurePosixPath(name).is_absolute() and '..' not in PurePosixPath(name).parts
  data=z.read(name);assert len(data)==row['Bytes'] and hashlib.sha256(data).hexdigest().upper()==row['Sha256']
  assert len(published[name])==row['CanonicalGitBytes'] and hashlib.sha256(published[name]).hexdigest().upper()==row['CanonicalGitSha256']
  raw[name]=data
nonproduction=[x for x in owned if not x['Path'].startswith('src/')]
assert nonproduction and all(x['Path'].startswith(('tests/Erlang.Tests/','tools/Erlang.Differential/')) for x in nonproduction)
d=a/args.output_name;assert d.parent==a and not d.exists();c=d/'candidate';c.mkdir(parents=True)
for name,data in raw.items():
 q=c/name;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data)
for row in nonproduction:
 q=c/row['Path'];q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes((green/'candidate'/row['Path']).read_bytes())
names=sorted(set(raw)|{x['Path'] for x in nonproduction});rows=[{'Path':name,'Bytes':(c/name).stat().st_size,'Sha256':sha(c/name)} for name in names]
for row in b['Files']:
 if row['Path'].startswith('src/'):
  assert sha(c/row['Path'])==row['Sha256']
closure=d/'source-resource-closure.json';closure.write_text(json.dumps({'Files':rows},indent=2)+'\n')
freeze={'Status':'HISTORICAL327_WITH_IDENTICAL_FINAL328_NONPRODUCTION_HELPERS_UNBUILT_UNEXECUTED','ParentHead':b['ParentHead'],'CandidateDirectory':str(c),'SourcePaths':len(nonproduction),'CSharpPaths':len(nonproduction),'SourceClosurePaths':len(rows),'SourceClosureSha256':sha(closure),'Files':nonproduction,'AllPublished327ProductionRawExact':True,'All2379ParentGitBlobsVerified':True,'FinalGreenFormattedFreezeSha256':sha(fp),'ParentRawZipSha256':sha(bz),'Selectors':['Erlang.Tests.UndefinedLambdaPublicationHostChecks:RunOriginalIdentity'],'NewOrdinaryCases':8,'HistoricalOrdinaryExpectedFailure':'{error,error,undef}','NoNewProductionFilesCopied':True}
(d/'source-freeze.json').write_text(json.dumps(freeze,indent=2)+'\n')
print(json.dumps({'Freeze':str(d/'source-freeze.json'),'Sha256':sha(d/'source-freeze.json'),'SourcePaths':len(rows),'IdenticalHelperFiles':len(nonproduction),'Executed':False}))
