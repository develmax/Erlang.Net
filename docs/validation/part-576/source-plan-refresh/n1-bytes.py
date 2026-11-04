from pathlib import Path
import hashlib,json,os,re,subprocess
r=Path.cwd();h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();s=Path('.github/workflows/nif-version651-linux.yml').read_text()
for key in ['OTP_PIN','ORIGINAL_PLAN','NATIVE_PLAN','CAPTURE_PLAN','ACCEPTANCE_PLAN']:os.environ[key]=re.search(r'^      '+key+r': ([A-Fa-f0-9]+)',s,re.M)[1]
os.environ['EXPECTED_HEAD']=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()
rows=[]
for label,name in [('entry','Erlang.NifEntryOriginalObservations'),('term','Erlang.NifTermCallbackOriginalObservations'),('text','Erlang.NifTextBinaryOriginalObservations'),('unix','Erlang.NifUnixFilenameObservations')]:
 tool=r/'tools/Native'/name;p=tool/'manifest.json';d=json.loads(p.read_bytes());assert d['PinnedOtpCommit']==os.environ['OTP_PIN']
 for group in ['Files','Dependencies']:
  for x in d[group]:
   base=tool if group=='Files' and label!='unix' else r;t=base/x['Path'];assert h(t)==x['Sha256'] and t.stat().st_size==x['Bytes'],str(t)
 x=d['Workflow'];assert h(r/x['Path'])==x['Sha256'];assert d['RuntimeBuildManifestSha256']==os.environ['ORIGINAL_PLAN']
 rows.append(dict(Name=label,Passed=True,ManifestSha256=h(p)))
admissions=rows
code=Path('artifacts/unicode-ci-fix/n1-preflight.py').read_text();code=code[code.index('for name,pin'):]
exec('repo=r;tool=repo/"tools/Native/Erlang.NifVersion651Observations";sha=h\n'+code)
Path('artifacts/unicode-ci-fix/n1-passive-after.json').write_text(json.dumps(dict(Passed=True,Scope='Equivalent byte checks only on Windows; Linux platform-gated verifier execution remains CI-only',LinuxVerifiersExecuted=False,NativeExecuted=False,Results=admissions,FrozenSourceRows=len(rows)),indent=2)+'\n')
print('N1 source bytes and complete downstream input closure passed')
