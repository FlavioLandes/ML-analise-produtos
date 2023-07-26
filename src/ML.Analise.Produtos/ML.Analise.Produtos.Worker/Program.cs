using ML.Analise.Produtos.Worker;
using ML.Analise.Produtos.Worker.MLService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddTransient<IMercadoLivreService, MercadoLivreService>();
        services.AddHttpClient();
    })
    .Build();

await host.RunAsync();
