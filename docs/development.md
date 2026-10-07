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

## O que não versionar

Segredos novos, `.env` local, `appsettings.*.local.json`, `node_modules`, pastas `android/` e `ios/` geradas, e o APK. A configuração de desenvolvimento deste desafio já está no repositório de propósito.
