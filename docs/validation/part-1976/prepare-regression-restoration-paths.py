from pathlib import Path
import json, sys
stage,prior,release=map(Path,sys.argv[1:])
sources={r.split("\t")[0]:r.split("\t")[1].strip() for r in
         (prior/"all-final-label-paths.tsv").read_text(encoding="utf-8-sig").splitlines()}
report=json.loads((stage/"pilot.json").read_text(encoding="utf-8-sig"))
current={row["Beam"].replace(chr(92),"/"):row["Status"] for row in report["Modules"]}
witnesses=[("erl_parse","yeccgoto_zc_exprs",7,"normalized"),
           ("crashdump_viewer","find_truncated_proc",1,"frame"),
           ("ets","count_mandatory",1,"frame")]
output=[]
for module,name,arity,metric in witnesses:
    keys=[rel for rel in current if Path(rel).stem==module]
    assert len(keys)==1,(module,keys)
    key=keys[0]
    pinned=release/key
    before=Path(sources[key])
    after=pinned if current[key]=="Equal" else stage/"pilot.json.generated"/key
    assert all(p.is_file() for p in [pinned,before,after]),key
    output.append("\t".join(map(str,[module,name,arity,metric,pinned,before,after])))
(stage/"regression-restoration-paths.tsv").write_text("\n".join(output)+"\n",encoding="utf-8",newline="\n")
