from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'artifacts/ordsets371-map-fun-decoder-proposal1'
OUT.mkdir(exist_ok=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
base=ROOT/'artifacts/ordsets371-remaining-original-proposal2/tool/passive_etf_family.py'
assert sha(base)=='2A7CE62661669FA91D03A0231D0911FEDACAB6C37B0D2FBE76F2DA325726A519'
s=base.read_text()
s=s.replace("    # Provenance spans must never become map-key semantic identity. Restrict\n    # map keys to the original decoder profile; extended terms remain values.\n", "    # Existing scalar/compound profile plus structurally checked zero-free NEW_FUN.\n    # Transport positions are independently verified and never semantic identity.\n")
s=s.replace("    if value[0] in ('tuple', 'list'):", """    if value[0] == 'new_fun':
        if len(value) != 10:
            return False
        _, size, arity, uniq, index, module, old_index, old_uniq, pid, free = value
        # Narrow profile: no arbitrary closure environments as map keys.
        return (isinstance(size, int) and size >= 29
                and isinstance(arity, int) and 0 <= arity <= 255
                and isinstance(uniq, bytes) and len(uniq) == 16
                and isinstance(index, int) and 0 <= index <= 0xffffffff
                and module[0] == 'atom'
                and old_index[0] == old_uniq[0] == 'integer'
                and 0 <= old_index[1] <= 0xffffffff
                and 0 <= old_uniq[1] <= 0xffffffff
                and len(pid) == 6 and pid[0] == 'pid' and pid[1][0] == 'atom'
                and pid[5] in (88, 103)
                and free == ())
    if value[0] in ('tuple', 'list'):""")
s=s.replace('def decode_one(data):','def decode_one(data, fun_spans=None):')
old="""            # Passive original-file span: never copy nested fun encodings.
            # The guard records the raw ETF file SHA; this span recovers exact bytes.
            return ('new_fun', size, arity, uniq, index, module, old_index, old_uniq, pid, free, ('raw_span', size_start - 1, end - size_start + 1))"""
new="""            # Validate transport span independently of semantic ETF fields.
            start, length = size_start - 1, end - size_start + 1
            require(start >= 1 and start + length == position
                    and data[start] == 112
                    and int.from_bytes(data[start + 1:start + 5], 'big') == size,
                    'New fun raw span integrity')
            if fun_spans is not None:
                import hashlib
                fun_spans.append({'Start': start, 'Length': length,
                                  'DeclaredSize': size,
                                  'Sha256': hashlib.sha256(data[start:end]).hexdigest().upper()})
            return ('new_fun', size, arity, uniq, index, module, old_index, old_uniq, pid, free)"""
assert old in s
s=s.replace(old,new)
s+='''\n\ndef decode_exact_with_fun_spans(data):
    spans = []
    result, used = decode_one(data, spans)
    require(used == len(data), 'Trailing bytes in result ETF')
    return result, spans
'''
(OUT/'passive_etf_family.py').write_text(s,encoding='utf-8')
review={'Status':'SOURCE_REVIEWED_PROPOSAL_ROOT_MUST_EXECUTE_CONTROLS','BasePath':str(base.relative_to(ROOT)).replace('\\','/'),'BaseSha256':sha(base),'ProposalSha256':sha(OUT/'passive_etf_family.py'),'SemanticFieldsRetained':['declared size','arity','NEW_UNIQ all16bytes','index','module','old_index','old_uniq','creator PID allfields and wiretag','free environment allterms'],'SoleRemovedStructuralItem':'raw_span offset/length transport bookkeeping; all raw ETF bytes remain in original capture and span boundaries are independently checked on every NEW_FUN parse. Optional decode_exact_with_fun_spans returns source spans+SHA256 sidechannel.','NewMapKeyProfile':'Only NEW_FUN with fully parsed fixed field types/ranges and exactlyzero free terms. No blanket allowance for arbitrary FUN environments, PIDs, ports or native record map keys. Existing supported key profile unchanged.','BoundsPreserved':['MAX_BYTES','MAX_DEPTH','MAX_TERMS','aggregate container budget','declared NEW_FUN end','free-count room','exact new_fun end','truncation and trailing data rejection','duplicate structural map key rejection'],'Comparison':'All serialized FUN fields remain in semantic tuples and therefore strict equality/frozenset map identity. No creator normalization, no OLD_UNIQ approximation, no result projection, no drop of arity/index/size or environment. Raw span positions may differ when physical map pair order changes; they are evidence sidechannel, not Erlang map semantics.','KnownProducerGap':'Actual Main reader/writer may fail to preserve restored FUN creator PID. This proposal explicitly keeps creator so such a mismatch stays visible, rather than being normalized away.','NoBuildNetworkMainWrites':True}
(OUT/'review.json').write_text(json.dumps(review,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'DecoderSha256':sha(OUT/'passive_etf_family.py')}))
