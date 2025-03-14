namespace Buffer.Message
open System
open System.Buffers
open System.IO
open System.Text.Json
open Xunit
open Buffer.Message.MyAssert

module Envelope_Test =
    let hello = QueueEnvelope.encloseDefault "Hello"
    let readJson ( input : inref<ReadOnlyMemory<byte>>) =
        let arr = ArrayPool<byte>.Shared.Rent(input.Length)                        
        try 
            do input.CopyTo(arr)
            use body = new MemoryStream(arr, 0, input.Length)
            let envelope = JsonSerializer.Deserialize<QueueEnvelope<string>>(body)
            envelope                
        finally                            
            ArrayPool<byte>.Shared.Return(arr)
    [<Fact>]
    let ``Can Serialize``() =
        
        JsonSerializer.Serialize(hello)        
        |> JsonSerializer.Deserialize<QueueEnvelope<string>>
        |> Assert.EqualTo hello
    [<Fact>]
    let ``Can Serialize Stream``() =
        let helloBytes = JsonSerializer.SerializeToUtf8Bytes(hello)
        let mem = ReadOnlyMemory(helloBytes)
        let arr = ArrayPool<byte>.Shared.Rent(mem.Length)
        mem.CopyTo(arr)        
        use body = new MemoryStream(arr, 0, mem.Length)
        //let envelope = JsonSerializer.Deserialize<QueueEnvelope<string>>(body)
        let envelope = JsonSerializer.Deserialize<QueueEnvelope<string>>(body)
        ArrayPool.Shared.Return(arr)
        Assert.EqualTo hello envelope 
    [<Fact>]
    let ``Serialize with Inref``() =
        let helloBytes = JsonSerializer.SerializeToUtf8Bytes(hello)
        let mem = ReadOnlyMemory(helloBytes)
        readJson (&mem) |> Assert.EqualTo hello