namespace Test.Units

open WebSharper
open WebSharper.Mocha
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
[<RequireQualifiedAccess>]
module Store =

  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Patch
  open Iris.Web.Core.Store
  open Iris.Web.Core.State
  open Iris.Web.Core.Events
  open Iris.Web.Core.Reducer

  [<Direct "Object.is($o1, $o2)">]
  let identical (o1 : obj) (o2 : obj) = X

  let withStore (wrap : Patch -> Store<State> -> unit) =
    let patch : Patch =
      { id = "0xb4d1d34"
      ; name = "patch-1"
      ; ioboxes = Array.empty
      }

    let store : Store<State> = new Store<State>(reducer, State.Empty)
    wrap patch store
      

  let main () =
    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - Immutability"
    (*--------------------------------------------------------------------------*)

    withStore <| fun patch store ->
      test "store should be immutable" <| fun cb ->
        let state = store.State
        store.Dispatch <| PatchEvent(AddPatch, patch)
        let newstate = store.State
        (identical state newstate ==>> false) cb
      

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - Patch operations"
    (*--------------------------------------------------------------------------*)

    withStore <| fun patch store ->
      test "should add a patch to the store" <| fun cb ->
        (List.length store.State.Patches) |==| 0
        store.Dispatch <| PatchEvent(AddPatch, patch)
        ((List.length store.State.Patches) ==>> 1) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should update a patch already in the store" <| fun cb ->
        let name1 = patch.name
        let name2 = "patch-2"

        let isPatch (p : Patch) : bool = p.id = patch.id

        store.Dispatch <| PatchEvent(AddPatch, patch)
        (List.exists isPatch store.State.Patches) |==| true
        (List.find isPatch store.State.Patches |> (fun p -> p.name = name1)) |==| true

        let updated = { patch with name = name2 }
        store.Dispatch <| PatchEvent(UpdatePatch,updated)
        ((List.find isPatch store.State.Patches |> (fun p -> p.name = name2)) ==>> true) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "should remove a patch already in the store" <| fun cb ->
        let isPatch (p : Patch) : bool = p.id = patch.id

        store.Dispatch <| PatchEvent(AddPatch, patch)
        (List.exists isPatch store.State.Patches) |==| true

        store.Dispatch <| PatchEvent(RemovePatch, patch)
        ((List.exists isPatch store.State.Patches) ==>> false) cb

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - IOBox operations"
    (*--------------------------------------------------------------------------*)

    withStore <| fun patch store -> 
      test "should add an iobox to the store if patch exists" <| fun cb ->
        store.Dispatch <| PatchEvent(AddPatch, patch)

        match store.State.Patches with
          | patch :: [] -> check ((Array.length patch.ioboxes) = 0) "iobox array length should be 0"
          | _ -> check false "patches list is empty but should contain at least one patch"

        let iobox =
          { id     = "0xb33f"
          ; name   = "url input"
          ; patch  = patch.id
          ; kind   = "string"
          ; slices = [| { idx = 0; value = "Hey" } |]
          }

        store.Dispatch <| IOBoxEvent(AddIOBox, iobox)

        match store.State.Patches with
          | patch :: [] -> ((Array.length patch.ioboxes) ==>> 1) cb
          | _ -> fail "patches list is empty but should contain at least one patch"

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should not add an iobox to the store if patch does not exists" <| fun cb ->
        let iobox =
          { id     = "0xb33f"
          ; name   = "url input"
          ; patch  = patch.id
          ; kind   = "string"
          ; slices = [| { idx = 0; value = "Hey" } |]
          }

        store.Dispatch <| IOBoxEvent(AddIOBox, iobox)
        ((List.length store.State.Patches) ==>> 0) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "should update an iobox in the store if it already exists" <| fun cb ->
        let name1 = "can a cat own a cat?"
        let name2 = "yes, cats are re-entrant."

        let iobox =
          { id     = "0xb33f"
          ; name   = name1
          ; patch  = patch.id
          ; kind   = "string"
          ; slices = [| { idx = 0; value = "swell" } |]
          }

        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| IOBoxEvent(AddIOBox, iobox)

        match findIOBox store.State.Patches iobox.id with
          | Some(i) -> i.name |==| name1
          | None -> fail "iobox is mysteriously missing"

        let updated = { iobox with name = name2 }
        store.Dispatch <| IOBoxEvent(UpdateIOBox, updated)

        match findIOBox store.State.Patches iobox.id with
          | Some(i) -> (i.name ==>> name2) cb
          | None -> fail "iobox is mysteriously missing"

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "should remove an iobox from the store if it exists" <| fun cb ->
        let iobox =
          { id     = "0xb33f"
          ; name   = "hi"
          ; patch  = "0xb4d1d34"
          ; kind   = "string"
          ; slices = [| { idx = 0; value = "swell" } |]
          }
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| IOBoxEvent(AddIOBox, iobox)

        match findIOBox store.State.Patches iobox.id with
          | Some(_) -> ()
          | None    -> fail "iobox is mysteriously missing"

        store.Dispatch <| IOBoxEvent(RemoveIOBox, iobox)

        match findIOBox store.State.Patches iobox.id with
          | Some(_) -> fail "iobox should be missing by now but isn't"
          | None    -> success cb

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - Debug Mode"
    (*--------------------------------------------------------------------------*)

    withStore <| fun patch store -> 
      test "should have correct number of historic states when starting fresh" <| fun cb ->
        let patch2 : Patch = { patch with name = "patch-2" }
        let patch3 : Patch = { patch2 with name = "patch-3" }
        let patch4 : Patch = { patch3 with name = "patch-4" }

        store.Debug(true) 

        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, patch2)
        store.Dispatch <| PatchEvent(UpdatePatch, patch3)
        store.Dispatch <| PatchEvent(UpdatePatch, patch4)

        (store.Dump().Length ==>> 5) cb


    (*------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "should have correct number of historic states when started after 1 event" <| fun cb ->
        let patch2 : Patch = { patch with name = "patch-2" }
        let patch3 : Patch = { patch2 with name = "patch-3" }
        let patch4 : Patch = { patch3 with name = "patch-4" }

        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Debug(true) 
        store.Dispatch <| PatchEvent(UpdatePatch, patch2)
        store.Dispatch <| PatchEvent(UpdatePatch, patch3)
        store.Dispatch <| PatchEvent(UpdatePatch, patch4)

        (store.Dump().Length ==>> 4) cb


    (*------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "should have correct number of historic states when started after 2 events" <| fun cb ->
        let patch2 : Patch = { patch with name = "patch-2" }
        let patch3 : Patch = { patch2 with name = "patch-3" }
        let patch4 : Patch = { patch3 with name = "patch-4" }

        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, patch2)
        store.Debug(true) 
        store.Dispatch <| PatchEvent(UpdatePatch, patch3)
        store.Dispatch <| PatchEvent(UpdatePatch, patch4)

        (store.Dump().Length ==>> 3) cb


    (*------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "store should store previous states in debug mode" <| fun cb ->
        let patch2 : Patch = { patch with name = "patch-2" }
        let patch3 : Patch = { patch2 with name = "patch-3" }
        let patch4 : Patch = { patch3 with name = "patch-4" }

        store.Debug(true) 

        store.Dispatch <| PatchEvent(AddPatch, patch)

        (List.head store.State.Patches).name |==| "patch-1"

        store.Dispatch <| PatchEvent(UpdatePatch, patch2)
        (List.head store.State.Patches).name |==| "patch-2"

        store.Dispatch <| PatchEvent(UpdatePatch, patch3)
        (List.head store.State.Patches).name |==| "patch-3"

        // this is HEAD
        store.Dispatch <| PatchEvent(UpdatePatch, patch4)
        (List.head store.State.Patches).name |==| "patch-4"

        store.Previous()
        (List.head store.State.Patches).name |==| "patch-3"

        store.Previous()
        (List.head store.State.Patches).name |==| "patch-2"

        store.Previous()
        (List.head store.State.Patches).name |==| "patch-1"

        store.Previous()
        List.length store.State.Patches |==| 0
                                           
        store.Previous()                   
        List.length store.State.Patches |==| 0

        store.Next()
        (List.head store.State.Patches).name |==| "patch-1"

        store.Next()
        (List.head store.State.Patches).name |==| "patch-2"

        store.Next()
        (List.head store.State.Patches).name |==| "patch-3"

        store.Next()
        (List.head store.State.Patches).name |==| "patch-4"

        store.Next()
        (List.head store.State.Patches).name |==| "patch-4"


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "store should trigger listeners on tick" <| fun cb ->
        store.Subscribe(fun st ev ->
          match ev with
            | PatchEvent(AddPatch, p) -> if p.name = patch.name then cb ()
            | _ -> ())

        store.Debug(true) 
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-2" })

        store.Dump().Length |==| 3
        store.Previous()

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "tick should not do anything when not in debug mode" <| fun cb ->
        store.Dispatch <| PatchEvent(AddPatch, patch)
        (List.head store.State.Patches).name |==| "patch-1"
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-2" })
        (List.head store.State.Patches).name |==| "patch-2"
        store.Previous()
        ((List.head store.State.Patches).name ==>> "patch-2") cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "store should dump previous states for inspection" <| fun cb ->
        store.Debug(true) 
        store.Dump().Length |==| 1
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-2" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-3" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-4" })
        (store.Dump().Length ==>> 5) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "store should release states when turning Debug off" <| fun cb ->
        store.Debug(true) 
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-2" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-3" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-4" })
        store.Debug(false) 
        (store.Dump().Length ==>> 0) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store -> 
      test "should restore to last state added on leaving debug mode" <| fun cb -> 
        store.Debug(true) 
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-2" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-3" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-4" })
        store.Previous()
        store.Previous()
        (List.head store.State.Patches).name |==| "patch-2"
        store.Debug(false) 
        ((List.head store.State.Patches).name ==>> "patch-4") cb

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - Undo/Redo"
    (*--------------------------------------------------------------------------*)

    pending "should undo a change"
    pending "should redo an undone change"
    pending "should redo an undone change after intermittend change"
    pending "should reflect an undo in debug history"
    pending "should reflect an redo in debug history"
    pending "should debug history should be append-only during undo/redo"
    pending "should only keep specified number of undo/redo steps"