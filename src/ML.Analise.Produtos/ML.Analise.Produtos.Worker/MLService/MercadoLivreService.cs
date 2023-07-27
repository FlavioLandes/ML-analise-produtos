namespace ML.Analise.Produtos.Worker.MLService
{
    public class MercadoLivreService : IMercadoLivreService
    {
        private readonly ILogger<MercadoLivreService> _logger;
        private readonly IMercadoLivreApi _mercadoLivreApi;

        public MercadoLivreService(IMercadoLivreApi mercadoLivreApi, ILogger<MercadoLivreService> logger)
        {
            _mercadoLivreApi = mercadoLivreApi;
            _logger = logger;
        }

        public async Task<MercadoLivreResultado> AnalyzeProductsAsync(string productName)
        {
            try
            {
                var responseML = await _mercadoLivreApi.GetProductsByname(productName);

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