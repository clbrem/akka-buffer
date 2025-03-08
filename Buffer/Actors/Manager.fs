namespace Buffer.Actors
open Akka
open Akka.Util
open Akka.FSharp
open Akka.Actor
open System

type ManagerMessage =
    | Start of Guid
    | Finished of Guid

module Manager =
    let retrieve (mailbox:Actor<ManagerMessage>) name =
        let a = mailbox.Context.Child(name)
        if a.IsNobody() then
            TimeSpan.FromSeconds(5.0)
            |> DoAThing.create
            |> spawn mailbox.Context name 
        else
            a
    let create (onFinished: Guid -> 'msg) =
        fun (mailbox: Actor<ManagerMessage>) ->
            let rec loop (sender: IActorRef option, active: Set<Guid>) =
                actor {
                    match! mailbox.Receive() with
                    | Start id ->
                        let child = retrieve mailbox (DoAThing.nameOfGuid id)
                        child.Tell(DoAThing (Finished id))                        
                        return! loop (Some (mailbox.Sender()), active.Add id)
                    | Finished id ->
                        match sender with
                        | Some s -> s.Tell(onFinished id)
                        | _ -> ()
                        return! loop (None, active.Remove id)
                }
            loop (None, Set.empty)