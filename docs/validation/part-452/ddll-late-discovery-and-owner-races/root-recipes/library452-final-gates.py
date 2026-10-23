from pathlib import Path
import hashlib,importlib.util,json,os
r=Path.cwd();out=r/'artifacts/library452-final-gates1';assert not out.exists();out.mkdir()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
read=lambda p:json.loads((r/p).read_bytes())
runtime=read('artifacts/library452-build-focus4/completion.json')['RuntimeSha256']
assert runtime=='18AE9106B6A4396D402BFCD9058ACE77CC2863A57C627EBFEBA496029625D307'
old=read('artifacts/library452-affected-run1/completion.json');assert old['Passed'] and old['Examined']==591 and old['ExactCoverage'] and old['ImmutableInputs']
focus=read('artifacts/library452-build-focus4/focus.json');assert focus['Passed']==focus['SelectedCount']==1 and focus['Failed']==0 and focus['TotalCount']==8828
guards=read('artifacts/library452-final-test-guards.json');p=r/'tests/Erlang.Tests/RetiredCandidateHostChecks452.cs';assert sha(p)==guards['FinalSha256']
before=p.read_bytes().replace(b'                    Require(!adoption.IsCompleted);\n',b'').replace(b'            Require(!obsolete.IsCompleted);\n',b'');assert hashlib.sha256(before).hexdigest().upper()==guards['PriorSha256']
selection=read('artifacts/library452-affected-selection1/selection.json');changed=[]
for path,want in selection['Bindings'].items():
 if sha(r/path)!=want:changed.append(path)
source=[p for p in changed if not any(x in Path(p).parts for x in ['bin','obj'])]
assert source==['tests/Erlang.Tests/RetiredCandidateHostChecks452.cs'],source
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll')==runtime==sha(r/'artifacts/library452-driver-package1/package/Erlang.Runtime.dll')
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned452final',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1')
m.owned(['C:/Program Files/dotnet/dotnet.exe','tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--check','.'],out,'style',120,env,str(r))
report={'Passed':True,'Prior591ImmutablePass':True,'UnchangedRetainedCases':590,'FinalStrengthenedCasePassed':1,'CompositeAffected':591,'FinalCatalog':8828,'ChangedBindings':changed,'ChangedSourceOnlyTwoAssertions':source,'RuntimeSha256':runtime,'FinalStylePassed':True,'FinalWholeSourceCiRequired':True}
(out/'completion.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
