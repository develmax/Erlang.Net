from pathlib import Path
import hashlib,json,os,re,shutil,subprocess,sys
stage,repo,gitdir,*patches=map(Path,sys.argv[1:])
draft=os.environ.get('PARITY_PREFLIGHT_DRAFT')=='1'
suffix='-draft' if draft else ''
shadow=stage/('strict-preflight'+suffix)
shadow.mkdir()
digest=lambda path:hashlib.sha256(path.read_bytes()).hexdigest()
names=set()
for file in patches:
    body=file.read_text(encoding='utf-8-sig')
    names.update(re.findall(r'^\+\+\+ b/(.+)$',body,re.MULTILINE))
    assert not re.search(r'^\+\+\+ /dev/null$',body,re.MULTILINE),file
before={}
for name in sorted(names):
    old=(repo/name).resolve()
    new=(shadow/name).resolve()
    assert old.is_relative_to(repo.resolve()) and new.is_relative_to(shadow.resolve()),name
    if old.is_file():
        new.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(old,new)
        before[name]=digest(old)
    else:
        assert not old.exists(),name
        before[name]=None
command=['git',f'--git-dir={gitdir.resolve()}',f'--work-tree={shadow.resolve()}','apply']
for index,patch in enumerate(patches):
    for mode,args in [('check',['--check']),('replay',[])]:
        log=stage/f'patch-{index}-{mode}{suffix}.log'
        with log.open('wb') as stream:
            subprocess.run([*command,*args,'--verbose','--whitespace=error-all',str(patch.resolve())],
                           cwd=shadow,stdout=stream,stderr=subprocess.STDOUT,check=True)
results={}
for name,old in before.items():
    path=repo/name
    assert old== (digest(path) if path.is_file() else None),name
    target=shadow/name
    after=digest(target)
    if after!=old and not draft:
        path.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(target,path)
        assert digest(path)==after
    results[name]=dict(Before=old,After=after)
(stage/('strict-preflight'+suffix+'.json')).write_text(json.dumps(results,indent=2)+'\n',encoding='utf-8')
with (stage/('replayed'+suffix+'.txt')).open('x',encoding='utf-8') as log:
    for patch in patches:
        print(digest(patch).upper(),patch,file=log)
print(len(results),'preflight output files verified')



