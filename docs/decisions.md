# Decisões

## Saldo e preço em centavos

### Contexto

Dinheiro em ponto flutuante acumula erro e complica a comparação de saldo.

### Decisão

Saldo, preço e valor de lançamento são `int` em centavos. A JSON expõe `balanceCents`, `priceCents` e `amountCents`. O app formata com `formatBrl`.

### Motivo

A comparação e o débito ficam exatos. R$ 50,00 é 5000, lavagem 1890, secagem 2090.

### Alternativas consideradas

`decimal` na API e número na JSON. Foi descartado porque o cliente e o banco passariam a concordar sobre escala e arredondamento em cada campo.

## Débito condicional no banco

### Contexto

Duas compras simultâneas com saldo para uma só não podem ambas passar.

### Decisão

A compra faz `UPDATE users SET balance_cents = balance_cents - preço WHERE id = ? AND balance_cents >= preço` dentro de uma transação. Zero linhas afetadas significa saldo insuficiente ou usuário inexistente, sem lançamento. Um `CHECK (balance_cents >= 0)` impede saldo negativo. Recarga e estorno usam o mesmo tipo de `UPDATE`, com teto em `int.MaxValue`.

### Motivo

A condição e a escrita são uma sentença só. O teste `Two_purchases_at_once_cannot_spend_the_same_balance` cobre a corrida.

### Alternativas consideradas

Ler o saldo em memória e gravar depois. Foi descartado porque duas requisições leem o mesmo valor e ambas debitam.

## Lançamento na mesma transação

### Contexto

O extrato precisa explicar cada mudança de saldo, inclusive o estorno.

### Decisão

Compra, recarga e cancelamento gravam `wallet_entries` na mesma transação do `UPDATE` de saldo. Kinds: `purchase`, `top_up`, `cancellation`. Cancelar marca a compra original como `cancelled` e cria outro lançamento com `reverses_entry_id`. O `UPDATE` de `cancelled` exige `cancelled = false`, então o valor volta uma vez.

### Motivo

Saldo e extrato não divergem se a requisição falha no meio. Recarga e cancelamento não são compras, então não entram no fluxo de estorno.

### Alternativas consideradas

Apagar a compra cancelada. Foi descartado porque o extrato perderia o fato de que a compra existiu.

## Identidade própria

### Contexto

O app precisa de cadastro, login e sessão longa, sem papéis administrativos.

### Decisão

Tabela `users` própria. Senha com PBKDF2-SHA256, 100.000 iterações, sal de 16 bytes. Access token JWT HMAC de 8 horas, claims `sub` e e-mail. Refresh opaco de 30 dias, só o hash SHA-256 no banco; ao renovar, o token apresentado é revogado.

### Motivo

O fluxo cabe em poucos endpoints. Não há recuperação de senha, provedor externo nem ASP.NET Identity.

### Alternativas consideradas

ASP.NET Identity. Foi descartado porque traria usuário, role e store além do que o produto usa.

## PostgreSQL 16 via Compose na porta 5433

### Contexto

A API e os testes precisam de Postgres. A máquina pode já usar a porta 5432.

### Decisão

`docker-compose.yml` publica `5433:5432`. A connection string de desenvolvimento aponta para essa porta. Os testes sobem outro Postgres com Testcontainers, não reutilizam o Compose.

### Motivo

O banco do desafio não toma a porta padrão de um Postgres local. O teste não depende do Compose estar no ar.

### Alternativas consideradas

SQLite para o desafio. Foi descartado porque o `UPDATE` condicional e o comportamento de concorrência precisavam do Postgres de verdade.

## App e API no mesmo repositório, sem camadas extras

### Contexto

O escopo é carteira, compra e testes de concorrência.

### Decisão

Telas em `apps/mobile/src/app`. Regra de saldo em `Cicclo.Api`. Sem projetos Domain, Application e Infrastructure.

### Motivo

A regra cabe em `WalletService`. Separar projetos agora espalharia o mesmo fluxo sem novo comportamento.

### Alternativas consideradas

A estrutura futura descrita no `README.md` (screens, services, hooks no mobile; quatro projetos no backend). Fica para quando houver mais serviços, máquinas ou várias lavanderias, e somente se a tarefa pedir a separação.
