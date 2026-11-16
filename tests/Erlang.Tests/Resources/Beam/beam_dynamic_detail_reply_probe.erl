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

-module(beam_dynamic_detail_reply_probe).
-export([init/1]).

init([Id, Data, ParentFrame, Callback, App, Parent]) ->
    display_progress(ParentFrame,App),
    case Callback:get_details(Id, Data) of
	{ok,Details} ->
            display_progress_pulse(Callback,Id),
	    init(Id,ParentFrame,Callback,App,Parent,Details);
	{yes_no, Info, Fun} ->
            destroy_progress(App),
	    case observer_lib:display_yes_no_dialog(Info) of
		5103 -> Fun();
		5104 -> ok
	    end,
	    {stop,normal};
	{info,Info} ->
            destroy_progress(App),
	    observer_lib:display_info_dialog(ParentFrame,Info),
	    {stop,normal}
    end.

display_progress(Frame, App) -> detail_view:progress(Frame, App).
display_progress_pulse(cdv_proc_cb, Pid) ->
    observer_lib:report_progress({ok,"Displaying data for "++Pid}),
    observer_lib:report_progress({ok,start_pulse});
display_progress_pulse(_,_) ->
    ok.
destroy_progress(App) -> detail_view:destroy(App).
init(Id, Frame, Callback, App, Parent, Details) -> detail_view:init(Id, Frame, Callback, App, Parent, Details).
