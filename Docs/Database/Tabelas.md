## Definição das tabelas

### Tabela de Recibos de vendas

- Fornecedor (ID da tabela de clientes)
- Data do Recibo (Data Compra)
- Peso total
- Valor (Pagamento)
- Referencia a tabela de materiais (ID da tabela de materiais)

### Tabela de materiais

- Deve conter colunas para cada item 
- Deve ter o peso para cada item, se não tiver peso ele deixa zerado.
- Os valores devem chegar em toneladas (verificar qual o tipo compatível para suportar altos valores inteiros flutuantes)

### Tabela de clientes

- Nome do cliente
- CPF/CNPJ
- Chave PIX
