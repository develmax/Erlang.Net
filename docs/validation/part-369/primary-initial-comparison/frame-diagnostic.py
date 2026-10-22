"""Diagnostic display only. Whole trees remain in whole-differences and raw ETF."""
import ast, collections, json
from pathlib import Path

root = Path(__file__).resolve().parent
data = json.loads((root / 'whole-differences.json').read_bytes())['Rows']

def parse(text):
    def read(node):
        if isinstance(node, ast.Constant):
            return node.value
        if isinstance(node, ast.Tuple):
            return tuple(read(x) for x in node.elts)
        if isinstance(node, ast.Set):
            return set(read(x) for x in node.elts)
        if isinstance(node, ast.List):
            return [read(x) for x in node.elts]
        if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.USub):
            return -read(node.operand)
        if isinstance(node, ast.Call) and isinstance(node.func, ast.Name) and node.func.id == 'frozenset' and not node.keywords and len(node.args) <= 1:
            return frozenset(read(node.args[0]) if node.args else ())
        raise ValueError('Unsupported diagnostic data representation')
    return read(ast.parse(text, mode='eval').body)

def frame(node):
    fields = node[1]
    metadata = fields[3]
    location = []
    if metadata[0] == 'list':
        for item in metadata[1]:
            key, value = item[1]
            if key == ('atom', 'line'):
                location.append(str(value[1]))
            elif key == ('atom', 'file'):
                location.append(''.join(chr(x[1]) for x in value[1]))
    return fields[0][1] + ':' + fields[1][1] + '/' + (str(fields[2][1]) if fields[2][0] == 'integer' else 'args') + '@' + ':'.join(location)

counts = collections.Counter()
examples = {}
first_fields = []
for row in data:
    left, right = parse(row['OriginalWhole']), parse(row['SdkWhole'])
    if row['Depth'] == 'values':
        counts['values_' + ('opaque' if row['OpaqueOnly'] else 'other')] += 1
    if len(left[1]) == len(right[1]) == 5:
        lf, rf = left[1][3][1], right[1][3][1]
        kind = 'first_frame' if lf and rf and lf[0] != rf[0] else 'caller_frame_or_opaque'
        counts[kind] += 1
        key = (row['Group'], kind)
        if key not in examples:
            examples[key] = dict(Group=row['Group'], Kind=kind, Case=row['Case'], Depth=row['Depth'], OriginalFrames=[frame(x) for x in lf], SdkFrames=[frame(x) for x in rf])
        if not row['OpaqueOnly']:
            for index in range(max(len(lf),len(rf))):
                if index >= len(lf) or index >= len(rf):
                    first_fields.append(dict(Group=row['Group'], Case=row['Case'], Depth=row['Depth'], Path='Stack['+str(index)+']', Original=repr(lf[index]) if index<len(lf) else '<absent>', Sdk=repr(rf[index]) if index<len(rf) else '<absent>'))
                    break
                if lf[index] != rf[index]:
                    for field, a, b in zip(['Module','Function','ArityOrArguments','Metadata'],lf[index][1],rf[index][1]):
                        if a != b:
                            first_fields.append(dict(Group=row['Group'], Case=row['Case'], Depth=row['Depth'], Path='Stack['+str(index)+'].'+field, Original=repr(a), Sdk=repr(b)))
                            break
                    break
result = dict(DiagnosticOnly=True, Counts=dict(counts), Examples=list(examples.values()), FirstDifferingFrameFields=first_fields)
(root / 'frame-diagnostics.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(Counts=dict(counts), FirstFields=len(first_fields), AtEmpty=[x for x in first_fields if x['Case']=='at2_empty' and 'default' in x['Depth']]), indent=2))
