import json
from pathlib import Path
import re
import sys

stage,parent,previous = map(Path,sys.argv[1:])
def read(root,name):
    return (root/name).read_text(encoding="utf-8-sig")
def write(name,text):
    (stage/name).write_text(text,encoding="utf-8",newline="\n")
def rebase(text):
    replacements={parent.as_posix():stage.as_posix(),previous.as_posix():parent.as_posix()}
    pattern=re.compile("|".join(re.escape(p) for p in sorted(replacements,key=len,reverse=True)))
    return pattern.sub(lambda match:replacements[match[0]],text)

value=read(parent,"verify-linked-runtime.py")
value=value.replace('"ct_logs","unix_telnet"}','"ct_logs","unix_telnet","mnesia_backup","mnesia_dumper"}',1)
value=value.replace('result=group("GuardedBerResult")','result=inputs/"ber-result-liveness-staged"')
value=value.replace('tagged=group("CommonTaggedEntryProjections")','tagged=inputs/"megaco-shared-entry-projections-1963"')
insert="""checked=group("CheckedResultRecord")
rel=beam_names["megaco_monitor"]
log=otp("checked-result-record",checked/"native-monitor.eval",refs/rel,previous/rel,candidate/rel,caught/"native-support")
verified["checked-result-record"]={"Cases":5,"Evidence":sha(log)}

mnesia=group("DeferredArgumentLists")
def file_scenarios(name, module, recipe, extras, count):
    rel=beam_names[module]
    values={}
    for role, origin in [("pinned",refs),("before",previous),("linked",candidate)]:
        prefix=root/(name+"-"+role)
        output=prefix.with_suffix(".terms")
        otp(name+"-"+role,recipe,origin/rel,*extras,prefix,output)
        values[role]=sha(output)
    assert len(set(values.values()))==1,(name,values)
    verified[name]={"Cases":count,"Evidence":values["linked"]}

file_scenarios("mnesia-backup","mnesia_backup",mnesia/"native-release.eval",[],4)
file_scenarios("mnesia-dumper-load","mnesia_dumper",mnesia/"native-release.eval",[],2)
file_scenarios("mnesia-dumper-dump","mnesia_dumper",mnesia/"native-dump-release.eval",[mnesia/"stub-images"],3)

mixed=group("MixedTerminalCase")
fixture=root/"mixed-identity-candidate.beam"
run("compile-mixed-identity",[sdk,stage/"frozen-payload/Erlang.Tool.dll","compile-beam",
    mixed/"beam_mixed_case_selected_error.erl",fixture])
observations=otp("mixed-identity",mixed/"native-witness.eval",fixture,mixed/"pinned-native/mixed_case_provider.beam")
assert sha(observations)==sha(mixed/"native-pinned.terms")
synthetic={"mixed-identity":{"Cases":13,"Evidence":sha(observations),"Image":sha(fixture)}}

"""
needle='receipt={"Compiler":identity["Compiler"],"Runtime":identity["Runtime"],'
assert value.count(needle)==1
value=value.replace(needle,insert+needle)
value=value.replace('"SyntheticCases":0,','"SyntheticCases":13,"SyntheticObservations":synthetic,')
assert '"mnesia_backup","mnesia_dumper"}' in value
write("verify-linked-runtime.py",value)
write("run-linked-runtime.ps1",rebase(read(parent,"run-linked-runtime.ps1")))
value=read(parent,"bind-final-native.py")
assert value.count('witness["ActualCases"]>=107 and witness["SyntheticCases"]==0')==1
write("bind-final-native.py",value.replace('witness["ActualCases"]>=107 and witness["SyntheticCases"]==0',
    'witness["ActualCases"]>=121 and witness["SyntheticCases"]==13'))
value=read(parent,"intake-1963.ps1")
value=rebase(value).replace("intake-1963","intake-1964")
write("intake-1964.ps1",value)
