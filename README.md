# Cicclo

Aplicativo de lavanderia com carteira digital. O app é React Native (Expo) e a API é .NET, com PostgreSQL.

O saldo nasce em 5000 centavos (R$ 50,00). Lavagem custa 1890 e secagem 2090. A compra só grava se o saldo alcança o preço.

## Executar

Pré-requisitos: Docker, SDK .NET 10 e Node.js 22.

```bash
docker compose up -d
dotnet run --project src/Cicclo.Api --urls http://0.0.0.0:8080
```

No Rider, abra `Cicclo.sln` (o arquivo clássico, ao lado do `Cicclo.slnx`). Defina `Cicclo.Api` como projeto de inicialização e execute o perfil `http`. O Postgres precisa estar no ar antes, senão a API para na migration.

A API aplica as migrations ao subir. Ela escuta em todas as interfaces, na porta 8080. O Postgres do Compose fica na porta 5433 do computador, para não brigar com um Postgres que já use a 5432.

Conta pronta:

- e-mail: `usuario@cicclo.dev`
- senha: `Cicclo.Dev.50`

A chave JWT de desenvolvimento está em `src/Cicclo.Api/appsettings.Development.json`. Ela serve só para este desafio.

App:

```bash
cd apps/mobile
npm install
npx expo start
```

No navegador: `npx expo start --web` abre em `http://localhost:8081`.

A URL da API fica em `apps/mobile/.env.development`:

```
EXPO_PUBLIC_API_URL=http://localhost:8080
```

No emulador Android, `localhost` vira `10.0.2.2` sozinho. No celular físico, troque pelo IP da máquina na mesma rede, por exemplo `http://192.168.0.10:8080`.

## O que a solução faz

- Cadastro e login. A conta nova também começa com R$ 50,00.
- Lista lavagem e secagem, mostra o saldo em reais e solicita o serviço.
- Recusa a compra quando o saldo não alcança, sem alterar o valor.
- Recarga, extrato e cancelamento de uma compra (o valor volta uma única vez).
- Access token de 8 horas e refresh token de 30 dias. O refresh antigo deixa de valer quando um novo é emitido.

## Decisões

- Preço e saldo são inteiros em centavos. A JSON usa `balanceCents` e `priceCents`. O app formata em reais.
- O débito é um `UPDATE` em `users` com `balance_cents >= preço`. Duas compras ao mesmo tempo não gastam os mesmos centavos. Um `CHECK` no banco também impede saldo negativo.
- Cada compra, recarga e cancelamento grava um lançamento na mesma transação da mudança de saldo.
- A tabela de usuários é própria. A senha usa PBKDF2. O token é JWT HMAC, sem ASP.NET Identity.
- PostgreSQL 16 sobe com Docker Compose.

## Testes

```bash
dotnet test
```

Os testes sobem um PostgreSQL com Testcontainers. Eles cobrem o que muda dinheiro de verdade: saldo inicial, lavagem e secagem deixando 1020 centavos, terceira compra recusada, serviço inexistente, e-mail duplicado, senha errada, duas compras paralelas com saldo para uma só, recarga e extrato, cancelamento único, e rotação do refresh token.

## APK

O perfil `preview` em `apps/mobile/eas.json` gera um APK Android.

```bash
cd apps/mobile
npx eas-cli build -p android --profile preview
```

O comando pede uma conta Expo e roda o build na nuvem. O arquivo não vai no Git.

## A fazer

Gerar o APK com a conta Expo, pelo comando acima, e instalar no celular.

## Com mais tempo

A API está em um projeto e as telas estão em `apps/mobile/src/app`. A regra do saldo cabe nesse fluxo, e o tempo foi para a compra, a concorrência e os testes.

Se o domínio crescer (mais serviços, máquinas, várias lavanderias), eu separaria assim:

```text
mobile/src/
├── app/            rotas do Expo
├── screens/
├── components/
├── services/
├── hooks/
├── types/
└── utils/

backend/
├── Cicclo.Domain/           usuário, serviço, lançamento e a regra dos centavos
├── Cicclo.Application/      comprar, recarregar, cancelar e login
├── Cicclo.Infrastructure/   Postgres, hash da senha e JWT
├── Cicclo.Api/              endpoints
└── tests/
```

`app/` continua existindo porque o Expo Router precisa da pasta de rotas. As telas em si passariam para `screens/`. A regra do `UPDATE` condicional seguiria a mesma; só mudaria de projeto, da API para a Infrastructure, chamada pela Application.
