using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace LojaCupCake.Services
{
    public class CarrinhoService
    {
        private const string ChaveCarrinho = "LojaCupCake.Carrinho";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CarrinhoService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Sessao =>
            _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException(
                "Sessão HTTP não disponível."
            );

        public CarrinhoSessao Obter()
        {
            var json = Sessao.GetString(ChaveCarrinho);

            if (string.IsNullOrWhiteSpace(json))
                return new CarrinhoSessao();

            try
            {
                return JsonSerializer.Deserialize<CarrinhoSessao>(json)
                    ?? new CarrinhoSessao();
            }
            catch (JsonException)
            {
                return new CarrinhoSessao();
            }
        }

        public void Salvar(CarrinhoSessao carrinho)
        {
            if (carrinho.Itens.Count == 0)
                carrinho.FilialId = null;

            var json = JsonSerializer.Serialize(carrinho);

            Sessao.SetString(ChaveCarrinho, json);
        }

        public void Limpar()
        {
            Sessao.Remove(ChaveCarrinho);
        }
    }
}
