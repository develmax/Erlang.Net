from pathlib import Path
import hashlib,json
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
planpath=OUT/'copy-plan.json'
plan=json.loads(planpath.read_text())
plan['Edits']=[{'File':'tests/Erlang.Tests/Program.cs','AppendBefore':'catalog snapshot','Insert':'Test(Erlang.Tests.MapFunKeyOrderHostNames.TestName, Erlang.Tests.MapFunKeyOrderHostChecks.Run);'}]
planpath.write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8')
bindingpaths=['src/Erlang.Terms/MapTerm.cs','src/Erlang.Terms/TermComparisonPurpose.cs','src/Erlang.Terms/NodeComparisonContext.cs','src/Erlang.Terms/FunctionTerm.cs','src/Erlang.Terms/Term.cs']
(OUT/'unchanged-dependencies.json').write_text(json.dumps({'Files':[{'Path':p,'Sha256':sha(ROOT/p),'Bytes':(ROOT/p).stat().st_size} for p in bindingpaths]},indent=2)+'\n',encoding='utf-8')
paths=[p for p in OUT.rglob('*') if p.is_file() and p.name!='source-freeze.json']
(OUT/'source-freeze.json').write_text(json.dumps({'Status':'UNEXECUTED_HOLD_FOR_NATIVE14','Files':[{'Path':str(p.relative_to(OUT)).replace('\\','/'),'Sha256':sha(p),'Bytes':p.stat().st_size} for p in sorted(paths)]},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'CopyPlanSha256':sha(planpath),'FreezeSha256':sha(OUT/'source-freeze.json'),'Files':len(paths)}))
