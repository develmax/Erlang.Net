from pathlib import Path
import json,hashlib
O=Path(__file__).resolve().parent;R=O.parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def out(n,x):(O/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
files=[]
for p in sorted((O/'source').rglob('*.cs')):
 dest=p.relative_to(O/'source').as_posix()
 prev=R/'artifacts/lists372-usort-source-proposal1/source'/dest
 files.append({'Source':p.relative_to(R).as_posix(),'Destination':dest,'BaseSha256':sha(R/dest) if (R/dest).exists() else None,'PredecessorProposalSha256':sha(prev),'Sha256':sha(p)})
anchor='    Test(Erlang.Tests.ListUniqueSourceHostNames.TestName, Erlang.Tests.ListUniqueSourceHostChecks.Run);'
out('copy-plan.json',{'Status':'HOST_ONLY_SUCCESSOR_NO_PRODUCTION_CHANGE_NOT_EXECUTED','Predecessor':'artifacts/lists372-usort-source-proposal1','Files':files,'RegistrationEdit':{'Destination':'tests/Erlang.Tests/Program.cs','BaseSha256':sha(R/'tests/Erlang.Tests/Program.cs'),'ExactUniqueAnchor':anchor,'InsertAfter':'\n'.join('    Test(Erlang.Tests.ListUniqueSortSourceHostNames.'+a+', Erlang.Tests.ListUniqueSortSourceHostChecks.'+b+');' for a,b in [('IdentityTest','Identity'),('MalformedTest','Malformed'),('DeepTest','Deep'),('ReverseTest','DynamicReverse')]),'IfFirstThreeAlreadyRegistered':'Insert only ReverseTest after DeepTest; do not duplicate prior registrations.'},'ProductionSources':'Use exactly five frozen predecessor source files without changes.'})
out('source-proof.json',{'Status':'SOURCE_BACKED_HOST_ONLY_CONTROL_NOT_EXECUTED','PublicApisOnly':True,'NoReflection':True,'Method':'Admit usort into original lists code, stop real ReduceAsync continuation through existing deterministic ListLengthYieldContext during long ascending split, delete current lists module and register replacement reverse/2, resume admitted old usort code. A genuine remote call must select new reverse/2 and preserve its unique failure marker. Proper source split ends in rumergel single-list case then actual lists:reverse/2; no callback is patched inside the algorithm.','WhatItTests':['Actual dynamic remote lists:reverse/2 dispatch after usort code admission.','Public hot-code replacement during explicit awaited source machine traversal.','Replacement failure reaches original process, source/code scopes unwind and process completes.'],'WhatItDoesNotClaim':['No native lists replacement experiment is claimed.','No completed execution or successful code-version lease behavior is claimed before parent runs it.'],'NoBuildRunNetworkOrMainEdits':True})
out('freeze.json',{'Files':[{'Path':p.relative_to(O).as_posix(),'Sha256':sha(p)} for p in sorted(O.rglob('*')) if p.is_file() and p.name!='freeze.json'],'NoBuildRuntimeRunNetworkOrMainEdits':True})
print(sha(O/'freeze.json'))
