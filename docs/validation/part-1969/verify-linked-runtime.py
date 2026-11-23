from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import sys
import time

stage,parent,refs,inputs,archive,capture=map(Path,sys.argv[1:])
root=stage/"linked-runtime-witnesses"
root.mkdir()
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def data(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))
def wait(path,seconds):
    deadline=time.monotonic()+seconds
    while not path.is_file() or not path.stat().st_size:
        if time.monotonic()>deadline:
            raise TimeoutError(path)
        time.sleep(4)
def run(name,args,**env):
    stdout=root/(name+".stdout.log")
    with stdout.open("wb") as out, (root/(name+".stderr.log")).open("wb") as err:
        subprocess.run([str(arg) for arg in args],cwd=root,stdout=out,stderr=err,check=True,timeout=400,
                       env={**os.environ,"DOTNET_PROCESSOR_COUNT":"1","ERL_CRASH_DUMP":str(root/(name+"-crash.dump")),**env})
    return stdout
def otp(name,recipe,*args):
    return run(name,[refs/"bin/erl.exe","+S","1:1","+A","1","-noshell","-eval",recipe.read_text(encoding="utf-8-sig"),"-extra",*args])
wait(stage/"pilot-receipt.json",2300)
wait(parent/"full-uncached/timing.json",2300)
wanted={"OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","ex_gauge","ct_default_gl" ,"ex_listCtrl","megaco_monitor","ct_logs","unix_telnet","mnesia_backup","mnesia_dumper","megaco_digit_map","snmpa_mib_storage_ets","ssh_dbg","asn1ct_gen_ber_bin_v2"}
wanted.update({"application_master","megaco_flex_scanner","observer_html_lib","shell",
    "beam_a","beam_asm","epp_dodger","beam_ssa_destructive_update","edoc_html_to_markdown","observer_wx","ssl_pkix_db"})
wanted.update({"beam_ssa_pre_codegen","eunit_data","cdv_multi_wx","ssh_transport",
    "xmerl_sax_parser_latin1","xmerl_sax_parser_list","xmerl_sax_parser_utf16be",
    "xmerl_sax_parser_utf16le","xmerl_sax_parser_utf8",
    "edlin_type_suggestion","test_server_ctrl"})
found={Path(row["Beam"].replace(chr(92),"/")).stem:row for row in data(stage/"pilot.json")["Modules"]}
assert wanted<=found.keys(),wanted-found.keys()
beam_names={name:found[name]["Beam"].replace(chr(92),"/") for name in sorted(wanted)}
baseline={row["Beam"].replace(chr(92),"/"):row["Status"] for row in data(parent/"full-uncached/otp-corpus.json")["Modules"]}
identity=data(stage/"focused-identity.json")
payload=stage/"corpus-bin"
assert all(sha(payload/f"Erlang.{name}.dll")==identity[name] for name in ("Runtime","Compiler"))
candidate=root/"candidate"
previous=root/"previous"
exact=[beam_names[name] for name in wanted if found[name]["Status"]=="Equal"]
sdk=inputs/"sdk/dotnet.exe"
if exact:
    tool=root/"capture"
    shutil.copytree(capture,tool)
    for name in ("Compiler","Runtime"):
        shutil.copyfile(payload/f"Erlang.{name}.dll",tool/f"Erlang.{name}.dll")
    manifest=tool/"modules.txt"
    manifest.write_text("\n".join(sorted(exact))+"\n",encoding="utf-8")
    temp=tool/"temp"
    temp.mkdir()
    report=tool/"compiled.json"
    run("capture-exact",[sdk,tool/"Erlang.BeamCorpus.dll",refs,report,inputs/"public-key-generated",
        inputs/"pinned-inputs-full","--modules-file",manifest,"--jobs","1"],
        TEMP=str(temp),TMP=str(temp),ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES="1")
    assert data(report)["Counts"]=={"Equal":len(exact)}
for rel in beam_names.values():
    output=candidate/rel
    output.parent.mkdir(parents=True,exist_ok=True)
    source=(tool/"compiled.json.generated"/rel) if rel in exact else stage/"pilot.json.generated"/rel
    if rel in exact:
        assert sha(source)==sha(refs/rel)
    os.link(source,output)
    dest=previous/rel
    dest.parent.mkdir(parents=True,exist_ok=True)
    old=[refs/rel] if baseline[rel]=="Equal" else list((parent/"full-uncached").glob("shard-*/otp-corpus.json.generated/"+rel))
    assert len(old)==1,(rel,old)
    os.link(old[0],dest)

