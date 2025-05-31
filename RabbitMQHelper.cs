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
        }

        public void EnsureQueueExists(string queueName)
        {
            // Ensure the queue exists, this will create it if it doesn't
            _queueName = queueName;
            
            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        public void EnsureQueueExists(string queueName, string exchangetype, string? routingkey)
        {
            // Ensure the queue exists, this will create it if it doesn't
            _queueName = queueName;
            if (exchangetype.Equals(ExchangeType.Fanout) && routingkey != null)
            {
                throw new ArgumentException("Routing key should not be provided for Fanout exchange type.");
            }

            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            _channel.ExchangeDeclare(exchange: $"{_queueName}.{exchangetype}.exchange", type: exchangetype);
            if (exchangetype.Equals(ExchangeType.Topic))
            {
                _channel.QueueBind(queue: $"{_queueName}", exchange: $"{_queueName}.{exchangetype}.exchange", routingKey: routingkey);
            }
            else _channel.QueueBind(queue: _queueName, exchange: $"{_queueName}.{exchangetype}.exchange", routingKey: string.Empty);
        }

        //public void EnsureQueueExists(string queueName, string exchangetype, Dictionary<string, object> header)
        //{
        //    // Ensure the queue exists, this will create it if it doesn't
        //    _queueName = queueName;
        //    _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        //    _channel.ExchangeDeclare(exchange: $"{_queueName}_{exchangetype}_exchange", type: exchangetype);
        //    _channel.QueueBind(queue: _queueName, exchange: _queueName, routingKey: string.Empty);
        //}

        //Publish Methods============================================================================

        public Task PublishMessage(string message, string queueName)
        {
            EnsureQueueExists(queueName);

            var body = Encoding.UTF8.GetBytes(message);

            try
            {
                _channel.BasicPublish(exchange: string.Empty, routingKey: queueName, body: body);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to publish message to RabbitMQ.", ex);
            }

            return Task.CompletedTask;
        }

        public Task PublishMessage(string message, string queueName, string routingkey, string exchangetype)
        {
            EnsureQueueExists(queueName, exchangetype, routingkey);
            var body = Encoding.UTF8.GetBytes(message);

            try
            {
                if (exchangetype.Equals(ExchangeType.Topic))
                {
                    _channel.BasicPublish(exchange: $"{queueName}.{exchangetype}.exchange", routingKey: routingkey, body: body);
                }
                else _channel.BasicPublish(exchange: $"{queueName}.{exchangetype}.exchange", routingKey: string.Empty, body: body);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to publish message to RabbitMQ.", ex);
            }

            return Task.CompletedTask;
        }

        //Consume Methods============================================================================

        public async Task<string> ConsumeSingleMessageAsync(string queueName)
        {
            EnsureQueueExists(queueName);

            var tcs = new TaskCompletionSource<string>();
            var consumer = new AsyncEventingBasicConsumer(_channel);
            string? consumerTag = null;

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                tcs.TrySetResult(message);
                if (consumerTag != null)
                {
                    _channel.BasicCancel(consumerTag); // Stop consuming
                }
                await Task.CompletedTask;
            };

            consumerTag = _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            return await tcs.Task;
        }

        public async Task<string> ConsumeSingleMessageAsync(string queueName, string exchangetype, string? routingkey)
        {
            EnsureQueueExists(queueName, exchangetype, routingkey);

            var tcs = new TaskCompletionSource<string>();
            var consumer = new AsyncEventingBasicConsumer(_channel);
            string? consumerTag = null;

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                tcs.TrySetResult(message);
                if (consumerTag != null)
                {
                    _channel.BasicCancel(consumerTag); // Stop consuming
                }
                await Task.CompletedTask;
            };

            consumerTag = _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            return await tcs.Task;
        }


        //public async Task<string> ConsumeMessagesAsync(string queueName, string reqmessage)
        //{
        //    EnsureQueueExists(queueName);

        //    var tcs = new TaskCompletionSource<string>();
        //    var consumer = new AsyncEventingBasicConsumer(_channel);
        //    string? consumerTag = null;

        //    consumer.Received += async (model, ea) =>
        //    {
        //        var body = ea.Body.ToArray();
        //        var message = Encoding.UTF8.GetString(body);

        //        if (message == reqmessage)
        //        {
        //            tcs.TrySetResult(message);
        //            if (consumerTag != null)
        //            {
        //                _channel.BasicCancel(consumerTag); // Stop consuming
        //            }
        //        }
        //        await Task.CompletedTask;
        //    };

        //    consumerTag = _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        //    return await tcs.Task;
        //}

        public void CloseConnection()
        {
            _channel.Close();
            _connection.Close();
        }
    }
}
