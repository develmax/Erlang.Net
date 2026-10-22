from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
files=[]
for p in sorted((OUT/'source').rglob('*')):
    if p.is_file():
        rel=str(p.relative_to(OUT/'source')).replace('\\','/')
        files.append({'Source':str(p.relative_to(ROOT)).replace('\\','/'),'Destination':rel,'Sha256':sha(p),'Bytes':p.stat().st_size})
plan={'Files':files,'Edits':[{'File':'tests/Erlang.Tests/Program.cs','AppendBefore':'catalog snapshot','Insert':'Test(Erlang.Tests.BoundedBifHostNames.TestName, Erlang.Tests.BoundedBifHostChecks.Run);'},{'File':'tests/Erlang.Tests/Erlang.Tests.csproj','Insert':'<EmbeddedResource Include="Resources/Erlang371/bounded_bif_contract.erl" />'}],'NoMainEditsByHelper':True,'NoBuildByHelper':True,'NoOriginalExecutionCredit':True}
(OUT/'copy-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8')
source=next(p for p in (OUT/'source').rglob('*.erl')).read_text()
for index,name in enumerate(['hd_context','tl_context','tuple_context','integer_context']):
    assert source.splitlines()[12+index].startswith(name+'(Value)')
notes={'Status':'SOURCE_BASED_HOST_PROPOSAL_NOT_EXECUTED','TestDeltaAfterIntegration':1,'CompiledExecutions':'20 errors (4 BIFs x5 depths),8 guard outcomes,2 object identity assertions and5 4096-bit range outcomes','ErrorAssertions':'Complete BIF tuple module/function/argument list/error_info formatter; error class and reason; caller tuple module/function/arity/exact fixture file/line; depth0/1 exactly1frame,depth2 exactly2frames;default/8bounded<=8. Any remaining frames retain4tuple shape. Deeper full-stack names are not invented: exact all-field native comparison remains separately mandatory.','Scope':'Permanent regression complements the50/250 actual original packet; source proof remains conditional until Root CI/final-source acceptance. No status change, no existing ordered registration replacement.'}
(OUT/'review.json').write_text(json.dumps(notes,indent=2)+'\n',encoding='utf-8')
paths=[p for p in OUT.rglob('*') if p.is_file() and p.name!='source-freeze.json']
(OUT/'source-freeze.json').write_text(json.dumps({'Files':[{'Path':str(p.relative_to(OUT)).replace('\\','/'),'Sha256':sha(p),'Bytes':p.stat().st_size} for p in sorted(paths)]},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'PlanSha256':sha(OUT/'copy-plan.json'),'FreezeSha256':sha(OUT/'source-freeze.json')}))
