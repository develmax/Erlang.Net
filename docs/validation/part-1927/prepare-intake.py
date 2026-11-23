from pathlib import Path
import json,re,sys,zipfile
work,stage,parent,boolean,wx1,wx2=map(Path,sys.argv[1:])
lines=lambda p:p.read_text(encoding='utf-8-sig').splitlines()
clean=lambda name:name.replace(chr(92),'/')
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
harness='tests/Erlang.Tests/Harness/Program.cs'
with zipfile.ZipFile(parent/'source.zip') as archive:
    old=set(archive.read(harness).decode('utf-8').splitlines())
current=lines(work/harness)
test_names=[line for line in current if line not in old
            and re.search(r'^(?:Test\(Erlang\.Tests|\s*Erlang\.Tests\.).*\.(?:TestName|FrameTestName)',line)]
assert len(test_names)==3,test_names
(stage/'harness-registrations.txt').write_text('\n'.join(test_names)+'\n',encoding='utf-8')
baseline=read(parent/'full-uncached/otp-corpus.json')
known={clean(row['Beam']):row['Status'] for row in baseline['Modules']}
exact=[clean(path) for path in lines(parent/'exact-controls.txt')+lines(parent/'exact-new-files.txt')]
assert all(known[x]=='Equal' for x in exact)
chosen=set(exact)
for path in (boolean,wx1/'broad-modules.txt',wx2/'broad-modules.txt',wx2/'supplement-modules.txt'):
    chosen.update(clean(line) for line in lines(path) if line)
assert chosen<=known.keys(),chosen-known.keys()
(stage/'exact-controls.txt').write_text('\n'.join(sorted(set(exact),key=str.casefold))+'\n',encoding='utf-8')
(stage/'pilot-modules.txt').write_text('\n'.join(sorted(chosen,key=str.casefold))+'\n',encoding='utf-8')
print(json.dumps({'Scoped':len(chosen),'PreviouslyExact':sum(known[p]=='Equal' for p in chosen),
                  'ExactControls':len(set(exact)),'NewTests':test_names}))