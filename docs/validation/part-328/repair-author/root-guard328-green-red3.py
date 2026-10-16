"""Actual source/DLL/process guard for corrected328 and published327 semantic RED."""
from pathlib import Path
import hashlib,json
r=Path.cwd();a=r/'artifacts';g=a/'undefined-lambda328-root-focus3';h=a/'undefined-lambda328-historical-focus2';old=a/'undefined-lambda328-historical2'
j=lambda p:json.loads(p.read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
bindings={}
def bind(p):
 p=p.resolve();assert p.is_relative_to(r);bindings[p.relative_to(r).as_posix()]={'Path':p.relative_to(r).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)}
 return j(p)
def sources(d,formatted=True):
 fp=d/('formatted-source-freeze.json' if formatted else 'source-freeze.json');f=bind(fp)
 cp=d/('formatted-source-resource-closure.json' if formatted else 'source-resource-closure.json');rows=bind(cp)['Files'];assert sha(cp)==f.get('SourceClosureSha256',f.get('FinalSourceClosureSha256')) and len(rows)==f['SourceClosurePaths']
 for row in rows:
  q=d/'candidate'/row['Path'];assert q.stat().st_size==row['Bytes'] and sha(q)==row['Sha256']
 return f,{e['Path']:e for e in rows}
gf,gr=sources(g);assert sha(g/'formatted-source-freeze.json')=='4B93B56228351AE390BFD88103E2FA56B64EDCE5039410D4FF76D5B5A549E47C' and len(gr)==2393 and gf['CSharpPaths']==24
for d in (g,h):
 assert (d/'tokens-before.json').read_bytes()==(d/'tokens-after.json').read_bytes()
 for name in ('tokens-before.json','tokens-after.json','preformat-source-freeze.json','preformat-source-resource-closure.json'):
  bind(d/name)
 for phase in ('token-before','sdk-format','style-write','token-after','style-check','target-build'):
  p=bind(d/phase/'process.json');assert p['ExitCode']==0 and p['OwnedProcessSettled'] and not p['TimedOut'] and p['ValidationError'] is None
  for name in ('stdout.log','stderr.log'):bind(d/phase/name) if (d/phase/name).suffix=='.json' else bindings.update({(d/phase/name).relative_to(r).as_posix():{'Path':(d/phase/name).relative_to(r).as_posix(),'Bytes':(d/phase/name).stat().st_size,'Sha256':sha(d/phase/name)}})
summary=bind(g/'diagnostic-summary.json');assert summary['Passed'] and len(summary['Results'])==12 and all(e['Classification']=='SELECTOR_PASS' for e in summary['Results'])
for n in range(12):
 p=bind(g/f'selector-{n:02d}/process.json');v=bind(g/f'selector-{n:02d}/actual.json');assert p['ExitCode']==0 and p['OwnedProcessSettled'] and not p['TimedOut'] and p['ValidationError'] is None
 assert v['Status']=='PASS' and len(v['Cases'])==1 and v['Cases'][0]['Status']=='PASS'
 for identity in v['Identities']:
  assert sha(Path(identity['Path']))==identity['Sha256']
managed=bind(g/'managed-run/actual.json');mp=bind(g/'managed-run/process.json');mb=bind(g/'managed-build/process.json')
assert mb['Passed'] and mp['Passed'] and mp['ExitCode']==0 and mp['OwnedProcessSettled'] and not mp['TimedOut']
assert managed['RecipePassed'] and managed['Executed']==managed['FixturePassed']==8 and managed['FixtureFailed']==0
for row in managed['Cases']:assert row['FixturePassed'] and row['Exception'] is None and row['StandardOutputHex']==row['StandardErrorHex']==''
for identity in managed['ActualLoadedAssemblies']:assert sha(Path(identity['Path']))==identity['Sha256']
peer=bind(a/'undefined-lambda328-focus3-passive-allocator309-review.json');assert sha(a/'undefined-lambda328-focus3-passive-allocator309-review.json')=='A8F887ED3F36EC2B5D0A2933D49B757BFB7DDAF4CBD6E04F4C6506EAD8977AF3'
greenbindings=list(bindings.values())
green={'Status':'PASS_ACTUAL328_TWELVE_HOST_EIGHT_GENERATED_BOUND_SOURCE_AND_PROCESSES','Passed':True,'TargetBuildPassed':True,'GreenSourceFreezeSha256':sha(g/'formatted-source-freeze.json'),'SourceClosureSha256':gf['SourceClosureSha256'],'GeneratedCasesPassed':8,'HostGroupsPassed':12,'FeatureHostGroupsPassed':7,'UnchangedLegacyRegressionGroupsPassed':5,'SourceClosureFiles':2393,'Bindings':greenbindings,'OriginalExecuted':False,'MainFullClaimed':False}
gp=a/'undefined-lambda328-green-actual-guard3.json';assert not gp.exists();gp.write_text(json.dumps(green,indent=2)+'\n')
hf,hr=sources(h);of,orr=sources(old,False);assert sha(old/'source-freeze.json')=='BC42B39AA97835C9833F2D50393FAC61968EE945AC4224E4521F00623894898F' and len(hr)==len(orr)==2387
assert hr==orr
base=bind(a/'undefined-lambda328-candidate1/baseline-source-resource-closure.json')['Files']
for row in base:
 if row['Path'].startswith('src/'):
  assert hr[row['Path']]['Sha256']==row['Sha256']
