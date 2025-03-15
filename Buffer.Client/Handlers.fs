namespace Buffer.Client
open System
open Giraffe
open Microsoft.Extensions.DependencyInjection
open RabbitMQ.Client
open Buffer.Message
open System.Text.Json

module Handlers =
  let defaultId = Guid.NewGuid()
  let sendMessage (factory: ConnectionFactory) (message: string) =
       task {
           use! connection = factory.CreateConnectionAsync()
           use! channel = connection.CreateChannelAsync()
           let! _= channel.QueueDeclareAsync("hello", durable= false, exclusive= false, autoDelete= false, arguments= null)
           let body =
               message
               |> QueueEnvelope.encloseDefault
               |> JsonSerializer.SerializeToUtf8Bytes
           do! channel.BasicPublishAsync(exchange = String.Empty, routingKey = "hello", body =body)           
       }
  let sendDefault (factory: ConnectionFactory) (message: string) =
       task {
           use! connection = factory.CreateConnectionAsync()
           use! channel = connection.CreateChannelAsync()
           let! _= channel.QueueDeclareAsync("hello", durable= false, exclusive= false, autoDelete= false, arguments= null)
           let body =
               message
               |> QueueEnvelope.enclose defaultId
               |> JsonSerializer.SerializeToUtf8Bytes
           do! channel.BasicPublishAsync(exchange = String.Empty, routingKey = "hello", body =body)           
       }       
  
  let sendHandler input: HttpHandler =
      fun next context ->
          task {
              let factory = context.RequestServices.GetService<ConnectionFactory>()
              do! sendMessage factory input
              return! next context
          }
  let sendDefaultHandler input : HttpHandler=
      fun next context ->
          task {
              let factory = context.RequestServices.GetService<ConnectionFactory>()
              do! sendDefault factory input
              return! next context
          }      