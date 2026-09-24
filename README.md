# Gestão de Viaturas API

API REST em ASP.NET Core 8 e C# para cadastro e acompanhamento de viaturas. Utiliza EF Core com SQL Server, autenticação JWT, senhas com hash do ASP.NET Core Identity, FluentValidation e respostas de erro ProblemDetails.

## Funcionalidades

- CRUD persistido, com `201 Created` e endereço do recurso no cadastro.
- Placas brasileiras no padrão antigo ou Mercosul, sem hífen, normalizadas em maiúsculas e únicas no banco.
- Ano de fabricação maior que 2000 e até o próximo ano; modelo obrigatório de até 50 caracteres.
- Quilometragem inicial imutável e quilometragem atual que nunca diminui.
- Estados: `Disponível`, `Em Patrulha` e `Manutenção`.
- Uma viatura em manutenção não pode passar diretamente para patrulha. Atualizá-la para `Disponível` representa a conclusão da manutenção; depois ela pode entrar em patrulha.
- Controle otimista de concorrência impede duas atualizações simultâneas de sobrescreverem dados silenciosamente.
- Login verifica o hash da senha no banco. JWT valida assinatura, emissor, público e expiração.
- Todos os endpoints de viaturas exigem autenticação. Não há perfis de acesso distintos: todos os usuários autenticados têm o mesmo acesso.

## Executar localmente no Windows

Requisitos: SDK .NET 8 ou superior e SQL Server LocalDB. Para abrir o arquivo `.slnx`, use Visual Studio/SDK compatível (SDK 9.0.200 ou superior); os comandos abaixo funcionam com o SDK 8 porque apontam para os projetos.

Execute na pasta que contém este README:

```powershell
dotnet restore GestaoViaturasAPI.Tests/GestaoViaturasAPI.Tests.csproj
powershell -File scripts/Configurar-Local.ps1
dotnet tool restore
dotnet ef database update --project GestaoViaturasAPI
powershell -File scripts/Configurar-Local.ps1 -CriarUsuario
dotnet run --project GestaoViaturasAPI --launch-profile https
```

O script solicita usuário e senha sem mostrar a senha no terminal. Não existe usuário nem senha padrão. O comando de criação não altera a senha de um usuário existente. A senha é passada somente ao processo filho via ambiente e não é salva na configuração.

Abra `https://localhost:7235/swagger`. Se necessário, configure o certificado local com `dotnet dev-certs https --trust`. Faça login e cole **somente o token** no botão **Authorize** do Swagger. Swagger fica disponível apenas em Development.

### Banco anterior

A configuração padrão desta revisão usa **GestaoViaturasDB_v2**, um banco separado. O banco antigo **GestaoViaturasDB** não foi alterado. A migration inicial destina-se a um banco novo: não a aplique sobre tabelas antigas existentes. A importação dos dados anteriores exige definir ano e quilometragem para cada viatura e tratar placas repetidas antes da migração.

### Configuração e segredos

- `appsettings.json`: configurações compartilháveis, emissor/público JWT e conexão LocalDB.
- `appsettings.Local.json`: chave JWT local; ignorado pelo Git e excluído de `dotnet publish`.
- Variáveis de ambiente têm precedência: `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `ConnectionStrings__DefaultConnection`.
- A API falha na inicialização se a chave JWT tiver menos de 32 bytes ou estiver ausente; não existe chave alternativa embutida.
- Em outros ambientes, configure conexão e chave por variáveis de ambiente ou pelo gerenciador de segredos da hospedagem.
- As migrations nunca são aplicadas automaticamente ao iniciar a API.

## Exemplos

`POST /api/auth/login`

```json
{ "nomeUtilizador": "seu-usuario", "palavraPasse": "sua-senha" }
```

`POST /api/viaturas` (Bearer token obrigatório):

```json
{ "placa": "ABC1D23", "modelo": "SUV", "anoFabricacao": 2024, "quilometragemInicial": 100 }
```

`PUT /api/viaturas/1`:

```json
{ "placa": "ABC1D23", "modelo": "SUV", "anoFabricacao": 2024, "quilometragemAtual": 150, "estado": "Em Patrulha" }
```

O ID vem apenas da URL; data de cadastro e quilometragem inicial não são editáveis. Na resposta, `matricula` é o nome do campo que representa a placa, mantido por compatibilidade com a entidade original.

| Rota | Resultado normal |
|---|---|
| GET /api/viaturas | 200, lista ordenada por ID |
| GET /api/viaturas/{id} | 200 ou 404 |
| POST /api/viaturas | 201 |
| PUT /api/viaturas/{id} | 204 ou 404 |
| DELETE /api/viaturas/{id} | 204 ou 404 |

Entradas inválidas retornam 400; autenticação inválida/ausente, 401; duplicidade, conflito de concorrência ou regra de negócio, 409. Erros inesperados retornam 500 sem expor detalhes internos, com identificador para correlação com os logs.

## Testes

```powershell
dotnet test GestaoViaturasAPI.Tests/GestaoViaturasAPI.Tests.csproj
```

Testes unitários verificam as regras da entidade. Testes de integração executam o pipeline HTTP real com JWT e banco SQLite relacional em memória isolado por teste: CRUD, validação, duplicidade, concorrência, login, tokens inválidos e tratamento global de erros. Não usam nem modificam seu SQL Server.

SQLite não substitui a validação das migrations e dos detalhes específicos do SQL Server. O tratamento de violação de índice único em requisições concorrentes usa os códigos 2601/2627 do SQL Server.

O workflow de GitHub Actions restaura, compila e executa os testes a cada push/PR após a publicação do repositório.

## Organização e limites atuais

`Controllers` cuida do HTTP e acesso ao EF; `DTOs`/`Validators` cuidam das entradas; `Entidades/Viatura.cs` concentra invariantes e transições; `Data` configura o banco; `Middlewares` trata exceções. Não foi adicionada uma camada de repositório sem necessidade.

O projeto ainda não inclui Docker, paginação, histórico de manutenção, perfis de autorização ou recuperação de senha. Esses itens são evoluções futuras, não funcionalidades implementadas.