components={**data(Path("D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/scheduled-constructions-and-owned-result-roots-1964-linked")/"components.json"),**data(Path("D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/shared-returns-and-initialized-call-roots-1965-linked")/"components.json"),**data(parent/"components.json"),**data(stage/"components.json")}
group=lambda name:Path(components[name]["Root"])
verified={}
def three(name,scope,recipe,count):
    rel=beam_names[scope]
    log=otp(name,recipe,refs/rel,previous/rel,candidate/rel)
    verified[name]={"Cases":count,"Evidence":sha(log)}
def roles(name,module,recipe,extra,count):
    values={}
    for role,origin in [("pinned",refs),("before",previous),("linked",candidate)]:
        out=root/(name+"-"+role+".terms")
        otp(name+"-"+role,recipe,origin/beam_names[module],*extra,out)
        values[role]=sha(out)
    assert len(set(values.values()))==1,(name,values)
    verified[name]={"Cases":count,"Evidence":values["linked"]}

ber=inputs/"asn-named-bits-replay-1931/ber-directive-seed-effect-1959"
log=otp("directive-seed",ber/"native-ber-directive-seed-effect.eval",refs,previous,candidate)
assert "12 cases, baseline 12, candidate 12" in log.read_text(encoding="utf-8")
verified["directive-seed"]={"Cases":12,"Evidence":sha(log)}
integer=inputs/"asn-named-bits-replay-1931/closed-recursive-integer-1958-recovered"
retry=otp("recursive-entry-retry",integer/"native-ct-retry.eval",refs,previous,candidate,beam_names["ct_default_gl"])
assert retry.read_text(encoding="utf-8").count("[clean_start,clean_stop,old_owner_killed,retry_start,retry_stop]")==3
verified["recursive-entry-retry"]={"Cases":5,"Evidence":sha(retry)}

mixed=inputs/"branched-map-update-lifetime-1957-aliases-early"
roles("mixed-entry","ct_default_gl",mixed/"native-actual.eval",[],5)
gauge=inputs/"mixed-selected-cold-layout-2011"
for kind,count in [("event",11),("toggle",3),("undetermined",3)]:
    roles("gauge-"+kind,"ex_gauge",gauge/f"native-{kind}.eval",[gauge/"native-stubs"],count)
wx=inputs/"pending-wx-gauge-initializers-2002"
roles("gauge-initialization","ex_gauge",wx/"init-native.eval",[wx/"native"],2)
list_ctrl=inputs/"dying-slot-call-result-copy-2004"
roles("list-initialization","ex_listCtrl",list_ctrl/"list-native.eval",[list_ctrl/"list-wx"],1)
verified["list-initialization"]["OrderedInsertions"]=51
three("protected-tuples","megaco_monitor",(inputs/"megaco-monitor-audit-1961/native-monitor.eval"),9)
three("relocated-error-lines","megaco_monitor",(inputs/"final-line-first-use-1961/native-megaco.eval"),2)

single=inputs/"listctrl-checked-callback-entry-2012"
roles("checked-list-callback","ex_listCtrl",single/"native-event.eval",[single/"stub-images"],7)
caught=inputs/"megaco-caught-branch-frame-1962"
m=beam_names["megaco_monitor"]
caught_log=otp("caught-branch",caught/"native-megaco-pinned-stack.eval",refs/m,previous/m,candidate/m,caught/"native-support")
verified["caught-branch"]={"Cases":5,"Evidence":sha(caught_log)}

common=inputs/"case-arm-proper-list-staged/ct-native"
ct_outputs={}
for role,origin in [("pinned",refs),("before",previous),("linked",candidate)]:
    jt=root/("common-test-"+role)
    jt.mkdir()
    suite=jt/"suite"
    suite.mkdir()
    shutil.copyfile(common/"case_arm_ct_SUITE.erl",suite/"case_arm_ct_SUITE.erl")
    output=jt/"log"
    output.mkdir()
    log=otp("common-test-"+role,common/"run.eval",origin/beam_names["ct_logs"],suite,output)
    verdict=[line for line in log.read_text(encoding="utf-8-sig").splitlines() if line.startswith("RESULT ")]
    assert len(verdict)==1 and "{1,0,{0,0}}" in verdict[0],role
    subrun,=[p for p in output.iterdir() if p.name.startswith("ct_run.")]
    assets={f.name:sha(f) for f in output.iterdir() if f.suffix in (".css",".js")}
    assert len(assets)==4 and all(sha(subrun/name)==digest for name,digest in assets.items())
    ct_outputs[role]={"Result":verdict[0],"Assets":assets}
