using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Beysik_Common 
{
    public class RabbitMqHelper
    {
        private readonly string _hostName;
        private readonly string _queueName;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        public string _message { get; set; } = string.Empty;

        public RabbitMqHelper(string queueName)
        {
            _hostName = "192.168.191.216";
            _queueName = queueName;

            var factory = new ConnectionFactory() { HostName = _hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        public Task PublishMessage(string message)
        {
            var body = System.Text.Encoding.UTF8.GetBytes(message);

            try
            {
                _channel.BasicPublish(exchange: string.Empty, routingKey: "hello", body: body);
            }
            catch (Exception)
            {
                throw;
            }

            //Console.WriteLine($" [x] Sent {message}");
            return Task.CompletedTask;
        }

        public Task ConsumeMessages()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                _message = System.Text.Encoding.UTF8.GetString(body);

                //Console.WriteLine($" [x] Received {message}");
            };
            _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        public void CloseConnection()
        {
            _channel.Close();
            _connection.Close();
        }
    }
}
