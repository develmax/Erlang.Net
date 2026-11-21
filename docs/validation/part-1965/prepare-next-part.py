from pathlib import Path
import hashlib,json,re,sys

stage,parent,old,inputs=map(Path,sys.argv[1:])
def get(name):
    return (parent/name).read_text(encoding="utf-8-sig")
def save(name,text):
    (stage/name).write_text(text,encoding="utf-8",newline="\n")
def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
def rebase(text):
    mapping={parent.as_posix():stage.as_posix(),old.as_posix():parent.as_posix()}
    pattern=re.compile("|".join(re.escape(p) for p in sorted(mapping,key=len,reverse=True)))
    return pattern.sub(lambda match:mapping[match[0]],text)

parts=inputs/"megaco-fallthrough-and-heap-1965"
split=["fallthrough-return-verified.patch","checked-projection-heap-verified.patch"]
replay={p:sha(parts/p) for p in split}
dest=stage/"combined-return-and-heap-production.patch"
dest.write_bytes(b"".join((parts/p).read_bytes() for p in split))
save("combined-return-and-heap-inputs.json",json.dumps(replay,indent=2)+"\n")

groups=[
 ("SharedReturnAndHeap",
  parts, [dest.as_posix(),"tests-verified.patch"], "first/scope.json",
  ["receipt.md","receipt-hashes.json","first/scope-delta.json","first/scope-result.json","first/independent-measurements.terms"]),
 ("LegacyProjectedArguments",
  inputs/"megaco-legacy-apply-fields-1966", ["production-verified.patch","tests-verified.patch"],"first/scope.json",
  ["verified-receipt.md","verified-receipt-hashes.json","first/scope-delta.json","first/module-residual.terms"]),
 ("ConstructedArgumentSlotKills",
  inputs/"constructed-call-slot-liveness-draft/validation-narrow", ["production.patch","test.patch"],"scope-candidate.json",
  ["RESULT.md","receipt.json","scope-delta.json","scope-candidate.json.timings.json","native-nonempty-load.eval"]),
]
spec={}
for name,root,patch_sources,scope,evidence in groups:
    patches=[root/p for p in patch_sources]
    assert all(f.is_file() for f in patches),name
    have=[p for p in evidence if (root/p).is_file()]
    spec[name]=dict(Root=root.as_posix(),Patches=patch_sources,SHA=[sha(p) for p in patches],Report=scope,Evidence=have)
    print(name,len(have),len(evidence))
save("components.json",json.dumps(spec,indent=2)+"\n")

prep=get("prepare-1964-linked.py").replace(
    '"mnesia_backup","mnesia_dumper",',
    '"mnesia_backup","mnesia_dumper","megaco_digit_map","snmpa_mib_storage_ets",')
prep=prep.replace('value.replace("Part 1963","Part 1964")','value.replace("Part 1964","Part 1965")')
prep=prep.replace('value.replace("record-and-archive-1963.py","record-and-archive-1964.py")',
                  'value.replace("record-and-archive-1964.py","record-and-archive-1965.py")')
start=prep.index("        assert len(names)==1 and names[0] not in base_cases|set(newcases),")
end=prep.index("    manifest.append(",start)
prep=prep[:start]+r"""        assert names and len(names)==len(set(names)) and not set(names).intersection(base_cases|set(newcases)),(tag,names)
        registrations=re.findall(r'Test\(\s*(Erlang\.Tests\.[\w.]+\.\w*TestName),\s*(Erlang\.Tests\.[\w.]+\.\w+)\s*\);',added)
        assert len(registrations)==len(names),(tag,registrations)
        newcases+=names
        for name, callback in registrations:
            assert name.rsplit(".",1)[0]==callback.rsplit(".",1)[0],(name,callback)
            reg.append(f"Test({name}, {callback});")
            method=callback.rsplit(".",1)[1]
            if f"internal static void {method}(" in added: sync.append(callback)
"""+prep[end:]
prep=prep.replace("reg.append(f\"Test({name}, {callback});\")","reg.append(f\"Test({name}, {callback});\")")
save("prepare-1965-linked.py",prep)
value=get("verify-linked-runtime.py").replace(
    'components=data(stage/"components.json")','components={**data(parent/"components.json"),**data(stage/"components.json")}')
insert="""shared=group("SharedReturnAndHeap")
rel=beam_names["megaco_monitor"]
log=otp("shared-return-cancel",shared/"native-monitor.eval",refs/rel,previous/rel,candidate/rel,caught/"native-support")
verified["shared-return-cancel"]={"Cases":3,"Evidence":sha(log)}

slot_group=group("ConstructedArgumentSlotKills")
file_scenarios("mnesia-dumper-nonempty-load","mnesia_dumper",slot_group/"native-nonempty-load.eval",[],1)

"""
needle='receipt={"Compiler":identity["Compiler"],"Runtime":identity["Runtime"],'
assert value.count(needle)==1
save("verify-linked-runtime.py",value.replace(needle,insert+needle))
save("bind-final-native.py",get("bind-final-native.py").replace('witness["ActualCases"]>=142','witness["ActualCases"]>=146'))
save("run-linked-runtime.ps1",rebase(get("run-linked-runtime.ps1")))
save("intake-1965.ps1",rebase(get("intake-1964.ps1")).replace("intake-1964","intake-1965"))
