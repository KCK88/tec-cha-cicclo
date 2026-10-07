# Desenvolvimento

Pré-requisitos: Docker, SDK .NET 10 e Node.js 22.

## Banco e API

Na raiz do repositório:

```bash
docker compose up -d
dotnet run --project src/Cicclo.Api --urls http://0.0.0.0:8080
```

No Rider, abra `Cicclo.sln`. Defina `Cicclo.Api` como projeto de inicialização e use o perfil `http`. O Postgres precisa estar no ar antes, senão a API para na migration.

A API aplica as migrations ao subir e escuta em todas as interfaces, na porta 8080. O Compose publica o Postgres na porta 5433 do computador.

`GET /health` responde `{ "status": "ok" }`. Em Development, o documento OpenAPI fica mapeado.

A conta e a chave JWT de desenvolvimento estão em `src/Cicclo.Api/appsettings.json`, seções `Seed` e `Jwt`. O `README.md` lista o e-mail e a senha de seed. Não copie a chave de assinatura para a documentação.

## App

```bash
cd apps/mobile
npm install
npx expo start
```

No navegador: `npx expo start --web` abre em `http://localhost:8081`.

A URL da API fica em `apps/mobile/.env.development`:

```text
EXPO_PUBLIC_API_URL=http://localhost:8080
```

No emulador Android, `localhost` vira `10.0.2.2` em `api.ts`. No celular físico, use o IP da máquina na mesma rede, por exemplo `http://192.168.0.10:8080`.

## Testes e tipos

Na raiz:

```bash
dotnet test
```

Os testes sobem um PostgreSQL com Testcontainers. Eles cobrem saldo inicial, e-mail duplicado, senha errada, lavagem e secagem deixando 1020 centavos, terceira compra recusada, serviço inexistente, duas compras paralelas com saldo para uma só, recarga e extrato, cancelamento único, e rotação do refresh token.

No mobile, quando a tarefa mudar TypeScript:

```bash
cd apps/mobile
npx tsc --noEmit
```

Não há ESLint, Prettier nem testes de componente no app.

## APK

O perfil `preview` em `apps/mobile/eas.json` gera um APK Android.

```bash
cd apps/mobile
npx eas-cli build -p android --profile preview
```

O comando pede uma conta Expo e roda o build na nuvem. O arquivo não vai no Git.

A build já publicada, gerada com `http://localhost:8080`, está em:

https://expo.dev/accounts/claudiombs/projects/cicclo/builds/2d6ddbf8-adee-4e7b-abbf-e3078ec5ec62

No celular esse endereço não alcança a API. Outra build só passa a usar a API publicada se `EXPO_PUBLIC_API_URL` estiver no perfil `preview` de `apps/mobile/eas.json` antes do comando.

## Publicar a API

A plataforma é a Render: HTTPS incluso, Postgres no mesmo lugar e o repositório já está no GitHub. O APK de release não chama HTTP puro.

`render.yaml` cria o serviço `cicclo-api` e o banco `cicclo-db`, os dois em Oregon, no plano gratuito. O `Dockerfile` escuta a porta `PORT`. Na subida, `DATABASE_URL` vira a connection string do Npgsql. As migrations e o seed rodam como no ambiente local.

No painel da Render: New → Blueprint, repositório `KCK88/tec-cha-cicclo`. Quando o deploy ficar verde, `GET /health` na URL `https://….onrender.com` responde `{ "status": "ok" }`.

O serviço gratuito dorme. A primeira chamada depois disso pode levar cerca de um minuto. O Postgres gratuito expira 30 dias depois da criação.

A chave JWT desta imagem continua a do `appsettings.json` do desafio. Para trocar sem gravar outro segredo no Git, defina `Jwt__SigningKey` no painel da Render.

## O que não versionar

Segredos novos, `.env` local, `appsettings.*.local.json`, `node_modules`, pastas `android/` e `ios/` geradas, e o APK. A configuração de desenvolvimento deste desafio já está no repositório de propósito.
