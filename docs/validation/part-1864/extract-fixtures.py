from pathlib import Path
import textwrap,zipfile,sys
stage=Path(sys.argv[1])
out=stage/"native-fixtures"
out.mkdir()
fixtures={
 "BeamKeyFindTupleTypeChecks":"TaggedSource",
 "BeamLocalAtomTupleResultChecks":"Source"
}
with zipfile.ZipFile(stage/"source.zip") as archive:
    for name,field in fixtures.items():
        item="tests/Erlang.Tests/Compiler/Beam/"+name+".cs"
        source=archive.read(item).decode()
        fixture=textwrap.dedent(source.split(field+' = """',1)[1].split('""";',1)[0]).strip()+"\n"
        module=fixture.split("-module(",1)[1].split(").",1)[0]
        (out/(module+".erl")).write_text(fixture,encoding="utf-8")

