using ML.Analise.Produtos.Worker.MLService.DTOs;
using System.Net.Http.Json;

namespace ML.Analise.Produtos.Worker.MLService
{
    public class MercadoLivreService : IMercadoLivreService
    {
        private const string URL_ML_Search = "https://api.mercadolibre.com/sites/MLB/search?q=";
        private readonly ILogger<MercadoLivreService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public MercadoLivreService(IHttpClientFactory httpClientFactory, ILogger<MercadoLivreService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<MercadoLivreResultado> AnalyzeProductsAsync(string productName)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                var responseML = await httpClient.GetFromJsonAsync<MercadoLivreResponseDTO>(URL_ML_Search + productName);

                if (responseML.Paging.Total == 0)
                {
                    return new MercadoLivreResultado
                    {
                        NomeProduto = productName,
                        StatusVerificacao = StatusVerificacao.NaoEncontrado
                    };
                }

                foreach (var produto in responseML.Results)
                {
                    if (produto.Title.Contains(productName))
                    {
                        return new MercadoLivreResultado
                        {
                            NomeProduto = productName,
                            StatusVerificacao = StatusVerificacao.Encontrato,
                            UrlProduto = produto.Permalink,
                            ValorProduto = produto.Price
                        };
                    }
                }

                return new MercadoLivreResultado
                {
                    NomeProduto = productName,
                    StatusVerificacao = StatusVerificacao.Suspeito,
                    UrlProduto = responseML.Results[0].Permalink,
                    ValorProduto = responseML.Results[0].Price
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao analisar produto no ML. Erro: {erro}", ex.Message);
                throw;
            }
        }
    }
}