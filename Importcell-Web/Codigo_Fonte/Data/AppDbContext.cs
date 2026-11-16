
using System;
using Microsoft.EntityFrameworkCore;
using ImportCell.Models;

namespace ImportCell.Data
{
    // Configura as entidades e seus relacionamentos no SQLite.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // TABELAS DO BANCO DE DADOS
        // ==========================================

        public DbSet<Smartphone> Smartphones { get; set; }

        public DbSet<Cor> Cores { get; set; }

        public DbSet<Armazenamento> Armazenamentos { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<ItemPedido> ItensPedido { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // TABELA SMARTPHONE
            // ==========================================

            modelBuilder.Entity<Smartphone>(entity =>
            {
                entity.ToTable("Smartphone");

                entity.HasKey(s => s.ProdutoId);

                entity.Property(s => s.Nome)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(s => s.Marca)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.Descricao)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(s => s.PrecoBase)
                    .HasPrecision(10, 2);

                entity.Property(s => s.DataCadastro)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(s => s.Ativo)
                    .HasDefaultValue(true);
            });

            // ==========================================
            // TABELA COR
            // ==========================================

            modelBuilder.Entity<Cor>(entity =>
            {
                entity.ToTable("Cor");

                entity.HasKey(c => c.CorId);

                entity.Property(c => c.NomeCor)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(c => c.CodigoHex)
                    .HasMaxLength(7);

                entity.Property(c => c.ImagemCor)
                    .HasMaxLength(500);

                entity.HasOne(c => c.Smartphone)
                    .WithMany(s => s.Cores)
                    .HasForeignKey(c => c.ProdutoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // TABELA ARMAZENAMENTO
            // ==========================================

            modelBuilder.Entity<Armazenamento>(entity =>
            {
                entity.ToTable("Armazenamento");

                entity.HasKey(a => a.ArmazenamentoId);

                entity.Property(a => a.Tamanho)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(a => a.Preco)
                    .HasPrecision(10, 2);

                entity.Property(a => a.Estoque)
                    .IsRequired();

                entity.HasOne(a => a.Smartphone)
                    .WithMany(s => s.Armazenamentos)
                    .HasForeignKey(a => a.ProdutoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // TABELA USUARIO
            // ==========================================

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");

                entity.HasKey(u => u.UsuarioId);

                entity.Property(u => u.Nome)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.Property(u => u.Senha)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(u => u.Telefone)
                    .HasMaxLength(20);

                entity.Property(u => u.Endereco)
                    .HasMaxLength(200);

                entity.Property(u => u.Cidade)
                    .HasMaxLength(100);

                entity.Property(u => u.Cep)
                    .HasMaxLength(10);
            });

            // ==========================================
            // TABELA PEDIDO
            // ==========================================

            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.ToTable("Pedido");

                entity.HasKey(p => p.PedidoId);

                entity.Property(p => p.EnderecoEntrega)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.CidadeEntrega)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.CepEntrega)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(p => p.MetodoPagamento)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(p => p.Status)
                    .HasMaxLength(30);

                entity.Property(p => p.Subtotal)
                    .HasPrecision(10, 2);

                entity.Property(p => p.Frete)
                    .HasPrecision(10, 2)
                    .HasDefaultValue(15.00m);

                entity.Property(p => p.ValorTotal)
                    .HasPrecision(10, 2);

                entity.Property(p => p.DataPedido)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Usuario 1:N Pedido

                entity.HasOne(p => p.Usuario)
                    .WithMany(u => u.Pedidos)
                    .HasForeignKey(p => p.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // TABELA ITEM PEDIDO
            // ==========================================

            modelBuilder.Entity<ItemPedido>(entity =>
            {
                entity.ToTable("ItemPedido");

                entity.HasKey(i => i.ItemPedidoId);

                entity.Property(i => i.Quantidade)
                    .IsRequired();

                entity.Property(i => i.PrecoUnitario)
                    .HasPrecision(10, 2);

                entity.Property(i => i.Subtotal)
                    .HasPrecision(10, 2);

                // Pedido 1:N ItemPedido

                entity.HasOne(i => i.Pedido)
                    .WithMany(p => p.Itens)
                    .HasForeignKey(i => i.PedidoId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Armazenamento 1:N ItemPedido

                entity.HasOne(i => i.Armazenamento)
                    .WithMany(a => a.ItensPedido)
                    .HasForeignKey(i => i.ArmazenamentoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // DADOS INICIAIS - DATA SEEDING
            // ==========================================

            modelBuilder.Entity<Smartphone>().HasData(

                new Smartphone
                {
                    ProdutoId = 1,
                    Nome = "iPhone 16",
                    Marca = "Apple",
                    Descricao = "Smartphone Apple iPhone 16.",
                    PrecoBase = 3999.00m,
                    DataCadastro = new DateTime(2026, 1, 1),
                    Ativo = true
                },

                new Smartphone
                {
                    ProdutoId = 2,
                    Nome = "Samsung Galaxy Z Flip 6",
                    Marca = "Samsung",
                    Descricao = "Smartphone dobrável Samsung Galaxy Z Flip 6.",
                    PrecoBase = 4999.00m,
                    DataCadastro = new DateTime(2026, 1, 1),
                    Ativo = true
                },

                new Smartphone
                {
                    ProdutoId = 3,
                    Nome = "Xiaomi Poco X7",
                    Marca = "Xiaomi",
                    Descricao = "Smartphone Xiaomi Poco X7.",
                    PrecoBase = 1999.00m,
                    DataCadastro = new DateTime(2026, 1, 1),
                    Ativo = true
                }
            );
        }
    }
}
