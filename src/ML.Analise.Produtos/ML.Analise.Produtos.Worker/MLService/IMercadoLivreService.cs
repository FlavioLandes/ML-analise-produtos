namespace ML.Analise.Produtos.Worker.MLService
{
    public interface IMercadoLivreService
    {
        Task<MercadoLivreResultado> VerificarProdutoAsync(string nome);
    }
}