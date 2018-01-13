module Disco.Web.NewWidget

open Disco.Core
open Disco.Web.Types
open Elmish.React
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Helpers

let private renderProject dispatch (state: State) =
  div [ ClassName "funny-list" ] [
    div [ ClassName "list-row" ] [
      div [ ClassName "list-left" ][
        str "Name"
      ]
      div [ ClassName "list-right"][
        str (string state.Project.Name)
      ]
    ]
    div [ ClassName "list-row" ] [
      div [ ClassName "list-left" ][
        str "Path"
      ]
      div [ ClassName "list-right"][
        str (string state.Project.Path)
      ]
    ]
  ]

let private body dispatch model =
  match model.state with
  | None -> div [ ClassName "testWidget" ] [ str "no state Present" ]
  | Some state ->
    div [ClassName "funny-widget"] [
      div [ClassName "headline"] [str "Project"]
      renderProject dispatch state
    ]

let createWidget id =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.NewWidget
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 4
        minH = 1 }
    member this.Render(dispatch, model) =
      widget this.Id this.Name None body dispatch model
  }