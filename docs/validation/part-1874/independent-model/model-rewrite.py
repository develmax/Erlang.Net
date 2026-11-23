from collections import defaultdict
from hashlib import md5, sha256
from pathlib import Path
import re, struct, sys

catalog, root, installed, scratch = map(Path, sys.argv[1:])
arities = {int(op): int(arity) for op, arity in re.findall(r'new\(\s*(\d+),\s*"[^"]+",\s*(\d+),', catalog.read_text())}

def read_int(code, p):
    n = code[p]
    tag = n & 7
    if not n & 8:
        return tag, n >> 4, p + 1
    if not n & 16:
        return tag, ((n >> 5) << 8) | code[p + 1], p + 2
    size, p = (n >> 5) + 2, p + 1
    if size == 9:
        _, extra, p = read_int(code, p)
        size += extra
    return tag, int.from_bytes(code[p:p + size], 'big', signed=tag == 1), p + size

def operand(code, p):
    tag, value, end = read_int(code, p)
    if tag == 7:
        if value in (2, 4):
            _, _, end = read_int(code, end)
        elif value == 5:
            _, _, end = read_int(code, end)
            _, _, end = read_int(code, end)
        elif value == 3:
            _, count, end = read_int(code, end)
            for _ in range(count * 2):
                _, _, end = read_int(code, end)
        else:
            raise ValueError((tag, value))
    return (tag, value, code[p:end]), end

def decode(code):
    p = 0
    while p < len(code):
        start = p
        op = code[p]
        p += 1
        n = arities[op]
        args = []
        for _ in range(n):
            if code[p] == 0x17:
                tag, count, end = read_int(code, p + 1)
                args.append((7, count, code[p:end]))
                p = end
                for _ in range(count):
                    arg, p = operand(code, p)
                    args.append(arg)
            else:
                arg, p = operand(code, p)
                args.append(arg)
        yield op, args, code[start:p]

def rewrite(code):
    ins = list(decode(code))
    matches = []
    updated = []
    copied = 0
    for i in range(1, len(ins) - 10):
        sequence = [7,159,66,56,65,159,66,159,52,16]
        if [n for n,_,_ in ins[i-1:i+9]] != sequence:
            continue
        arg = lambda at: [(t,v) for t,v,_ in ins[at][1]]
        x0, x1, x2 = (3,0), (3,1), (3,2)
        f, call_result, outer_len, outer_tag = arg(i)
        record_len = arg(i+6)[2][1]
        if f[0] != 5 or f[1] == 0 or call_result != x0 or outer_len != (0,2) or outer_tag[0] != 2:
            continue
        if arg(i+1) != [x0,(0,1),x0] or arg(i+2) != [f,x0] or arg(i+3) != [x0,x1,x2]:
            continue
        if arg(i+4)[:3] != [f,x1,(0,2)] or arg(i+4)[3][0] != 2 or arg(i+5) != [x1,(0,1),x0]:
            continue
        if arg(i+6)[:3] != [f,x0,(0,record_len)] or arg(i+6)[3][0] != 2 or arg(i+7) != [f,x2]:
            continue
        j, fields = i + 9, {}
        while ins[j][0] == 66:
            source, field, dest = arg(j)
            if source != x0 or field[0] != 0 or not max(fields.values(), default=0) < field[1] < record_len:
                break
            if dest[0] != 3 or dest in fields or x0 in fields:
                break
            fields[dest] = field[1]
            j += 1
        if len(fields) < 2:
            continue
        if [n for n,_,_ in ins[j:j+14]] != [164,64,64,64,153,8,1,16,164,64,64,64,153,8]:
            continue
        def check_arm(pos, live):
            words, live_op = arg(i+8 if live else pos-1)
            target, count, *terms = arg(pos)
            if words != (0,count[1] + 1) or live_op != (0,live) or target != x0 or arg(pos+1) != [x0,x1]:
                return False
            first, last = arg(pos + 2), arg(pos + 3)
            arity, _, frame = arg(pos+5)
            return (first[0][0] == last[0][0] == 4 and first[1] == x0 and last[1] == x2 and arity == (0,3)
                and all(t in (0,1,2,3,4) or (t,v) == (7,4) for t,v in terms)
                and all(v < frame[1] for t,v in terms+first+last if t == 4))
        if arg(j+6) != [(0,f[1])] or not check_arm(j, 1) or not check_arm(j+8, 0):
            continue
        if [arg(j+p) for p in (2,3,5)] != [arg(j+p) for p in (10,11,13)]:
            continue
        if {a for a in arg(j)[2:] if a[0] == 3} != fields.keys() or any(t == 3 for t,v in arg(j+8)[2:]):
            continue

        def reg(n):
            assert 0 <= n < 16
            return bytes([(n << 4) | 3])

        def encoded(op, args):
            return bytes([op]) + b''.join(args)

        for at in range(copied, i+3):
            updated.append(ins[at][2])
        updated.extend([
            encoded(65, [reg(0), reg(1), reg(0)]),
            ins[i+4][2],
            encoded(66, [reg(1), ins[i+5][1][1][2], reg(1)]),
            encoded(159, [ins[i+6][1][0][2], reg(1), ins[i+6][1][2][2], ins[i+6][1][3][2]]),
            encoded(52, [ins[i+7][1][0][2], reg(0)]),
            encoded(16, [ins[i+8][1][0][2], bytes([0x20])]),
        ])
        mapped = {}
        for k, (dest, _) in enumerate(fields.items()):
            mapped[dest] = reg(1 if k == len(fields)-1 else (0 if k == 0 else k+1))
            updated.append(encoded(66, [reg(1), ins[i+9+k][1][1][2], mapped[dest]]))
        def emit_arm(at):
            updated.append(encoded(164,[reg(1)] + [mapped.get((t,v),raw) for t,v,raw in ins[at][1][1:]]))
            updated.extend(ins[at+n][2] for n in (3,2,4,5))
        emit_arm(j)
        updated.extend(ins[j+n][2] for n in (6,7))
        emit_arm(j+8)
        copied = j + 14
        meta = next(args for op,args,_ in reversed(ins[:i]) if op == 2)
        matches.append((meta[1][1], meta[2][1]))
    if not matches:
        return code, []
    updated.extend(raw for _,_,raw in ins[copied:])
    return b''.join(updated), matches

