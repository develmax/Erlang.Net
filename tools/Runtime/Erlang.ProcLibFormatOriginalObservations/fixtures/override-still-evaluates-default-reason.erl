-module(oracle_proc_format).
-export([run/0,format_error/2]).
run()->Stack=[{oracle_proc_format,run,0,[{error_info,#{module=>oracle_proc_format}}]}],Reason={badarity,{fun()->ok end,bad}},try proc_lib:format([[{error_info,{error,Reason,Stack}}],[]]) of Text->{unexpected,Text} catch error:badarg->ok end.
format_error(_Reason,_Stack)->#{reason=>"override"}.