nonprod=[x for x in gf['Files'] if not x['Path'].startswith('src/')];assert len(nonprod)==10
for row in nonprod:assert hr[row['Path']]==row
rp=bind(h/'selector-00/process.json');rv=bind(h/'selector-00/actual.json');assert rp['ExitCode']==1 and rp['OwnedProcessSettled'] and not rp['TimedOut'] and rp['ValidationError'] is None
assert rv['Status']=='FAIL' and len(rv['Cases'])==1 and rv['Cases'][0]['Name']=='Erlang.Tests.UndefinedLambdaPublicationHostChecks:RunOriginalIdentity' and rv['Cases'][0]['Status']=='FAIL'
error=rv['Cases'][0]['Exception'];assert error.startswith('Erlang.ErlangException: undef') and 'RunOriginalIdentity' in error
assert not any(x in error for x in ('TimeoutException','AggregateException','MissingMethodException','TypeLoadException','TargetInvocationException','StackOverflowException'))
for identity in rv['Identities']:assert sha(Path(identity['Path']))==identity['Sha256']
red={'Status':'PASS_ACTUAL328_HISTORICAL327_PUBLIC_RESTORED_FUN_UNDEF_DISCRIMINATOR','Passed':True,'TargetBuildPassed':True,'RedSourceFreezeSha256':sha(old/'source-freeze.json'),'SourceClosureSha256':of['SourceClosureSha256'],'HistoricalProductionExactPublished327':True,'SameFormattedTenNonProductionHelpersRaw':True,'OwnedProcessSettled':True,'SinglePrimaryException':True,'NoTimeoutOrCleanupAggregate':True,'ActualFailure':'public_restored_local_fun_undef','Bindings':list(bindings.values()),'HistoricalSelectorExitCode':1,'HistoricalOrdinaryNotExecutedYet':True,'OriginalExecuted':False}
rp=a/'undefined-lambda328-red-actual-guard3.json';assert not rp.exists();rp.write_text(json.dumps(red,indent=2)+'\n')
ledger={'Passed':True,'ManagedActual':{'Path':(g/'managed-run/actual.json').relative_to(r).as_posix(),'Sha256':sha(g/'managed-run/actual.json')},'FormattedSourceFreeze':{'Path':(g/'formatted-source-freeze.json').relative_to(r).as_posix(),'Sha256':sha(g/'formatted-source-freeze.json')},'HostActuals':[{'Path':(g/f'selector-{n:02d}/actual.json').relative_to(r).as_posix(),'Sha256':sha(g/f'selector-{n:02d}/actual.json')} for n in range(12)],'NewHostRows':[{'Name':'host_restored_local_fun_publication_and_lambda_dispatch','NamesPath':'tests/Erlang.Tests/UndefinedLambdaPublicationHostNames.cs','Constant':'Test','ProgramLine':'Test(Erlang.Tests.UndefinedLambdaPublicationHostNames.Test, Erlang.Tests.UndefinedLambdaPublicationHostChecks.Run);'}]}
lp=a/'undefined-lambda328-actual-case-ledger3.json';assert not lp.exists();lp.write_text(json.dumps(ledger,indent=2)+'\n')
print(json.dumps({'GreenGuardSha256':sha(gp),'RedGuardSha256':sha(rp),'CaseLedgerSha256':sha(lp)}))
