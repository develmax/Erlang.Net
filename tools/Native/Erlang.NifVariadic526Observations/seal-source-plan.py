"""Seal promoted sources. No build, native loading or VM execution."""
import hashlib
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
TOOL = Path(__file__).resolve().parent
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def main():
    rows=[]
    for path in sorted(TOOL.rglob("*")):
        if path.is_file() and path.name != "source-plan.json" and "__pycache__" not in path.parts:
            rows.append({"Scope":"Tool","Path":path.relative_to(TOOL).as_posix(),"HashMode":"Raw","Sha256":sha(path)})
    groups=["tools/Native/Erlang.NifNativeBackend","tools/Runtime/Erlang.OwnedGuest506Observations","tools/Native/Erlang.DdllFormatterOriginalObservations/scripts","src/Runtime/Erlang.Runtime.Core/Native/Nif"]
    for name in groups:
        for path in sorted((ROOT/name).rglob("*")):
            if path.is_file() and path.suffix in (".cs",".csproj",".py",".c") and not any(part in ("bin","obj","__pycache__") for part in path.parts):
                rows.append({"Scope":"Repository","Path":path.relative_to(ROOT).as_posix(),"HashMode":"Raw","Sha256":sha(path)})
    baseline=json.loads((ROOT/"tools/Native/Erlang.NifMapFamily504Observations/source-plan.json").read_bytes())
    native=TOOL/"fixtures/variadic_family526.c"; erlang=TOOL/"fixtures/variadic_family526.erl"
    plan={"PinnedCommit":PIN,"Platform":"windows-x64","NativeSourcePath":native.relative_to(ROOT).as_posix(),"ErlangSourcePath":erlang.relative_to(ROOT).as_posix(),"NativeSourceSha256":sha(native),"ErlangSourceSha256":sha(erlang),"BuildScriptSha256":sha(TOOL/"build-native.py"),"Headers":baseline["Headers"],"Inputs":rows,"ExecutionApproved":False,"OriginalCollected":False,"WholeContractsClosed":0}
    (TOOL/"source-plan.json").write_bytes((json.dumps(plan,indent=2)+"\n").encode("utf-8"))
    print(sha(TOOL/"source-plan.json"))
if __name__=="__main__":
    main()
