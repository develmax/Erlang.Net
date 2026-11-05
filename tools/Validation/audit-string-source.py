import argparse, hashlib, json, pathlib, re, subprocess

root = pathlib.Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser(description='Catalog all pinned string source clauses and C# handlers; not a compatibility promotion.')
p.add_argument('--reference', required=True)
p.add_argument('--output', required=True)
a = p.parse_args()
reference = (root / a.reference).read_text(encoding='utf-8')
official_source = subprocess.check_output([
    'git', '-C', str(root.parent / 'otp'), 'show',
    'OTP-29.1.1:lib/stdlib/src/string.erl'])
assert (root / a.reference).read_bytes() == official_source, 'Reference must be unchanged pinned string.erl'
source_dir = root / 'src/Otp/Managed/Erlang.Otp.Stdlib/Text'
names = dict(re.findall(r'const string (\w+) = "([^"]+)";', (source_dir/'StringFunctionNames.cs').read_text()))
exports = list(dict.fromkeys((names[n], int(v)) for n,v in re.findall(r'\(StringFunctionNames\.(\w+),\s*(\d+)\)', (source_dir/'StringModule.cs').read_text())))
assert len(exports) == 71, len(exports)

def head_end(text, start):
    depth, commas, i, quote = 1, 0, start, None
    while i < len(text):
        c = text[i]
        if quote:
            if c == '\\': i += 2; continue
            if c == quote: quote = None
        elif c in ('"', "'"): quote = c
        elif c == '$':
            i += 2 if text[i+1:i+2] != '\\' else 3
            continue
        elif text[i:i+2] in ('<<','>>'):
            depth += 1 if text[i:i+2] == '<<' else -1
            i += 2; continue
        elif c in '([{': depth += 1
        elif c in ')]}':
            depth -= 1
            if depth == 0: return i, commas + (text[start:i].strip() != '')
        elif c == ',' and depth == 1: commas += 1
        i += 1
    raise ValueError('Unclosed function head')

handlers = {}
def assign(functions, filename, summary):
    for f in functions.split(): handlers[f] = (filename, summary)
