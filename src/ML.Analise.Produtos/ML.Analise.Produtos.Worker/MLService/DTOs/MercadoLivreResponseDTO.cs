using System.Text.Json.Serialization;

namespace ML.Analise.Produtos.Worker.MLService.DTOs
{
    public class MercadoLivreResponseDTO
    {
        [JsonPropertyName("paging")]
        public Paging Paging { get; set; }

        [JsonPropertyName("results")]
        public List<Result> Results { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("permalink")]
        public string Permalink { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }

    public class Paging
    {
        [JsonPropertyName("total")]
        public long Total { get; set; }
    }
}
