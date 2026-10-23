from pathlib import Path
p=Path('artifacts/library452-probe1/ddll_tcp452.erl');s=p.read_text();s=s.replace('Previous=application:get_env(kernel,epmd_module),put(previous_epmd452,Previous),','Previous=application:get_env(kernel,epmd_module),put(previous_epmd452,Previous),put(race_env_saved452,true),')
s=s.replace('    put(last_checker452,CheckPid),{Reason,Result,wait_up(Node,5000)}.','    erase(race_env_saved452),put(last_checker452,CheckPid),{Reason,Result,wait_up(Node,5000)}.')
s=s.replace('    case erase(previous_epmd452) of undefined->ok;{ok,Previous}->application:set_env(kernel,epmd_module,Previous) end,','    case erase(race_env_saved452) of\n        true->case erase(previous_epmd452) of undefined->ok=application:unset_env(kernel,epmd_module);{ok,Previous}->ok=application:set_env(kernel,epmd_module,Previous) end;\n        _->ok\n    end,')
p.write_bytes(s.encode())
