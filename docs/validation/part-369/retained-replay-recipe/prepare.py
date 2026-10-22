from pathlib import Path
import json,hashlib
root=Path(__file__).resolve().parents[2]
base=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
queue='artifacts/queue-integration368/main-capture'
proplist='artifacts/proplists-integration365/main-capture'
stages=[
 ('queue-all',queue,'artifacts/queue-original368-original1/tool',540),
 ('queue-core',queue,'artifacts/queue-original367-original1/tool',300),
 ('queue-long',queue,'artifacts/queue-original367-original2/tool',300),
 ('proplist-base',proplist,'artifacts/proplists-stack-next1/candidate/tools/Erlang.ProplistFamilyObservations',240),
 ('proplist-extra',proplist,'artifacts/proplists-stack-extra-original1/tool',180),
 ('proplist-strict',proplist,'artifacts/proplists-integration365/main-replay3/strict-tool',180),
 ('proplist-binding',proplist,'artifacts/proplists-stack-binding-original1/tool',180),
 ('proplist-iterator',proplist,'artifacts/proplists-integration365-original1/tool',180),
 ('proplist-raw',proplist,'artifacts/proplists-integration365/main-replay3/raw-tool',240),
]
rows=[]
native={
 'queue-all':'artifacts/queue-original368-original1/download/run-39078144460/artifact-12117323576/capture',
 'queue-core':'artifacts/queue-original367-original1/download/run-39071324870/artifact-12114654364/capture',
 'queue-long':'artifacts/queue-original367-original2/download/run-39072770120/artifact-12116022082/capture',
 'proplist-base':'artifacts/proplists-stack-original-ci-capture1/run-39053627861/artifact-12108698525/capture',
 'proplist-extra':'artifacts/proplists-stack-extra-original1/download/run-39055797975/artifact-12110170182/capture',
 'proplist-strict':'artifacts/proplists-stack-close-original1/download/run-39056890376/artifact-12109847910/capture',
 'proplist-binding':'artifacts/proplists-stack-binding-original1/download/run-39059210136/artifact-12110778492/capture',
 'proplist-iterator':'artifacts/proplists-integration365-original1/download/run-39061299103/artifact-12111562130/capture',
 'proplist-raw':'artifacts/proplists-integration365-original2/download/run-39062211544/artifact-12112450983/capture',
}
for name,harness,tool,timeout in stages:
    t=root/tool;p=json.loads((t/'profile.json').read_bytes())
    sourcepaths={x['Path'] for x in p['FixtureSources']}
    for f in p['Fixtures']:
        assert f['Location'] in ['tool','repository'],f['Id']
        sourcepaths.add(f['Path']);sourcepaths.update(f['Companions'])
    sourcefiles=[]
    for x in sorted(sourcepaths):
        source=t/x if (t/x).exists() else root/x
        assert source.is_file(),source
        sourcefiles.append({'Path':x,'Location':'tool' if (t/x).exists() else 'repository','Sha256':sha(source),'Bytes':source.stat().st_size})
    sourcecs=[{'Path':q.relative_to(root).as_posix(),'Sha256':sha(q)} for q in sorted((root/harness).glob('*')) if q.suffix in ['.cs','.csproj']]
    binary=root/harness/'bin/Debug/net10.0/MainCapture.dll'
    assert binary.exists(),binary
    original=root/native[name];completion=json.loads((original/'completion.json').read_bytes())
    assert completion['PinnedOtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
    assert completion['ProfileSha256']==p.get('OriginalProfileSha256',sha(t/'profile.json'))
    ngroups={g['Id']:g for g in completion['Groups']}
    nativefiles=[original/'completion.json']
    for f in p['Fixtures']:
        assert ngroups[f['Id']]['ExpectedRows']==len(f['Names']) and ngroups[f['Id']]['Acquired']
        nativefiles.extend(q for q in (original/f['Id']).rglob('*') if q.is_file() and q.suffix in ['.etf','.beam','.erl','.json'])
    rows.append({'Name':name,'Harness':harness,'HarnessSha256':sha(binary),'HarnessSource':sourcecs,'Tool':tool,'ProfileSha256':sha(t/'profile.json'),'Sources':sourcefiles,'Timeout':timeout,'Fixtures':[{'Id':f['Id'],'Cases':len(f['Names'])} for f in p['Fixtures']],'Native':native[name],'NativeHead':completion['SubjectHead'],'NativeFiles':[{'Path':q.relative_to(root).as_posix(),'Sha256':sha(q)} for q in nativefiles]})
helper='artifacts/spawn-priority351-preparation/execute-root.py'
assert sha(root/helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
(base/'inputs.json').write_text(json.dumps({'OwnedHelper':helper,'OwnedHelperSha256':sha(root/helper),'Stages':rows,'Status':'prepared only; Root executes current Main captures; no acceptance implied'},indent=2)+'\n',encoding='utf-8',newline='\n')
print([(r['Name'],sum(f['Cases'] for f in r['Fixtures'])) for r in rows])
