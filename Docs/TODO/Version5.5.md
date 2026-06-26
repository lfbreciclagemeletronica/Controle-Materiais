
## Melhorias no sistema

### Estoque

#### Atualização banco de dados após recibos

- Toda vez que vai ser criado um recibo de compra ou de de venda, deve atualizar o banco de dados da seguinte forma.
- Os dados no software, como a data do recibo, nome e os itens com seus pesos devem ser pegos na hora que clicar em criar o recibo.
- De vez de pegar os dados do recibo gerado em pdf, devem ser pegos do programa e ser enviados para seus respectivos jsons no repositório banco-de-dados.
- Deve criar um unico arquivo no banco de dados, exemplo `compra-06-2026.json` para todos os recibos do mês 06 e do ano 2026.
- A estrutura do json deve ser a seguinte:

```json
{
  "mes": "06-2026": {
    "registros": [
        {
          "nome": "fulano",
          "data": "01-06-2026",  
          "materiais": [
            "descricao": "Placa Drive",
            "peso": 0.040
          ]
        }
    ]
  }
}
```

- Deve extrair dos recibos de venda o nome do cliente, data e os itens com seus devidos pesos.
- Deve criar um unico arquivo no banco de dados, exemplo `venda-09-06-2026.json` para todos os recibos do dia 09 do mês 06 e do ano 2026.
- Deve ter a mesma estrutura .json do arquivo de compra.
- Os dados requeridos na compra e da venda deve ser pego toda vez que um recibo novo é gerado e ir adicionando aos seus devidos .json.
- Caso não exista um json de compra ou venda do mes e do ano, ele deve ser gerado um novo seguindo o padrão `compra-MM-YYYY.json` se for recibo de compra ou `venda-DD-MM-YYYY.json` se for recibo de venda.

#### Criação do estoque inicial

- Deve possuir o estoque inicial, colocado manualmente pela primeira vez (sistema já tem a área de geração do estoque inicial)
- Ele deve ficar salvo no arquivo `estoque-inicial-MM-YYYY.json` onde DD-YYYY deve ser pego da data colocada no sistema.
- Na página do estoque, deve ler o estoque inicial primeiro (do arquivo `estoque-inicial-MM-YYYY.json) onde deve escolher o estoque inicial do mês selecionado na página (deve ter um dropdown com o mês e ano dos estoque iniciais existentes no repositório banco-de-dados).
- Pode deixar inicialmente ativo o estoque inicial do mês atual.
- Caso não exista, mostre uma mensagem dizendo que não existe ainda o estoque inicial para aquele mes e ano.

#### Reconstrução do banco de dados

- Quando for clicado no botão de recriar o banco de dados na tela de recibos, deve remover todos os .json de compras e vendas, deixando somente o estoque inicial no banco-de-dados.
- Deve depois fazer todo o processo de extrair os dados dos recibos de PDF já existentes.
- Deve seguir a mesma estrutura definida no tópico de extração de dados.
- Nesse caso, ele deve extrair os dados dos recibos já gerados no sistema, pegando a data, nome do cliente, os itens e os pesos.
- Deve criar os jsons como é a estrutura do sistema de compras e vendas já definido no tópico de extração de dados.

#### Cálculo do estoque

- Depois de lido o estoque inicial, deve somar os itens salvos no .json de compras do mês do estoque inicial (ex. se leu `estoque-inicial-06-2026.json` deve somar com `compras-06-2026.json` somente).
- Depois deve pegar o .json de vendas e fazer o cálculo **vendas - (inicial + compras)** porque pode ter valores negativos (vendeu mais do que tinha em estoque).
- Então o fluxo se estiver no mês 06-2026 é:
    1. Ler itens do `estoque-inicial-06-2026.json`
    2. Ler itens de `compras-06-2026.json`
    3. Somar os itens pelos mesmos nomes de `estoque-inicial-06-2026.json` com o `compras-06-2026.json`.
    4. Ler os itens do `venda-09-06-2026.json` e de outros registros de vendas de outros dias.
    5. Soma todos itens das vendas feitas no mes e ano (`venda-09-06-2026.json` + `venda-10-06-2026.json` e assim por diante).
    7. Depois subtraia de cada item o que tem das vendas daquele item com a soma das compras e do estoque inicial, porque pode ter estoque negativo desse item.
    8. Apresente o resultado na página de estoque.

#### Exclusão de um recibo de compra ou venda no estoque

- Quando for deletar um recibo, deve pegar a data e o nome do cliente.
- Deve procurar no .json de compra do mês e do ano aquele cliente e remover os registros daquela data e cliente.
- Deve refazer o calculo do estoque novamente como definido anteriormente.



-  Poder adicionar mais itens personalizados se necessário.
-  Os itens personalizados adicionados no estoque inicial devem permanecer.
-  Ajustar o arquivo .json mensal, onde está com os acentos errados.
-  Mudar forma de sincronização (sistema não precisa a cada interação salvar nos repositórios)
-  Qualquer venda de itens do estoque tem que gerar um .json com as quantidades para depois ser descontado do estoque.
-  No recibo de vendas poder colocar a data da venda, hoje ele pega a data atual somente.
-  Quando é refeito o banco de dados, deve refazer os arquivos .json das vendas também.
-  
