"""Whole-field pairwise relation: only the source-created live reference may differ."""

def provenance(outcome):
    fields = outcome[1]
    assert outcome[0] == 'tuple' and fields[0] == ('atom', 'error')
    assert fields[1] == ('atom', 'error') and fields[2] == ('atom', 'badarg')
    effects = fields[-1]
    assert effects[0] == 'tuple' and len(effects[1]) == 2
    assert effects[1][0] == ('list', ())
    entries = effects[1][1]
    assert entries[0] == 'list' and len(entries[1]) == 1
    witness = entries[1][0]
    assert witness[0] == 'tuple'
    values = witness[1]
    role = values[0]
    assert role in [('atom', 'ordinary'), ('atom', 'compiled')]
    if role == ('atom', 'ordinary'):
        assert len(values) == 2 and values[1][0] == 'reference'
        return witness, values[1], 'ordinary'
    assert len(values) == 6 and values[1] == ('atom', 'bm') and values[2][0] == 'reference'
    assert values[3][0] == values[4][0] == 'binary'
    assert values[5][0] == 'tuple' and len(values[5][1]) == 2
    assert all(x[0] == 'integer' for x in values[5][1])
    return witness, values[2], 'compiled'

def check_binding(left, right, own, decoder):
    native_source, native_ref, native_role = provenance(left)
    own_source, own_ref, own_role = provenance(right)
    assert native_role == own_role == own['Role']
    assert own['LocalIdentityBound'] is True
    assert decoder.decode_exact(bytes.fromhex(own['ReferenceEtfHex'])) == own_ref
    assert decoder.decode_exact(bytes.fromhex(own['ProvenanceEtfHex'])) == own_source
    if own_role == 'compiled':
        assert own['RegistryAcquired'] is True and own['ResourceKind'] == 'BinarySearchPattern'
        assert own['PayloadTag'] == own_source[1][1][1]
        assert decoder.decode_exact(bytes.fromhex(own['ReplayedMatchEtfHex'])) == own_source[1][5]
    else:
        assert own['RegistryAcquired'] is False
        assert own['ResourceKind'] is None and own['PayloadTag'] is None
        assert own['ReplayedMatchEtfHex'] is None
    # Compare source tags, pattern/subject bytes and match result without altering either tree.
    assert equal(native_source, own_source, native_ref, own_ref)
    return native_ref, own_ref

def equal(left, right, native_ref, own_ref):
    if isinstance(left, tuple) and left and left[0] == 'reference':
        return left == native_ref and right == own_ref
    if isinstance(right, tuple) and right and right[0] == 'reference':
        return False
    if isinstance(left, tuple) and isinstance(right, tuple):
        return len(left) == len(right) and all(equal(a, b, native_ref, own_ref) for a, b in zip(left, right))
    # Identity-bearing maps are outside the six predeclared source expressions.
    # All maps here (error_info metadata) remain strict whole-map equality.
    return left == right