def chunks(path):
    data = path.read_bytes()
    assert data[:4] == b'FOR1' and data[8:12] == b'BEAM'
    p = 12
    view = {}
    while p < len(data):
        kind, size = struct.unpack_from('>4sI', data, p)
        view[kind] = data[p+8:p+8+size]
        p += 8 + (size + 3) // 4 * 4
    return view

def beam(view):
    body = b'BEAM'
    for kind, value in view.items():
        body += struct.pack('>4sI',kind,len(value)) + value + b'\0' * (-len(value)%4)
    return b'FOR1' + struct.pack('>I',len(body)) + body

def version(view):
    return md5(b''.join(view[k] for k in [b'AtU8',b'Code',b'StrT',b'ImpT',b'ExpT',b'LitT',b'Meta'] if k in view)).digest()

inputs = list(root.glob('late-list-and-tail-copy-provenance-1870/full-uncached/shard-*/otp-corpus.json.generated/lib/*/ebin/*.beam'))
overlays = list(root.glob('argument-copy-lifetimes-and-sctp-cleanup-1871/pilot.json.generated/lib/*/ebin/*.beam'))
replace = {tuple(p.parts[-3:]): p for p in overlays}
inputs = [replace.pop(tuple(p.parts[-3:]),p) for p in inputs] + list(replace.values())
inputs += [scratch/'managed-before.beam']
found, bad = defaultdict(list), []
for path in inputs:
    view = chunks(path)
    code = view[b'Code']
    origin = 4 + int.from_bytes(code[:4], 'big')
    try:
        updated, hits = rewrite(code[origin:])
    except (KeyError, ValueError, IndexError) as error:
        bad.append((path.name, str(error)))
        continue
    if not hits:
        continue
    old_digest = version(view)
    view[b'Code'] = code[:origin] + updated
    if b'FunT' not in view:
        if old_digest[::-1] in view[b'Attr']:
            view[b'Attr'] = view[b'Attr'].replace(old_digest[::-1], version(view)[::-1], 1)
        else:
            print('Missing old VS N digest',path)
    changed = scratch / 'modeled' / path.name
    changed.parent.mkdir(exist_ok=True)
    changed.write_bytes(beam(view))
    ref = installed / 'lib' / Path(*path.parts[-3:]) if path.suffix == '.beam' else path
    if ref.exists():
        ref_view = chunks(ref)
        different = [key.decode() for key in view.keys() | ref_view.keys() if view.get(key) != ref_view.get(key)]
        print('cohort',path.name,'hits',len(hits),'different',different,'exact',changed.read_bytes() == ref.read_bytes(), 'sha256',sha256(changed.read_bytes()).hexdigest())
    else:
        print('fixture', path.name,'hits',len(hits))
    found[path.name] = hits
print('scanned', len(inputs), 'matched', len(found), 'unsupported_decode', bad)
