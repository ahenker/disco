module Iris.Web.FileBrowserView

open System
open System.Collections.Generic
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Iris.Core
open Iris.Raft
open Iris.Web.Core
open Helpers
open State
open Types

///  ____       _            _
/// |  _ \ _ __(_)_   ____ _| |_ ___
/// | |_) | '__| \ \ / / _` | __/ _ \
/// |  __/| |  | |\ V / (_| | ||  __/
/// |_|   |_|  |_| \_/ \__,_|\__\___|

let private machine dispatch model node =
  div [ Class "machine" ] [
    span [
      Class "iris-output iris-icon icon-host"
      OnClick (fun _ -> Select.clusterMember dispatch node)
      Style [ Cursor "pointer" ]
    ] [
      str (unwrap node.HostName)
      span [
        classList [
          "iris-icon icon-bull",true
          "iris-status-off", node.State <> RaftMemberState.Running
          "iris-status-on", node.State = RaftMemberState.Running
        ]
      ] []
    ]
    div [ ] [
      div [ Class "headline" ] [ str "Assets" ]
    ]
  ]

let private machineBrowser dispatch model =
  let trees =
    model.state
    |> Option.map (State.fsTrees >> Map.toList)
    |> Option.defaultValue List.empty

  let sites =
    model.state
    |> Option.map (State.sites >> Array.toList)
    |> Option.defaultValue List.empty

  let members =
    model.state
    |> Option.bind State.activeSite
    |> Option.bind (fun id -> List.tryFind (fun site -> ClusterConfig.id site = id) sites)
    |> Option.map (ClusterConfig.members >> Map.toList)
    |> Option.defaultValue List.empty
    |> List.sortBy (snd >> Member.hostName)
    |> List.map (snd >> machine dispatch model)

  div [ Class "fb-panel column is-one-quarter" ] [
    nav [ Class "breadcrumb is-large" ]  [
      ul [] [
        li [ Class "is-active" ] [
          a [] [ str "Machines" ]
        ]
      ]
    ]
    div [ Class "machines" ] members
  ]

let private assetList dispatch model =
  div [ Class "fb-main column" ] [
    nav [ Class "breadcrumb is-large has-arrow-separator" ]  [
      ul [] [
        li [] [ a [] [ str "assets" ] ]
        li [] [ a [] [ str "stack_01" ] ]
        li [ Class "is-active" ] [
          a [] [ str "substack_04" ]
        ]
      ]
    ]
    div [ Class "columns is-gapless" ] [
      str "file.txt"
    ]
  ]

let private fileInfo dispatch model =
  div [ Class "fb-panel column is-one-quarter" ] [
    nav [ Class "breadcrumb is-large" ]  [
      ul [] [
        li [ Class "is-active" ] [
          a [] [ str "Fileinfo" ]
        ]
      ]
    ]
  ]

let private body dispatch (model: Model) =
  div [ Class "columns is-gapless iris-file-browser" ] [
    machineBrowser dispatch model
    assetList dispatch model
    fileInfo dispatch model
  ]


/// __        ___     _            _
/// \ \      / (_) __| | __ _  ___| |_
///  \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
///   \ V  V / | | (_| | (_| |  __/ |_
///    \_/\_/  |_|\__,_|\__, |\___|\__|
///                     |___/

let createWidget (id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.FileBrowser
    member this.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 6
        minW = 6
        minH = 2 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 -> equalsRef s1.FsTrees s2.FsTrees
          | None, None -> true
          | _ -> false)
        (widget id this.Name None body dispatch)
        model
  }