assert all(r==ct_outputs["pinned"] for r in ct_outputs.values())
ct_file=root/"common-test-assets.json"
ct_file.write_text(json.dumps(ct_outputs,indent=2)+"\n",encoding="utf-8")
verified["common-test-proper-list"]={"Cases":1,"Evidence":sha(ct_file)}

telnet=Path(r"D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/unused-case-list-bindings-1961")
rel=beam_names["unix_telnet"]
log=otp("unix-telnet-list-patterns",telnet/"telnet-native.eval",telnet/"telnet-stubs",refs/rel,previous/rel,candidate/rel)
assert len(log.read_text(encoding="utf-8-sig").splitlines())==8
verified["unix-telnet-list-patterns"]={"Cases":8,"Evidence":sha(log)}

result=inputs/"ber-result-liveness-staged"
log=otp("guarded-ber-result",result/"release-native.eval",refs,previous,candidate)
assert "cases 24, pinned matches baseline 24, candidate 24, baseline versus candidate true" in log.read_text(encoding="utf-8")
verified["guarded-ber-result"]={"Cases":24,"Evidence":sha(log)}

tagged=inputs/"megaco-shared-entry-projections-1963"
rel=beam_names["megaco_monitor"]
log=otp("shared-tagged-entries",tagged/"native-megaco.eval",refs/rel,previous/rel,candidate/rel,caught/"native-support")
verified["shared-tagged-entries"]={"Cases":9,"Evidence":sha(log)}

checked=group("CheckedResultRecord")
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

recipe=inputs/"snmp-ets-audit-1961/native.eval"
rel=beam_names["snmpa_mib_storage_ets"]
for role, image in [("before",previous/rel),("linked",candidate/rel)]:
    log=otp("snmp-ets-root-"+role,recipe,refs/rel,image)
    assert "16/16 exact values and selected stack frames" in log.read_text(encoding="utf-8")
verified["snmp-ets-root"]={"Cases":16,"Evidence":sha(log)}

mixed=group("MixedTerminalCase")
three("mixed-terminal-release","megaco_digit_map",mixed/"native-megaco-digit-map.eval",5)
fixture=root/"mixed-identity-candidate.beam"
run("compile-mixed-identity",[sdk,stage/"frozen-payload/Erlang.Tool.dll","compile-beam",
    mixed/"beam_mixed_case_selected_error.erl",fixture])
observations=otp("mixed-identity",mixed/"native-witness.eval",fixture,mixed/"pinned-native/mixed_case_provider.beam")
assert sha(observations)==sha(mixed/"native-pinned.terms")
synthetic={"mixed-identity":{"Cases":13,"Evidence":sha(observations),"Image":sha(fixture)}}

shared=group("SharedReturnAndHeap")
rel=beam_names["megaco_monitor"]
log=otp("shared-return-cancel",shared/"native-monitor.eval",refs/rel,previous/rel,candidate/rel,caught/"native-support")
verified["shared-return-cancel"]={"Cases":3,"Evidence":sha(log)}

slot_group=group("ConstructedArgumentSlotKills")
file_scenarios("mnesia-dumper-nonempty-load","mnesia_dumper",slot_group/"native-nonempty-load.eval",[],1)

three("checked-carrier-release","megaco_monitor",group("DisjointCheckedEntry")/"validation/release-native.eval",3)
args=Path("D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/stack-tuple-construction-1966-intake")/"apply-after-native-retake.eval"
values={role:sha(otp("timer-tuple-"+role,args,origin/rel)) for role,origin in
    [("pinned",refs),("before",previous),("linked",candidate)]}
assert len(set(values.values()))==1, values
verified["timer-tuple"]={"Cases":6,"Evidence":values["linked"]}

