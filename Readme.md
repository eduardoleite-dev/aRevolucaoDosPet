# A Revolução dos Pets — API

API REST desenvolvida em ASP.NET Core para gerenciamento social, administrativo e de saúde de pets. A plataforma centraliza usuários, perfis de pets, seguidores, publicações, interações, histórias, consultas veterinárias, rotina de cuidados, passeios, locais, avaliações e notificações.

## Visão geral

- **Framework:** ASP.NET Core com .NET 10
- **Persistência:** MongoDB Atlas ou MongoDB compatível
- **Autenticação:** JWT Bearer
- **Documentação:** Swagger/OpenAPI disponível na raiz da aplicação
- **Arquitetura:** Controllers → Services → MongoDB collections
- **Hospedagem de referência:** Azure App Service
- **Formato:** JSON

## Funcionalidades

- Cadastro, autenticação, consulta, atualização e exclusão lógica de usuários.
- Perfis detalhados de pets e cadastro de múltiplos tutores.
- Busca geográfica por usuários e documentos com localização.
- Publicações, comentários, curtidas, reações e histórias temporárias.
- Vacinas, consultas, peso, diário, despesas, ciclos de cio e gravidez.
- Passeios, encontros durante passeios, locais e avaliações.
- Configurações de privacidade, idioma, tema e notificações.
- Controle de acesso por perfil: tutor e administrador.
- Criação automática de índices MongoDB durante a inicialização da aplicação.

## Arquitetura

```mermaid
flowchart LR
    Client[Cliente / Front-end / Swagger] --> Controllers
    Controllers --> Services
    Services --> MongoDB[(MongoDB)]
    Services --> JWT[JWT Authentication]
    MongoIndexInitializer[Inicializador de índices] --> MongoDB
```

### Estrutura principal

```text
Controllers/    Endpoints HTTP e controle de autorização
Services/       Regras de negócio e acesso aos dados
Models/         Documentos persistidos no MongoDB
DTOs/           Contratos de entrada e saída da API
Enums/          Domínios de status, espécies, sexos e perfis
Config/         MongoDB, JWT, Swagger e injeção de dependências
Properties/     Configuração de execução local
publish/        Artefatos gerados para publicação
```

## Pré-requisitos

- .NET SDK 10
- MongoDB Atlas ou instância MongoDB acessível pela aplicação
- Azure CLI, opcionalmente, para implantação
- PowerShell, opcionalmente, para executar o script de deploy

## Configuração local

### 1. Restaurar dependências

```powershell
cd C:\caminho\aRevolucaoDosPet
dotnet restore
```

### 2. Configurar variáveis de ambiente

A aplicação aceita a conexão MongoDB pelas seguintes chaves, nesta ordem de prioridade:

- `ConnectionStrings__MongoDB`
- `ConnectionStrings:MongoDB`
- `MongoDbSettings:ConnectionString`

Exemplo no PowerShell:

```powershell
$env:ConnectionStrings__MongoDB = "mongodb+srv://USUARIO:SENHA@CLUSTER.mongodb.net/aRevolucaoDosPets?retryWrites=true&w=majority"
$env:JwtSettings__SecretKey = "sua-chave-secreta-forte-e-com-privilégio-apenas-no-servidor"
$env:ASPNETCORE_ENVIRONMENT = "Development"
```


O nome padrão do banco usado pelo projeto é `aRevolucaoDosPet`. Quando o banco estiver informado explicitamente na URI MongoDB, ele prevalece sobre esse valor.

Para configuração no Azure App Service, registre os valores como:

- `ConnectionStrings__MongoDB`
- `JwtSettings__SecretKey`

### 3. Executar a API

```powershell
dotnet run
```

URLs configuradas localmente:

- HTTP: `http://localhost:5159`
- HTTPS: `https://localhost:7067`

A interface Swagger é disponibilizada na raiz da aplicação:

- `http://localhost:5159/`
- `https://localhost:7067/`

## Autenticação JWT

### Login

```http
POST /api/Auth/login
Content-Type: application/json

{
  "username": "usuario-ou-email",
  "password": "senha"
}
```

A resposta contém:

```json
{
  "token": "<JWT>",
  "expiresAt": "2026-01-01T00:00:00Z",
  "username": "nome",
  "userId": "<ObjectId>"
}
```

### Uso nas requisições

```http
Authorization: Bearer SEU_TOKEN_JWT
```

No Swagger, utilize o botão **Authorize**, informe apenas o token JWT e confirme. O token possui validade de duas horas.

### Níveis de acesso

- **TUTOR:** acesso aos próprios recursos conforme regras dos serviços/controllers.
- **ADMINISTRADOR:** acesso a consultas administrativas, como busca global de usuários.

## Endpoints

Os nomes entre colchetes representam os módulos disponíveis.

