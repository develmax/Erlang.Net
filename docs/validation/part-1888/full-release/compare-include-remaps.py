from pathlib import Path
import collections,hashlib,json,sys
old,new,profiles,current=map(Path,sys.argv[1:])
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def fingerprint(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
a,b,pairs=read(old),read(new),read(profiles)
assert a['Count']==b['Count']==pairs['Count']==1366
left={r['Beam']:r for r in a['Modules']}
right={r['Beam']:r for r in b['Modules']}
info={r['Beam']:r for r in pairs['Modules']}
assert a['Mapped']==len(left) and b['Mapped']==len(right)
changes=[]
assert not left.keys()-right.keys()
for key in sorted(left.keys()|right.keys()):
    previous=left.get(key)
    recipe=right[key]
    if previous==recipe:
        continue
    assert recipe['Resolver']=='OtpWindowsBootstrapLibraryIncludePaths',key
    row=info[key]
    profile=row['Profile']
    assert profile['SourcePath'].endswith('/'+recipe['SourceArgument']),key
    assert profile['IncludeDirectories']==[v['RecordedDirectory'] for v in recipe['IncludeDirectories']],key
    assert recipe['WorkingDirectory']['RecordedDirectory']=='.',key
    assert {name:recipe['Libraries'][name]['Recorded'] for name in ('kernel','public_key')}=={
        'kernel':'/daily_build/otp_src/bootstrap/lib/kernel','public_key':'/daily_build/otp_src/bootstrap/lib/public_key'}
    assert recipe['Libraries']['stdlib']['Recorded']==('../../stdlib' if row['Application']=='eunit-2.11' else '/daily_build/otp_src/bootstrap/lib/stdlib')
    if previous:
        assert previous['SourceSha256']==recipe['SourceSha256'],key
    changes.append(dict(Beam=key,PreviousResolver=previous['Resolver'] if previous else None,
                        CurrentResolver=recipe['Resolver'],SourceSha256=recipe['SourceSha256']))
assert len(changes)>0
output=dict(ModuleCount=1366,PriorMappings=len(left),CurrentMappings=len(right),
            PriorSHA256=fingerprint(old),CurrentSHA256=fingerprint(new),
            ResolverCounts=dict(collections.Counter(r['Resolver'] for r in right.values())),
            Deltas=changes)
current.write_text(json.dumps(output,indent=2)+'\n',encoding='utf-8')
print(len(changes))
