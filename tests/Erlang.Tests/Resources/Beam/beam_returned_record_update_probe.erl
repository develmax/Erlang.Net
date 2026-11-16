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
-module(beam_returned_record_update_probe).
-export([init/1]).

-record(state,
	{parent,
         panel,
	 app,         %% which tool is the user
	 expand_table,
         expand_wins=[],
         delayed_fetch,
         trunc_warn=[]}).

init([ParentWin, Callback]) when is_atom(Callback) ->
    init(ParentWin, Callback);
init([ParentWin, {App, Fun}]) when is_function(Fun) ->
    init([ParentWin, {App, Fun()}]);
init([ParentWin, {expand,HtmlText,Tab}]) ->
    init(ParentWin, HtmlText, Tab, cdv);
init([ParentWin, {App, {expand,HtmlText,Tab}}]) ->
    init(ParentWin, HtmlText, Tab, App);
init([ParentWin, {App,HtmlText}]) ->
    init(ParentWin, HtmlText, undefined, App);
init([ParentWin, HtmlText]) ->
    init(ParentWin, HtmlText, undefined, cdv).

init(ParentWin, HtmlText, Tab, App) ->
    %% If progress dialog is shown, remove it now - and sett cursor busy instead
    observer_lib:destroy_progress_dialog(),
    wx_misc:beginBusyCursor(),
    HtmlWin = observer_lib:html_window(ParentWin),
    wxHtmlWindow:setPage(HtmlWin,HtmlText),
    wx_misc:endBusyCursor(),
    {HtmlWin, #state{parent=ParentWin, panel=HtmlWin,expand_table=Tab,app=App}}.

init(ParentWin, Callback) ->
    {HtmlWin, State} = init(ParentWin, "", undefined, cdv),
    {HtmlWin, State#state{delayed_fetch=Callback}}.
