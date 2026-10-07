# Arquitetura

O repositório tem um app Expo e uma API .NET. O Postgres sobe com Docker Compose. Não há camada Domain/Application/Infrastructure separada: a regra de negócio da carteira está em `WalletService`, na própria API.

```text
Telas (apps/mobile/src/app)
  ↓
Sessão (session.tsx) e formatação (money.ts)
  ↓
HTTP (api.ts)
  ↓
Endpoints (AuthEndpoints, WalletEndpoints)
  ↓
WalletService / serviços de token
  ↓
PostgreSQL
```

## Mobile

| Caminho | Responsabilidade |
|---|---|
| `src/app/index.tsx` | Espera a sessão e redireciona para `/home` ou `/login` |
| `src/app/login.tsx`, `register.tsx` | Credenciais |
| `src/app/home.tsx` | Saldo, serviços, compra, recarga, extrato, cancelamento |
| `src/app/_layout.tsx` | `SessionProvider` e stack sem header |
| `src/session.tsx` | Access token em memória, gravação dos dois tokens, renovação em 401 |
| `src/api.ts` | `fetch`, `ApiError`, troca de `localhost` por `10.0.2.2` no Android |
| `src/storage.ts` | SecureStore no aparelho; `localStorage` na web |
| `src/money.ts` | Centavos para BRL |

Estado de servidor não usa biblioteca de cache. Cada tela busca de novo. Estado global é a sessão. Formulários são `useState` local.

## API

`Program.cs` aplica migrations, faz o seed e mapeia `/health`, `/auth/*` e as rotas da carteira. OpenAPI só em Development.

Autenticação: `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`. O access token é JWT HMAC com `sub` e e-mail. O refresh é opaco; o banco guarda o SHA-256. Rotacionar revoga o token apresentado.

Carteira, todas com Bearer:

| Método | Rota | Efeito |
|---|---|---|
| GET | `/services` | Catálogo |
| GET | `/wallet` | `balanceCents` |
| GET | `/wallet/entries` | Extrato, mais recente primeiro |
| POST | `/wallet/top-ups` | Recarga |
| POST | `/services/{id}/purchases` | Compra |
| POST | `/wallet/entries/{id}/cancellations` | Estorno de uma compra |

Códigos que o app já trata incluem `network`, `invalid_credentials`, `email_taken`, `invalid_request`, `insufficient_balance`, `service_not_found`, `entry_not_found`, `not_a_purchase`, `already_cancelled` e `invalid_refresh_token`.

## Dados

Tabelas: `users`, `services`, `wallet_entries`, `refresh_tokens`.

Checks: `balance_cents >= 0`, `price_cents > 0`, `amount_cents > 0`. E-mail único. Kinds de lançamento: `purchase`, `top_up`, `cancellation`.

Compra, recarga e cancelamento abrem transação, alteram saldo com `UPDATE` condicional e só então inserem o lançamento. Compra sem saldo, ou com saldo insuficiente, não grava lançamento. Cancelamento marca `cancelled` na compra original e cria um lançamento `cancellation` com `reverses_entry_id`.

O catálogo fixo está em `Catalog`: lavagem `8f1c2a10-6b3e-4d5a-9c11-0a1b2c3d4e01`, secagem `8f1c2a10-6b3e-4d5a-9c11-0a1b2c3d4e02`. O seed só insere serviços e o usuário de desenvolvimento se ainda não existirem.

## O que não fazer agora

Separar Domain, Application e Infrastructure, ou mover telas para `features/`, fica para quando o domínio crescer e a tarefa pedir essa mudança. A pasta `app/` continua sendo a das rotas do Expo Router.
