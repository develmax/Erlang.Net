from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import sys
import time

stage,parent,refs,inputs,archive,capture=map(Path,sys.argv[1:])
final_witnesses=stage/"linked-runtime-witnesses"
witness_key=hashlib.sha256(stage.name.encode("utf-8")).hexdigest()[:10]
root=inputs/("native-"+witness_key)
assert root.resolve().parent == inputs.resolve()
assert not final_witnesses.exists()
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
wait(stage/"source-freeze.json",2300)
wanted={"OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","ex_gauge","ct_default_gl" ,"ex_listCtrl","megaco_monitor","ct_logs","unix_telnet","mnesia_backup","mnesia_dumper","megaco_digit_map","snmpa_mib_storage_ets","ssh_dbg","asn1ct_gen_ber_bin_v2"}
wanted.update({"application_master","megaco_flex_scanner","observer_html_lib","shell",
    "beam_a","beam_asm","epp_dodger","beam_ssa_destructive_update","edoc_html_to_markdown","observer_wx","ssl_pkix_db"})
wanted.update({"beam_ssa_pre_codegen","eunit_data","cdv_multi_wx","ssh_transport",
    "xmerl_sax_parser_latin1","xmerl_sax_parser_list","xmerl_sax_parser_utf16be",
    "xmerl_sax_parser_utf16le","xmerl_sax_parser_utf8",
    "edlin_type_suggestion","test_server_ctrl"})
wanted.update({"erl_syntax_lib","beam_doc"})
wanted.update({"diameter_codegen","snmp_generic_mnesia","ssh_connection","qlc_pt"})
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

real_annotations=stage/"live-annotation-native.eval"
three("live-annotation-bindings","erl_syntax_lib",real_annotations,2)

record=group("EntryRecordHeads")/"fixture"
record_image=compile_fixture("entry-record-heads",record/"list_head_record_frame_fixture.erl")
record_provider=record/"pinned/list_head_record_frame_provider"
record_results=otp("entry-record-heads-linked",record/"native.eval",record_image.with_suffix(""),record_provider)
record_pin=otp("entry-record-heads-pinned",record/"native.eval",record/"pinned/list_head_record_frame_fixture",record_provider)
assert sha(record_results)==sha(record_pin)
synthetic["entry-record-heads"]={"Cases":8,"Evidence":sha(record_results),"Image":sha(record_image)}

bound=group("BoundOrderedCons")
bound_image=compile_fixture("bound-ordered-cons",bound/"reference-fixture/ordered_cons_call_fixture.erl")
bound_recipe=root/"bound-native.eval"
text=(bound/"reference-fixture.eval").read_text(encoding="utf-8-sig")
head="[Out]=init:get_plain_arguments(),"
assert text.count(head)==1
text=text.replace(head,"[Main,Provider,Out]=init:get_plain_arguments(),\n"
    "{module,ordered_cons_call_fixture}=code:load_abs(Main),\n"
    "{module,ordered_cons_call_provider}=code:load_abs(Provider),")
bound_recipe.write_text(text,encoding="utf-8",newline="\n")
bound_results={}
for label,image in [("pinned",bound/"reference-images/ordered_cons_call_fixture"),("linked",bound_image.with_suffix(""))]:
    output=root/("bound-"+label+".terms")
    otp("bound-call-"+label,bound_recipe,image,bound/"reference-images/ordered_cons_call_provider",output)
    bound_results[label]=sha(output)
assert len(set(bound_results.values()))==1,bound_results
synthetic["fresh-bound-cons"]={"Cases":6,"Evidence":bound_results["linked"],"Image":sha(bound_image)}

scratch=group("SavedCasePayloadScratch")
scratch_image=compile_fixture("saved-case-scratch",scratch/"case_payload_scratch_probe.erl")
old=(scratch/"oracle.eval").read_text(encoding="utf-8-sig")
checks=old[old.index("Cases="):]
recipe=root/"saved-case-scratch-beam.eval"
recipe.write_text("[Path]=init:get_plain_arguments(),\n"
    "M=case_payload_scratch_probe,\n{ok,B}=file:read_file(Path),\n{module,M}=code:load_binary(M,Path,B),\n"+checks,
    encoding="utf-8",newline="\n")