covered=group("EliminateCoveredArm")/"validation/native"
file_scenarios("covered-branch-remap","mnesia_backup",covered/"branch-remap.eval",[covered/"stub-images/disk_log"],9)
damaged={}
for role,origin in [("pinned",refs),("before",previous),("linked",candidate)]:
    work=root/("damaged-mnesia-"+role)
    out=work.with_suffix(".terms")
    otp("damaged-mnesia-"+role,covered/"actual-damaged.eval",origin/beam_names["mnesia_backup"],work,out)
    raw=out.read_text(encoding="utf-8")
    directory=work.as_posix()
    prefix=(directory[0].lower()+directory[1:])+"/"
    normalized=raw.replace(prefix,"<fixture>/")
    assert normalized!=raw and "<fixture>/malformed.log" in normalized and "<fixture>/truncated.log" in normalized
    out=work.with_suffix(".canonical.terms")
    out.write_text(normalized,encoding="utf-8",newline="\n")
    damaged[role]=sha(out)
assert len(set(damaged.values()))==1,damaged
verified["mnesia-damaged"]={"Cases":3,"Evidence":damaged["linked"]}

comprehension=group("ConstructorColumnComprehensions")
three("constructor-ssh-debug","ssh_dbg",stage/"ssh-file-roles.eval",3)
asn=comprehension/"asn1-roundtrip"
asn_output={}
for role,origin in [("pinned",refs),("before",previous),("linked",candidate)]:
    target=root/("asn-objects-"+role)
    target.mkdir()
    observed=otp("asn-objects-"+role,asn/"run.eval",origin/beam_names["asn1ct_gen_ber_bin_v2"],asn/"Packets.asn1",target)
    assert "{typed_open_values_roundtrip,2}" in observed.read_text(encoding="utf-8")
    asn_output[role]=sha(observed)
assert len(set(asn_output.values()))==1,asn_output
verified["asn1-roundtrip"]={"Cases":2,"Evidence":asn_output["linked"]}

roles_for_package=[("pinned",refs),("before",previous),("linked",candidate)]
trailing=group("PreferTrailingXRoots")
for module,count in [("application_master",1),("megaco_flex_scanner",2),("observer_html_lib",2),("shell",3)]:
    name="trailing-root-"+module
    proofs={}
    for role,origin in roles_for_package:
        return_file=root/(name+"-"+role+".terms")
        otp(name+"-"+role,trailing/"native.eval",module,origin/beam_names[module],return_file)
        proofs[role]=sha(return_file)
    assert len(set(proofs.values()))==1,(name,proofs)
    verified[name]={"Cases":count,"Evidence":proofs["linked"]}

ordered=group("OrderedConsAfterCall")/"release-native"
for mode,module,fixture in [
    ("a","beam_a","ordered_cons_preproc.erl"),("asm","beam_asm","ordered_cons_preproc.erl"),
    ("syntax","epp_dodger","ordered_cons_preproc.erl"),
    ("ssa","beam_ssa_destructive_update","ordered_cons_ssa_probe.erl")]:
    name="scheduled-cons-"+module
    proofs={role:sha(otp(name+"-"+role,ordered/"release-witness-final.eval",
        mode,origin/beam_names[module],ordered/fixture)) for role,origin in roles_for_package}
    assert len(set(proofs.values()))==1,(name,proofs)
    verified[name]={"Cases":1,"Evidence":proofs["linked"]}

terminal=group("TerminalTupleAtomPartition")
file_scenarios("terminal-mnesia-tuple-arities","mnesia_backup",terminal/"native/mnesia-read.eval",
               [terminal/"native/read-stub/disk_log"],8)

literal=group("LiteralConsComprehension")
literal_fixture=literal/"oracle/beam_literal_cons_comprehension.erl"
literal_candidate=root/"beam_literal_cons_comprehension.beam"
run("literal-cons-compile",[sdk,stage/"frozen-payload/Erlang.Tool.dll","compile-beam",literal_fixture,literal_candidate])
native=(literal/"oracle/native.eval").read_text(encoding="utf-8")
native=native[native.index("Exact="):]
literal_recipe=root/"literal-cons-native.eval"
literal_recipe.write_text(
    "[Image]=init:get_plain_arguments(),\nM=beam_literal_cons_comprehension,\n"
    "{ok,Bin}=file:read_file(Image),\n{module,M}=code:load_binary(M,Image,Bin),\n"+native,
    encoding="utf-8",newline="\n")
literal_pinned=otp("literal-cons-pinned",literal_recipe,literal/"oracle/beam_literal_cons_comprehension.beam")
literal_linked=otp("literal-cons-linked",literal_recipe,literal_candidate)
assert sha(literal_pinned)==sha(literal_linked)
synthetic["literal-cons"]={"Cases":8,"Evidence":sha(literal_linked),"Image":sha(literal_candidate)}
stem_keys={}
for row in data(literal/"receipt.json")["Scope"]["Cases"]:
    stem=Path(row["Module"]).stem
    stem_keys.setdefault(stem,[]).append(row["Helper"])
