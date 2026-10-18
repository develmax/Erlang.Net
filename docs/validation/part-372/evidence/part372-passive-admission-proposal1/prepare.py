from pathlib import Path
import hashlib,json,ast,shutil
O=Path(__file__).resolve().parent;R=O.parents[1]
def out(n,x):(O/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
packet=R/'artifacts/part372-original-proposal6'
cohorts=json.loads((packet/'cohorts.json').read_text())
definitions=[('ordsets',62,2,4,'Erlang.Ordsets372OriginalObservations'),('numeric',60,1,3,'Erlang.Numeric372OriginalObservations'),('usort',45,1,3,'Erlang.OrdsetsUsort372OriginalObservations')]
for name,count,groups,sources,tool in definitions:
 dest=O/name;dest.mkdir(exist_ok=True)
 base=R/('artifacts/erlang371-passive-proposal3' if name=='numeric' else 'artifacts/ordsets371-full-passive-proposal1')
 shutil.copyfile(base/'receipt_helpers.py',dest/'receipt_helpers.py')
 s=(base/'audit-provenance.py').read_text()
 s=s.replace("ROOT=Path(__file__).resolve().parents[2]", "ROOT=Path(__file__).resolve().parents[3]")
 s=s.replace('ROOT=HERE.parents[1]','ROOT=HERE.parents[2]')
 oldpath='tools/Erlang.ProplistFamilyObservations' if name=='numeric' else 'tools/Erlang.OrdsetsCoreOriginalObservations'
 s=s.replace(oldpath,'tools/'+tool)
 required=[f'Admit and parse independent {name} cohort','Clone pinned source once after successful cohort preparation','Build one pinned original installation shared by independent collectors',f'Collect independent {name} full outcomes and stacks','Run actions/upload-artifact@v7']
 own=f"    steps=jobs[0]['steps']\n    for stepname in {required!r}:\n        match=[step for step in steps if step['name']==stepname]\n        assert len(match)==1 and match[0]['status']=='completed' and match[0]['conclusion']=='success',stepname\n    outcomes=J(a.artifact/'outcomes.json')\n    assert all(outcomes[key]=='success' for key in {[name.upper()+'_PREPARATION','SOURCE','BUILD',name.upper()+'_COLLECTION']!r})\n"
 if name=='numeric':
  a=s.index('    required=[');b=s.index("    ars=J(remote/'artifacts.json')",a)
  s=s[:a]+own+s[b:]
  s=s.replace("len(completion['Groups'])==5",f"len(completion['Groups'])=={groups}")
  s=s.replace("len(parse['Fixtures'])==7",f"len(parse['Fixtures'])=={sources}").replace("len({r['Path'] for r in parse['Fixtures']})==7",f"len({{r['Path'] for r in parse['Fixtures']}})=={sources}")
  s=s.replace("a.artifact/'preflight/",f"a.artifact/'{name}-preflight/")
  s=s.replace('len(stages)==13',f'len(stages)=={3+2*groups}').replace("len({r['Pid'] for r in stages})==13",f"len({{r['Pid'] for r in stages}})=={3+2*groups}")
  s=s.replace('count==751 and profiles==3755',f'count=={count} and profiles=={count*5}')
  s=s.replace('PASS_PASSIVE_ERLANG371_PRELOADED_ORIGINAL_PROVENANCE','PASS_PASSIVE_NUMERIC372_PRELOADED_ORIGINAL_PROVENANCE')
  old="    preflights=[helper.normal_preflight(Path(p)) for p in a.preflight_receipt];assert len(preflights)==3"
  new="""    preflight=a.artifact/'numeric-preflight';gate=J(preflight/'completion.json')
    assert [x['Name'] for x in gate['Stages']]==['parse-build','parse-sources','collector-admit'] and len(a.preflight_receipt)==3
    preflights=[]
    for receipt,stage in zip(a.preflight_receipt,gate['Stages']):
        receipt=Path(receipt)
        assert receipt.parent.resolve()==preflight.resolve() and H(receipt)==stage['ReceiptSha256'] and stage['Failure'] is None
        preflights.append(helper.normal_preflight(receipt))"""
  assert old in s;s=s.replace(old,new)
 else:
  s=s.replace("run['conclusion']=='success'","run['conclusion'] in ['success','failure']")
  s=s.replace("jobs[0]['conclusion']=='success'","jobs[0]['conclusion']==run['conclusion']")
  anchor="    manifest=a.source_snapshot/TOOLPATH/'source-inputs.corrected.json';"
  assert anchor in s;s=s.replace(anchor,own+anchor)
  s=s.replace("len(profile['Fixtures'])==4",f"len(profile['Fixtures'])=={groups}")
  s=s.replace("a.artifact/'ordsets-preflight'",f"a.artifact/'{name}-preflight'")
  s=s.replace("len(parse['Fixtures'])==6",f"len(parse['Fixtures'])=={sources}").replace("len({x['Path'] for x in parse['Fixtures']})==6",f"len({{x['Path'] for x in parse['Fixtures']}})=={sources}")
  s=s.replace('len(stages)==11',f'len(stages)=={3+2*groups}').replace("len({x['Pid'] for x in stages})==11",f"len({{x['Pid'] for x in stages}})=={3+2*groups}")
  s=s.replace('count==436 and profiles==2180',f'count=={count} and profiles=={count*5}')
  s=s.replace('PASS_PASSIVE_ORDSETS371_ORDINARY_PROVENANCE',f'PASS_PASSIVE_{name.upper()}372_ORDINARY_PROVENANCE')
  s=s.replace("Head=a.expected_head,Run=a.run_id", "ParentRunConclusion=run['conclusion'],FamilyOnlyAdmission=True,ParentRunRelabelled=False,Head=a.expected_head,Run=a.run_id")
 (dest/'audit-provenance.py').write_text(s,encoding='utf-8')
for p in O.rglob('*.py'):ast.parse(p.read_text(),filename=str(p))
out('cohorts.json',cohorts)
out('reviewed-source-packet.json',{'SourcePacket':'artifacts/part372-original-proposal6','SourceSealSha256':sha(packet/'source-seal.json'),'Cohorts':[dict(c,ProfileSha256=sha(packet/c['Id']/'profile.json'),SourceInputsSha256=sha(packet/c['Id']/'source-inputs.corrected.json')) for c in cohorts['Cohorts']],'NoExpectedOutcomes':True})
print('Prepared three passive provenance successors; syntax parsed only, no native/SDK execution.')