scratch_result=otp("saved-case-scratch-execution",recipe,scratch_image)
assert scratch_result.read_text(encoding="utf-8").strip()=="oracle 8 cases passed"
synthetic["saved-case-scratch"]={"Cases":8,"Evidence":sha(scratch_result),"Image":sha(scratch_image)}

mixed_pair=group("MixedAtomTuplePair")
folder=root/"linked-mixed-case"
folder.mkdir()
mixed_pair_image=compile_fixture("mixed-case-pair",mixed_pair/"beam_atom_tuple_case_pair.erl")
shutil.copyfile(mixed_pair_image,folder/mixed_pair_image.name)
shutil.copyfile(mixed_pair/"pinned/atom_tuple_case_provider.beam",folder/"atom_tuple_case_provider.beam")
linked_pair=otp("mixed-case-pair-linked",mixed_pair/"native-cases.eval",folder)
pinned_pair=otp("mixed-case-pair-pinned",mixed_pair/"native-cases.eval",mixed_pair/"pinned")
assert sha(linked_pair)==sha(pinned_pair)
synthetic["mixed-atom-tuple-case"]={"Cases":11,"Evidence":sha(linked_pair),"Image":sha(mixed_pair_image)}

cold=group("StandaloneCaseFailure")
cold_beam=compile_fixture("standalone-case-exit",cold/"beam_projected_case_exit.erl")
cold_behaviors=otp("standalone-case-native",cold/"native-fixture-retake.eval",
    cold_beam,cold/"fixture-pinned/beam_projected_case_exit.beam")
assert cold_behaviors.read_text(encoding="utf-8")==(cold/"native-fixture-retake.stdout.log").read_text(encoding="utf-8")
synthetic["isolated-case-exit"]={"Cases":12,"Evidence":sha(cold_behaviors),"Image":sha(cold_beam)}

projected=group("ProjectedReturns")
projected_image=compile_fixture("projected-returns",projected/"beam_projected_fallthrough.erl")
results={}
for role,beam in [("linked",projected_image),("pinned",stage/"projected-pinned/beam_projected_fallthrough.beam")]:
    term=root/(role+"-projected.etf")
    otp("projected-return-"+role,stage/"projected-return-native.eval",beam.with_suffix(""),
        stage/"projected-pinned/beam_return_provider",term)
    results[role]=sha(term)
assert results["pinned"]==results["linked"]
synthetic["projected-construction-return"]={"Cases":10,"Evidence":results["linked"],"Image":sha(projected_image)}

generator=group("TupleOuterGenerators")
generator_image=compile_fixture("tuple-outer-generators",generator/"beam_tuple_outer_generators.erl")
sender=(generator/"oracle.eval").read_text(encoding="utf-8-sig")
old="[Path]=init:get_plain_arguments(),{ok,M,B}=compile:file(Path,[binary]),"
assert sender.count(old)==1
sender=sender.replace(old,
    "[Path]=init:get_plain_arguments(), M=beam_tuple_outer_generators, {ok,B}=file:read_file(Path),")
recipe=root/"tuple-outer-native.eval"
recipe.write_text(sender,encoding="utf-8",newline="\n")
generator_behavior=otp("tuple-outer-generator-behavior",recipe,generator_image)
assert "OTP 7 behavior profiles passed" in generator_behavior.read_text(encoding="utf-8")
synthetic["tuple-outer-generators"]={"Cases":7,"Evidence":sha(generator_behavior),"Image":sha(generator_image)}
three("qlc-mforms", "qlc_pt", stage/"qlc-mforms-native-retake.eval", 6)

atom=group("TwoAtomSelection")
atom_image=compile_fixture("direct-two-atom-selector",atom/"fixture/two_atom_entry_fixture.erl")
atom_fixture=otp("two-atom-fixture-native",atom/"fixture/native.eval",atom_image.with_suffix(""),
                 atom/"fixture/pinned/two_atom_entry_provider")
assert atom_fixture.read_text(encoding="utf-8").strip()=="{ok,13}"
synthetic["two-atom-selector"]={"Cases":13,"Evidence":sha(atom_fixture),"Image":sha(atom_image)}
atom_release=otp("two-atom-release-native",atom/"integration/release-native.eval",refs,previous,candidate)
verified["two-atom-release"]={"Cases":6,"Evidence":sha(atom_release)}

