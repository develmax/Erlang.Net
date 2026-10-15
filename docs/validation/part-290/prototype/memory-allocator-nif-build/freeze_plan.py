import pathlib, subprocess, hashlib, json
root=pathlib.Path(__file__).resolve().parents[2]
directory=pathlib.Path(__file__).resolve().parent
pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
sha=lambda data: hashlib.sha256(data).hexdigest().upper()
headers=[]
for path in ['erts/emulator/beam/erl_nif.h','erts/emulator/beam/erl_drv_nif.h','erts/emulator/beam/erl_nif_api_funcs.h','erts/include/erl_int_sizes_config.h.in']:
    data=subprocess.check_output(['git','-C',str(root.parent/'otp'),'show',pin+':'+path])
    headers.append(dict(GitPath=path,OutputName=pathlib.Path(path).name,Sha256=sha(data)))
sources=[]
for source,name in [('artifacts/memory-allocator-nif-audit/fixture.c','fixture.c'),('artifacts/memory-allocator-nif-build/layout.c','layout.c')]:
    sources.append(dict(Path=source,OutputName=name,Sha256=sha((root/source).read_bytes())))
plan=dict(Status='Prepared source-only; build and layout executable never run by author',PinnedCommit=pin,Profile='Windows x64 LLP64',BuildScriptSha256=sha((directory/'build_fixture.py').read_bytes()),Sources=sources,Headers=headers,GeneratedSizes={'SIZEOF_CHAR':1,'SIZEOF_SHORT':2,'SIZEOF_INT':4,'SIZEOF_LONG':4,'SIZEOF_LONG_LONG':8,'SIZEOF_VOID_P':8},ExpectedExports=['nif_init','memory_allocator_fixture_unload_count'],ExpectedLayout=dict(PointerBytes=8,LongBytes=4,TermBytes=8,EntryBytes=96,FunctionBytes=32,BinaryBytes=40,CallbackBytes=1456,EntryOffsets={'major':0,'minor':4,'name':8,'num_of_funcs':16,'funcs':24,'load':32,'reload':40,'upgrade':48,'unload':56,'vm_variant':64,'options':72,'sizeof_ErlNifResourceTypeInit':80,'min_erts':88},FunctionOffsets={'name':0,'arity':8,'fptr':16,'flags':24}),Trust='Independent review of source plan, native build receipt and layout/export evidence required; computed DLL hash is not self-authorization')
target=directory/'source-plan.json'
target.write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Plan=str(target),Sha256=sha(target.read_bytes()))))
