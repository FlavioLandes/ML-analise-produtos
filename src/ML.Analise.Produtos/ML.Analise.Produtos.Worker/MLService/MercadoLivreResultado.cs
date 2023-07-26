namespace ML.Analise.Produtos.Worker.MLService
{
    public class MercadoLivreResultado
    {
        public string NomeProduto { get; set; }
        public string UrlProduto { get; set; }
        public decimal ValorProduto { get; set; }
        public StatusVerificacao StatusVerificacao { get; set; }
    }
}
