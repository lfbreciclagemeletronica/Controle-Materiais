# Corrigir Sincronização Recibos de Venda + Reconstruir Banco

Garante que os recibos de venda são sempre enviados ao GitHub, que a aba de Recibos de Venda sempre mostra dados atualizados do remoto, e que o botão "Reconstruir Banco" subtrai corretamente as vendas do estoque.

---

## Diagnóstico Atual

| Ponto | Problema |
|---|---|
| `VendaViewModel.SalvarVendaAsync` | Já chama `PublicarReciboVendaAsync` ✅ — mas o push é silencioso se credenciais não existem; sem garantia de que o repo local existe |
| `EstoqueViewModel.AtualizarCommand` | Chama só `Recarregar()` (leitura local) — não faz pull do GitHub |
| `ReconstruirBancoDadosService.Reconstruir` | Já subtrai vendas (linhas 86–108) ✅ — mas só lê PDFs locais de `Recibos_Venda/`; se a pasta não existir ou estiver desatualizada, não subtrai nada |

---

## Mudanças Planejadas

### 1. `EstoqueViewModel` — Botão "⟳ Atualizar" faz pull do GitHub

**Arquivo:** `ViewModels/EstoqueViewModel.cs`

`AtualizarCommand` hoje chama só `Recarregar()`. Mudar para um novo `AtualizarAsync()` que:
1. Chama `GarantirRecibosRepoAsync` + `SincronizarRecibosAsync` (pull de `Recibos_Venda/`)
2. Chama `Recarregar()` para repopular a lista

Adicionar propriedade `Atualizando` (bool) para desabilitar o botão durante a operação e mostrar status.

**Arquivo AXAML:** `Views/EstoqueView.axaml` — adicionar `IsEnabled="{Binding !Atualizando}"` no botão Atualizar.

---

### 2. `ReconstruirBancoDadosService` — Sincronizar Recibos_Venda antes de reconstruir

**Arquivo:** `ViewModels/PesagensViewModel.cs` — método `ReconstruirBancoDadosAsync`

Antes de chamar `ReconstruirBancoDadosService.Reconstruir`, adicionar:
1. `GarantirRecibosRepoAsync` — garante que o repo existe
2. `SincronizarRecibosAsync` — faz pull de `Recibos_Venda/` do GitHub

Assim o Reconstruir sempre trabalha com os PDFs de venda mais recentes.

---

### 3. Validar que o `VendaViewModel` garante push do recibo

**Arquivo:** `ViewModels/VendaViewModel.cs`

O push já existe via `PublicarReciboVendaAsync`, mas é só invocado se `CredenciaisExistem`. Garantir que:
- Se o repo local não existir ainda, chama `GarantirRecibosRepoAsync` antes do push
- O `.meta.json` também é commitado junto com o PDF (hoje o `PublicarReciboVendaAsync` só adiciona o PDF pelo nome, o meta fica de fora)

**Arquivo:** `Services/GitHubService.cs` — `PublicarReciboVendaAsync`

Após copiar o PDF, também copiar e adicionar o `.meta.json` ao `git add` se existir ao lado do arquivo original.

---

## Arquivos Modificados

```
ViewModels/EstoqueViewModel.cs       — AtualizarCommand → AtualizarAsync (pull + reload)
Views/EstoqueView.axaml              — IsEnabled no botão Atualizar
ViewModels/PesagensViewModel.cs      — ReconstruirBancoDadosAsync: pull antes de reconstruir
Services/GitHubService.cs            — PublicarReciboVendaAsync: inclui .meta.json no commit
```

## Arquivos NÃO modificados

```
Services/ReconstruirBancoDadosService.cs  — subtração já funciona corretamente
VendaViewModel.cs                         — fluxo de push já existe; apenas garantia de repo
```
