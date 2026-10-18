"""Passive relation primitives. No capture execution, rewriting or native partition seeding."""
def snapshot(arguments, count):
    assert arguments[0] == 'list' and len(arguments[1]) == 2
    remaining, accumulator = arguments[1]
    assert remaining[0] == 'improper' and remaining[2] == ('atom', 'tail')
    assert accumulator[0] == 'list'
    consumed = len(accumulator[1])
    assert 0 <= consumed < count and len(remaining[1]) == count - consumed
    assert all(value == ('integer', consumed + index + 1) for index, value in enumerate(remaining[1]))
    assert all(value == ('integer', consumed - index) for index, value in enumerate(accumulator[1]))
    return dict(Consumed=consumed,Remaining=count-consumed,Terminal=('atom','tail'),InitialCount=count,
        ExactUniqueSuffix=True,ExactReversedAccumulator=True)

def frame(outcome):
    assert outcome[0] == 'tuple' and len(outcome[1]) == 4
    tag, kind, reason, stack = outcome[1]
    assert tag == ('atom','error') and kind == ('atom','error') and reason == ('atom','badarg')
    assert stack[0] == 'list' and stack[1]
    first = stack[1][0]
    assert first[0] == 'tuple' and len(first[1]) == 4
    assert first[1][0] == ('atom','lists') and first[1][1] == ('atom','reverse')
    return first

def compare_reverse(original, sdk, count, own_protocol_review=None):
    """First-frame arguments only. Review input must be independently source/hash-admitted by caller."""
    a, b = frame(original), frame(sdk)
    # Inspect every remaining field explicitly; do not project a rewritten outcome.
    assert original[1][:3] == sdk[1][:3]
    assert len(original[1][3][1]) == len(sdk[1][3][1])
    assert original[1][3][1][1:] == sdk[1][3][1][1:]
    assert a[1][0:2] == b[1][0:2] and a[1][3] == b[1][3]
    left, right = snapshot(a[1][2], count), snapshot(b[1][2], count)
    exact = a[1][2] == b[1][2]
    reviewed = bool(own_protocol_review and own_protocol_review.get('ActualOwnTrapEntrySnapshotsSourceReviewed') is True
        and own_protocol_review.get('NativePartitionSeededIntoMain') is False
        and own_protocol_review.get('EveryBudgetHeapStateDifferenceExplained') is True)
    return dict(StrictArgumentsEqual=exact,OriginalSnapshot=left,SdkSnapshot=right,
        OtherWholeFieldsExact=True,DecompositionProved=True,
        ActualOwnProtocolSourceReviewRequired=not exact,
        Accepted=exact or reviewed,
        Status='PASS_STRICT' if exact else 'PASS_SOURCE_BOUND_OWN_TRAP_PROTOCOL' if reviewed else 'INSUFFICIENT_OWN_PROTOCOL_BINDING')
