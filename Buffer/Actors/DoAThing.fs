namespace Buffer.Actors
open System
open Akka.Event
open Buffer.Timeout
open Akka.FSharp
open Akka.Actor

type DoAThing =
    | DoAThing of obj
    | ThingIsDone
    | Stop

module DoAThing =
    let nameOfGuid guid = $"doAThing-{guid.ToString()}" 
    let create timespan =
        fun (mailbox: Actor<DoAThing>) ->
           let logger = mailbox.Context.GetLogger()
           do mailbox.Context.SetReceiveTimeout(TimeSpan.FromSeconds(5.0))
           let rec loop  =
               function
               // We're live! waiting on a thing to be done!
               | Some (sender: IActorRef, msg) -> 
                   actor {                       
                       match! mailbox.Receive() with
                       // new message better stash
                       | DoAThing newMsg ->
                           logger.Info("Stashing message {0}", newMsg)
                           do mailbox.Stash()
                           return! loop (Some (sender, msg))
                       | ThingIsDone ->
                           logger.Info("Done with {0}", [|msg|])
                           sender.Tell msg
                           mailbox.Unstash()
                           return! loop None
                       | Stop ->
                           logger.Warning("Received stop but not done with {0}", [|msg|])
                           mailbox.Context.Stop(mailbox.Self)
                   }
               // Just chillaxin' waiting for a new thing to do
               | None -> 
                   actor {
                       match! mailbox.Receive() with
                       | DoAThing msg ->
                           logger.Info("Received message {0}", msg)
                           mailbox.Context.OnTimeout(ThingIsDone, "timer", timespan)                           
                           return! loop (Some (mailbox.Sender(), msg))
                       // yeah this shouldn't happen
                       | ThingIsDone -> 
                           mailbox.Unhandled(ThingIsDone)
                           return! loop None
                       | Stop ->                           
                           mailbox.Context.Stop(mailbox.Self)
                   }
           loop None 