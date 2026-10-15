import pathlib,subprocess,hashlib,json
directory=pathlib.Path(__file__).resolve().parent
root=directory.parents[1]
pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
sha=lambda data:hashlib.sha256(data).hexdigest().upper()
for name in ['fixture.c','memory_allocator_nif_fixture.erl']:
    data=(root/'artifacts/memory-allocator-nif-audit'/name).read_bytes()
    target=directory/name
    if target.exists() and target.read_bytes()!=data:
        raise RuntimeError('Existing observer source differs; preserve prior revision before changing '+name)
    target.write_bytes(data)
headers=[]
for name in ['erl_nif.h','erl_drv_nif.h','erl_nif_api_funcs.h']:
    path='erts/emulator/beam/'+name
    data=subprocess.check_output(['git','-C',str(root.parent/'otp'),'show',pin+':'+path])
    headers.append(dict(Name=name,GitPath=path,Sha256=sha(data)))
names=['fixture.c','memory_allocator_nif_fixture.erl','nif_original_observer.erl','collect.py']
plan=dict(Status='Prepared source only; no compiler/native VM/dispatch run',PinnedCommit=pin,FixtureSourceSha256='7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074',Sources=[dict(Name=name,Sha256=sha((directory/name).read_bytes()),Bytes=(directory/name).stat().st_size) for name in names],Headers=headers,Cases=['increment_zero','increment_negative','increment_maximum_success','increment_maximum_badarg','increment_atom_badarg','greeting','load_veto'],NativeUnloadCounterCompared=False,SemanticAssertionsExecuted=False)
assert sha((directory/'fixture.c').read_bytes())==plan['FixtureSourceSha256']
target=directory/'source-plan.json'
target.write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Plan=str(target),Sha256=sha(target.read_bytes()))))
