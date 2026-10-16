from pathlib import Path
r=Path.cwd();p=r/'artifacts/part331-remote-new-review/prepare-native-reader.py';s=(r/'artifacts/part329-new-publication-review/prepare-native-reader.py').read_text().replace('part329-remote-new-review','part331-remote-new-review').replace("'artifacts/part329-'+kind+'-new-independent'","'artifacts/part331-'+kind+'-new-independent'")
s=s.replace("prior=r/('artifacts/part328-'+kind+'-allocator309-independent')/('audit.py' if kind=='native' else 'audit3.py')","prior=r/('artifacts/part329-'+kind+'-new-independent')/'audit.py'")
s=s.replace(".replace('part328-remote-beam310','part331-remote-new-review').replace('part328-'+kind+'-allocator309-independent','part329-'+kind+'-new-independent').replace('bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612','d83ed875d0c77939e20049afcb46fa297fee82dd')",".replace('part329-remote-new-review','part331-remote-new-review').replace('part329-'+kind+'-new-independent','part331-'+kind+'-new-independent').replace('d83ed875d0c77939e20049afcb46fa297fee82dd','6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d')")
start=s.index('oldrun,oldjob,oldartifact=');end=s.index('ast.parse(s)',start)
s=s[:start]+'''oldrun,oldjob,oldartifact=(38899284241,116967248782,12045812047) if kind=='native' else (38899307374,116967324775,12045009296)
for old,new in [(oldrun,mapping['Run']),(oldjob,mapping['Job']),(oldartifact,api[0]['id'])]:s=s.replace(str(old),str(new))
oldapi=json.loads((r/'artifacts/part329-remote-new-review'/kind/'artifacts.json').read_bytes())['artifacts'][0];oldmanifest=hashlib.sha256((r/'artifacts/part329-remote-new-review'/kind/'download-manifest.json').read_bytes()).hexdigest().upper()
manifest=hashlib.sha256((raw/'download-manifest.json').read_bytes()).hexdigest().upper()
s=s.replace(oldapi['digest'][7:].upper(),api[0]['digest'][7:].upper()).replace(oldmanifest,manifest)
s=s.replace('3187D751083A34B7434E8B6C229C6D631277CBC7F98612D1888FB27ECA56D342','D5959E69FADBEC0FB6D48DF356965F2212050454EA1B3B35927CBF73BEA09A5E').replace('0A1FAE308FBFC653924ED02A99705DE8C4622D293A15197784C205FE664D778D','E3221E2A0B12DA57598BF67B00285538AD964396EB0747F254F1FE2FAAB6C6A1')
s=s.replace('part-329','part-331').replace('MAIN329','MAIN331').replace('HEAD329','HEAD331').replace('6747','6775').replace('2397','2417').replace('1120','1123').replace('1145','1148')
'''+s[end:]
p.write_bytes(s.encode());print('331 native/reallocation passive reader adaptation prepared; awaiting actual terminal artifacts')
