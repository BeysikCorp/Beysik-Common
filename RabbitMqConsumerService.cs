using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Beysik_Common.RabbitMqConsumerService;

namespace Beysik_Common;
public class RabbitMqConsumerService : BackgroundService
{
    private readonly RabbitMqHelper _rabbitMqHelper;
    private readonly string _queueName;
    private readonly string? _exchangeType;
    private readonly string? _routingKey;
    private readonly RabbitMqEventAggregator _eventAggregator;

    // Define event for message received
    //public event EventHandler<MessageReceivedEventArgs> MessageReceived;
    //public event EventHandler<MessageReceivedEventArgs> ExchangeMessageReceived;

    public RabbitMqConsumerService(RabbitMqHelper rabbitMqHelper, RabbitMqEventAggregator eventAggregator, string queueName, string? exchangeType, string? routingKey)
    {
        _rabbitMqHelper = rabbitMqHelper;
        _eventAggregator = eventAggregator;
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName), "Queue name cannot be null");
        _exchangeType = exchangeType ?? null;
        _routingKey = routingKey ?? null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_exchangeType == null)
                {
                    var message = await _rabbitMqHelper.ConsumeSingleMessageAsync(_queueName);
                    if (message != null)
                    {
                        Console.WriteLine($"Received message: {message}");
                        //OnMessageReceived(new MessageReceivedEventArgs(message));
                        _eventAggregator.Publish(this, new MessageReceivedEventArgs(message));
                    }
                }
                else if (_exchangeType.Equals(ExchangeType.Topic, StringComparison.OrdinalIgnoreCase))
                {
                    var message = await _rabbitMqHelper.ConsumeSingleMessageAsync(_queueName, _exchangeType, _routingKey);
                    if (message != null)
                    {
                        Console.WriteLine($"Received topic message: {message}");
                        //OnExchangeMessageReceived(new MessageReceivedEventArgs(message));
                        _eventAggregator.Publish(this, new MessageReceivedEventArgs(message));
                    }
                }
                else
                {
                    var message = await _rabbitMqHelper.ConsumeSingleMessageAsync(_queueName, _exchangeType, null);
                    if (message != null)
                    {
                        Console.WriteLine($"Received exchange message: {message}");
                        //OnExchangeMessageReceived(new MessageReceivedEventArgs(message));
                        _eventAggregator.Publish(this, new MessageReceivedEventArgs(message));
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error receiving message: {ex.Message}");
            }

             // If no message is received, you might want to wait for a while before trying again
             await Task.Delay(5000, stoppingToken);
        }
    }

    //// Event invocation methods
    //protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
    //{
    //    MessageReceived?.Invoke(this, e);
    //}

    //protected virtual void OnExchangeMessageReceived(MessageReceivedEventArgs e)
    //{
    //    ExchangeMessageReceived?.Invoke(this, e);
    //}

    public override void Dispose()
    {
        _rabbitMqHelper.CloseConnection();
        base.Dispose();
    }

    // Event args class to pass message data
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; }

        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }
    }
}

// C#
public class RabbitMqEventAggregator
{
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public void Publish(object sender, MessageReceivedEventArgs args)
    {
        MessageReceived?.Invoke(sender, args);
    }
}
