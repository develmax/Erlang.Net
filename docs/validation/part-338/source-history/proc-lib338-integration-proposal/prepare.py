from pathlib import Path
import json,hashlib,shutil
out=Path('artifacts/proc-lib338-integration-proposal');src=out/'source';life=Path('artifacts/proc-lib-lifecycle-candidate');parent=Path('artifacts/proc-lib337-candidate');own=json.loads(Path('artifacts/proc-lib-lifecycle-source-proposal/source-freeze.json').read_bytes());logger=json.loads(Path('artifacts/proc-lib-logger-source/source-freeze3.json').read_bytes());paths=[]
for r in own['OwnedFiles']:
 paths.append(r['Path'])
for r in logger['Files']:
 p=Path(r['Path'])
 if p.suffix=='.cs':paths.append(p.relative_to(life).as_posix())
assert len(paths)==23
for rel in paths:
 data=(life/rel).read_bytes()
 if rel.endswith('/ProcLib.cs'):
  t=data.decode('utf-8-sig');marker='        SysTermination.Register(registry);';assert marker in t;t=t.replace(marker,marker+'\n        ProcLibReportFormattingRegistrations.Register(registry);\n        ProcLibLifecycle.Register(registry);');data=t.encode()
 p=src/rel;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
rel='src/Erlang.Otp.Behaviours/OtpModules.cs';t=(parent/rel).read_text(encoding='utf-8-sig');marker='            ProcLib.Register(registry);';assert t.count(marker)==1;t=t.replace(marker,'            ProcLibCrashReportDependencies.Register(registry);\n'+marker);p=src/rel;p.write_text(t,encoding='utf-8');paths.append(rel)
fixtures=[]
for man in ['cases.json','limiter-cases.json']:
 j=json.loads((life/'fixtures'/man).read_bytes())
 for r in j['Cases']:
  original=life/r['Path'];relative=Path('fixtures/lifecycle')/original.name;p=out/relative;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(original.read_bytes());fixtures.append(dict(Name=r['Name'],Path=relative.as_posix(),Expected=r['Expected'],ObservedOriginal=False))
for r in json.loads(Path('artifacts/proc-lib-logger-source/source-derived-cases.json').read_bytes())['Cases']:
 original=Path('artifacts/proc-lib-logger-source')/r['SourcePath'];relative=Path('fixtures/logger')/original.name;p=out/relative;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(original.read_bytes());fixtures.append(dict(Name=r['Name'],Path=relative.as_posix(),Expected=r['ExpectedErlang'],ObservedOriginal=False))
assert len(fixtures)==43
rows=[]
for rel in paths:
 b=(src/rel).read_bytes();old=parent/rel;rows.append(dict(Path=rel,Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper(),Parent337Sha256=hashlib.sha256(old.read_bytes()).hexdigest().upper() if old.exists() else None))
for r in fixtures:
 b=(out/r['Path']).read_bytes();r.update(Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper())
(out/'fixtures.json').write_text(json.dumps(dict(Status='UNPARSED_SOURCE_DERIVED_CASES_NOT_ORIGINAL_BASELINE',Cases=fixtures),indent=2)+'\n')
f=out/'source-freeze.json';f.write_text(json.dumps(dict(Status='SOURCE_ONLY_FUTURE338_INTEGRATION_DRAFT_NOT_APPLIED',Files=rows,ProductionCSharpFiles24=True,FixtureCases43=True,SourceLifecycleFreeze='AF11AD6517F78D363BF78DF78415C2EF1DA988D203D4D8190C5ED3A559E2A331',LoggerFreezeSha256=hashlib.sha256(Path('artifacts/proc-lib-logger-source/source-freeze3.json').read_bytes()).hexdigest().upper(),FixtureManifestSha256=hashlib.sha256((out/'fixtures.json').read_bytes()).hexdigest().upper(),RegistrationOrder='OtpModules registers real crash dependencies then ProcLib; ProcLib preserves337formatter and appends lifecycle4. Registry idempotence remains OtpModules.Installed.',NeedRebase='Actual337publication/sourcefreeze must be parent before apply; compare shared3files carefully, neveroverwrite337formatterregistration.',KnownGaps='Logger source scope.md limits retained. Actual initial_call traces/structuredreport/streams still need observed OTP. No publiccompletion claim.',SdkExecuted=False,MainChanged=False,Candidate337Changed=False),indent=2)+'\n');print(hashlib.sha256(f.read_bytes()).hexdigest().upper())
