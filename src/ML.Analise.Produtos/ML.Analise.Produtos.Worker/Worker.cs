using ML.Analise.Produtos.Worker.MLService;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ML.Analise.Produtos.Worker
{
    public class Worker : BackgroundService
    {
        private const string QueueNameSolicitation = "produto.analise.solicitacao";
        private const string QueueNameResult = "produto.analise.resultado";
        private const string ExchangeName = "produto.analise";
        private const string RoutingKeyOutput = "resultado";

        private readonly ILogger<Worker> _logger;
        private readonly IModel _channelInput;
        private readonly IModel _channelOutput;
        private readonly IMercadoLivreService _mercadoLivreService;

        public Worker(ILogger<Worker> logger, IMercadoLivreService mercadoLivreService)
        {
            _logger = logger;
            _mercadoLivreService = mercadoLivreService;

            string rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            var factory = new ConnectionFactory
            {
                Uri = new Uri($"amqp://guest:guest@{rabbitMqHost}:5672"),
                DispatchConsumersAsync = true
            };

            IConnection conn = factory.CreateConnection();

            _channelInput = conn.CreateModel();
            _channelInput.QueueDeclarePassive(QueueNameSolicitation);

            _channelOutput = conn.CreateModel();
            _channelOutput.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
            _channelOutput.QueueDeclare(QueueNameResult, durable: true, false, false, new Dictionary<string, object> { { "x-queue-type", "classic" } });
            _channelOutput.QueueBind(QueueNameResult, ExchangeName, RoutingKeyOutput, null);
            _channelOutput.ConfirmSelect();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(_channelInput);
            consumer.Received += async (ch, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string bodyString = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Mensagem lida: {bodyString}", bodyString);

                try
                {
                    var products = JsonSerializer.Deserialize<List<string>>(bodyString);
                    var analyzeResults = await AnalyzeProductsInMLAsync(products);

                    SendAnalizeResultsToQueue(analyzeResults);

                    _channelInput.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao analisar mensagem: {mensagem}", bodyString);
                    _channelInput.BasicNack(ea.DeliveryTag, false, requeue: true);
                }

                await Task.Yield();
            };

            _channelInput.BasicConsume(QueueNameSolicitation, autoAck: false, consumer);
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _channelInput.Close();
            _channelOutput.Close();
            await base.StopAsync(cancellationToken);
        }

        private async Task<MercadoLivreResultado[]> AnalyzeProductsInMLAsync(List<string> products)
        {
            var tasks = new List<Task<MercadoLivreResultado>>();
            foreach (var product in products)
            {
                tasks.Add(_mercadoLivreService.AnalyzeProductsAsync(product));
            }

            return await Task.WhenAll(tasks);
        }

        private void SendAnalizeResultsToQueue(MercadoLivreResultado[] analyzeResults)
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

            string analyzeResultsJson = JsonSerializer.Serialize(analyzeResults, options);
            _logger.LogInformation("Resultados analise json: {analyzeResultsJson}", analyzeResultsJson);

            IBasicProperties props = _channelOutput.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2;//2 = persistente

            byte[] messageBodyBytes = Encoding.UTF8.GetBytes(analyzeResultsJson);
            _channelOutput.BasicPublish(ExchangeName, RoutingKeyOutput, props, messageBodyBytes);
            _channelOutput.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        }
    }
}