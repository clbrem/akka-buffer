namespace Buffer.Message
open System.Text.Json
open System

[<CLIMutable>]
type QueueEnvelope<'T> = {
    message: 'T
    id: Guid
}
module QueueEnvelope =
    let enclose guid message =
        {
            message = message
            id = guid
        }
    let encloseDefault message =
        enclose (Guid.NewGuid()) message

