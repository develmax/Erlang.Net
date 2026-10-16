# Passive bounded diagnostic decoder, never an Erlang evaluator.
# Baseline decode_one copied from the hash-bound existing verifier; extension.diff
# contains every parser change. This bounded profile rejects COMPRESSED/FUN_EXT.
MAX_BYTES = 16 * 1024 * 1024
MAX_DEPTH = 128
MAX_TERMS = 100_000
VERSION = 131


def require(value, message):
    if not value:
        raise ValueError(message)



def baseline_map_key(value):
    # Provenance spans must never become map-key semantic identity. Restrict
    # map keys to the original decoder profile; extended terms remain values.
    if value[0] in ('atom', 'integer'):
        return True
    if value[0] in ('tuple', 'list'):
        return all(baseline_map_key(item) for item in value[1])
    if value[0] == 'improper':
        return all(baseline_map_key(item) for item in value[1]) and baseline_map_key(value[2])
    if value[0] == 'map':
        return all(baseline_map_key(key) and baseline_map_key(item) for key, item in value[1])
    return False


def decode_one(data):
    require(len(data) <= MAX_BYTES and len(data) > 1 and data[0] == VERSION, 'ETF version/size')
    position = 1
    decoded_terms = 0

    def reserve_terms(count):
        nonlocal decoded_terms
        require(0 <= count <= MAX_TERMS - decoded_terms, 'Aggregate decoded term budget')
        decoded_terms += count

    def require_room(count):
        require(0 <= count <= MAX_TERMS - decoded_terms, 'Container exceeds remaining term budget')

    def take(count):
        nonlocal position
        require(count >= 0 and position + count <= len(data), 'Truncated ETF')
        result = data[position:position + count]
        position += count
        return result

    def count32():
        return int.from_bytes(take(4), 'big')

    def term(depth):
        require(depth <= MAX_DEPTH, 'ETF diagnostic depth limit')
        reserve_terms(1)
        tag = take(1)[0]
        if tag == 97:
            return ('integer', take(1)[0])
        if tag == 98:
            return ('integer', int.from_bytes(take(4), 'big', signed=True))
        if tag in (110, 111):
            count = take(1)[0] if tag == 110 else count32()
            sign = take(1)[0]
            require(sign in (0, 1), 'Invalid big integer sign')
            number = int.from_bytes(take(count), 'little')
            return ('integer', -number if sign else number)
        if tag in (100, 115, 118, 119):
            size = take(1)[0] if tag in (115, 119) else int.from_bytes(take(2), 'big')
            return ('atom', take(size).decode('utf-8' if tag in (118, 119) else 'latin1'))
        if tag in (104, 105):
            size = take(1)[0] if tag == 104 else count32()
            require(size <= len(data) - position, 'Tuple size exceeds available data')
            require_room(size)
            return ('tuple', tuple(term(depth + 1) for _ in range(size)))
        if tag == 106:
            return ('list', ())
        if tag == 107:
            size = int.from_bytes(take(2), 'big')
            reserve_terms(size)
            return ('list', tuple(('integer', byte) for byte in take(size)))
        if tag == 108:
            size = count32()
            require(size <= len(data) - position, 'List size exceeds available data')
            require_room(size + 1)
            items = tuple(term(depth + 1) for _ in range(size))
            tail = term(depth + 1)
            if tail[0] == 'list':
                return ('list', items + tail[1])
            if tail[0] == 'improper':
                return ('improper', items + tail[1], tail[2])
            return ('improper', items, tail)
        if tag == 116:
            size = count32()
            require(size <= (len(data) - position) // 2, 'Map size exceeds available data')
            require_room(size * 2)
            pairs = [(term(depth + 1), term(depth + 1)) for _ in range(size)]
            require(all(baseline_map_key(key) for key, _ in pairs), 'Extended identity-bearing map key outside diagnostic profile')
            require(len({key for key, _ in pairs}) == size, 'Duplicate map key')
            return ('map', frozenset(pairs))
        if tag in (88, 103):
            node = term(depth + 1)
            require(node[0] == 'atom', 'PID node must be atom')
            identity, serial = count32(), count32()
            creation = count32() if tag == 88 else take(1)[0]
            require(tag == 88 or creation <= 3, 'Legacy PID creation bits')
            return ('pid', node, identity, serial, creation, tag)
        if tag in (90, 114):
            count = int.from_bytes(take(2), 'big')
            require(1 <= count <= 5, 'Reference word count bound')
            node = term(depth + 1)
            require(node[0] == 'atom', 'Reference node must be atom')
            creation = count32() if tag == 90 else take(1)[0]
            require(tag == 90 or creation <= 3, 'Legacy reference creation bits')
            return ('reference', node, creation, tuple(count32() for _ in range(count)), tag)
        if tag == 109:
            return ('binary', take(count32()))
        if tag == 77:
            count = count32()
            bits = take(1)[0]
            require(count > 0 and 1 <= bits <= 8, 'Bit binary dimensions')
            value = take(count)
            require(value[-1] & ((1 << (8 - bits)) - 1) == 0, 'Bit binary padding')
            return ('bit_binary', value, bits)
        if tag == 70:
            return ('float_bits', take(8))
        if tag == 113:
            module, function, arity = term(depth + 1), term(depth + 1), term(depth + 1)
            require(module[0] == function[0] == 'atom' and arity[0] == 'integer' and 0 <= arity[1] <= 255, 'Export fun shape')
            return ('export_fun', module, function, arity)
        if tag == 112:
            reserve_terms(16)
            size_start = position
            size = count32()
            end = size_start + size
            require(size >= 4 and end <= len(data), 'New fun declared size bound')
            arity = take(1)[0]
            uniq, index, count = take(16), count32(), count32()
            require(count <= end - position, 'New fun free count bound')
            require_room(count + 4)
            module, old_index, old_uniq, pid = (term(depth + 1) for _ in range(4))
            require(module[0] == 'atom' and old_index[0] == old_uniq[0] == 'integer' and pid[0] == 'pid', 'New fun fixed field types')
            free = tuple(term(depth + 1) for _ in range(count))
            require(position == end, 'New fun exact declared boundary')
            # Passive original-file span: never copy nested fun encodings.
            # The guard records the raw ETF file SHA; this span recovers exact bytes.
            return ('new_fun', size, arity, uniq, index, module, old_index, old_uniq, pid, free, ('raw_span', size_start - 1, end - size_start + 1))
        raise ValueError('Unsupported ETF tag: ' + str(tag))

    result = term(0)
    return result, position


def decode_exact(data):
    result, used = decode_one(data)
    require(used == len(data), 'Trailing bytes in result ETF')
    return result
