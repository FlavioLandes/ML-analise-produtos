using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ML.Analise.Produtos.Worker
{
    public class Worker : BackgroundService
    {
        private const string QueueName = "produto.analise.solicitacao";
        private readonly ILogger<Worker> _logger;
        private readonly IModel _channel;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                Uri = new Uri("amqp://guest:guest@localhost:5672"),
                DispatchConsumersAsync = true
            };

            IConnection conn = factory.CreateConnection();

            _channel = conn.CreateModel();
            _channel.QueueDeclarePassive(QueueName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();


            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (ch, ea) =>
            {
                byte[] body = ea.Body.ToArray();

                string bodyString = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Mensagem lida: {bodyString}", bodyString);

                _channel.BasicAck(ea.DeliveryTag, false);
                await Task.Yield();
            };

            _channel.BasicConsume(QueueName, autoAck: false, consumer);
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.Close();
            await base.StopAsync(cancellationToken);
        }
    }
}