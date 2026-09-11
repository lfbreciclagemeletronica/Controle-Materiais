// Estrutura do banco de dados ControleMateriais.Data
// Compatível com https://dbdiagram.io
// Cole o conteúdo deste arquivo no editor do dbdiagram.io

Table Usuarios {
  Id int [pk, increment, note: 'Identificador unico']
  Nome varchar [not null, note: 'Nome do usuario/cliente/fornecedor']
  Cpf varchar [unique, note: 'CPF (apenas pessoa fisica)']
  Cnpj varchar [unique, note: 'CNPJ (apenas pessoa juridica)']
  ChavePix varchar [not null, note: 'Chave PIX para pagamento']
  CriadoEm datetime [not null, default: `datetime('now')`, note: 'Data de criacao']
  AtualizadoEm datetime [not null, default: `datetime('now')`, note: 'Data da ultima atualizacao']
  Ativo boolean [not null, default: true, note: 'Usuario ativo (1) ou inativo (0)']

  indexes {
    Cpf [unique]
    Cnpj [unique]
  }
}

Table Itens {
  Id int [pk, increment]
  Nome varchar [not null, unique, note: 'Nome do item do catalogo']
  Ordem int [not null, default: 0, note: 'Ordem de exibicao na interface']
  Ativo boolean [not null, default: true, note: 'Item ativo (1) ou inativo (0)']

  indexes {
    Nome [unique]
  }
}

Table RecibosCompra {
  Id int [pk, increment]
  UsuarioId int [not null, ref: > Usuarios.Id, note: 'Usuario que vendeu os materiais']
  NumeroRecibo bigint [unique, note: 'Numero unico do recibo (opcional)']
  DataRecibo datetime [not null, note: 'Data do recibo no formato DD/MM/YYYY']
  ValorTotal decimal [not null, default: 0, note: 'Valor total do recibo']
  PesoTotalKg decimal [not null, default: 0, note: 'Peso total em quilogramas']
  CriadoEm datetime [not null, default: `datetime('now')`, note: 'Data de criacao']
}

Table ReciboCompraItens {
  Id int [pk, increment]
  ReciboCompraId int [not null, ref: > RecibosCompra.Id, note: 'Recibo de compra pai']
  ItemId int [not null, ref: > Itens.Id, note: 'Item do catalogo']
  PesoKg decimal [not null, note: 'Peso do item neste recibo (>= 0)']
  PrecoPorKg decimal [not null, default: 0, note: 'Preco por quilograma']
  Total decimal [not null, default: 0, note: 'PesoKg * PrecoPorKg']
}

Table RecibosVenda {
  Id int [pk, increment]
  ClienteNome varchar [not null, note: 'Nome do cliente que comprou']
  ClienteCpf varchar [note: 'CPF do cliente (PF)']
  ClienteCnpj varchar [note: 'CNPJ do cliente (PJ)']
  ClienteChavePix varchar [note: 'Chave PIX do cliente']
  NumeroRecibo bigint [unique, note: 'Numero unico do recibo (opcional)']
  DataVenda datetime [not null, note: 'Data da venda no formato DD/MM/YYYY']
  ValorTotal decimal [not null, default: 0, note: 'Valor total da venda']
  PesoTotalKg decimal [not null, default: 0, note: 'Peso total em quilogramas']
  CriadoEm datetime [not null, default: `datetime('now')`, note: 'Data de criacao']
}

Table RecibosVendaItens {
  Id int [pk, increment]
  ReciboVendaId int [not null, ref: > RecibosVenda.Id, note: 'Recibo de venda pai']
  ItemId int [not null, ref: > Itens.Id, note: 'Item do catalogo']
  PesoKg decimal [not null, note: 'Peso do item neste recibo (>= 0)']
  PrecoPorKg decimal [not null, default: 0, note: 'Preco por quilograma']
  Total decimal [not null, default: 0, note: 'PesoKg * PrecoPorKg']
}

Table Estoque {
  Id int [pk, increment]
  ItemId int [not null, unique, ref: > Itens.Id, note: 'Item do catalogo']
  TotalComprasKg decimal [not null, default: 0, note: 'Total comprado (tabela fisica alternativa)']
  TotalVendasKg decimal [not null, default: 0, note: 'Total vendido (tabela fisica alternativa)']
  SaldoKg decimal [not null, default: 0, note: 'Saldo fisico (alternativa a VIEW)']
  AtualizadoEm datetime [not null, default: `datetime('now')`, note: 'Data da ultima atualizacao']
}

Table EstoqueInicial {
  Id int [pk, increment]
  ItemId int [not null, ref: > Itens.Id, note: 'Item do catalogo']
  Mes int [not null, note: 'Mes do estoque inicial (1-12)']
  Ano int [not null, note: 'Ano do estoque inicial']
  PesoKg decimal [not null, default: 0, note: 'Peso inicial registrado']
  CriadoEm datetime [not null, default: `datetime('now')`, note: 'Data de criacao']

  indexes {
    (ItemId, Mes, Ano) [unique, name: 'UQ_EstoqueInicial_Item_Mes_Ano']
  }
}

Table Auditoria {
  Id int [pk, increment]
  Tabela varchar [not null, note: 'Nome da tabela alterada']
  Operacao varchar [not null, note: 'INSERT, UPDATE ou DELETE']
  RegistroId int [note: 'Id do registro alterado']
  ValorAntigo text [note: 'Valor antigo serializado em JSON']
  ValorNovo text [note: 'Valor novo serializado em JSON']
  Usuario varchar [note: 'Nome do usuario que fez a alteracao']
  DataHora datetime [not null, note: 'Data e hora da alteracao']
}

// Views nao sao representadas como tabelas no dbdiagram.io, mas documentam-se abaixo:
// EstoqueView: calcula saldo por item a partir de ReciboCompraItens e RecibosVendaItens
// EstoqueInicialView: calcula saldo incluindo EstoqueInicial (permite saldo negativo)
