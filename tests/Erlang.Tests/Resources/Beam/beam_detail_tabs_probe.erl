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

-module(beam_detail_tabs_probe).
-export([init_tab/4]).

init_tab(Notebook,Title,Fun,FunArgs) ->
    Panel = init_panel(Notebook,Fun,FunArgs),
    true = wxNotebook:addPage(Notebook, Panel, Title),
    Panel.

init_panel(ParentWin, Fun, FunArgs) ->
    Panel  = wxScrolledWindow:new(ParentWin),
    wxScrolledWindow:enableScrolling(Panel,true,true),
    wxScrolledWindow:setScrollbars(Panel,1,1,0,0),
    Sizer  = wxBoxSizer:new(4),
    Window = apply(Fun, [Panel | FunArgs]),
    wxSizer:add(Sizer, Window, [{flag, 8192 bor 240},
				{proportion, 1},
				{border, 5}]),
    wxPanel:setSizer(Panel, Sizer),
    Panel.

