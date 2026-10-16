from pathlib import Path
import hashlib,importlib.util,json,subprocess,sys
sys.dont_write_bytecode=True
R=Path.cwd();A=R/'artifacts';s=importlib.util.spec_from_file_location('m',A/'pack327-audit-common.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
head='4bc4c00b713c1cc0bbf32a1c74add45174bbd10a';old=A/'pack317-reallocation-published-passive-contracts';base=A/'pack327-reallocation-published-passive-contracts';assert not base.exists()
paths=[p.relative_to(old).as_posix() for p in old.rglob('*') if p.is_file()];assert len(paths)==8;blobs=m.blobs(head,paths)
for name,b in blobs.items():p=base/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b)
review=base/'tools/Erlang.NifReallocationObservations/review-results.py';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();assert sha(review)=='875F4EA8DC6AC00494D4F88F21CB6C18EDDF825FBBAE7E0C8926463B4E2CF031'
mapping=m.read(A/'pack327-reallocation-run-map.json');download=A/f"pack327-watch-{mapping['Run']}-terminal";items=m.read(download/'artifacts.json')['artifacts'];assert len(items)==1;e=download/f"artifact-{items[0]['id']}";out=A/'pack327-reallocation-independent-published-comparison.json';assert not out.exists()
args=[sys.executable,'-B',str(review),'--repository',str(base),'--plan',str(base/'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'),'--expected-head',head,'--approved-plan-sha256','0096F13F945A7494F704C518EDFCEC766DF38167B8EE76898F53FA3077E118D9','--original',str(e/'original3'),'--sdk',str(e/'sdk-native'),'--approved-original-receipt-sha256',sha(e/'original3/receipt.json'),'--approved-sdk-receipt-sha256',sha(e/'sdk-native/sdk-receipt.json'),'--output',str(out)]
p=subprocess.run(args,capture_output=True,text=True);report={'ExitCode':p.returncode,'Arguments':args,'Stdout':p.stdout,'Stderr':p.stderr,'SourceVerifierSha256':sha(review),'ReportSha256':sha(out) if out.exists() else None,'PassiveOnlyNoNativeSdkVmExecution':True};capture=A/'pack327-reallocation-independent-published-comparison-process.json';assert not capture.exists();capture.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report));assert p.returncode==0
