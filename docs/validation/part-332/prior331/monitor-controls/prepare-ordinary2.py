from pathlib import Path
import json,hashlib,importlib.util,ast
r=Path.cwd();d=r/'artifacts/part331-remote-new-review';s=(d/'review-ordinary.py').read_text();needle="for native in v['ModuleResults'][-21:]:";s=s.replace(needle,"catalogpaths=['tools/Erlang.Differential/'+n for n in ['ProcLibCaseNames.cs','ProcLibCaseSources.cs','ProcLibCases.cs']]\nspec=importlib.util.spec_from_file_location('proc_contracts',Path(__file__).with_name('proc-lib-source-contracts.py'));catalogreader=importlib.util.module_from_spec(spec);spec.loader.exec_module(catalogreader);declared=catalogreader.contracts(m.blobs(H,catalogpaths))\nfor native,contract in zip(v['ModuleResults'][-21:],declared):\n assert native['Name']==contract['Name'] and native['Source']==contract['Source'] and native['FixtureExpected']==contract['Expected']")
ast.parse(s);(d/'review-ordinary2.py').write_bytes(s.encode())
spec=importlib.util.spec_from_file_location('contracts',d/'proc-lib-source-contracts.py');mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod);prefix='tools/Erlang.Differential/';items=mod.contracts({prefix+n:(r/prefix/n).read_bytes() for n in ['ProcLibCaseNames.cs','ProcLibCaseSources.cs','ProcLibCases.cs']});assert len(items)==21
rows=[]
for n in ['review-ordinary2.py','proc-lib-source-contracts.py']:
 p=d/n;rows.append(dict(Path=n,Bytes=p.stat().st_size,Sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()))
p=d/'ordinary-source-contracts-freeze2.json';p.write_bytes((json.dumps(dict(Status='SOURCE_ONLY_STRONGER_21_PUBLISHED_CASE_SOURCE_AND_EXPECTED_CONTRACTS',OriginalTemplateUnchanged=True,ParsedCases=21,Files=rows),indent=2)+'\n').encode());print(hashlib.sha256(p.read_bytes()).hexdigest().upper())
