[<RequireQualifiedAccess>]
module Disco.Web.Editable

open System
open Disco.Core
open Disco.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Disco.Web
open Types
open Helpers

type private RCom = React.ComponentClass<obj>
let private ContentEditable: RCom = importDefault "../../js/widgets/ContentEditable"
let private DropdownEditable: RCom = importDefault "../../js/widgets/DropdownEditable"

let string content tooltip (update: string -> unit) =
  from ContentEditable
    %["tagName" ==> "div"
      "html" ==> content
      "title" ==> tooltip
      "className" ==> "disco-contenteditable"
      "onChange" ==> update] []

let dropdown
  (content:string)
  (selected: string option)
  (props: (string*string)[])
  (update: string option -> unit) =
  from DropdownEditable
    %["tagName" ==> "span"
      "html" ==> content
      "data-selected" ==> selected
      "data-options" ==> props
      "onChange" ==> update] []
