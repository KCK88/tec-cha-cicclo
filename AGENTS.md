# Cicclo — como o agente trabalha

Você é um engenheiro trabalhando neste repositório. O objetivo é mudar o mínimo necessário, com código claro, e preservar o comportamento que já existe.

Antes de uma tarefa que altere comportamento, leia `docs/product.md`, `docs/architecture.md` e `docs/decisions.md`. Para rodar ou testar, use `docs/development.md`. O `README.md` é a entrada do projeto; não o duplique inteiro.

## Stack em uso

Mobile (`apps/mobile`): Expo, Expo Router, React Native, TypeScript. Sessão em `src/session.tsx`, HTTP em `src/api.ts`, tokens em `src/storage.ts`, reais em `src/money.ts`. Rotas em `src/app/`.

API (`src/Cicclo.Api`): .NET 10, EF Core, PostgreSQL 16, JWT HMAC, senha PBKDF2. Testes em `tests/Cicclo.Api.Tests` com Testcontainers.

Não há TanStack Query, Zustand, Zod, React Hook Form, ESLint nem Prettier. Não introduza essas bibliotecas, nem outra dependência, se o que já está no projeto resolve a tarefa. Uma dependência nova só entra quando a tarefa pede e o código atual não cobre o caso de forma simples.

## Arquitetura atual

Mantenha a organização que já está no repositório. `app/` existe porque o Expo Router precisa das rotas. A regra de dinheiro fica em `WalletService`, com `UPDATE` condicional no Postgres. A UI chama `api.ts` e não conhece SQL.

Não reorganize pastas, não extraia camadas e não reescreva um arquivo inteiro quando uma alteração localizada basta. Antes de criar componente, função, endpoint ou teste, procure um equivalente no projeto.

## Decisões e requisitos

Uma decisão que mude arquitetura, dinheiro, autenticação ou contrato da API vai para `docs/decisions.md`, no formato contexto, decisão, motivo e alternativas.

Se um requisito não estiver em `docs/product.md` nem no pedido, explicite a suposição. Se ela mudar o produto, pare e peça confirmação antes de implementar. Não preencha login social, pagamento real, várias lavanderias ou outras funcionalidades que o produto não descreve.

## Validação

Depois de mexer em saldo, compra, recarga, cancelamento, autenticação ou contrato da API, rode `dotnet test`.

Depois de mexer em TypeScript do mobile, rode `npx tsc --noEmit` em `apps/mobile`.

Não diga que um teste passou se ele não foi executado. Diga o que ficou sem teste. O mobile não tem suíte de testes.

## Segurança

Não coloque segredo novo no código, na documentação nem no Git. Chave JWT, senha de seed e connection string de desenvolvimento já estão em `src/Cicclo.Api/appsettings.json` para este desafio; não as copie para `docs/`. Tudo que vai no aplicativo pode ser extraído do aparelho. A chave de assinatura fica só na API.

## Ao terminar

Responda com o que mudou, os arquivos, como o fluxo funciona, quais comandos rodaram e quais decisões ou suposições ficaram. Uma tarefa está concluída quando o código pedido existe, o comportamento anterior que não devia mudar continua valendo, a validação acima foi feita ou declarada como não executada, e a documentação foi atualizada se a mudança for relevante.
