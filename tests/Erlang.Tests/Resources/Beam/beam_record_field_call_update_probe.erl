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
-module(beam_record_field_call_update_probe).
-export([handle_cast/2]).

-record(state,
	{parent,
         panel,
	 app,         %% which tool is the user
	 expand_table,
         expand_wins=[],
         delayed_fetch,
         trunc_warn=[]}).

handle_cast({detail_win_closed, Id},#state{expand_wins=Opened0}=State) ->
    Opened = lists:keydelete(Id, 1, Opened0),
    {noreply, State#state{expand_wins=Opened}};

handle_cast(Msg, State) ->
    io:format("~p~p: Unhandled cast ~tp~n",[?MODULE, ?LINE, Msg]),
    {noreply, State}.
