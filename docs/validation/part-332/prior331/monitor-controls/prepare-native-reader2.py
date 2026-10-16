from pathlib import Path
import json,sys,hashlib,ast,re
r=Path.cwd();kind=sys.argv[1];assert kind in ['native','reallocation'];raw=r/'artifacts/part331-remote-new-review'/kind
mapping=json.loads((raw/'run-map.json').read_bytes());api=json.loads((raw/'artifacts.json').read_bytes())['artifacts'];assert len(api)==1
out=r/('artifacts/part331-'+kind+'-new-independent2');out.mkdir(exist_ok=False)
prior=r/('artifacts/part329-'+kind+'-new-independent')/'audit.py'
s=prior.read_text().replace('part329-remote-new-review','part331-remote-new-review').replace('part329-'+kind+'-new-independent','part331-'+kind+'-new-independent').replace('d83ed875d0c77939e20049afcb46fa297fee82dd','6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d')
oldrun,oldjob,oldartifact=(38899284241,116967248782,12045812047) if kind=='native' else (38899307374,116967324775,12045009296)
for old,new in [(oldrun,mapping['Run']),(oldjob,mapping['Job']),(oldartifact,api[0]['id'])]:s=s.replace(str(old),str(new))
oldapi=json.loads((r/'artifacts/part329-remote-new-review'/kind/'artifacts.json').read_bytes())['artifacts'][0];oldmanifest=hashlib.sha256((r/'artifacts/part329-remote-new-review'/kind/'download-manifest.json').read_bytes()).hexdigest().upper()
manifest=hashlib.sha256((raw/'download-manifest.json').read_bytes()).hexdigest().upper()
s=s.replace(oldapi['digest'][7:].upper(),api[0]['digest'][7:].upper()).replace(oldmanifest,manifest)
s=s.replace('3187D751083A34B7434E8B6C229C6D631277CBC7F98612D1888FB27ECA56D342','D5959E69FADBEC0FB6D48DF356965F2212050454EA1B3B35927CBF73BEA09A5E').replace('0A1FAE308FBFC653924ED02A99705DE8C4622D293A15197784C205FE664D778D','E3221E2A0B12DA57598BF67B00285538AD964396EB0747F254F1FE2FAAB6C6A1')
s=s.replace('part-329','part-331').replace('MAIN329','MAIN331').replace('HEAD329','HEAD331').replace('6747','6775').replace('2397','2417')
s=re.sub(r'\b1120\b','1123',s);s=re.sub(r'\b1145\b','1148',s)
ast.parse(s);(out/'audit.py').write_bytes(s.encode())
freeze=dict(Status='SOURCE_ONLY_ADAPTED_TRUSTED_PASSIVE_READER_NO_REPLAY_YET',Head=mapping['Head'],Run=mapping['Run'],Job=mapping['Job'],Artifact=api[0]['id'],PriorSource=str(prior.relative_to(r)),PriorSourceSha256=hashlib.sha256(prior.read_bytes()).hexdigest().upper(),ActualApiZipDigest=api[0]['digest'],ActualDownloadManifestSha256=manifest,Files=[dict(Path='audit.py',Bytes=len(s.encode()),Sha256=hashlib.sha256(s.encode()).hexdigest().upper())],NoDownloadedCodeExecution=True)
(out/'source-freeze.json').write_bytes((json.dumps(freeze,indent=2)+'\n').encode());print(str(out.relative_to(r)))
