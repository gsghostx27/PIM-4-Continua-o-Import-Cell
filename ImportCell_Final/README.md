# ImportCell_Final — Projeto acadêmico

API ASP.NET Core (.NET 8) com Entity Framework Core e SQL Server e interface web em `wwwroot/index.html`.

## Organização

- `Controllers/`: rotas HTTP, validações e regras de acesso.
- `Models/`: entidades persistidas no banco.
- `Data/AppDbContext.cs`: mapeamento das tabelas e relacionamentos.
- `Program.cs`: configuração da aplicação.
- `wwwroot/`: página da loja.

## Como iniciar no PowerShell

Abra o terminal na pasta que contém `ImportCell_Final.csproj` e execute:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5000"
dotnet restore
dotnet run --no-launch-profile
```

Depois abra `http://localhost:5000`. Mantenha o terminal aberto. Para parar, use Ctrl+C.

**Pré-requisitos:** .NET 8 SDK, SQL Server acessível e configuração local de conexão e chave JWT.

## Fluxo da aplicação

1. O usuário consulta os smartphones, cores, armazenamento e estoque.
2. Faz login para receber um token JWT.
3. Escolhe os produtos e registra um pedido com endereço e método de pagamento demonstrativo.
4. A API registra os itens, calcula os valores e atualiza o estoque.
5. Um pedido pendente pode ser cancelado, com devolução do estoque e preservação do histórico.

**Importante:** PIX, crédito e débito são apenas simulações; não existe cobrança real.

## Antes de apresentar ou publicar

- Confira a conexão com SQL Server e as configurações JWT.
- Não publique senhas, chaves JWT, credenciais ou backups reais do banco.
- Teste cadastro, login, catálogo, pedido, cancelamento e estoque.
- Os comentários adicionados explicam a finalidade das classes; a lógica executável não foi alterada.

## Observação sobre o nome

O arquivo de projeto agora é `ImportCell_Final.csproj`. Os namespaces C# continuam como `ImportCell` para preservar as referências e o funcionamento já testado. O banco SQL Server continua sendo `ImportCellBackend`; renomear o projeto não renomeia nem apaga o banco.

## Acessibilidade — VLibras

O frontend inclui o widget VLibras, que disponibiliza um botão de acessibilidade para tradução de conteúdo em português para Libras. A integração foi adicionada em `wwwroot/index.html`, sem mudanças nos controllers, banco de dados ou regras de compra.

O widget é carregado de `https://vlibras.gov.br/app/vlibras-plugin.js` e **precisa de conexão com a internet**. A disponibilidade e a qualidade da tradução dependem do serviço externo.

Para verificar: execute a aplicação, abra `http://localhost:5000`, aguarde o carregamento e procure o botão flutuante do VLibras. Clique e teste a tradução de um trecho da página.