check=root/"literal-cons-helper-keys.eval"
check.write_text("""[Image|Keys]=init:get_plain_arguments(),
{beam_file,_,_,_,_,Fs}=beam_disasm:file(Image),
Present=[{atom_to_list(N),integer_to_list(A)}||{function,N,A,_,_}<-Fs],
Selected=[list_to_tuple(string:split(K,"#",trailing))||K<-Keys],
true=lists:all(fun(K)->lists:member(K,Present) end,Selected),
io:format("~tp.~n",[Selected]),halt().
""",encoding="utf-8",newline="\n")
for module,keys in stem_keys.items():
    for role,origin in [("pinned",refs),("linked",candidate)]:
        otp("literal-cons-keys-"+module+"-"+role,check,origin/beam_names[module],
            *[name+"#"+str(arity) for name,arity in keys])

branch=group("OrdinaryBranchCons").parent/"native"
branch_image=root/"ordered_branch_cons_fixture.beam"
run("ordinary-cons-compile",[sdk,stage/"frozen-payload/Erlang.Tool.dll","compile-beam",
    branch/"ordered_branch_cons_fixture.erl",branch_image])
branch_provider=branch/"pinned/ordered_cons_call_provider"
branch_obs=otp("ordinary-cons-branch",branch/"witness.eval",branch_image.with_suffix(""),branch_provider)
branch_pin=otp("ordinary-cons-pinned-branch",branch/"witness.eval",
    branch/"pinned/ordered_branch_cons_fixture",branch_provider)
assert sha(branch_obs)==sha(branch_pin)
synthetic["ordinary-cons-branch"]={"Cases":7,"Evidence":sha(branch_obs),"Image":sha(branch_image)}
owned=group("OwnedFrameAliases")/"validation"
for module,count in [("beam_ssa_pre_codegen",4),("eunit_data",5),("cdv_multi_wx",2),("ssh_transport",4)]:
    name="owned-frame-"+module
    results={}
    for role,origin in roles_for_package:
        output=root/(name+"-"+role+".terms")
        otp(name+"-"+role,owned/"native.eval",module,origin/beam_names[module],output)
        results[role]=sha(output)
    assert len(set(results.values()))==1,(name,results)
    verified[name]={"Cases":count,"Evidence":results["linked"]}

def compile_fixture(label,source):
    image=root/(source.stem+".beam")
    run("compile-"+label,[sdk,stage/"frozen-payload/Erlang.Tool.dll","compile-beam",source,image])
    return image

carriers=group("NonterminalCaseCarriers")
carrier_image=compile_fixture("nonterminal-carrier",carriers/"source/case_carrier_otp_probe.erl")
carrier_term=otp("nonterminal-carrier",carriers/"carrier-probe.eval",carriers/"pinned/case_carrier_otp_probe.beam",
                 carrier_image,carriers/"explicit-alias/case_carrier_otp_probe.beam")
synthetic["nonterminal-carrier"]={"Cases":5,"Evidence":sha(carrier_term),"Image":sha(carrier_image)}

token=group("NestedHeadTokenAliases")/"fixture"
token_image=compile_fixture("nested-token-alias",token/"ordered_token_alias_fixture.erl")
token_provider=token/"pinned/ordered_token_alias_provider"
token_term=otp("token-alias-linked",token/"verify.eval",token_image.with_suffix(""),token_provider)
token_pin=otp("token-alias-pinned",token/"verify.eval",token/"pinned/ordered_token_alias_fixture",token_provider)
assert sha(token_term)==sha(token_pin)
synthetic["nested-token-alias"]={"Cases":5,"Evidence":sha(token_term),"Image":sha(token_image)}

def comprehension_fixture(kind,directory,module,first,count,recipe_name="native.eval"):
    image=compile_fixture(kind,directory/(module+".erl"))
    text=(directory/recipe_name).read_text(encoding="utf-8-sig")
    text=text[text.index(first):]
    recipe=root/(kind+".eval")
    recipe.write_text(
        "[Image]=init:get_plain_arguments(),\nM="+module+",\n"
        "{ok,Bin}=file:read_file(Image),\n{module,M}=code:load_binary(M,Image,Bin),\n"+text,
        encoding="utf-8",newline="\n")
    pin=otp(kind+"-pinned",recipe,directory/(module+".beam"))
    observed=otp(kind+"-linked",recipe,image)
    assert sha(pin)==sha(observed)
    synthetic[kind]={"Cases":count,"Evidence":sha(observed),"Image":sha(image)}

