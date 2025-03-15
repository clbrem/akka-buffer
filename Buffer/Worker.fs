namespace Buffer

open System
open System.Buffers
open System.IO
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Akka.Actor
open Akka.Hosting
open Buffer.Actors
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open RabbitMQ.Client
open RabbitMQ.Client.Events
open Buffer.Message

type Worker(connectionFactory: ConnectionFactory, logger: ILogger<Worker>, manager: IRequiredActor<Manager>) =
    inherit BackgroundService() 
    let mutable _connection: IConnection option = None
    let mutable _channel : IChannel option = None
    
    let channel (ct: CancellationToken)=        
       task {
            match _channel, _connection with
            | Some ch , _ -> return ch
            | None, Some conn -> 
                let! channel = conn.CreateChannelAsync(cancellationToken = ct)
                _channel <- Some channel
                let!  _  = channel.QueueDeclarePassiveAsync("hello")
                return channel
            | _, _ ->
                let! connection = connectionFactory.CreateConnectionAsync(cancellationToken = ct)
                let! channel = connection.CreateChannelAsync(cancellationToken = ct)
                let!  _  = channel.QueueDeclareAsync("hello", durable= false, exclusive= false, autoDelete= false, arguments= null)
                return channel
       }
    
    let readJson (input : inref<ReadOnlyMemory<byte>>) =
            let arr = ArrayPool<byte>.Shared.Rent(input.Length)
            try 
                do input.CopyTo(arr)
                use body = new MemoryStream(arr, 0, input.Length)
                let envelope = JsonSerializer.Deserialize<QueueEnvelope<string>>(body)
                envelope
            finally
                ArrayPool<byte>.Shared.Return(arr)
    
    let stop (ct: CancellationToken)=
        task {
            match _connection with
            | Some conn -> do! conn.CloseAsync(ct)
            | _ -> ()            
        } :> Task
    let listen(ct: CancellationToken) =
        task {
            let! chan = channel ct 
            let consumer = AsyncEventingBasicConsumer(chan)
            consumer.add_ReceivedAsync (
                fun ch ea ->
                    task {
                        let body = ea.Body
                        let envelope = readJson(&body)
                        logger.LogInformation("Received {0}: {0}", envelope.id, envelope.message)
                        do manager.ActorRef.Tell(ManagerMessage.Start envelope.id) 
                        do! chan.BasicAckAsync(ea.DeliveryTag, false)
                        })
            return! chan.BasicConsumeAsync(queue = "hello",  autoAck = false, consumer = consumer)
        }:> Task

    override this.ExecuteAsync(ct: CancellationToken) =
        listen ct
        
    override _.StopAsync (ct: CancellationToken) =
        base.StopAsync(ct).ContinueWith(fun _ -> stop ct)
 