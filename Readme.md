# ArquiteturaBaseApi - Template para Web API .NET 10

Este repositório contém uma arquitetura base para criação de APIs RESTful em .NET 10, com MongoDB, JWT e organização em camadas. Além disso, ele é empacotado como um template personalizado do .NET, permitindo criar novos projetos com um único comando.

---

## 1. Criar o projeto e instalar as dependências

Verifique se o SDK do .NET 10 está instalado:

```powershell
dotnet --version
```

Crie o projeto Web API e entre na pasta criada:

```powershell
dotnet new webapi -n ArquiteturaBaseApi
Set-Location ArquiteturaBaseApi
```

Adicione os pacotes utilizados pela aplicação:

```powershell
dotnet add package BCrypt.Net-Next --version 4.2.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.11
dotnet add package MongoDB.Driver --version 3.11.0
dotnet add package Swashbuckle.AspNetCore --version 10.2.3
```

Crie as pastas usadas para organizar o código:

```powershell
New-Item -ItemType Directory -Force -Path Config, Controllers, DTOs, Models, Services
New-Item -ItemType Directory -Force -Path Services/Interfaces
```

Restaure as dependências e confirme que o projeto compila:

```powershell
dotnet restore
dotnet build
```

Para executar localmente:

```powershell
dotnet run
```

---

## 2. Criar um Template Próprio do .NET (`dotnet new`)

Essa é a forma oficial e mais integrada ao ecossistema .NET. Você transforma a sua `ArquiteturaBaseApi` em um template personalizado que pode ser chamado via terminal com um simples comando (`dotnet new minha-api`).

### Passo a passo:

1. **Limpe o projeto base**: Remova arquivos temporários (`bin`, `obj`) e dados específicos do projeto anterior.

2. **Crie a pasta `.template.config`**: Dentro da raiz do seu projeto base, crie uma pasta chamada `.template.config` e, dentro dela, um arquivo chamado `template.json` com o seguinte conteúdo:

   ```json
   {
     "$schema": "http://json.schemastorage.org/template",
     "author": "Seu Nome",
     "classifications": ["Web", "API", "Arquitetura Limpa"],
     "identity": "ArquiteturaBaseApi.Template",
     "name": "Arquitetura Base API .NET 10",
     "shortName": "baseapi",
     "sourceName": "ArquiteturaBaseApi",
     "guids": [],
     "primaryOutputFiles": []
   }
   ```

   > O `sourceName` é mágico: quando você criar um projeto novo, ele substitui automaticamente o nome "ArquiteturaBaseApi" pelo nome do seu novo projeto em todos os namespaces e arquivos.

3. **Instale o template localmente**: Abra o terminal na pasta do projeto e execute:

   ```powershell
   dotnet new install .
   ```

4. **Use o template em qualquer lugar**: Agora, para criar um projeto novo já com pastas, pacotes e estrutura prontos, basta rodar:

   ```powershell
   dotnet new baseapi -n MeuNovoProjeto
   ```

---

## Resumo dos comandos

| Comando | Descrição |
|---------|-----------|
| `dotnet new webapi -n ArquiteturaBaseApi` | Cria o projeto base |
| `dotnet add package ...` | Adiciona os pacotes necessários |
| `dotnet restore` | Restaura dependências |
| `dotnet build` | Compila o projeto |
| `dotnet run` | Executa a API |
| `dotnet new install .` | Instala o template a partir da pasta atual |
| `dotnet new baseapi -n MeuNovoProjeto` | Cria um novo projeto usando o template |

---

Agora você tem uma base sólida para iniciar seus projetos com .NET 10, seguindo boas práticas de arquitetura e produtividade.