assign('is_empty next_codepoint next_grapheme to_float to_integer jaro_similarity', 'StringUnicode.cs', 'Term/stream dispatch and pinned Unicode/native dependencies; retain lazy validation and result shape.')
assign('length reverse to_graphemes slice pad', 'StringModernExecution.cs', 'Grapheme traversal, binary/list representation, integer bounds and source-ordered guards.')
assign('equal', 'StringComparisonExecution.cs', 'Exact binary shortcut, scalar streams, casefold and dynamic normalization dispatch.')
assign('uppercase lowercase titlecase casefold', 'StringCaseExecution.cs', 'Latin1/ASCII fast paths and unchanged pinned mapping; preserve suffix and helper continuations.')
assign('prefix split find replace', 'StringPatternSearch.cs', 'Preserve converted term before helper matching; actual native binary pattern construction/match and directional traversal.')
assign('trim chomp', 'StringTrim.cs', 'Source defaults, empty separators, leading/trailing traversal and original list membership.')
assign('take', 'StringTake.cs', 'Boolean/direction guards and empty separators; single/multiple inverse source branches.')
assign('lexemes nth_lexeme', 'StringLexemes.cs', 'Source clause priority, separator patterns and arbitrary-precision lexeme index.')
assign('len concat chars copies to_upper to_lower join', 'StringLegacyExecution.cs', 'Obsolete API preserves arbitrary term payloads, exact guards, append/native calls and error continuations.')
assign('chr rchr str rstr span cspan tokens', 'StringLegacySearchExecution.cs', 'One-based positions, matching order, list/improper-tail behavior and native reverse frames.')
assign('substr sub_string', 'StringLegacySubstringExecution.cs', 'One-based drop/keep recursion and inclusive end alias; non-list errors at actual reached helper.')
assign('words sub_word strip left right centre', 'StringLegacyWordExecution.cs', 'Space defaults, count-before-select order, integer guards and Latin1 padding.')
assign('list_to_integer', 'StringLegacy.cs', 'ASCII sign/digit prefix induction with BigInteger and exact untouched tail; native big fallback is a dependency.')
handlers['list_to_float'] = ('../Primitives/StringFloatNative.cs', 'Shared native grammar plus invariant finite IEEE754 conversion; source owner is erts/emulator/beam/bif.c, not its Erlang NIF stub.')
defaults = {
 ('equal',2): 'IgnoreCase=false, normalization=none', ('equal',3): 'normalization=none',
 ('slice',2): 'length=infinity', ('pad',2): 'direction=trailing, character=space', ('pad',3): 'character=space',
 ('trim',1): 'direction=both, pinned pattern whitespace', ('trim',2): 'pinned pattern whitespace',
 ('take',2): 'complement=false, direction=leading', ('take',3): 'direction=leading',
 ('split',2): 'direction=leading', ('find',2): 'direction=leading', ('replace',3): 'direction=leading',
 ('chars',2): 'tail=[]', ('words',1): 'separator=space', ('strip',1): 'direction=both, separator=space',
 ('strip',2): 'separator=space', ('sub_word',2): 'separator=space', ('left',2): 'character=space',
 ('right',2): 'character=space', ('centre',2): 'character=space', ('sub_string',2): 'alias substr/2'
}
rows = []
reviews = {
 'is_empty': 'Only recursively empty lists/binaries are empty; other terms return false without Unicode validation.',
 'length': 'Latin1 pair fast path excludes CR; count grapheme clusters, preserve malformed-tail helper errors.',
 'to_graphemes': 'Return cluster terms, not scalar strings; retain non-tail recursion source frames.',
 'reverse': 'Reverse grapheme terms into a list even for binary input; do not reverse bytes or codepoints.',
 'equal': 'Binary equality shortcut, exact scalar comparison, identity shortcuts and dynamic normalization dispatch; boolean guard precedence.',
 'slice': 'Zero length precedes index validation; nonnegative start and positive/infinity length; preserve binary/list result and lazy suffix.',
 'pad': 'Length is integer; direction checked before traversal; arbitrary fill term, zero/negative deficit and odd both-padding placement.',
 'trim': 'Empty separators bypass direction validation; pinned whitespace default; original list membership and binary/mixed suffix boundaries.',
 'chomp': 'Trailing CRLF cluster or LF only; other whitespace retained; use original trim helper stack.',
 'take': 'Empty separator and direction/complement branch order; singleton versus multiple separators; cp_prefix exact continuation equality and lazy malformed suffix.',
 'lexemes': 'Literal empty list ignores separator type; other inputs require list separators; omit empty lexemes and retain chardata representation.',
 'nth_lexeme': 'N=1 and empty separators return original input; otherwise positive integer N and list separators, including huge absent indexes.',
 'uppercase': 'Pinned Unicode mapping and expansion; Latin1 fast path and malformed binary/list source frames; preserve unchanged binary.',
 'lowercase': 'Pinned Unicode mapping and expansion; Latin1 fast path and malformed binary/list source frames; preserve unchanged binary.',
 'titlecase': 'Map first codepoint only and preserve unvisited suffix; input shape guard differs from full case conversion.',
 'casefold': 'Pinned Unicode full casefold and expansions; no culture-sensitive BCL substitutions.',
 'prefix': 'Convert pattern first; converted empty returns input; malformed conversion tuple reaches prefix_1 unchanged; compare grapheme boundary and preserve remainder.',
 'split': 'Recursive empty pattern returns singleton input before direction checks; preserve split_1 matching and actual native resource/error order.',
 'replace': 'Use split result then lists:join with arbitrary replacement term; preserve nested output and replace call frames.',
 'find': 'Only literal [] or <<>> bypass direction and conversion; normalized-empty nested patterns reach native compile error; leading/trailing boundaries and native resources.',
 'jaro_similarity': 'Left grapheme list before right index-map traversal; strict open matching window, earliest unused candidate, sorted right matches and identical floating arithmetic order.',
 'next_codepoint': 'Direct pinned unicode_util:cp result, including error tuple and improper chardata continuation; no error rewriting.',
 'next_grapheme': 'Direct pinned unicode_util:gc result and CRLF/combining/ZWJ cluster representation; no error rewriting.',
 'to_float': 'Take numeric prefix, one Unicode list conversion, actual native list parser, exact original remainder, catch Erlang errors as badarg.',
 'to_integer': 'Take numeric prefix, one Unicode list conversion, actual string list parser, exact original remainder, catch Erlang errors as badarg; 4096 digits avoid repeated traversal.',
 'len': 'Native list length semantics and original string caller frame; binary rejected, arbitrary list elements permitted.',
 'concat': 'Actual list append primitive: arbitrary right tail, left traversal and improper-left error order.',
 'chr': 'Integer target; one-based first exact match can return before malformed tail; zero for absent match.',
 'rchr': 'Integer target; one-based last exact match requires traversal to end, including improper-tail helper failure.',
 'str': 'List pattern and exact element equality; first one-based match, original empty-pattern behavior and lazy improper suffix.',
 'rstr': 'List pattern and exact element equality; last match requires full source traversal and correct prefix helper failures.',
 'span': 'Actual lists:member exact equality; stop on first nonmember without reading suffix; preserve malformed separator errors.',
 'cspan': 'Actual lists:member exact equality; stop on first member without reading suffix; preserve malformed separator errors.',
 'substr': 'One-based drop then keep, source integer guards and zero-length precedence; reached tail must match original list clause.',
 'tokens': 'Distinct zero/single/multiple separator branches, exact term membership, suppress empty tokens and preserve reverse helper errors.',
 'chars': 'Positive count recurses with arbitrary C, but terminating zero clause requires integer C; arbitrary tail retained and reached error arguments preserved.',
 'copies': 'List input and nonnegative integer count guards; zero bypasses input traversal, positive count uses actual append semantics.',
 'words': 'Default space; trim/count branch order and integer separator guard; obsolete flat-list behavior, not Unicode segmentation.',
 'strip': 'left/right/both direction vocabulary, default space, exact integer separator, left-before-right and recursive error frames.',
 'sub_word': 'Count-before-select behavior, positive index and integer separator, source trimming and absent-word result.',
 'left': 'Integer length and pad character guards; actual native length before truncation/padding, negative length reaches original substr/chars failure.',
 'right': 'Integer length and pad character guards; native length then one-based suffix/padding, exact original returned list.',
 'sub_string': 'Alias substr using inclusive Stop-Start+1 without premature narrowing or additional validation.',
 'centre': 'Source zero-length shortcut, native length otherwise; floor split for padding and truncation with odd deficit.',
 'join': 'Empty outer list guards separator; nonempty clause permits lazy separator validation; actual append/comprehension bad-generator errors.',
 'to_upper': 'Obsolete scalar or list Latin1 mapping with fixed ranges; other integers retained, improper tail and noninteger payloads follow source helpers.',
 'to_lower': 'Obsolete scalar or list Latin1 mapping with fixed ranges; other integers retained, improper tail and noninteger payloads follow source helpers.',
 'list_to_float': 'Owning native bif.c grammar: sign, mandatory integer/radix/fraction digits, incomplete exponent rollback, untouched tail, finite IEEE conversion; platform dependency remains explicit.',
 'list_to_integer': 'ASCII sign/digit prefix recurrence with BigInteger, distinguish not_a_list/no_integer, preserve untouched proper or improper remainder.'
}
for name, arity in exports:
    clauses = []
    for m in re.finditer(r'^'+re.escape(name)+r'\(', reference, re.M):
        end, n = head_end(reference, m.end())
        if n != arity: continue
        arrow = reference.find('->', end, end+512)
        if arrow < 0 or '-doc' in reference[end:arrow] or '-spec' in reference[end:arrow]: continue
        clauses.append({'Line': reference.count('\n',0,m.start())+1, 'HeadAndGuard': ' '.join(reference[m.start():arrow].split())})
    assert clauses, (name, arity)
    filename, summary = handlers[name]
    path = source_dir/filename if name != 'list_to_float' else root/'src/Runtime/Erlang.Runtime.Core/Primitives/StringFloatNative.cs'
    assert path.is_file(), path
    rows.append({'Function':name, 'Arity':arity, 'OriginalClauses':clauses,
        'CSharpHandler':str(path.relative_to(root)).replace('\\','/'),
        'HandlerSha256':hashlib.sha256(path.read_bytes()).hexdigest().upper(),
        'SourceReviewObligation':summary, 'Defaults':defaults.get((name,arity),'explicit arguments'),
        'ContractReview':reviews[name],
        'AuditState':'Source contract reviewed; measured differential closure and remaining acceptance are recorded separately',
        'CompatibilityStatus':'Needs verification'})
output = root/a.output
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps({'Baseline':'OTP-29.1.1','Commit':'ad05823719d77c8faee87348ea39513d4e2f99c5',
    'ReferenceSha256':hashlib.sha256((root/a.reference).read_bytes()).hexdigest().upper(),
    'ExportCount':len(rows),'Method':'Source clauses, not documentation specs, determine acceptance. A catalog or case count does not certify compatibility.',
    'Apis':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'{len(rows)} exports; {sum(len(r["OriginalClauses"]) for r in rows)} public source clauses')
