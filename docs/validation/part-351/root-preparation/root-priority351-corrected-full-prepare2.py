from pathlib import Path
r=Path.cwd()
s=(r/'artifacts/root-priority351-corrected-full-prepare.py').read_text(encoding='utf-8')
s=s.replace("'artifacts/spawn-priority351-full-corrected-candidate'","'artifacts/spawn-priority351-full-corrected-candidate2'").replace("'artifacts/spawn-priority351-full-source3'","'artifacts/spawn-priority351-full-source4'")
s=s.replace('priority351-corrected-metadata-proposal/copy-plan.json','priority351-corrected-metadata-proposal2/copy-plan.json').replace('459B6844636AC0FB8A560F8F2A54AC7A1630B8D598936D2F0768424485F18B74','8D3D69257D5A253D93B85E7889ED64DC2E070759D12D47B655D5EABBF32AA763')
# The frozen old runner contains its original candidate literal. Adapt it to this
# new candidate after the first script's candidate replacement has been applied.
s=s.replace("runner=(oldseed/'run-full.py').read_text(encoding='utf-8').replace(\"r/'artifacts/spawn-priority351-full-candidate'\",\"r/'artifacts/spawn-priority351-full-corrected-candidate2'\")", "runner=(oldseed/'run-full.py').read_text(encoding='utf-8').replace(\"r/'artifacts/spawn-priority351-full-candidate'\",\"r/'artifacts/spawn-priority351-full-corrected-candidate2'\")")
exec(compile(s,str(r/'artifacts/root-priority351-corrected-full-prepare2.py'),'exec'))
