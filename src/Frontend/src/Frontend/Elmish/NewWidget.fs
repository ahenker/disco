module Disco.Web.NewWidget

open Disco.Web.Types
open Elmish.React
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Helpers

let private body () =
  div [] [
    str "We are live"
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
      widget this.Id this.Name None (fun _ _ -> body()) dispatch model
  }