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
                    var produtos = JsonSerializer.Deserialize<List<string>>(bodyString);

                    var tasks = new List<Task<MercadoLivreResultado>>();
                    foreach (var produto in produtos)
                    {
                        tasks.Add(_mercadoLivreService.VerificarProdutoAsync(produto));
                    }

                    MercadoLivreResultado[] resultadosVerificao = await Task.WhenAll(tasks);

                    IBasicProperties props = _channelOutput.CreateBasicProperties();
                    props.ContentType = "application/json";
                    props.DeliveryMode = 2;//2 = persistente

                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonStringEnumConverter());
                    string resultadosVerificacaoJson = JsonSerializer.Serialize(resultadosVerificao, options);
                    _logger.LogInformation("Resultados verificao json: {resultadosVerificacaoJson}", resultadosVerificacaoJson);

                    byte[] messageBodyBytes = Encoding.UTF8.GetBytes(resultadosVerificacaoJson);
                    _channelOutput.BasicPublish(ExchangeName, RoutingKeyOutput, props, messageBodyBytes);

                    _channelInput.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "erro ao processar mensagem: {mensagem}", bodyString);
                    _channelInput.BasicNack(ea.DeliveryTag, false, false);
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
    }
}