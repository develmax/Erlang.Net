from pathlib import Path
import argparse,hashlib,json,re,struct,zlib

MAX_BYTES=16*1024*1024
MAX_DEPTH=128
def require(value,message):
    if not value: raise ValueError(message)
def sha(data): return hashlib.sha256(data).hexdigest().upper()
def decode(data):
    require(1<len(data)<=MAX_BYTES and data[0]==131,"ETF version/bound");position=1
    def take(size):
        nonlocal position
        require(size>=0 and position+size<=len(data),"Truncated ETF")
        result=data[position:position+size];position+=size;return result
    def u32():return int.from_bytes(take(4),"big")
    def term(depth):
        require(depth<=MAX_DEPTH,"ETF depth");tag=take(1)[0]
        if tag==97:return ("integer",take(1)[0])
        if tag==98:return ("integer",int.from_bytes(take(4),"big",signed=True))
        if tag in (110,111):
            count=take(1)[0] if tag==110 else u32();sign=take(1)[0];require(sign<2,"Big sign")
            value=int.from_bytes(take(count),"little");return ("integer",-value if sign else value)
        if tag in (100,115,118,119):
            count=take(1)[0] if tag in (115,119) else int.from_bytes(take(2),"big")
            return ("atom",take(count).decode("utf-8" if tag in (118,119) else "latin1"))
        if tag in (104,105):
            count=take(1)[0] if tag==104 else u32();require(count<=len(data)-position,"Tuple count")
            return ("tuple",tuple(term(depth+1) for _ in range(count)))
        if tag==106:return ("list",())
        if tag==107:return ("list",tuple(("integer",b) for b in take(int.from_bytes(take(2),"big"))))
        if tag==108:
            count=u32();require(count<=len(data)-position,"List count");items=tuple(term(depth+1) for _ in range(count));tail=term(depth+1)
            return ("list",items+tail[1]) if tail[0]=="list" else ("improper",items+tail[1],tail[2]) if tail[0]=="improper" else ("improper",items,tail)
        if tag==109:return ("binary",take(u32()))
        if tag==116:
            count=u32();require(count<=(len(data)-position)//2,"Map count");pairs=[(term(depth+1),term(depth+1)) for _ in range(count)]
            require(len({k for k,v in pairs})==count,"Duplicate map");return ("map",frozenset(pairs))
        if tag in (90,114):
            count=int.from_bytes(take(2),"big");require(0<count<=5,"Reference bound");node=term(depth+1);creation=u32() if tag==90 else take(1)[0]
            return ("reference",node,creation,tuple(u32() for _ in range(count)))
        if tag in (88,103):
            node=term(depth+1);pid=u32();serial=u32();creation=u32() if tag==88 else take(1)[0]
            return ("pid",node,pid,serial,creation)
        if tag in (89,102):
            node=term(depth+1);port=u32();creation=u32() if tag==89 else take(1)[0]
            return ("port",node,port,creation)
        raise ValueError("Unsupported raw diagnostic ETF tag "+str(tag))
    result=term(0);require(position==len(data),"Trailing ETF bytes");return result
def literal(source):
    tokens=re.findall(r"[a-z][A-Za-z0-9_@]*|-?\d+|[{}\[\],]",source);require("".join(tokens)==re.sub(r"\s+","",source),"Static literal syntax");position=0
    def term():
        nonlocal position
        token=tokens[position];position+=1
        if token in ("{","["):
            end="}" if token=="{" else "]";values=[]
            if tokens[position]!=end:
                values.append(term())
                while tokens[position]==",":position+=1;values.append(term())
            require(tokens[position]==end,"Static closing delimiter");position+=1
            return ("tuple" if token=="{" else "list",tuple(values))
        return ("integer",int(token)) if re.fullmatch(r"-?\d+",token) else ("atom",token)
    result=term();require(position==len(tokens),"Static trailing token");return result
def iodata(value):
    if value[0]=="binary":return value[1]
    if value[0]=="integer":require(0<=value[1]<=255,"Invalid iodata byte");return bytes([value[1]])
    if value[0]=="list":return b"".join(iodata(x) for x in value[1])
    if value[0]=="improper":require(value[2][0]=="binary","Invalid improper tail");return b"".join(iodata(x) for x in value[1])+value[2][1]
    raise ValueError("Invalid iodata term")
def declarations(source):
    raw=(source/"loader_iodata_wrapper.erl").read_bytes();declraw=(source/"predeclared-contracts.json").read_bytes();decl=json.loads(declraw)
    clauses=dict(re.findall(r'^expected\("([^"]+)"\) -> (.*?)[;.][\r]?$',raw.decode(),re.M));require(len(clauses)==len(decl["Scenarios"])==16,"Sixteen independent clauses")
    for row in decl["Scenarios"]:require(literal(clauses[row["Name"]])==literal(row["PredeclaredProjection"]),"Plan/static source mismatch "+row["Name"])
    wanted={name:literal(text) for name,text in clauses.items()}
    corrupt=decode(bytes([131,119,5])+b"wrong")
    require(corrupt!=wanted[decl["Scenarios"][0]["Name"]],"Equal wrong terms cannot satisfy static expected")
    return raw,declraw,decl,wanted
def verify(source,directory):
    raw,declraw,decl,wanted=declarations(source);report=json.loads((directory/"loader-iodata-contrasts.json").read_text())
    require(report["Complete"] and report["CaseCount"]==report["FreshVmExecutions"]==report["ExpectedFreshVmExecutions"]==report["Passed"]==16,"Complete16 native successes")
    require(report["SourceCommit"]=="ad05823719d77c8faee87348ea39513d4e2f99c5" and report["SourceTag"]=="OTP-29.1.1" and report["MfaRegistrations"]==0 and not report["SdkExecuted"],"Independent pinned scope")
    require(report["PredeclaredSha256"]==sha(declraw) and report["WrapperSourceSha256"]==sha(raw),"Static source hashes")
    require((directory/"loader_iodata_wrapper.erl").read_bytes()==raw and (directory/"predeclared-contracts.json").read_bytes()==declraw,"Captured source exact")
    require(report["WrapperBeamSha256"]==sha((directory/"loader_iodata_wrapper.beam").read_bytes()),"Actual compiled wrapper bytes")
    inputrow=decl["InputBytes"][0];beam=(directory/"inputs"/inputrow["Path"]).read_bytes();require(sha(beam)==inputrow["Sha256"],"Immutable original input")
    require(len(report["Results"])==16,"Sixteen rows");rows=[]
    for case,plan in zip(report["Results"],decl["Scenarios"]):
        name=plan["Name"];folder=directory/name;require(case["Name"]==case["Scenario"]==name and case["Passed"] and case["RawComplete"] and not case["TimedOut"] and case["ExitCode"]==0 and case["Stage"]=="completed","Actual case completion "+name)
        files={x["Name"]:x for x in case["Files"]};require(len(files)==len(case["Files"]) and set(files)=={p.name for p in folder.iterdir() if p.is_file()},"Raw case inventory")
        for filename,item in files.items():
            data=(folder/filename).read_bytes();require(len(data)==item["Length"] and sha(data)==item["Sha256"],"Raw file digest "+name+"/"+filename)
        require(decode((folder/"actual.etf").read_bytes())==decode((folder/"expected.etf").read_bytes())==wanted[name],"Independent actual/static projection "+name)
        args=decode((folder/"input-arguments.etf").read_bytes());require(args[0]=="tuple" and len(args[1])==2,"Raw input argument shape")
        module,code=args[1];expectedmodule=("integer",7) if name.startswith("invalid-module-") else ("atom","loader_gzip_wrong_module") if name=="wrong-module-gzip-proper-list" else ("atom","beam_fixture_constant")
        require(module==expectedmodule,"Source-declared Module")
        require(code==decode((folder/"original-code0.etf").read_bytes()),"Original Code0 preserved")
        selected=decode((folder/"independent-selected-code.etf").read_bytes());domain,selectedcode=selected[1]
        gzip=(folder/"original-gzip.bin").read_bytes();require((folder/"original-beam.bin").read_bytes()==beam and zlib.decompress(gzip,31)==beam,"Genuine original gzip payload")
        if plan["SelectedCodeDomain"] in ("gzip","gzip-empty"):
            require(domain==("atom","decompressed_code") and selectedcode==("binary",b"" if plan["SelectedCodeDomain"]=="gzip-empty" else beam),"Independent decoded argument bytes")
            flat=iodata(code);require(zlib.decompress(flat,31)==selectedcode[1],"Original input really gzip iodata")
            if name!="gzip-empty-payload-list":require(flat==gzip,"Exact gzip input bytes")
        else:require(domain==("atom","original_code") and selectedcode==code,"Untouched original fallback argument")
        if name in ("gzip-proper-byte-list","invalid-module-gzip-proper-list","wrong-module-gzip-proper-list"):require(code[0]=="list" and all(x[0]=="integer" for x in code[1]),"Proper byte list")
        if name in ("gzip-nested-iodata","invalid-module-gzip-nested"):require(code[0]=="list" and any(x[0]=="list" for x in code[1]),"Nested iodata")
        if name in ("gzip-improper-binary-tail","invalid-module-gzip-binary-tail"):require(code[0]=="improper" and code[2][0]=="binary","Improper binary tail")
        if name=="empty-list-fallback":require(code==("list",()),"Empty list fallback")
        if name in ("truncated-gzip-list-fallback","invalid-module-truncated-list"):require(code[0]=="list" and iodata(code)==gzip[:2],"Exact truncated gzip list")
        if name=="nonmagic-raw-beam-list-fallback":require(code[0]=="list" and iodata(code)==beam,"Raw BEAM list fallback")
        if name=="empty-binary-fallback":require(code==("binary",b""),"Empty binary")
        if name=="truncated-gzip-binary-fallback":require(code==("binary",gzip[:2]),"Exact truncated binary")
        if name=="invalid-iodata-leaf-fallback":require(code[0]=="list" and code[1][0]==("integer",256),"Invalid byte preserved")
        if name=="invalid-improper-atom-tail-fallback":require(code[0]=="improper" and code[2]==("atom","invalid_tail"),"Invalid tail preserved")
        decompression=decode((folder/"0-gunzip-observed.etf").read_bytes())
        rawprepare=decode((folder/"1-prepare-loading.etf").read_bytes())
        if domain==("atom","decompressed_code"):
            require(decompression==("tuple",(("atom","value"),selectedcode)),"Actual original gunzip bytes")
        else:
            reason=("atom","badarg") if name in ("invalid-iodata-leaf-fallback","invalid-improper-atom-tail-fallback") else ("atom","data_error")
            require(decompression[0]=="tuple" and decompression[1][:3]==(("atom","raised"),("atom","error"),reason),"Actual original gunzip failure")
        expected=wanted[name]
        if expected[1][0]==("atom","true"):
            require(rawprepare[0]=="tuple" and rawprepare[1][0]==("atom","value") and rawprepare[1][1][0]=="reference","Actual genuine prepared resource")
            require(decode((folder/"2-flag.etf").read_bytes())==("tuple",(("atom","value"),("atom","false"))),"Actual flagfalse")
            require(decode((folder/"3-finish.etf").read_bytes())==("tuple",(("atom","value"),("atom","ok"))),"Actual finishok")
            require(decode((folder/"4-invoke.etf").read_bytes())==("tuple",(("atom","value"),expected[1][3])),"Actual genuine run")
        elif expected==literal("{error,badfile}"):
            require(rawprepare==("tuple",(("atom","value"),expected)),"Actual loader badfile return")
        else:
            require(rawprepare[0]=="tuple" and rawprepare[1][:3]==(("atom","raised"),("atom","error"),("atom","badarg")),"Actual native badarg")
            stack=rawprepare[1][3];require(stack[0]=="list" and stack[1],"Complete native stack")
            frame=stack[1][0];require(frame[0]=="tuple" and frame[1][:3]==(("atom","erlang"),("atom","prepare_loading"),("list",(module,selectedcode))),"Actual rewritten native stack arguments")
            require((folder/"selected-stack-arguments.etf").exists() and (folder/"raw-error-info.etf").exists(),"Required full argument/error metadata captures")
        if (folder/"selected-stack-arguments.etf").exists():
            require(decode((folder/"selected-stack-arguments.etf").read_bytes())==("tuple",(module,selectedcode)),"Exact rewritten stack arguments")
            info=decode((folder/"raw-error-info.etf").read_bytes());require(info[0]=="map" and (("atom","module"),("atom","erl_erts_errors")) in info[1],"Raw native error_info module")
        rows.append({"Name":name,"SelectedDomain":domain[1],"RawFileCount":len(files),"ActualSha256":files["actual.etf"]["Sha256"]})
    return {"Status":"SixteenIndependentIodataNativeSummariesVerified","NativeExecutions":16,"Rows":rows,"EqualWrongActualExpectedRejected":True}
def main():
    parser=argparse.ArgumentParser();parser.add_argument("--source",type=Path,default=Path(__file__).resolve().parent);parser.add_argument("--native-output",type=Path);parser.add_argument("--report",type=Path,required=True);args=parser.parse_args()
    raw,declraw,decl,wanted=declarations(args.source)
    result=verify(args.source,args.native_output) if args.native_output else {"Status":"SourceOnlyStaticSixteenPlansVerified","NativeExecuted":False,"WrapperSha256":sha(raw),"DeclarationSha256":sha(declraw),"CaseCount":16,"EqualWrongActualExpectedRejected":True}
    args.report.write_text(json.dumps(result,indent=2)+"\n");print(result["Status"])
if __name__=="__main__":main()
