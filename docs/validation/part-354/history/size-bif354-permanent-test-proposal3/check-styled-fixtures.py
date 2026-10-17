from pathlib import Path
import argparse,hashlib,json,re
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent;h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
a=argparse.ArgumentParser();a.add_argument("--view",required=True);a.add_argument("--output",required=True);args=a.parse_args();view=Path(args.view).resolve();out=Path(args.output).resolve();assert view.is_relative_to(R/"artifacts") and out.is_relative_to(R/"artifacts") and not out.exists()
text=(view/"tools/Erlang.Differential/SizeErrorCaseSources.cs").read_bytes().decode("utf-8");seen={}
for match in re.finditer(r'public const string (\w+) = """\r?\n(.*?)(^[ \t]*)""";',text,re.M|re.S):
 name,body,indent=match.groups();assert body.endswith("\n");body=body[:-2] if body.endswith("\r\n") else body[:-1]
 value="".join(line[len(indent):] if line.startswith(indent) else line for line in body.splitlines(True));seen[name]=value.encode()
manifest=json.loads((P/"fixtures.json").read_text());assert len(seen)==len(manifest["Cases"])==23
for row in manifest["Cases"]:
 name="".join(word.title() for word in row["Module"].split("_"));assert hashlib.sha256(seen[name]).hexdigest().upper()==row["Sha256"],row["Module"]
rows=[{"Path":p.relative_to(view).as_posix(),"Bytes":p.stat().st_size,"Sha256":h(p)} for p in sorted(view.rglob("*.cs"))];assert len(rows)==50
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps({"Status":"STYLED23_PORTABLE_SOURCE_BYTES_EXACT_REQUIRES_CSHARP_TOKEN_PEER","FixtureCases":23,"Files":rows,"NoSdkByReader":True,"NoFullAdmission":True},indent=2)+"\n",encoding="utf-8");print(h(out))
