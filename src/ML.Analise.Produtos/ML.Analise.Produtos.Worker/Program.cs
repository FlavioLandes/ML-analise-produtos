using ML.Analise.Produtos.Worker;
using ML.Analise.Produtos.Worker.MLService;
using Refit;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddTransient<IMercadoLivreService, MercadoLivreService>();
        services.AddRefitClient<IMercadoLivreApi>().ConfigureHttpClient(client =>
        {
            client.BaseAddress = new Uri("https://api.mercadolibre.com");
        });
    })
    .Build();

await host.RunAsync();
