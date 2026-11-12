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

-module(beam_detail_window_probe).
-export([start_link/5, init/6]).
-record(state, {parent,frame,id,pages}).

start_link(Id, Data, ParentFrame, Callback, App) ->
    wx_object:start_link(?MODULE, [Id, Data, ParentFrame, Callback, App, self()], []).

init(Id,ParentFrame,Callback,App,Parent,{Title,Info,TW}) ->
    Scale = observer_wx:get_scale(),
    Frame=wxFrame:new(ParentFrame, -1, [Title],
		      [{style, 541072960}, {size, {Scale*850,Scale*600}}]),
    MenuBar = wxMenuBar:new(),
    create_menus(MenuBar),
    wxFrame:setMenuBar(Frame, MenuBar),

    Panel = wxPanel:new(Frame, []),
    Sizer = wxBoxSizer:new(8),
    {InfoPanel,Pages} = create_pages(Panel,Callback:detail_pages(),[Info]),
    wxSizer:add(Sizer, InfoPanel, [{proportion, 1}, {flag, 8192}]),

    case TW of
	[] ->
	    undefined;
	_ ->
	    StatusBar = observer_lib:create_status_bar(Panel),
	    wxSizer:add(Sizer, StatusBar, [{flag, 8192 bor 240},
					   {proportion, 0},
					   {border,4}]),
	    wxTextCtrl:writeText(StatusBar, TW),
	    StatusBar
    end,

    wxPanel:setSizer(Panel, Sizer),

    wxFrame:connect(Frame, close_window),
    wxMenu:connect(Frame, command_menu_selected),
    wxFrame:show(Frame),
    destroy_progress(App),
    {Frame, #state{parent=Parent,
		   id=Id,
		   frame=Frame,
		   pages=Pages
		  }}.

create_menus(Menu) -> detail_view:menus(Menu).
create_pages(Panel, Specs, _Args) -> {Panel, Specs}.
destroy_progress(App) -> detail_view:destroy(App).
