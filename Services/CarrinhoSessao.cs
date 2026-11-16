namespace LojaCupCake.Services
{
    public class CarrinhoSessao
    {
        public long? FilialId { get; set; }

        public List<ItemCarrinhoSessao> Itens { get; set; }
            = new();
    }

    public class ItemCarrinhoSessao
    {
        public long ProdutoId { get; set; }

        public int Quantidade { get; set; }
    }
}