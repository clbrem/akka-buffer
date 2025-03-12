namespace Buffer.Actors
open Akka
open Akka.Event
open Akka.Util
open Akka.FSharp
open Akka.Actor
open System

type ManagerMessage =
    | Start of Guid
    | Finished of Guid

type Manager = private Manager of unit 

module Manager =
    let retrieve (mailbox:Actor<ManagerMessage>) name =
        let a = mailbox.Context.Child(name)
        if a.IsNobody() then
            TimeSpan.FromSeconds(5.0)
            |> DoAThing.create
            |> spawn mailbox.Context name 
        else
            a
    let (|InProgress|_|) (active: Map<Guid, IActorRef>) id =
        Map.tryFind id active
        
    let create (onFinished: Guid -> 'msg) =
        fun (mailbox: Actor<ManagerMessage>) ->
            let logger = mailbox.Context.GetLogger()
            let rec loop ( active: Map<Guid, IActorRef> ) =
                actor {
                    match! mailbox.Receive() with
                    | Start (id & InProgress active _) ->
                        logger.Info("Stashing task {0}", id)
                        mailbox.Stash()                        
                        return! loop active                    
                    | Start id ->
                        let child = retrieve mailbox (DoAThing.nameOfGuid id)
                        logger.Info("Starting task {0}", id)
                        child.Tell(DoAThing (Finished id))                        
                        return! loop (Map.add id (mailbox.Sender()) active)                        
                    | Finished (id & InProgress active sender) ->
                        sender.Tell(onFinished id)
                        logger.Info("Finishing task {0}", id)
                        mailbox.Unstash()
                        return! loop (active.Remove id)
                    | other -> mailbox.Unhandled(other); return! loop active
                }
            loop Map.empty