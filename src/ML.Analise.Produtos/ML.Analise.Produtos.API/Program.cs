using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var factory = new ConnectionFactory
{
    Uri = new Uri("amqp://guest:guest@localhost:5672")
};

IConnection conn = factory.CreateConnection();

IModel channel = conn.CreateModel();

const string ExchangeName = "produto.analise";
const string QueueName = "produto.analise.solicitacao";
const string RoutingKey = "solicitacao";

channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
channel.QueueDeclare(QueueName, durable: true, false, false, new Dictionary<string, object> { { "x-queue-type", "classic" } });
channel.QueueBind(QueueName, ExchangeName, RoutingKey, null);

app.MapPost("produtos/analise", (IList<string> nomesProdutos) =>
{
    try
    {
        IBasicProperties props = channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2;//2 = persistente

        byte[] messageBodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(nomesProdutos));
        channel.BasicPublish(ExchangeName, RoutingKey, props, messageBodyBytes);

        return Results.Ok();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Erro ao publicar mesagens a fila");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
})
.WithName("analise");

app.Run();