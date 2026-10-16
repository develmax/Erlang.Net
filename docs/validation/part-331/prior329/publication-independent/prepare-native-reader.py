from pathlib import Path
import json,sys,hashlib,ast
r=Path.cwd();kind=sys.argv[1];assert kind in ['native','reallocation'];raw=r/'artifacts/part329-remote-new-review'/kind
mapping=json.loads((raw/'run-map.json').read_bytes());api=json.loads((raw/'artifacts.json').read_bytes())['artifacts'];assert len(api)==1
out=r/('artifacts/part329-'+kind+'-new-independent');out.mkdir(exist_ok=False)
prior=r/('artifacts/part328-'+kind+'-allocator309-independent')/('audit.py' if kind=='native' else 'audit3.py')
s=prior.read_text().replace('part328-remote-beam310','part329-remote-new-review').replace('part328-'+kind+'-allocator309-independent','part329-'+kind+'-new-independent').replace('bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612','d83ed875d0c77939e20049afcb46fa297fee82dd')
oldrun,oldjob,oldartifact=(38865264886,116856512555,12030958282) if kind=='native' else (38865272148,116856534998,12030614599)
for old,new in [(oldrun,mapping['Run']),(oldjob,mapping['Job']),(oldartifact,api[0]['id'])]:s=s.replace(str(old),str(new))
oldzip='FB201C7AF70847C1591080ECB6FAAD4FBBB59BE533F8110C1B17062779C4D2D2' if kind=='native' else 'CAC770F15FF2275EC35D972EB1577CF76A5E602DAB0079EA8E3491699EC15650'
s=s.replace(oldzip,api[0]['digest'][7:].upper()).replace('97C12C85FF98EC3D98C33DD692F2F2474A14FA4150B4F39D0408434773FAD649','3187D751083A34B7434E8B6C229C6D631277CBC7F98612D1888FB27ECA56D342').replace('F835320BE11A28B4C1BDA71C962CFA5A8789FE49C1404FA54CA1E326EE9D9229','0A1FAE308FBFC653924ED02A99705DE8C4622D293A15197784C205FE664D778D')
manifest=hashlib.sha256((raw/'download-manifest.json').read_bytes()).hexdigest().upper()
s=s.replace('8A99E951794B203E341FCDA10D199A0BF3AD17717AD0FBEA0A93499C1244D8CA',manifest)
s=s.replace('part-328','part-329').replace('MAIN328','MAIN329').replace('HEAD328','HEAD329').replace('6738','6747').replace('2393','2397')
ast.parse(s);(out/'audit.py').write_bytes(s.encode())
freeze=dict(Status='SOURCE_ONLY_ADAPTED_TRUSTED_PASSIVE_READER_NO_REPLAY_YET',Head=mapping['Head'],Run=mapping['Run'],Job=mapping['Job'],Artifact=api[0]['id'],PriorSource=str(prior.relative_to(r)),PriorSourceSha256=hashlib.sha256(prior.read_bytes()).hexdigest().upper(),ActualApiZipDigest=api[0]['digest'],ActualDownloadManifestSha256=manifest,Files=[dict(Path='audit.py',Bytes=len(s.encode()),Sha256=hashlib.sha256(s.encode()).hexdigest().upper())],NoDownloadedCodeExecution=True)
(out/'source-freeze.json').write_bytes((json.dumps(freeze,indent=2)+'\n').encode());print(str(out.relative_to(r)))
