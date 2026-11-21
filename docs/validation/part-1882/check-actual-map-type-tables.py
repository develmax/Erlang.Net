import hashlib
import importlib.util
import json
import sys
from pathlib import Path

stage, inventory, audit, release = map(Path,sys.argv[1:])
spec=importlib.util.spec_from_file_location("inventory", inventory)
read=importlib.util.module_from_spec(spec)
spec.loader.exec_module(read)
known = {"asn1ct_eval_ext", "typer", "httpd_logger", "gen_tcp", "gen_udp", "tls_client_connection", "tls_server_connection", "inet_sctp", "supervisor_bridge"}
scoped = json.loads((stage/"pilot.json").read_bytes())
prior = {str(Path(row[0])):row for row in json.loads((audit/"type-residuals.json").read_bytes())}
digest=lambda b:hashlib.sha256(b).hexdigest()
rows=[]
for row in scoped["Modules"]:
    path=Path(row["Beam"])
    original=(release/path).read_bytes()
    image=(stage/"pilot.json.generated"/path).read_bytes() if row["Status"]=="Different" else original
    ref=read.chunks(original).get("Type")
    current=read.chunks(image).get("Type")
    old=prior.get(str(path))
    before=read.chunks(Path(old[-2]).read_bytes()).get("Type") if old else None
    if old and digest(Path(old[-1]).read_bytes()) != digest(original):
        raise ValueError(f"Reference digest changed: {path}")
    rows.append(dict(Beam=row["Beam"],ExpectedOnlyMap=path.stem in known,TypeEqual=current==ref,
                     BeforeTypeEqual=(before==ref if old else None),ReferenceHash=digest(ref) if ref else None,
                     GeneratedTypeHash=digest(current) if current else None))
assert sum(r["ExpectedOnlyMap"] for r in rows)==len(known)
result=dict(Count=len(rows),FinalTypeEqual=sum(r["TypeEqual"] for r in rows),
            PriorKnownTypeGain=sum(r["TypeEqual"] and r["BeforeTypeEqual"] is False for r in rows),
            PriorKnownTypeLoss=sum(not r["TypeEqual"] and r["BeforeTypeEqual"] is True for r in rows),
            PromisedNearGroup=[r for r in rows if r["ExpectedOnlyMap"]],Rows=rows)
(stage/"map-type-table-results.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
print(json.dumps({k:v for k,v in result.items() if k not in {"Rows","PromisedNearGroup"}}))
print([(Path(r["Beam"]).stem,r["TypeEqual"]) for r in result["PromisedNearGroup"]])

