namespace Buffer.Actors

open System

type ManagerMessage =
    | Start of Guid
    | Finished of Guid


