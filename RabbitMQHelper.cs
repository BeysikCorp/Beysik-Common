using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Beysik_Common 
{
    public class RabbitMqHelper
    {
        private readonly string _hostName;
        private string _queueName;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        public string _message { get; set; } = string.Empty;

        public RabbitMqHelper(string hostname)
        {
            _hostName = hostname;

            var factory = new ConnectionFactory() { HostName = _hostName, UserName="guest", Password="guest" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            //_channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        public void EnsureQueueExists(string queueName)
        {
            // Ensure the queue exists, this will create it if it doesn't
            _queueName = queueName;
            
            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }


        public Task PublishMessage(string queueName, string message)
        {
            EnsureQueueExists(queueName);
            var body = System.Text.Encoding.UTF8.GetBytes(message);

            try
            {
                _channel.BasicPublish(exchange: string.Empty, routingKey: _queueName, body: body);
            }
            catch (Exception ex)
            {
                // Optionally log the exception or handle it as needed
                throw new InvalidOperationException("Failed to publish message to RabbitMQ.", ex);
            }

            //Console.WriteLine($" [x] Sent {message}");
            return Task.CompletedTask;
        }

        public Task ConsumeMessages(string queueName, string reqmessage)
        {
            EnsureQueueExists(queueName);
            var consumer = new EventingBasicConsumer(_channel);
            //var message = "";
            do
            {
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    _message = System.Text.Encoding.UTF8.GetString(body);

                    //Console.WriteLine($" [x] Received {message}");
                };
                 _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
            }
            while (!(reqmessage.Equals(_message)));


            return Task.CompletedTask;
        }

        public void CloseConnection()
        {
            _channel.Close();
            _connection.Close();
        }
    }
}
