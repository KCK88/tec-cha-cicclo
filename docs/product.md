# Produto

O Cicclo é um aplicativo de lavanderia com carteira digital. A pessoa entra com e-mail e senha, vê o saldo em reais, pede lavagem ou secagem, recarrega a carteira e consulta o extrato. O saldo nasce em 5000 centavos (R$ 50,00). Lavagem custa 1890 centavos e secagem 2090. A compra só grava se o saldo alcança o preço. Cancelar uma compra devolve o valor uma única vez.

## Usuários

Uma pessoa com conta no app. Não há papel de operador, admin de lavanderia nem pagamento com cartão de terceiros. A conta de desenvolvimento está descrita no `README.md` e em `Seed` no `appsettings.json`.

## O que o app faz

- Cadastro e login com e-mail e senha. Conta nova começa com R$ 50,00.
- Lista lavagem e secagem, mostra o saldo e solicita o serviço.
- Recusa a compra quando o saldo não alcança, sem alterar o valor.
- Recarga por um valor em reais informado na tela. Não há operadora de pagamento.
- Extrato com compra, recarga e cancelamento.
- Cancelamento de uma compra ainda não cancelada.
- Access token de 8 horas e refresh token de 30 dias. O refresh antigo deixa de valer quando um novo é emitido.
- Sair da conta no aparelho.

## Fora de escopo

Máquinas, várias lavanderias, login social, biometria, magic link, MFA, gateway de pagamento, cupom e programa de fidelidade. Não implemente isso a menos que o pedido da tarefa inclua.

## Regras que não podem mudar por acidente

- Preço e saldo são inteiros em centavos. A JSON usa `balanceCents` e `priceCents`. O app formata em reais.
- Duas compras ao mesmo tempo não gastam os mesmos centavos.
- O saldo não fica negativo.
- Cada compra, recarga e cancelamento grava um lançamento na mesma transação da mudança de saldo.
- Cancelar devolve o valor uma vez. Recarga e lançamento de cancelamento não são canceláveis como compra.
- Recarga aceita de 1 a 1_000_000 centavos.
