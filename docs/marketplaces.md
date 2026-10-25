# Cadastro de marketplaces

O CRUD está disponível em `/marketplaces` e oferece suporte a Mercado Livre e Shopee. Ele armazena nome da conta, identificador de afiliado e status ativo. Senhas e segredos de API não fazem parte desse cadastro.

## MongoDB Atlas

O projeto usa o driver oficial `MongoDB.Driver` e lê a conexão por configuração externa. O nome padrão do banco é `southor_afiliados`.

Para desenvolvimento local, configure User Secrets (na pasta do projeto Web):

```powershell
dotnet user-secrets init --project src/ST.Afiliado.Web
dotnet user-secrets set "MongoDb:ConnectionString" "mongodb+srv://<usuario>:<senha>@<cluster>/?retryWrites=true&w=majority" --project src/ST.Afiliado.Web
```

Opcionalmente, altere `MongoDb:DatabaseName`. No container/Azure, defina `MongoDb__ConnectionString` e `MongoDb__DatabaseName` como variáveis de ambiente; mantenha a URI em um secret do ambiente de deploy, não no repositório ou em `appsettings.json`.

Sem a connection string, o app seleciona um repositório em memória para desenvolvimento e mostra um aviso: esses dados são descartados ao reiniciar. Configure um cluster MongoDB Atlas Free, crie um usuário de banco e autorize somente os endereços de rede necessários. O tier Free oferece 512 MB e impõe limites operacionais; confira os limites atuais antes de uso em produção. [Documentação dos limites](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/) · [Preços](https://www.mongodb.com/pricing).
