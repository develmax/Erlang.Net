from pathlib import Path
import argparse,hashlib,json,shutil
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent;h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();rd=lambda p:json.loads(p.read_bytes())
a=argparse.ArgumentParser();a.add_argument("--approved-freeze-sha256",required=True);args=a.parse_args();f=P/"source-freeze.json";assert h(f)==args.approved_freeze_sha256.upper()
for row in rd(f)["Files"]:
 q=P/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"]
v=rd(P/"style-inputs.json");dest=R/v["Destination"];assert dest.is_relative_to(R/"artifacts") and not dest.exists()
for row in v["Files"]:
 q=R/row["SourcePath"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"]
for directory in ("src","tools","tests","benchmarks","examples"):(dest/directory).mkdir(parents=True,exist_ok=True)
for row in v["Files"]:
 target=dest/row["Path"];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(R/row["SourcePath"],target)
(P/"style-materialization.json").write_text(json.dumps({"Status":"SOURCE_ONLY_OWNED_STYLE_VIEW_MATERIALIZED","Destination":str(dest),"Files":len(v["Files"]),"SourceFreezeSha256":h(f),"SdkOrStyleExecutedByMaterializer":False,"MainChanged":False},indent=2)+"\n",encoding="utf-8")
print(dest)
