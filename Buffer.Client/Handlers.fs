namespace Buffer.Client
open System
open Giraffe
open Microsoft.Extensions.DependencyInjection
open RabbitMQ.Client
module Handlers =
  let sendMessage (factory: ConnectionFactory) =
       task {
           use! connection = factory.CreateConnectionAsync()
           use! channel = connection.CreateChannelAsync()
           let! _= channel.QueueDeclareAsync("hello", durable= false, exclusive= false, autoDelete= false, arguments= null)
           let message = "Hello World"
           let body = System.Text.Encoding.UTF8.GetBytes(message)
           do! channel.BasicPublishAsync(exchange = String.Empty, routingKey = "hello", body =body)           
       }
  let SendHandler: HttpHandler =
      fun next context ->
          task {
              let factory = context.RequestServices.GetService<ConnectionFactory>()
              do! sendMessage factory
              return! next context
          }