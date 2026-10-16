-module(oracle_proc_format).
-export([run/0,format_error/2]).
run()->erase(format_callback_called),Stack=[{oracle_proc_format,missing,0,[{error_info,#{module=>oracle_proc_format}}]}],_Text=proc_lib:format([[{error_info,{error,undef,Stack}}],[]]),undefined=get(format_callback_called),ok.
format_error(_Reason,_Stack)->put(format_callback_called,true),#{reason=>"override"}.
