using FluentAssertions;
using Microsoft.Extensions.Logging;
using ML.Analise.Produtos.Worker.MLService;
using ML.Analise.Produtos.Worker.MLService.DTOs;
using Moq;

namespace ML.Analise.Produtos.Tests.MLService
{
    public class MercadoLivreServiceTest
    {
        private readonly Mock<ILogger<MercadoLivreService>> _loggerMock;
        private readonly Mock<IMercadoLivreApi> _mercadoLivreApiMock;

        public MercadoLivreServiceTest()
        {
            _loggerMock = new Mock<ILogger<MercadoLivreService>>();
            _mercadoLivreApiMock = new Mock<IMercadoLivreApi>();
        }

        [Fact]
        public async Task ThrowIfHttpStatusCodeNotOK()
        {
            _mercadoLivreApiMock.Setup(m => m.GetProductsByname(It.IsAny<string>())).ThrowsAsync(new Exception());

            var MLService = GetMercadoLivreService();
            Func<Task> func = async () => await MLService.AnalyzeProductsAsync("produto");
            await func.Should().ThrowAsync<Exception>();

            _mercadoLivreApiMock.Verify(m => m.GetProductsByname("produto"));
        }

        [Fact]
        public async Task ReturnProductNotFoundIfApiReturnsEmpty()
        {
            var MLResponseDTO = new MercadoLivreResponseDTO
            {
                Paging = new Paging
                {
                    Total = 0
                }
            };
            _mercadoLivreApiMock.Setup(m => m.GetProductsByname(It.IsAny<string>())).ReturnsAsync(MLResponseDTO);

            var MLService = GetMercadoLivreService();
            var analyzeResult = await MLService.AnalyzeProductsAsync("produto");

            analyzeResult.NomeProduto.Should().Be("produto");
            analyzeResult.StatusVerificacao.Should().Be(StatusVerificacao.NaoEncontrado);
            analyzeResult.UrlProduto.Should().BeNull();
            analyzeResult.ValorProduto.Should().Be(0m);

            _mercadoLivreApiMock.Verify(m => m.GetProductsByname("produto"));
        }

        [Fact]
        public async Task ReturnProductFoundIfApiReturnsProductContainsName()
        {
            var MLResponseDTO = new MercadoLivreResponseDTO
            {
                Paging = new Paging
                {
                    Total = 2
                },
                Results = new List<Result> {
                    new Result
                    {
                        Title = "teste",
                        Permalink = "link1",
                        Price = 0.1m
                    },
                    new Result
                    {
                        Title = "produto 1",
                        Permalink = "link2",
                        Price = 10.53m
                    }
                }
            };

            _mercadoLivreApiMock.Setup(m => m.GetProductsByname(It.IsAny<string>())).ReturnsAsync(MLResponseDTO);

            var MLService = GetMercadoLivreService();
            var analyzeResult = await MLService.AnalyzeProductsAsync("produto");

            analyzeResult.NomeProduto.Should().Be("produto");
            analyzeResult.StatusVerificacao.Should().Be(StatusVerificacao.Encontrato);
            analyzeResult.UrlProduto.Should().Be("link2");
            analyzeResult.ValorProduto.Should().Be(10.53m);

            _mercadoLivreApiMock.Verify(m => m.GetProductsByname("produto"));
        }


        [Fact]
        public async Task ReturnFirstProductAsSuspectIfApiReturnsProductsButNotContainsName()
        {
            var MLResponseDTO = new MercadoLivreResponseDTO
            {
                Paging = new Paging
                {
                    Total = 1
                },
                Results = new List<Result> {
                    new Result
                    {
                        Title = "teste1",
                        Permalink = "link1",
                        Price = 0.1m
                    },
                    new Result
                    {
                        Title = "teste2",
                        Permalink = "link2",
                        Price = 10.53m
                    }
                }
            };

            _mercadoLivreApiMock.Setup(m => m.GetProductsByname(It.IsAny<string>())).ReturnsAsync(MLResponseDTO);

            var MLService = GetMercadoLivreService();
            var analyzeResult = await MLService.AnalyzeProductsAsync("produto");

            analyzeResult.NomeProduto.Should().Be("produto");
            analyzeResult.StatusVerificacao.Should().Be(StatusVerificacao.Suspeito);
            analyzeResult.UrlProduto.Should().Be("link1");
            analyzeResult.ValorProduto.Should().Be(0.1m);
            _mercadoLivreApiMock.Verify(m => m.GetProductsByname("produto"));
        }

        private MercadoLivreService GetMercadoLivreService()
        {
            return new MercadoLivreService(_mercadoLivreApiMock.Object, _loggerMock.Object);
        }
    }
}