diameter=group("OutgoingBinaryRoot")
dictionary=refs/"lib/diameter-2.7.2/src/dict/base_rfc6733.dia"
diameter_results={}
for role,origin in roles_for_package:
    term=root/(role+"-diameter-outgoing.etf")
    otp("diameter-outgoing-native-"+role,diameter/"release-native-retake.eval",
        origin/beam_names["diameter_codegen"],dictionary,term)
    diameter_results[role]=sha(term)
assert len(set(diameter_results.values()))==1,diameter_results
verified["diameter-outgoing"]={"Cases":15,"Evidence":diameter_results["linked"]}


cascade=group("CascadePassThrough")
cascade_image=compile_fixture("case-cascade",cascade/"beam_result_cascade.erl")
cascade_log=otp("case-cascade-linked",cascade/"native-fixture.eval",
    cascade/"pinned/beam_result_cascade.beam",cascade_image,cascade/"pinned/beam_result_cascade_provider")
assert cascade_log.read_text(encoding="utf-8")== (cascade/"native-fixture.stdout.log").read_text(encoding="utf-8")
synthetic["case-cascade"]={"Cases":10,"Evidence":sha(cascade_log),"Image":sha(cascade_image)}

append=group("OrderedAppendPrerequisites")/"fixture"
append_image=compile_fixture("ordered-append",append/"ordered_append_fixture.erl")
append_output=root/"ordered-append-observed.etf"
order=otp("ordered-append-native",append/"native.eval",append_image,
    append/"pinned/ordered_append_provider.beam",append_output)
assert "6 native outcomes" in order.read_text(encoding="utf-8")
synthetic["ordered-append"]={"Cases":6,"Evidence":sha(append_output),"Image":sha(append_image)}

siblings=group("ListGeneratorSiblings")
siblings_image=compile_fixture("sibling-list-generators",siblings/"beam_nested_sibling_generators.erl")
source=(siblings/"oracle.eval").read_text(encoding="utf-8-sig")
old="{ok,M,B}=compile:file(Path,[binary]),"
assert source.count(old)==1
source=source.replace(old,"M=beam_nested_sibling_generators, {ok,B}=file:read_file(Path),")
recipe=root/"sibling-dispatch-native.eval"
recipe.write_text(source,encoding="utf-8",newline="\n")
siblings_log=otp("sibling-list-generator-native",recipe,siblings_image)
assert "OTP 4 sibling profiles passed" in siblings_log.read_text(encoding="utf-8")
synthetic["sibling-list-generators"]={"Cases":4,"Evidence":sha(siblings_log),"Image":sha(siblings_image)}

head=group("ConsHeadAtomSelector")/"fixture"
head_image=compile_fixture("cons-head-atom",head/"cons_head_atom_table_fixture.erl")
head_log=otp("cons-head-atom-native",head/"native.eval",head_image.with_suffix(""))
assert head_log.read_text(encoding="utf-8").strip()=="{ok,15}"
synthetic["cons-head-atom"]={"Cases":15,"Evidence":sha(head_log),"Image":sha(head_image)}

frames=group("TerminalProjectionFrame")
frame_source=frames/"beam_terminal_tuple_frame.erl"
frame_image=compile_fixture("terminal-frame-projections",frame_source)
frame_log=otp("terminal-frame-projection-native",frames/"oracle.eval",frame_source,frame_image)
assert "parent 8 scope profiles passed" in frame_log.read_text(encoding="utf-8")
synthetic["terminal-tuple-projection-frame"]={"Cases":8,"Evidence":sha(frame_log),"Image":sha(frame_image)}

alias=group("TerminalEffectfulCarrier")
alias_fixture=alias/"fixture"
alias_image=compile_fixture("effectful-case-carrier",alias_fixture/"terminal_effectful_carrier_fixture.erl")
alias_log=otp("effectful-case-carrier-native",alias_fixture/"native.eval",alias_image.with_suffix(""),
    alias_fixture/"pinned/terminal_effectful_carrier_provider")
assert alias_log.read_text(encoding="utf-8").strip()=="{ok,9}"
synthetic["effectful-terminal-carrier"]={"Cases":9,"Evidence":sha(alias_log),"Image":sha(alias_image)}
alias_role_checks={}
for role,origin in roles_for_package:
    result=otp("mnesia-write-effectful-carrier-"+role,alias/"release-native/verify-mnesia-write.eval",
        origin/beam_names["mnesia_backup"],alias/"release-native/stub/disk_log",root/("mnesia-write-"+role))
    alias_role_checks[role]=sha(result)
