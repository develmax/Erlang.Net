import hashlib,json,os,pathlib,stat,urllib.request,urllib.error,zipfile
ROOT=pathlib.Path.cwd().resolve()
PLAN_SHA='83667FC235724A7E618C6DA8AAE36FF6BCCA0C4616E1C1AE83E1BA1F25A43995'
plan_path=ROOT/'.validation/nif-callback361-source-plan.json'
assert hashlib.sha256(plan_path.read_bytes()).hexdigest().upper()==PLAN_SHA
changed={'tools/Erlang.NifNativeBackend/NifLinuxCallbackDriver.cs','tools/Erlang.NifLinuxCallbackFocus/Program.cs','.github/workflows/nif-entry-original.yml'}
verified=[]
for row in json.loads(plan_path.read_bytes())['Files']:
    path=(ROOT/row['Path']).resolve();assert path.is_relative_to(ROOT)
    actual=hashlib.sha256(path.read_bytes()).hexdigest().upper()
    instrumented=row['Path'].replace('\\','/') in changed
    if not instrumented:assert actual==row['Sha256']
    verified.append({'Path':row['Path'],'ActualSha256':actual,'ParentSha256':row['Sha256'],'Instrumented':instrumented})
EXPECTED='07996890D337B6EE669EC4A06948ADBAE5DDE055920771D3BD2DB8C80A35C540'
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args,**kwargs): return None
opener=urllib.request.build_opener(NoRedirect)
request=urllib.request.Request('https://api.github.com/repos/'+os.environ['GITHUB_REPOSITORY']+'/actions/artifacts/12099548874/zip',headers={'Authorization':'Bearer '+os.environ['GH_ARTIFACT_TOKEN'],'Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28'})
try:
    opener.open(request,timeout=30)
    raise RuntimeError('Expected explicit artifact redirect')
except urllib.error.HTTPError as response:
    assert response.code==302
    signed=response.headers['Location']
from urllib.parse import urlsplit
url=urlsplit(signed)
assert url.scheme=='https' and not url.username and not url.password and url.hostname and any(url.hostname.endswith(suffix) for suffix in ('.blob.core.windows.net','.actions.githubusercontent.com','.githubusercontent.com'))
archive=ROOT/'artifacts/linux-diagnostic-reused.zip';archive.parent.mkdir(exist_ok=True)
# The signed request has no GitHub Authorization header and refuses further redirects.
with opener.open(urllib.request.Request(signed),timeout=60) as incoming,archive.open('xb') as outgoing:
    total=0
    while chunk:=incoming.read(65536):
        total+=len(chunk);assert total<=16*1024*1024;outgoing.write(chunk)
assert hashlib.sha256(archive.read_bytes()).hexdigest().upper()==EXPECTED
out=ROOT/'artifacts/linux-callback17';assert not out.exists()
with zipfile.ZipFile(archive) as z:
    assert len(z.infolist())<5000 and sum(x.file_size for x in z.infolist())<64*1024*1024
    names=set()
    for member in z.infolist():
        name=member.filename;part=pathlib.PurePosixPath(name)
        assert name not in names and not part.is_absolute() and '..' not in part.parts and '\\' not in name and ':' not in name
        assert part.parts[0] in ('bridge','reference-native','reference-original','runtime-build','sdk','size-native','size-original')
        assert not stat.S_ISLNK(member.external_attr>>16);names.add(name)
    z.extractall(out)
(out/'reuse-proof.json').write_text(json.dumps({'ParentRun':39030222266,'ParentArtifact':12099548874,'ZipSha256':EXPECTED,'Members':sorted(names),'SubjectHead':'f7399755673e510c15ddcdc573a376141c1c5965','ExecutingCodeHead':os.environ['GITHUB_SHA'],'SourcePlanSha256':PLAN_SHA,'VerifiedSourceRows':verified},indent=2))