comprehension_fixture("structured-cons-equality",group("StructuredConsEquality")/"oracle",
    "beam_structured_cons_equality_comprehension","{[last],[]}",5)
comprehension_fixture("ordinary-list-append",group("OrdinaryAppendComprehensions")/"oracle",
    "beam_ordinary_append_comprehension","{[one,two,three]",4)
xmerl_audit=inputs/"xmerl-default-attributes-frame-1969-audit"
comprehension_fixture("leading-variable-comprehension",xmerl_audit,
    "beam_leading_variable_mixed_defs","shortcut=M:fill",4,"native-fixture.eval")
xmerl_names=sorted(name for name in beam_names if name.startswith("xmerl_sax_parser_"))
assert len(xmerl_names)==5
xmerl_paths=root/"xmerl-native-images.tsv"
with xmerl_paths.open("w",encoding="utf-8",newline="\n") as file:
    for name in xmerl_names:
        rel=beam_names[name]
        print(*(origin/rel for _,origin in roles_for_package),sep="\t",file=file)
xmerl_observed=otp("xmerl-default-attributes",xmerl_audit/"native-release.eval",xmerl_paths)
verified["xmerl-default-attributes"]={"Cases":10,"Evidence":sha(xmerl_observed)}
xmerl_structure=root/"verify-xmerl-restored-helper.eval"
xmerl_structure.write_text("""Paths=init:get_plain_arguments(),
lists:foreach(fun(Path)->
  {beam_file,M,_,_,_,Functions}=beam_disasm:file(Path),
  [{function,'-fill_default_attributes/2-lc$^1/1-0-',2,_,_}]=
    [F||F={function,'-fill_default_attributes/2-lc$^1/1-0-',2,_,_}<-Functions],
  [{function,fill_default_attributes,2,_,Body}]=[F||F={function,fill_default_attributes,2,_,_}<-Functions],
  Widths=[N||I<-Body,N<-case I of {allocate,V,_}->[V];{allocate_heap,V,_,_}->[V];_->[] end],
  true=lists:max(Widths)=<6,
  io:format("~tp.~n",[{M,Widths}])
end,Paths),halt().
""",encoding="utf-8",newline="\n")
otp("xmerl-restored-frames",xmerl_structure,*[candidate/beam_names[name] for name in xmerl_names])

script=root/"restored-comprehension-keys.eval"
script.write_text("""[Image|Keys]=init:get_plain_arguments(),
{beam_file,M,_,_,_,Fs}=beam_disasm:file(Image),
Actual=[atom_to_list(F)++"#"++integer_to_list(A)||{function,F,A,_,_}<-Fs],
true=lists:all(fun(K)->lists:member(K,Actual) end,Keys),
io:format("~tp.~n",[{M,Keys}]),halt().
""",encoding="utf-8",newline="\n")
edlin=[line.split("\t") for line in (group("OrdinaryAppendComprehensions")/"release-targets.tsv").read_text(encoding="utf-8-sig").splitlines()]
targets={"edlin_type_suggestion":[line[3]+"#"+line[4] for line in edlin],
    "ct_logs":["-make_relative1/2-lc$^1/1-0-#1","-make_relative1/2-lc$^0/1-1-#1"],
    "test_server_ctrl":["-make_relative1/2-lc$^1/1-0-#1","-make_relative1/2-lc$^0/1-1-#1"]}
assert len(targets["edlin_type_suggestion"])==6
for name,keys in targets.items():
    for role,origin in [("pinned",refs),("linked",candidate)]:
        otp("restored-helpers-"+name+"-"+role,script,origin/beam_names[name],*keys)

receipt={"Compiler":identity["Compiler"],"Runtime":identity["Runtime"],
         "ActualCases":sum(row["Cases"] for row in verified.values()),"SyntheticCases":sum(row["Cases"] for row in synthetic.values()),"SyntheticObservations":synthetic,
         "ActualImages":{path:sha(candidate/path) for path in sorted(beam_names.values())},"Observations":verified}
(root/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
print(json.dumps(receipt))