assert len(set(alias_role_checks.values()))==1,alias_role_checks
verified["mnesia-real-write-carrier"]={"Cases":5,"Evidence":alias_role_checks["linked"]}


saved=group("SavedTupleSelector")
saved_image=compile_fixture("saved-terminal-tuple",saved/"beam_saved_terminal_tuple.erl")
saved_result=otp("saved-terminal-tuple-native",saved/"native-fixture.eval",
    saved/"pinned/beam_saved_terminal_tuple.beam",saved_image,saved/"pinned/beam_saved_terminal_provider")
assert saved_result.read_text(encoding="utf-8")== (saved/"native-fixture.stdout.log").read_text(encoding="utf-8")
synthetic["saved-terminal-tagged-results"]={"Cases":8,"Evidence":sha(saved_result),"Image":sha(saved_image)}

edges=group("OwnedCaseResultEdges")
edge_source=edges/"owned_case_result_probe.erl"
edge_image=compile_fixture("owned-case-result-edges",edge_source)
edge_linked=otp("owned-case-edges-linked",edges/"owned-case-probe.eval",edge_source,edge_image)
edge_pin=otp("owned-case-edges-pinned",edges/"owned-case-probe.eval",edge_source)
def branch_outcomes(path):
    lines=path.read_text(encoding="utf-8").splitlines()
    assert len(lines)>4 and "native_cases => 14" in lines[-1]
    first=next(i for i,line in enumerate(lines) if line.startswith("{owned,1,"))
    return lines[:first]+lines[-1:]
assert branch_outcomes(edge_linked)==branch_outcomes(edge_pin)
synthetic["owned-case-result-edges"]={"Cases":14,"Evidence":sha(edge_linked),"Image":sha(edge_image)}

checked=group("CheckedEntryFrameOrder")
checked_src=checked/"fixture/checked_root_order_fixture.erl"
checked_image=compile_fixture("checked-entry-root-order",checked_src)
checked_output=root/"checked-entry-root-observed.etf"
otp("checked-entry-root-native",checked/"fixture/native.eval",checked_image,
    checked/"fixture/pinned/checked_root_order_provider.beam",checked_output)
synthetic["checked-entry-root-order"]={"Cases":7,"Evidence":sha(checked_output),"Image":sha(checked_image)}
for name,module,recipe,count in [
    ("mnesia-abort-checked-order","mnesia_backup","mnesia-native.eval",3),
    ("beam-doc-checked-order","beam_doc","beam-doc-native.eval",5)]:
    values={}
    for role,origin in roles_for_package:
        item=root/(name+"-"+role+".etf")
        args=[origin/beam_names[module]]
        if module=="mnesia_backup":
            args.append(root/(name+"-"+role+".backup"))
        otp(name+"-"+role,checked/recipe,*args,item)
        values[role]=sha(item)
    assert len(set(values.values()))==1,(name,values)
    verified[name]={"Cases":count,"Evidence":values["linked"]}

single=group("SingleOrdinaryReturnFrame")
single_source=single/"single_returning_call_frame_probe.erl"
single_image=compile_fixture("single-returning-call-frame",single_source)
single_pin=otp("single-returning-call-pinned",single/"fixture.eval",single_source)
single_run=otp("single-returning-call-linked",single/"fixture.eval",single_source,single_image)
def single_outcomes(path):
    lines=path.read_text(encoding="utf-8").splitlines()
    first=next(i for i,line in enumerate(lines) if line.startswith("{list_helper,1,"))
    assert first>=9 and "native_cases => 9" in lines[-1] and "passed => true" in lines[-1],lines
    return lines[:first]+lines[-1:]
assert single_outcomes(single_pin)==single_outcomes(single_run)
synthetic["single-returning-call-frame"]={"Cases":9,"Evidence":sha(single_run),"Image":sha(single_image)}

receipt={"Compiler":identity["Compiler"],"Runtime":identity["Runtime"],
         "ActualCases":sum(row["Cases"] for row in verified.values()),"SyntheticCases":sum(row["Cases"] for row in synthetic.values()),"SyntheticObservations":synthetic,
         "ActualImages":{path:sha(candidate/path) for path in sorted(beam_names.values())},"Observations":verified}
(root/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
root.rename(final_witnesses)
print(json.dumps(receipt))
