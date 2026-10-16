from pathlib import Path
import hashlib,json
r=Path.cwd();a=r/'artifacts';d=a/'undefined-lambda328-historical-focus2'
j=lambda p:json.loads(p.read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
prior=a/'undefined-lambda328-red-actual-guard3.json';assert sha(prior)=='DE0858D148C4295465FA4B59409E441E172336FF2FE445E863D53CF2FACE7A1F'
v=j(prior);assert v['Passed'] and v['SinglePrimaryException']
actual=j(d/'managed-run/actual.json');process=j(d/'managed-run/process.json');build=j(d/'managed-build/process.json')
assert build['Passed'] and process['Passed'] and process['OwnedProcessSettled'] and not process['TimedOut'] and process['ExitCode']==0
assert actual['RecipePassed'] and actual['Executed']==actual['HistoricalUndefinedFunctionConfirmed']==1 and actual['FixturePassed']==0
assert len(actual['Cases'])==1
case=actual['Cases'][0];assert case['Case']=='undefined-lambda-no-current-original-fun-environment-arguments-self'
assert case['Actual']=='{error,error,undef}' and case['Exception'] is None and case['StandardOutputHex']==case['StandardErrorHex']==''
for item in actual['ActualLoadedAssemblies']:assert sha(Path(item['Path']))==item['Sha256']
extra=[prior]+[d/role/name for role in ('managed-build','managed-run') for name in ('process.json','stdout.log','stderr.log')]+[d/'managed-run/actual.json']
rows={x['Path']:x for x in v['Bindings']}
for p in extra:rows[p.relative_to(r).as_posix()]={'Path':p.relative_to(r).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)}
v.update(Status='PASS_ACTUAL328_HISTORICAL327_PUBLIC_HOST_UNDEF_AND_GENERATED_UNDEF',PreviousGuardSha256=sha(prior),HistoricalOrdinaryNotExecutedYet=False,HistoricalGeneratedUndefConfirmed=1,HistoricalGeneratedActual='{error,error,undef}',GeneratedRedProcessExitZeroIsSuccessfulDiscriminator=True,Bindings=list(rows.values()))
out=a/'undefined-lambda328-red-actual-guard4.json';assert not out.exists();out.write_text(json.dumps(v,indent=2)+'\n');print(json.dumps({'Sha256':sha(out),'HostSelectorActualExit':1,'GeneratedRedVerifierActualExit':0}))
