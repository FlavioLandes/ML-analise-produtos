using ML.Analise.Produtos.Worker.MLService.DTOs;
using Refit;

namespace ML.Analise.Produtos.Worker.MLService
{
    public interface IMercadoLivreApi
    {
        [Get("/sites/MLB/search?q={productName}")]
        public Task<MercadoLivreResponseDTO> GetProductsByname(string productName);
    }
}
