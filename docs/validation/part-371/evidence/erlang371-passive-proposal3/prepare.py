"""Prepare passive, parameterized recipes only. No native or managed execution."""
from pathlib import Path
import ast,hashlib,json,shutil
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
BASE=ROOT/'artifacts/erlang370-closure-admission-proposal1'
def main():
    shutil.copyfile(BASE/'receipt_helpers.py',HERE/'receipt_helpers.py')
    s=(BASE/'audit-provenance.py').read_text()
    s=s.replace("HEAD='20001454356011e318f2dea6b3d7ed8b8d569f90';RUN=39108122243;PIN=", "HEAD=None;RUN=None;PIN=")
    s=s.replace("TOOL=ROOT/'artifacts/erlang370-closure-original-proposal2/tool'","TOOL=None")
    s=s.replace('def main():\n','def main():\n    global HEAD,RUN,TOOL\n')
    s=s.replace('a=p.parse_args()',"p.add_argument('--expected-head',required=True);p.add_argument('--run-id',type=int,required=True);p.add_argument('--manifest-sha256',required=True);p.add_argument('--tool',type=Path,required=True);a=p.parse_args();HEAD=a.expected_head;RUN=a.run_id;TOOL=a.tool.resolve()")
    s=s.replace("=='0258DDF97CE3940C7C07AD129747726CFB8263F6F196D9369A1EDCD4419A1182'","==a.manifest_sha256.upper()")
    s=s.replace("len(completion['Groups'])==4","len(completion['Groups'])==5")
    s=s.replace("len(parse['Fixtures'])==6","len(parse['Fixtures'])==7").replace("len({r['Path'] for r in parse['Fixtures']})==6","len({r['Path'] for r in parse['Fixtures']})==7")
    s=s.replace('len(stages)==11','len(stages)==13').replace("len({r['Pid'] for r in stages})==11","len({r['Pid'] for r in stages})==13").replace('count==44 and profiles==220','count==751 and profiles==3755')
    s=s.replace('count=profiles=0;groups=[]','count=profiles=0;groups=[];negative_controls=[]')
    anchor="admission=preloaded.validate(md,inputs,runtime,fixture,ebin,pid,profile['Mfas']);assert admission==group['MetadataAdmission']"
    insert="""
        for key,bad in [('module_md5',('binary',bytes(16))),('schedulers',('integer',1)),('names',('list',()))]:
            corrupt=('map',frozenset((k,bad if k==('atom',key) else v) for k,v in md[1]))
            refused=False
            try:preloaded.validate(corrupt,inputs,runtime,fixture,ebin,pid,profile['Mfas'])
            except AssertionError:refused=True
            assert refused,(fixture['Id'],key)
            negative_controls.append(dict(Group=fixture['Id'],Corruption=key,Rejected=True))
"""
    assert anchor in s;s=s.replace(anchor,anchor+insert)
    s=s.replace("Status='PASS_PASSIVE_ERLANG370_PRELOADED_ORIGINAL_PROVENANCE'","Status='PASS_PASSIVE_ERLANG371_PRELOADED_ORIGINAL_PROVENANCE',MetadataCorruptionControls=negative_controls")
    (HERE/'audit-provenance.py').write_text(s,encoding='utf-8')
    s=(BASE/'audit-source.py').read_text().replace('HEAD = "20001454356011e318f2dea6b3d7ed8b8d569f90"','HEAD = None').replace('TOOL = ROOT / "artifacts/erlang370-closure-original-proposal2/tool"','TOOL = None')
    s=s.replace('a=p.parse_args()',"p.add_argument('--expected-head',required=True);p.add_argument('--tool',type=Path,required=True);p.add_argument('--bindings',type=Path,required=True);a=p.parse_args()")
    s=s.replace('global SOURCE,OUT','global SOURCE,OUT,HEAD,TOOL\n    HEAD=a.expected_head;TOOL=a.tool.resolve()')
    s=s.replace('json.loads((ROOT / "artifacts/erlang-original370-closure1/ci/dispatch-intent.json").read_bytes())','json.loads(a.bindings.read_bytes())')
    (HERE/'audit-source.py').write_text(s,encoding='utf-8')
    for p in HERE.glob('*.py'):ast.parse(p.read_text(),filename=str(p))
    print('Prepared source-only parameterized validators')
if __name__=='__main__':main()
