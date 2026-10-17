from pathlib import Path
r=Path.cwd()
script=(r/'artifacts/root-priority351-corrected-prepare.py').read_text(encoding='utf-8')
script=script.replace("'artifacts/spawn-priority351-corrected-candidate'", "'artifacts/spawn-priority351-corrected-candidate2'")
script=script.replace("for row in json.loads(manifest.read_bytes())['Files']:", "data=json.loads(manifest.read_bytes());parents={row['Path']:row['Sha256'] for row in data.get('ParentFiles',[])}\n for row in data['Files']:\n  row={**row,'ParentSha256':row.get('ParentSha256',parents.get(row['Path']))}")
exec(compile(script,str(r/'artifacts/root-priority351-corrected-prepare2.py'),'exec'))
