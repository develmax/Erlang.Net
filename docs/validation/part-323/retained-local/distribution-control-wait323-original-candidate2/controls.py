from pathlib import Path
import copy
import importlib.util
import json

root = Path(__file__).resolve().parent
source = root / 'overlay/tools/Erlang.DistributionControlWaitObservations'
def load(name):
    spec = importlib.util.spec_from_file_location('synthetic323_' + name, source / (name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module
c = load('collect')
d = load('passive_control_etf')
A, I = c.atom, c.integer
T = lambda *items: ('tuple', tuple(items))
L = lambda *items: ('list', tuple(items))
names = {role: role + '@127.0.0.1' for role in ('a', 'b', 'c')}
pid = lambda role, n: ('pid', A(names[role]), n, 0, 1, 88)
ref = lambda n: ('reference', A(names['a']), 1, (n,), 90)
port = lambda n: ('port', A(names['a']), n, 1, 120)
owner, guest, filler, worker, killer = pid('a', 1), pid('a', 2), pid('a', 3), pid('b', 1), pid('c', 1)
bctrl, cctrl = port(1), port(2)
event = T(A('monitor'), guest, A('busy_dist_port'), bctrl)
observation = T(A('observation'), A('send'), T(A('identities'), owner, guest, worker, filler, killer, bctrl, cctrl),
    T(A('prepared'), L(T(A('links'), L(worker)), T(A('trap_exit'), A('true')), T(A('async_dist'), A('false'))), T(A('status'), A('suspended'))),
    T(A('busy'), event, L(T(T(A('monitor'), filler, A('busy_dist_port'), bctrl), I(1)), T(event, I(2))), L()),
    T(A('query'), ref(1), T(A('return'), T(L(T(A('links'), L(worker)), T(A('status'), A('suspended')),
        T(A('async_dist'), A('false')), T(A('current_function'), T(A('erlang'), A('send'), I(2)))), I(3), I(4)))),
    T(A('kill'), ref(2), T(A('kill_ack'), ref(2), killer, T(A('return'), A('true'))), T(A('DOWN'), ref(3), A('process'), guest, A('killed'))),
    T(A('postcall'), A('false')))

def replace(value, old, new):
    if value == old:
        return new
    if type(value) is tuple:
        return tuple(replace(item, old, new) for item in value)
    return value

passed = []
c.verify_observation(observation, 'send', names)
passed.append('synthetic_complete_observation_accepts')
refusals = {
    'filler_event_substitution': replace(observation, event, T(A('monitor'), filler, A('busy_dist_port'), bctrl)),
    'wrong_target_controller': replace(observation, event, T(A('monitor'), guest, A('busy_dist_port'), cctrl)),
    'wrong_killed_reason_noproc': replace(observation, A('killed'), A('noproc')),
    'wrong_killed_reason_noconnection': replace(observation, A('killed'), A('noconnection')),
    'postcall_returned': replace(observation, T(A('postcall'), A('false')), T(A('postcall'), A('true'))),
    'async_true': replace(observation, T(A('async_dist'), A('false')), T(A('async_dist'), A('true'))),
    'status_running': replace(observation, T(A('status'), A('suspended')), T(A('status'), A('running'))),
    'wrong_guest_node': replace(observation, guest, pid('b', 2)),
    'guest_filler_same': replace(observation, guest, filler),
    'same_query_kill_ref': replace(observation, ref(1), ref(2)),
    'query_links_missing': replace(observation, T(A('links'), L(worker)), T(A('links'), L())),
    'non_numeric_function_arity': replace(observation, T(A('erlang'), A('send'), I(2)), T(A('erlang'), A('send'), A('two'))),
    'reversed_query_times': replace(observation, I(4), I(0)),
    'same_two_controllers': replace(observation, cctrl, bctrl),
}
for name, value in refusals.items():
    try:
        c.verify_observation(value, 'send', names)
    except (ValueError, RuntimeError, IndexError, TypeError):
        passed.append('refuse_' + name)
    else:
        raise AssertionError(name)

node = bytes([119, 1]) + b'a'
vectors = [bytes([131, 102]) + node + (0x0fffffff).to_bytes(4, 'big') + bytes([3]),
           bytes([131, 89]) + node + (0x0fffffff).to_bytes(4, 'big') + (0xffffffff).to_bytes(4, 'big'),
           bytes([131, 120]) + node + (0xffffffffffffffff).to_bytes(8, 'big') + (0xffffffff).to_bytes(4, 'big')]
for raw in vectors:
    value, consumed = d.decode_one(raw)
    assert consumed == len(raw) and value[0] == 'port'
    passed.append('synthetic_port_tag_' + str(raw[1]))
    for length in range(len(raw)):
        try:
            d.decode_one(raw[:length])
        except (ValueError, UnicodeError):
            passed.append('refuse_truncated_' + str(raw[1]) + '_' + str(length))
        else:
            raise AssertionError('truncated port')
bad_ports = [vectors[0][:-1] + bytes([4]),
             bytes([131, 102]) + node + (0x10000000).to_bytes(4, 'big') + bytes([1]),
             bytes([131, 89]) + node + (0x10000000).to_bytes(4, 'big') + bytes(4),
             bytes([131, 120, 97, 1]) + bytes(12),
             bytes([130]) + vectors[2][1:]]
for index, raw in enumerate(bad_ports):
    try:
        d.decode_one(raw)
    except (ValueError, UnicodeError):
        passed.append('refuse_malformed_port_' + str(index))
    else:
        raise AssertionError('malformed port')

identity = {'Pid': 123, 'Pgid': 123, 'State': 'T', 'StartTime': '12345'}
phases = ['all-role-metadata', 'before-SIGSTOP', 'SIGSTOP-confirmed-T', 'stop-ack',
          'query-completed-while-stopped', 'proof-query-and-killed-before-resume', 'SIGCONT', 'resume-ack']
journal = [{'Phase': phase, 'ParentMonotonicNs': index} for index, phase in enumerate(phases)]
for index in (2, 4):
    journal[index]['Identity'] = copy.deepcopy(identity)
journal[5]['StoppedB'] = copy.deepcopy(identity)
journal[6].update(Before=copy.deepcopy(identity), After={**identity, 'State': 'S'})
c.verify_timeline(journal)
passed.append('synthetic_complete_parent_timeline_accepts')
bad_journals = []
value = copy.deepcopy(journal); value[4], value[6] = value[6], value[4]; bad_journals.append(value)
value = copy.deepcopy(journal); value[5]['StoppedB']['State'] = 'S'; bad_journals.append(value)
value = copy.deepcopy(journal); value[6]['After']['StartTime'] = 'other'; bad_journals.append(value)
value = copy.deepcopy(journal); value.pop(3); bad_journals.append(value)
value = copy.deepcopy(journal); value.append(copy.deepcopy(value[6])); bad_journals.append(value)
for index, value in enumerate(bad_journals):
    try:
        c.verify_timeline(value)
    except (ValueError, RuntimeError, KeyError):
        passed.append('refuse_timeline_' + str(index))
    else:
        raise AssertionError('timeline')
print(json.dumps({'Status': 'PASS_SYNTHETIC_PASSIVE_ONLY_323_CONTROLS', 'Count': len(passed), 'Names': passed,
                  'OriginalVmExecuted': False, 'NativeNetworkExecuted': False, 'SdkExecuted': False}, indent=2))
