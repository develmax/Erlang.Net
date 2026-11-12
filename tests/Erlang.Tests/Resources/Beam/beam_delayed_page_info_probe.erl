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
-module(beam_delayed_page_info_probe).
-export([handle_info/2]).

-record(state,
	{parent,
         panel,
	 app,         %% which tool is the user
	 expand_table,
         expand_wins=[],
         delayed_fetch,
         trunc_warn=[]}).

handle_info(active, #state{parent=Parent, panel=HtmlWin,delayed_fetch=Callback}=State)
  when Callback=/=undefined ->
    observer_lib:display_progress_dialog(HtmlWin,
                                         "Crashdump Viewer",
                                         "Reading data"),
    {{expand,Title,Info,Tab},TW} = Callback:get_info(),
    Cs = observer_lib:colors(Parent),
    HtmlText = observer_html_lib:expandable_term(Title,Info,Tab,Cs),

    observer_lib:sync_destroy_progress_dialog(),
    wx_misc:beginBusyCursor(),
    wxHtmlWindow:setPage(HtmlWin,HtmlText),
    cdv_wx_set_status(State, TW),
    wx_misc:endBusyCursor(),
    {noreply, State#state{expand_table=Tab,
                          delayed_fetch=undefined,
                          trunc_warn=TW}};

handle_info(active, State) ->
    cdv_wx_set_status(State, State#state.trunc_warn),
    {noreply, State};

handle_info(Info, State) ->
    io:format("~p:~p: Unhandled info: ~tp~n", [?MODULE, ?LINE, Info]),
    {noreply, State}.

cdv_wx_set_status(_, _) -> ok.
