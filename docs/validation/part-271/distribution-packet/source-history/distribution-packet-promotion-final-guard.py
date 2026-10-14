import pathlib,json,hashlib,subprocess,shutil
r=pathlib.Path.cwd();p=r/'artifacts/distribution-packet-promotion-prototype';e=r/'artifacts/distribution-packet-promotion-evidence';d=json.loads((r/'artifacts/distribution-packet-promotion-source-freeze-formatted.json').read_text());seed=json.loads((r/'artifacts/distribution-packet-promotion-seed.json').read_text());owned={f['Path'] for f in d['Files']};unchanged=[];unexpected=[]
for f in seed['Files']:
 if f['Path'] in owned:continue
 actual=hashlib.sha256((p/f['Path']).read_bytes()).hexdigest()
 (unchanged if actual==f['Sha256'] else unexpected).append(f['Path'])
assert not unexpected,unexpected
for f in d['Files']:
 raw=(p/f['Path']).read_bytes();assert hashlib.sha256(raw).hexdigest().upper()==f['Sha256'];actual=r/f['Path'];f['BeforeSha256']=hashlib.sha256(actual.read_bytes()).hexdigest().upper() if actual.exists() else None;f['BeforeSource']=actual.read_text() if actual.exists() else None
before=(r/'tests/Erlang.Tests/Program.cs').read_text();after=(p/'tests/Erlang.Tests/Program.cs').read_text();assert after.replace('Test(Erlang.Tests.DistributionPacketTestNames.Test, Erlang.Tests.DistributionPacketChecks.Run);\n\n','')==before
focus=json.loads((e/'focus.json').read_text(encoding='utf-8-sig'));rawcontrols=json.loads((e/'raw-controls.json').read_text(encoding='utf-8-sig'));cli=json.loads((e/'cli-controls.json').read_text());assert focus['Status']==rawcontrols['Status']==cli['Status']=='PASS';assert all(x['RefusedBeforeWriting'] for x in cli['Rows'])
closure=e/'actual-assembly-closures';closure.mkdir(exist_ok=True)
for project in ['Erlang.DistributionPacketContrasts','DistributionPacketFocus','DistributionPacketCaptureFocus']:
 for source in (p/'tools'/project/'bin/Release/net10.0').glob('*'):
  if source.is_file():
   dst=closure/project/source.name;dst.parent.mkdir(exist_ok=True);shutil.copyfile(source,dst)
builds={}
for name in ['collector-build.log','focus-build.log','capture-focus-build.log']:
 lines=(e/name).read_text(encoding='utf-8-sig').splitlines();builds[name]=lines[-8:]
receipt={'Status':'PASS_SCOPED_PROMOTION','MainHeadBoundAtFinalFreeze':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'SeedHead':seed['GitHead'],'SourceSeedIncludesRootPidImports':True,'OtherSeedFilesExact':len(unchanged),'OldRunnerNamesOrderSourceExactAfterAppendRemoval':True,'ProductionCodec5ExactRevision3':True,'Focus':focus,'RawControls':rawcontrols,'CliPreOutputRefusals':len(cli['Rows']),'CliEvidenceSynthetic':True,'NativeExecuted':False,'CoreMfaChanges':0,'ProductionFullRepeated':False,'Builds':builds,'StyleOwnedZero':(e/'style-final.log').read_text(encoding='utf-8-sig'),'UnexpectedMutations':unexpected}
d['Stage']='PostBuildScopedPromotion';d['Receipt']=receipt;d['PreviousFormattedFreezeSha256']=hashlib.sha256((r/'artifacts/distribution-packet-promotion-source-freeze-formatted.json').read_bytes()).hexdigest().upper();out=r/'artifacts/distribution-packet-promotion-source-freeze-final.json';out.write_text(json.dumps(d,indent=2));(e/'receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps({k:v for k,v in receipt.items() if k not in ['Builds','Focus','RawControls']},indent=2));print('FinalFreezeSHA',hashlib.sha256(out.read_bytes()).hexdigest().upper())
