import pathlib,json,hashlib,subprocess,shutil
root=pathlib.Path.cwd();p=root/'artifacts/pid-order-prototype';e=root/'artifacts/pid-order-evidence';seed=json.loads((root/'artifacts/pid-order-seed.json').read_text());owned=['src/Erlang.Terms/TermOrder.cs','tools/Erlang.Differential/CompiledModuleCases.cs','tools/Erlang.Differential/IdentifierOrderCases.cs']
unchanged=[];unexpected=[]
for row in seed['Files']:
 if row['Path'] in owned:continue
 actual=hashlib.sha256((p/row['Path']).read_bytes()).hexdigest()
 (unchanged if actual==row['Sha256'] else unexpected).append(row['Path'])
assert not unexpected,unexpected
before=(root/owned[1]).read_text();after=(p/owned[1]).read_text();assert after.replace(', .. IdentifierOrderCases.Modules','')==before
files=[]
for name in owned:
 data=(p/name).read_bytes();f=root/name
 files.append({'Path':name,'BeforeSha256':hashlib.sha256(f.read_bytes()).hexdigest() if f.exists() else None,'Sha256':hashlib.sha256(data).hexdigest(),'BeforeSource':f.read_text() if f.exists() else None,'SourceContent':data.decode('utf8')})
newbin=p/'tools/IdentifierOrderProbe/bin/Release/net10.0';dest=e/'final-dll';dest.mkdir(exist_ok=True)
for f in newbin.glob('*'):
 if f.is_file():shutil.copyfile(f,dest/f.name)
old=json.loads((e/'old-probe.json').read_text(encoding='utf-8-sig'));new=json.loads((e/'final-probe.json').read_text(encoding='utf-8-sig'))
assert len(old)==len(new)==11 and all(r['Pass'] for r in new) and sum(not r['Pass'] for r in old)==6
receipt={'Status':'PASS','SeedHead':seed['Head'],'CurrentMainHead':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'OriginalPin':subprocess.check_output(['git','-C','../otp','rev-parse','OTP-29.1.1^{commit}']).decode().strip(),'OwnedPaths':owned,'OldGenerated':{'Passed':5,'Failed':6,'Rows':old},'NewGenerated':{'Passed':11,'Failed':0,'Rows':new},'OriginalNativeExecuted':False,'OldCaseDeclarationsTextExactAfterRemovingAppend':True,'OtherSeedFilesExact':len(unchanged),'UnexpectedMutations':unexpected,'StyleOwnedZero':(e/'style-owned-check.log').read_text(encoding='utf-8-sig'),'MvidOldThenFinal':(e/'final-probe.stderr').read_text(encoding='utf-8-sig').splitlines(),'FinalBuildLogTail':(e/'final-build.log').read_text(encoding='utf-8-sig').splitlines()[-8:],'ProductionFullRun':False}
(root/'artifacts/pid-order-source-freeze.json').write_text(json.dumps({'Receipt':receipt,'Files':files},indent=2,ensure_ascii=False),encoding='utf8');(e/'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf8')
print(json.dumps({k:receipt[k] for k in ['Status','SeedHead','CurrentMainHead','OtherSeedFilesExact','MvidOldThenFinal','FinalBuildLogTail']},indent=2));print('Freeze SHA',hashlib.sha256((root/'artifacts/pid-order-source-freeze.json').read_bytes()).hexdigest())

