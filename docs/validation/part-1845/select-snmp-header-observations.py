import json,pathlib,sys
release, report, selection, output=map(pathlib.Path,sys.argv[1:])
oracle={}
for row in selection.read_text(encoding="utf-8-sig").splitlines():
    module,path=row.split(" ",1)
    if "/../misc/" in path or module=="snmp_note_store":
        oracle[module]=path
modules={pathlib.Path(r["Beam"]).stem:r for r in json.loads(report.read_text(encoding="utf-8-sig"))["Modules"]}
rows=[]
for module in sorted(oracle):
    row=modules[module]
    beam=pathlib.Path(row["Beam"])
    assert row["Status"] in {"Equal","Different"}
    expected=release/beam
    generated=(release if row["Status"]=="Equal" else report.with_suffix(".json.generated"))/beam
    if row["Status"] == "Different" and not generated.is_file():
        matches = [
            shard / "otp-corpus.json.generated" / beam
            for shard in report.parent.glob("shard-*")
            if (shard / "otp-corpus.json.generated" / beam).is_file()
        ]
        assert len(matches) == 1, beam
        generated = matches[0]
    assert expected.is_file() and generated.is_file()
    rows.append(f"{expected}\t{generated}")
output.write_bytes(("\n".join(rows)+"\n").encode("utf-8"))
print(f"{len(rows)} paired SNMP header observations")