| Módulo | Base da API | Operações principais |
|---|---|---|
| Autenticação | `/api/Auth` | Login |
| Usuários | `/api/Users` | Cadastro, busca administrativa, consulta, atualização e exclusão |
| Configurações do usuário | `/api/UserSettings` | CRUD das preferências |
| Pets | `/api/Pets` | CRUD, pesquisa e perfil do animal |
| Seguidores de pets | `/api/PetFollowers` | Seguimento por usuário ou por outro pet |
| Publicações | `/api/Posts` | CRUD e consulta de publicações |
| Reações | `/api/PostReactions` | Reagir e remover reação em publicação |
| Comentários | `/api/Comments` | CRUD, respostas e reações |
| Histórias | `/api/Stories` | CRUD, visualizações e publicação temporária |
| Vacinas | `/api/Vaccines` | CRUD do histórico de imunização |
| Consultas | `/api/Appointments` | CRUD de consultas veterinárias |
| Peso | `/api/WeightLogs` | Registro e histórico do peso |
| Diário | `/api/DiaryEntries` | CRUD das entradas do diário |
| Despesas | `/api/Expenses` | CRUD financeiro por pet |
| Ciclos de cio | `/api/HeatCycles` | CRUD do ciclo reprodutivo |
| Gravidez | `/api/Pregnancies` | CRUD de acompanhamentos gestacionais |
| Passeios | `/api/Walks` | CRUD dos passeios |
| Encontros | `/api/WalkEncounters` | Registro de encontros em passeios |
| Locais | `/api/Places` | CRUD de locais pet-friendly |
| Avaliações de locais | `/api/PlaceReviews` | Consulta e avaliação de locais |
| Notificações | `/api/Notifications` | Consulta, criação, leitura e exclusão |
| Status da API | `/api/HelloWorld` | Endpoint público e privado de testes |

A documentação completa dos parâmetros, corpos, respostas e status HTTP é gerada pelo Swagger.

## MongoDB

A aplicação utiliza o MongoDB Driver e cria ou atualiza índices na inicialização através de `MongoIndexInitializer`.

Principais collections:

- `Users`
- `UserSettings`
- `Pets`
- `PetFollowers`
- `Posts`
- `PostReaction`
- `Comments`
- `Stories`
- `Vaccines`
- `Appointments`
- `WeightLogs`
- `DiaryEntries`
- `Expenses`
- `HeatCycles`
- `Pregnancies`
- `Walks`
- `WalkEncounters`
- `Places`
- `PlaceReviews`
- `Notifications`

Os identificadores são armazenados como `ObjectId` sempre que aplicável. Dados geográficos seguem o formato GeoJSON compatível com índices `2dsphere`.

## Build, publicação e execução

### Compilar

```powershell
dotnet build -c Release
```

### Publicar localmente

```powershell
dotnet publish -c Release -o ./publish
```

### Executar os artefatos publicados

```powershell
dotnet ./publish/aRevolucaoDosPet.dll
```

## Deploy para Azure App Service

O script `deploy.ps1` executa limpeza, build, publicação, compactação e envio por Azure CLI:

```powershell
.\deploy.ps1
```

Antes da execução, ajuste no script:

```powershell
$AppName       = "aRevolucaoDosPet"
$ResourceGroup = "pet_group"
$Configuration = "Release"
```

Pré-requisitos do Azure:

```powershell
az login
az account set --subscription "ID-ou-nome-da-assinatura"
```

Configure no App Service:

- `ConnectionStrings__MongoDB` com a URI do banco correto.
- `JwtSettings__SecretKey` com uma chave forte exclusiva do ambiente.
- `ASPNETCORE_ENVIRONMENT=Production`.

É necessário que o MongoDB Atlas permita o acesso da aplicação Azure e que o usuário usado possua as permissões adequadas.

## Segurança e boas práticas

- As credenciais MongoDB não devem ficar no código-fonte.
- Rotacione imediatamente qualquer secret que tenha sido exposto anteriormente.
- Use uma chave JWT diferente para cada ambiente.
- Mantenha HTTPS habilitado em produção.
- Restringa a política CORS antes de disponibilizar a API publicamente.
- Configure usuários MongoDB com privilégios mínimos necessários.
- Não utilize credenciais reais em logs, issues ou exemplos.
- Para ambientes profissionais, prefira certificados/segredos gerenciados e Azure Key Vault.

## Observações técnicas

- Todas as controllers usam `ApiController`, validação automática e serialização JSON.
- Enumerações são serializadas como texto graças ao `JsonStringEnumConverter`.
- O Swagger é habilitado em todos os ambientes, incluindo produção, conforme a configuração atual.
- O middleware HTTPS redirection está ativo.
- Os serviços são registrados como escopo por operação HTTP.
- `MongoIndexInitializer` é um hosted service e pode impedir a inicialização caso não seja possível autenticar ou acessar o MongoDB.
- O projeto contém credenciais históricas em arquivos locais; elas devem ser consideradas comprometidas e devem ser trocadas.

## Contribuição

1. Crie uma branch a partir da principal.
2. Implemente e documente a alteração.
3. Execute `dotnet build -c Release` antes do envio.
4. Valide os endpoints alterados pelo Swagger ou pelo arquivo `.http`.
5. Abra um Pull Request com descrição técnica do impacto.