"""Source-only fresh materialization; Root execution after source admission. No SDK."""
from pathlib import Path
import argparse,hashlib,json,shutil,subprocess
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def load(p):return json.loads(p.read_bytes())
def safe(value):
 p=Path(value);assert not p.is_absolute() and ".." not in p.parts and ".git" not in p.parts;return p
ap=argparse.ArgumentParser();ap.add_argument("--approved-freeze-sha256",required=True);ap.add_argument("--receipt",required=True);a=ap.parse_args()
f=P/"source-freeze.json";assert h(f)==a.approved_freeze_sha256.upper()
for row in load(f)["Files"]:
 q=P/safe(row["Path"]);assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"]
v=load(P/"execution-inputs.json");assert subprocess.check_output(["git","rev-parse","HEAD"],cwd=R).decode().strip()==v["ParentHead"]
c=R/safe(v["Candidate"]);assert c.resolve().is_relative_to(R/"artifacts") and not c.exists()
out=Path(a.receipt).resolve();assert out.is_relative_to(R/"artifacts") and not out.exists()
assert len(v["Rows"])==len({x["Path"] for x in v["Rows"]})==3151
for row in v["Rows"]:
 safe(row["Path"]);q=R/safe(row["SourcePath"]);assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"],row["Path"]
c.mkdir()
for row in v["Rows"]:
 q=c/safe(row["Path"]);q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(R/row["SourcePath"],q)
for row in v["Rows"]:
 q=c/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"]
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps({"Status":"SOURCE_ONLY_MATERIALIZED354_NO_SDK","Candidate":str(c),"ExecutionFreezeSha256":h(f),"ExecutionInputsSha256":h(P/"execution-inputs.json"),"Rows":3151,"PrivateMetadataUnadmitted":True,"SdkExecuted":False,"MainChanged":False},indent=2)+"\n",encoding="utf-8")
print(h(out))
