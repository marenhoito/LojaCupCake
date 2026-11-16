
using LojaCupCake.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LojaCupCake.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes => Set<Cliente>();

        public DbSet<Funcionario> Funcionarios => Set<Funcionario>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cliente
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(c => c.ClienteId);

                entity.HasIndex(c => c.Cpf)
                    .IsUnique();

                entity.HasOne(c => c.Usuario)
                    .WithOne()
                    .HasForeignKey<Cliente>(c => c.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Funcionario
            modelBuilder.Entity<Funcionario>(entity =>
            {
                entity.HasKey(f => f.FuncionarioId);

                entity.HasIndex(f => f.Matricula)
                    .IsUnique();

                entity.HasOne(f => f.Usuario)
                    .WithOne()
                    .HasForeignKey<Funcionario>(f => f.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // Filial
            modelBuilder.Entity<Filial>(entity =>
            {
                entity.HasKey(f => f.FilialId);

                entity.HasIndex(f => f.Codigo)
                    .IsUnique();

                entity.Property(f => f.Codigo)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(f => f.Nome)
                    .HasMaxLength(120)
                    .IsRequired();
            });

            // FuncionarioFilial
            modelBuilder.Entity<FuncionarioFilial>(entity =>
            {
                // Chave primária composta
                entity.HasKey(ff => new
                {
                    ff.FuncionarioId,
                    ff.FilialId
                });

                // Relacionamento com Funcionario
                entity.HasOne(ff => ff.Funcionario)
                    .WithMany(f => f.Filiais)
                    .HasForeignKey(ff => ff.FuncionarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacionamento com Filial
                entity.HasOne(ff => ff.Filial)
                    .WithMany(f => f.Funcionarios)
                    .HasForeignKey(ff => ff.FilialId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasKey(c => c.CategoriaId);

                entity.HasIndex(c => c.Nome)
                    .IsUnique();

                entity.Property(c => c.Nome)
                    .HasMaxLength(80)
                    .IsRequired();
            });

            // Produto
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.HasKey(p => p.ProdutoId);

                entity.HasIndex(p => p.Codigo)
                    .IsUnique();

                entity.Property(p => p.PrecoAtual)
                    .HasPrecision(10, 2);

                entity.HasOne(p => p.Categoria)
                    .WithMany(c => c.Produtos)
                    .HasForeignKey(p => p.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);


                // Estoque
                modelBuilder.Entity<Estoque>(entity =>
                {
                    // Chave primária composta
                    entity.HasKey(e => new
                    {
                        e.FilialId,
                        e.ProdutoId
                    });

                    // Relacionamento com Filial
                    entity.HasOne(e => e.Filial)
                        .WithMany(f => f.Estoques)
                        .HasForeignKey(e => e.FilialId)
                        .OnDelete(DeleteBehavior.Restrict);

                    // Relacionamento com Produto
                    entity.HasOne(e => e.Produto)
                        .WithMany(p => p.Estoques)
                        .HasForeignKey(e => e.ProdutoId)
                        .OnDelete(DeleteBehavior.Restrict);
                });

                // MovimentacaoEstoque
                modelBuilder.Entity<MovimentacaoEstoque>(entity =>
                {


                    // Relacionamento com o usuário responsável
                    entity.HasOne(m => m.Responsavel)
                        .WithMany()
                        .HasForeignKey(m => m.UsuarioId)
                        .OnDelete(DeleteBehavior.Restrict);



                    entity.HasKey(m => m.MovimentacaoEstoqueId);

                    // Armazenar o tipo como texto no MySQL
                    entity.Property(m => m.Tipo)
                        .HasConversion<string>()
                        .HasMaxLength(20);

                    // Relacionamento com Estoque
                    entity.HasOne(m => m.Estoque)
                        .WithMany(e => e.Movimentacoes)
                        .HasForeignKey(m => new
                        {
                            m.FilialId,
                            m.ProdutoId
                        })
                        .OnDelete(DeleteBehavior.Restrict);

                    // Índice para consultas do histórico
                    entity.HasIndex(m => new
                    {
                        m.FilialId,
                        m.ProdutoId,
                        m.CriadoEm
                    });

                });





            });


            // Endereco
            modelBuilder.Entity<Endereco>(entity =>
            {
                entity.HasKey(e => e.EnderecoId);

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Enderecos)
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Pedido
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(p => p.PedidoId);

                entity.HasIndex(p => p.CodigoPedido)
                    .IsUnique();

                entity.HasIndex(p => new
                {
                    p.FilialId,
                    p.StatusPedido,
                    p.DataPedido
                });

                entity.Property(p => p.TipoRecebimento)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(p => p.StatusPedido)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.Property(p => p.Subtotal).HasPrecision(10, 2);
                entity.Property(p => p.Desconto).HasPrecision(10, 2);
                entity.Property(p => p.TaxaEntrega).HasPrecision(10, 2);
                entity.Property(p => p.Total).HasPrecision(10, 2);

                entity.Property(p => p.EntregaConfirmadaPelaLoja)
                    .HasDefaultValue(false);

                entity.Property(p => p.EntregaConfirmadaPeloCliente)
                    .HasDefaultValue(false);

                entity.HasOne(p => p.Cliente)
                    .WithMany(c => c.Pedidos)
                    .HasForeignKey(p => p.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Filial)
                    .WithMany(f => f.Pedidos)
                    .HasForeignKey(p => p.FilialId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ItemPedido
            modelBuilder.Entity<ItemPedido>(entity =>
            {
                entity.HasKey(i => i.ItemPedidoId);

                entity.Property(i => i.PrecoUnitario)
                    .HasPrecision(10, 2);

                entity.Property(i => i.DescontoItem)
                    .HasPrecision(10, 2);

                entity.Property(i => i.TotalItem)
                    .HasPrecision(10, 2);

                entity.HasOne(i => i.Pedido)
                    .WithMany(p => p.Itens)
                    .HasForeignKey(i => i.PedidoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Produto)
                    .WithMany(p => p.ItensPedido)
                    .HasForeignKey(i => i.ProdutoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // EnderecoEntregaPedido
            modelBuilder.Entity<EnderecoEntregaPedido>(entity =>
            {
                entity.HasKey(e => e.PedidoId);

                entity.HasOne(e => e.Pedido)
                    .WithOne(p => p.EnderecoEntrega)
                    .HasForeignKey<EnderecoEntregaPedido>(e => e.PedidoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Endereco)
                    .WithMany()
                    .HasForeignKey(e => e.EnderecoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Pagamento
            modelBuilder.Entity<Pagamento>(entity =>
            {
                entity.HasKey(p => p.PagamentoId);

                entity.Property(p => p.FormaPagamento)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(p => p.StatusPagamento)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(p => p.Valor)
                    .HasPrecision(10, 2);

                entity.HasIndex(p => p.CodigoTransacao)
                    .IsUnique();

                entity.HasOne(p => p.Pedido)
                    .WithMany(p => p.Pagamentos)
                    .HasForeignKey(p => p.PedidoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Chat vinculado ao pedido
            modelBuilder.Entity<ChatMensagem>(entity =>
            {
                entity.HasKey(m => m.ChatMensagemId);

                entity.Property(m => m.RemetenteNome)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(m => m.Conteudo)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.HasIndex(m => new
                {
                    m.PedidoId,
                    m.EnviadaEm
                });

                entity.HasOne(m => m.Pedido)
                    .WithMany(p => p.MensagensChat)
                    .HasForeignKey(m => m.PedidoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Remetente)
                    .WithMany()
                    .HasForeignKey(m => m.RemetenteUsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // HistoricoStatusPedido
            modelBuilder.Entity<HistoricoStatusPedido>(entity =>
            {
                entity.HasKey(h => h.HistoricoId);

                entity.Property(h => h.StatusAnterior)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.Property(h => h.StatusNovo)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.HasIndex(h => new
                {
                    h.PedidoId,
                    h.DataAlteracao
                });

                entity.HasOne(h => h.Pedido)
                    .WithMany(p => p.Historico)
                    .HasForeignKey(h => h.PedidoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.Responsavel)
                    .WithMany()
                    .HasForeignKey(h => h.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // FuncionamentoFilial
            modelBuilder.Entity<FuncionamentoFilial>(entity =>
            {
                entity.HasKey(f => f.FuncionamentoFilialId);

                entity.Property(f => f.DiaSemana)
                    .HasConversion<byte>()
                    .HasColumnType("tinyint unsigned");

                entity.HasOne(f => f.Filial)
                    .WithMany(f => f.Funcionamentos)
                    .HasForeignKey(f => f.FilialId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(f => new
                {
                    f.FilialId,
                    f.DiaSemana,
                    f.HoraAbertura
                });
            });



        }

        public DbSet<Filial> Filiais => Set<Filial>();

        public DbSet<FuncionarioFilial> FuncionariosFiliais
            => Set<FuncionarioFilial>();

        public DbSet<Categoria> Categorias => Set<Categoria>();

        public DbSet<Produto> Produtos => Set<Produto>();

        public DbSet<Estoque> Estoques => Set<Estoque>();

        public DbSet<MovimentacaoEstoque> MovimentacoesEstoque
            => Set<MovimentacaoEstoque>();

        public DbSet<Endereco> Enderecos => Set<Endereco>();

        public DbSet<Pedido> Pedidos => Set<Pedido>();

        public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

        public DbSet<EnderecoEntregaPedido> EnderecosEntregaPedido
            => Set<EnderecoEntregaPedido>();

        public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

        public DbSet<HistoricoStatusPedido> HistoricosStatusPedido
            => Set<HistoricoStatusPedido>();

        public DbSet<ChatMensagem> ChatMensagens => Set<ChatMensagem>();


        public DbSet<FuncionamentoFilial> FuncionamentosFiliais
            => Set<FuncionamentoFilial>();
    }
}
