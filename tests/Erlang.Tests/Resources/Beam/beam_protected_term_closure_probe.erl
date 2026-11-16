%%
%% %CopyrightBegin%
%%
%% SPDX-License-Identifier: Apache-2.0
%%
%% Copyright Ericsson AB 2013-2025. All Rights Reserved.
%%
%% Licensed under the Apache License, Version 2.0 (the "License");
%% you may not use this file except in compliance with the License.
%% You may obtain a copy of the License at
%%
%%     http://www.apache.org/licenses/LICENSE-2.0
%%
%% Unless required by applicable law or agreed to in writing, software
%% distributed under the License is distributed on an "AS IS" BASIS,
%% WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
%% See the License for the specific language governing permissions and
%% limitations under the License.
%%
%% %CopyrightEnd%

-module(beam_protected_term_closure_probe).
-export([format_term_fun/4]).

format_term_fun(Format,Term,Tab,Cs) ->
    fun() ->
            observer_lib:report_progress({ok,"Formatting term"}),
            observer_lib:report_progress({ok,start_pulse}),
	    try io_lib:format(Format,[Term]) of
		Str -> {expand, plain_html(Str,Cs), Tab}
	    catch error:badarg ->
		    Warning = "This term cannot be formatted with " ++ Format,
		    observer_html_lib:warning(Warning,Cs)
            after
                    observer_lib:report_progress({ok,stop_pulse})
	    end
    end.

plain_html(Text,Cs) ->
    observer_html_lib:plain_page(Text,Cs).

