namespace Iris.Tests.Raft

open System.Net
open Fuchu
open Fuchu.Test
open Iris.Raft
open Iris.Core

[<AutoOpen>]
module Log =

  ////////////////////////////////////////
  //  _                                 //
  // | |    ___   __ _                  //
  // | |   / _ \ / _` |                 //
  // | |__| (_) | (_| |                 //
  // |_____\___/ \__, |                 //
  //             |___/                  //
  ////////////////////////////////////////

  let log_new_log_is_empty =
    testCase "When create, a log should be empty" <| fun _ ->
      let log : Log = Log.empty
      expect "Should be zero" 0UL Log.length log

  let log_is_non_empty =
    testCase "When create, a log should not be empty" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1UL defSM)
      |> assume "Should be one"  1UL  Log.length
      |> Log.append (Log.make 1UL defSM)
      |> assume "Should be two" 2UL Log.length
      |> Log.append (Log.make 1UL defSM)
      |> assume "Should be two"  3UL  Log.length
      |> ignore

  let log_have_correct_index =
    testCase "When I add an entry, it should have the correct index" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1UL defSM)
      |> assume "Should have currentIndex 1" 1UL Log.index
      |> assume "Should have currentTerm 1" 1UL Log.term
      |> assume "Should have no lastTerm" None Log.prevTerm
      |> assume "Should have no lastIndex" None Log.prevIndex

      |> Log.append (Log.make 1UL defSM)

      |> assume "Should have currentIndex 2" 2UL Log.index
      |> assume "Should have currentTerm 1" 1UL Log.term
      |> assume "Should have lastTerm 1" (Some 1UL) Log.prevTerm
      |> assume "Should have lastIndex 1" (Some 1UL) Log.prevIndex
      |> ignore


  let log_get_at_index =
    testCase "When I get an entry by index, it should be equal" <| fun _ ->
      let id1 = Id.Create()
      let id2 = Id.Create()
      let id3 = Id.Create()

      let log =
        Log.empty
        |> Log.append (LogEntry(id1, 0UL, 1UL, defSM, None))
        |> Log.append (LogEntry(id2, 0UL, 1UL, defSM, None))
        |> Log.append (LogEntry(id3, 0UL, 1UL, defSM, None))

      Log.at 1UL log
      |> assume "Should be correct one" id1 (LogEntry.getId << Option.get)
      |> ignore

      Log.at 2UL log
      |> assume "Should also be correct one" id2 (LogEntry.getId << Option.get)
      |> ignore

      Log.at 3UL log
      |> assume "Should also be correct one" id3 (LogEntry.getId << Option.get)
      |> ignore

      expect "Should find none at invalid index" None (Log.at 8UL) log

  let log_find_by_id =
    testCase "When I get an entry by index, it should be equal" <| fun _ ->
      let id1 = Id.Create()
      let id2 = Id.Create()
      let id3 = Id.Create()

      let log =
        Log.empty
        |> Log.append (LogEntry(id1, 0UL, 1UL, defSM, None))
        |> Log.append (LogEntry(id2, 0UL, 1UL, defSM, None))
        |> Log.append (LogEntry(id3, 0UL, 1UL, defSM, None))

      Log.find id1 log
      |> assume "Should be correct one" id1 (LogEntry.getId << Option.get)
      |> ignore

      Log.find id2 log
      |> assume "Should also be correct one" id2 (LogEntry.getId << Option.get)
      |> ignore

      Log.find id3 log
      |> assume "Should also be correct one" id3 (LogEntry.getId << Option.get)
      |> ignore

      Log.find (Id.Create()) log
      |> assume "Should find none at invalid index" true Option.isNone
      |> ignore

  let log_depth_test =
    testCase "Should have the correct log depth" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1UL defSM)
      |> Log.append (Log.make 1UL defSM)
      |> Log.append (Log.make 1UL defSM)
      |> assume "Should have length 3" 3UL Log.length
      |> Log.append (Log.make 1UL defSM)
      |> Log.append (Log.make 1UL defSM)
      |> assume "Should have depth 5" 5UL Log.length
      |> ignore

  let log_resFold_short_circuit_test =
    testCase "Should short-circuit when folder fails" <| fun _ ->
      let sm = AddCue { Id = Id.Create(); Name = "Wonderful"; IOBoxes = [| |] }
      let log =
        Log.empty
        |> Log.append (Log.make 1UL defSM)
        |> Log.append (Log.make 1UL sm)
        |> Log.append (Log.make 1UL defSM)

      let folder (m: int) (log: LogEntry) : Continue<int> =
        let value = (LogEntry.data >> Option.get) log
        if value = sm
        then LogEntry.finish (m + 9)
        else LogEntry.next   (m + 2)

      log.Data
      |> Option.get
      |> LogEntry.aggregate folder 0
      |> assume "Should be 11" 11 id // if it does short-circuit, it should not
      |> ignore                     // add more to the result!

  let log_concat_length_test =
    testCase "Should have correct length" <| fun _ ->
      let log =
        Log.empty
        |> Log.append (Log.make 1UL defSM)
        |> Log.append (Log.make 1UL defSM)
        |> Log.append (Log.make 1UL defSM)

      log
      |> Log.append (Log.make 1UL defSM)
      |> Log.append (Log.make 1UL defSM)
      |> Log.append (Log.make 1UL defSM)
      |> assume "Should have length 6" 6UL Log.length
      |> ignore

  let log_concat_monotonicity_test =
    testCase "Should have monotonic index" <| fun _ ->
      let isMonotonic log =
        let __mono (last,ret) _log =
          let i = LogEntry.index _log
          if ret then (i, i = (last + 1UL)) else (i, ret)
        Log.foldLogR __mono (0UL,true) log

      let log =
        Log.empty
        |> Log.append (Log.make 1UL defSM)
        |> Log.append (Log.make 1UL defSM)
        |> Log.append (Log.make 1UL defSM)

      log
      |> Log.append (Log.make 1UL defSM)
      |> Log.append (Log.make 1UL defSM)
      |> Log.append (Log.make 1UL defSM)
      |> assume "Should be monotonic" true (isMonotonic >> snd)
      |> ignore

  let log_get_entries_until_test =
    testCase "Get all entries until (and including) a given index" <| fun _ ->
      let cues : Cue array =
        [| "one"; "two"; "three"; "four"; "five"; "six" |]
        |> Array.map (fun name -> { Id = Id.Create(); Name = name; IOBoxes = [| |] })

      let getData log =
        LogEntry.map
          (fun entry ->
            match LogEntry.data entry with
            | Some data -> data
            | None      -> failwith "oooops")
          log

      Log.empty
      |> Log.append (Log.make 1UL (AddCue cues.[0]))
      |> Log.append (Log.make 1UL (AddCue cues.[1]))
      |> Log.append (Log.make 1UL (AddCue cues.[2]))
      |> Log.append (Log.make 1UL (AddCue cues.[3]))
      |> Log.append (Log.make 1UL (AddCue cues.[4]))
      |> Log.append (Log.make 1UL (AddCue cues.[5]))
      |> Log.until 4UL
      |> assume "Should have 3 logs" 3UL (Option.get >> LogEntry.depth)
      |> assume "Should have log with these values" [AddCue cues.[5]; AddCue cues.[4]; AddCue cues.[3]] (Option.get >> getData)
      |> ignore

  let log_concat_ensure_no_duplicate_entries =
    testCase "concat ensure no duplicate entires" <| fun _ ->
      let id1 = Id.Create()
      let id2 = Id.Create()

      let term = 1UL

      let idx1 = 1UL
      let idx2 = 2UL

      let entries =
        LogEntry(id2,idx2,term,DataSnapshot State.Empty,
                 Some <| LogEntry(id1,idx1,term,DataSnapshot State.Empty,None))

      let log = Log.fromEntries entries

      Log.append entries log
      |> expect "Should be the same" log id

  let log_append_ensure_no_duplicate_entries =
    testCase "append ensure no duplicate entires" <| fun _ ->
      let id1 = Id.Create()
      let id2 = Id.Create()

      let term = 1UL
      let idx1 = 1UL
      let idx2 = 2UL

      let entries =
        LogEntry(id2,idx2,term,DataSnapshot State.Empty,
                 Some <| LogEntry(id1,idx1,term,DataSnapshot State.Empty,None))

      let log = Log.fromEntries entries

      Log.append entries log
      |> expect "Should be the same" log id

  let log_concat_ensure_no_duplicate_but_unique_entries =
    testCase "concat ensure no duplicate but unique entries" <| fun _ ->
      let id1 = Id.Create()
      let id2 = Id.Create()
      let id3 = Id.Create()

      let term = 1UL
      let idx1 = 1UL
      let idx2 = 2UL
      let idx3 = 3UL

      let entires =
        LogEntry(id2,idx2,term,DataSnapshot State.Empty,
                 Some <| LogEntry(id1,idx1,term,DataSnapshot State.Empty,None))

      let log = Log.fromEntries entires

      let newer =
        LogEntry(id3,idx3,term,DataSnapshot State.Empty,
                 Some <| LogEntry(id2,idx2,term,DataSnapshot State.Empty,
                                  Some <| LogEntry(id1,idx1,term,DataSnapshot State.Empty,None)))

      Log.append newer log
      |> assume "Should have length 3" 3UL Log.length
      |> expect "Should have proper id" id3 (Log.entries >> Option.get >> LogEntry.getId)


  let log_snapshot_remembers_last_state =
    testCase "snapshot remembers last state" <| fun _ ->
      let term = 8UL
      let data =
        [ for i in 0 .. 3 do
            yield DataSnapshot State.Empty ]

      let nodes =
        [ for n in 0UL .. 5UL do
            yield Node.create (Id.Create()) ]
        |> Array.ofList

      let log =
        List.fold (fun l t -> Log.append (Log.make term t) l) Log.empty data

      Log.snapshot nodes (DataSnapshot State.Empty) log
      |> assume "Should have correct lastTerm" (Some term) Log.lastTerm
      |> expect "Should have correct lastIndex" (Some <| Log.index log) Log.lastIndex

  let log_untilExcluding_should_return_expected_enries =
    testCase "untilExcluding should return expected enries" <| fun _ ->
      let num = 30UL

      [ for n in 1UL .. num do
          yield AddCue { Id = Id (string n); Name = string n; IOBoxes = [| |] } ]
      |> List.fold (fun m s -> Log.append (Log.make 0UL s) m) Log.empty
      |> assume "Should be at correct index" num                        Log.length
      |> assume "Should pick correct item"   16UL                      (Log.untilExcluding 15UL >> Option.get >> LogEntry.last >> LogEntry.index)
      |> assume "Should have correct index" (AddCue { Id = Id "16"; Name = "16"; IOBoxes = [| |] } |> Some) (Log.untilExcluding 15UL >> Option.get >> LogEntry.last >> LogEntry.data)
      |> assume "Should have correct index" (AddCue { Id = Id "15"; Name = "15"; IOBoxes = [| |] } |> Some) (Log.until 15UL >> Option.get >> LogEntry.last >> LogEntry.data)
      |> ignore

  let log_append_should_work_with_snapshots_too =
    testCase "append should work with snapshots too" <| fun _ ->
      let log =
        Log.empty
        |> Log.append (Snapshot(Id.Create(), 0UL, 0UL, 9UL, 1UL, Array.empty, DataSnapshot State.Empty))

      expect "Log should be size 1" 1UL Log.length log

  let log_firstIndex_should_return_correct_results =
    testCase "firstIndex should return correct results" <| fun _ ->
      let random = System.Random()

      let indices, log =
        let fidxs = ref List.empty

        let combine a b = (a, b)

        let def = LogEntry(Id.Create(),0UL,0UL,defSM,None)

        let folder log (id,term,index) =
          LogEntry(id,index,term,defSM,Some log)

        [ for term in 1UL .. 4UL do
            let offset = random.Next(1,60)
            for index in uint64(offset) .. uint64(offset + random.Next(10,70)) do
              let (_,t,i) as result = (Id.Create(), term, index)
              if index = uint64(offset) then
                fidxs := (t,i) :: !fidxs
              yield result ]
        |> List.fold folder def
        |> Log.fromEntries
        |> combine (!fidxs |> Map.ofList)

      for term in 1UL .. 4UL do
        let fidx = Log.firstIndex term log
        let result = Map.tryFind term indices
        expect "Should be equal" result id fidx

  let log_getn_should_return_right_number_of_entries =
    testCase "getn should return right number of entries" <| fun _ ->
      let n = 20

      let log =
        [ for n in 0 .. (n - 1) do
            yield DataSnapshot State.Empty ]
        |> List.fold (fun m n -> Log.append (Log.make 0UL n) m) Log.empty

      expect "should have correct depth" (uint64 n) Log.length log

      let getter = System.Random().Next(1,n - 1) |> uint64
      expect "should get corrent number" getter LogEntry.depth (Log.getn getter log |> Option.get)