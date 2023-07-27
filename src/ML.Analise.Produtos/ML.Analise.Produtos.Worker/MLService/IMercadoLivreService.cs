namespace ML.Analise.Produtos.Worker.MLService
{
    public interface IMercadoLivreService
    {
        Task<MercadoLivreResultado> AnalyzeProductsAsync(string productName);
    }
}