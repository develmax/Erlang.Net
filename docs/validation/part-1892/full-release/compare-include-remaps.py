from pathlib import Path
import collections,hashlib,json,sys
old,new,profiles,output=map(Path,sys.argv[1:])
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
a,b,profile_data=map(read,(old,new,profiles))
assert a['Count']==b['Count']==profile_data['Count']==1366
before={r['Beam']:r for r in a['Modules']}
after={r['Beam']:r for r in b['Modules']}
recipes={r['Beam']:r for r in profile_data['Modules']}
assert a['Mapped']==len(before) and b['Mapped']==len(after)
assert before.keys() <= after.keys()
changes=[]
for key in sorted(after):
    current=after[key]
    previous=before.get(key)
    if current==previous:
        continue
    entry=recipes[key]
    app=entry['Application']
    expected={'runtime_tools-2.5':'OtpWindowsBootstrapLibraryIncludePaths',
              'ssh-6.0.6':'OtpWindowsBootstrapLibraryIncludePaths',
              'wx-2.7':'OtpWindowsInTreeLibraryIncludePaths'}
    assert app in expected and current['Resolver']==expected[app],key
    recipe=entry['Profile']
    assert recipe['SourcePath'].endswith('/'+current['SourceArgument']),key
    assert recipe['IncludeDirectories']==[x['RecordedDirectory'] for x in current['IncludeDirectories']],key
    assert current['WorkingDirectory']['RecordedDirectory']=='.',key
    if previous:
        assert current['SourceSha256']==previous['SourceSha256'],key
    changes.append(dict(Beam=key,Application=app,Resolver=current['Resolver'],SourceSha256=current['SourceSha256']))
totals=dict(collections.Counter(row['Application'] for row in changes))
assert set(totals)=={'runtime_tools-2.5','ssh-6.0.6','wx-2.7'},totals
result=dict(Corpus=1366,PriorMappings=a['Mapped'],CurrentMappings=b['Mapped'],
            BeforeSha256=digest(old),AfterSha256=digest(new),Applications=totals,Changes=changes)
output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(totals))
