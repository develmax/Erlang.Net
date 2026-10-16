"""Bind a previously reviewed passive reader to actual terminal API/capture identities."""
from pathlib import Path
import json,sys,hashlib,re,ast
r=Path.cwd();kind=sys.argv[1];assert kind in ['native','reallocation'];raw=r/'artifacts/part332-remote-new-review'/kind
j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
mapping=j(raw/'run-map.json');api=j(raw/'artifacts.json')['artifacts'];assert len(api)==1
priorroot=r/'artifacts/part331-remote-new-review'/kind;oldmapping=j(priorroot/'run-map.json');oldapi=j(priorroot/'artifacts.json')['artifacts'][0]
out=r/('artifacts/part332-'+kind+'-independent');out.mkdir(exist_ok=False);prior=r/('artifacts/part331-'+kind+'-new-independent3/audit.py')
s=prior.read_text().replace('part331-remote-new-review','part332-remote-new-review').replace('artifacts/part331-'+kind+'-new-independent3','artifacts/part332-'+kind+'-independent').replace('6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d','aaf48552bee429b0447d3e28e8274d34ad590d73')
for x,y in [(oldmapping['Run'],mapping['Run']),(oldmapping['Job'],mapping['Job']),(oldapi['id'],api[0]['id'])]:s=s.replace(str(x),str(y))
s=s.replace(oldapi['digest'][7:].upper(),api[0]['digest'][7:].upper()).replace(h(priorroot/'download-manifest.json'),h(raw/'download-manifest.json'))
s=s.replace('D5959E69FADBEC0FB6D48DF356965F2212050454EA1B3B35927CBF73BEA09A5E','1DED3D93FDFD0FE001D017B9E340C0D0D9619C0FFEE1E5CFE255F5735DAFF182').replace('E3221E2A0B12DA57598BF67B00285538AD964396EB0747F254F1FE2FAAB6C6A1','8E72D62549B86793F9819B6C1D432C38F91B36D204007BEC9BD27CFE1C1EC155')
for x,y in [(1123,1128),(1148,1153),(6775,6822),(2417,2431)]:s=re.sub(r'\b'+str(x)+r'\b',str(y),s)
s=s.replace('part-331','part-332').replace('HEAD331','HEAD332')
if kind=='native':s=s.replace('PASS_ACTUAL_MAIN331_POST_FULL_WINDOWS_LEGACY_QUERY_FIVE_PHASES_AND_REALLOCATION_THREE','PASS_ACTUAL_ISOLATED332_POST_FULL_WINDOWS_NATIVE_THREE_ROLES').replace("wg['SourceFilesRehashed']","wg['SourceRowsRehashed']")
else:
 s=s.replace('part-332/final-main/full-guard.json','part-332/final-candidate/full-guard.json').replace("fullguard['IntegrationResults']==11","len(fullguard['IntegrationResults'])==11 and fullguard['InitialPipelineCompleted'] is False and fullguard['PackagingResumed'] is True")
 s=s.replace('PublishedMainFullSeparateScope=dict(Passed=6822,IntegrationResults=11)','PublishedLocalCompositeValidationScope=dict(Passed=6822,IntegrationResults=11,InitialPipelineCompleted=False,PackagingResumed=True)')
ast.parse(s);(out/'audit.py').write_bytes(s.encode());freeze=dict(Status='SOURCE_ONLY_ACTUAL332_TERMINAL_PASSIVE_READER_BEFORE_REPLAY',Head=mapping['Head'],Run=mapping['Run'],Job=mapping['Job'],Artifact=api[0]['id'],PriorReaderSha256=h(prior),ApiArtifactDigest=api[0]['digest'],ActualDownloadManifestSha256=h(raw/'download-manifest.json'),OutputDirectory=out.relative_to(r).as_posix(),HistoricalCompositeClassificationPreserved=True,Files=[dict(Path='audit.py',Bytes=len(s.encode()),Sha256=hashlib.sha256(s.encode()).hexdigest().upper())])
(out/'source-freeze.json').write_bytes((json.dumps(freeze,indent=2)+'\n').encode());print(str(out.relative_to(r)))
