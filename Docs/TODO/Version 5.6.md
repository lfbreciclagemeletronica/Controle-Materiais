# Ajustes versão 5.6

## Sistema de sincronização

- :check: Quero que quando abrir o programa, mostre uma tela antes de ir para a tela inicial onde ele vai mostrar um loading para cada repositório configurado.
- :check: Atualizando recibos do sistema remoto... (deve fazer pull do repositório https://github.com/lfbreciclagemeletronica/Recibos já configurado no sistema e na localização do repositório já definido).
- :check: Atualizando pesagens do sistema remoto.. (deve fazer pull do repositório https://github.com/lfbreciclagemeletronica/Pesagens já configurado no sistema e na localização do repositório já definido).
- :check: Atualizando banco de dados do sistema remoto... (deve fazer pull do repositório https://github.com/lfbreciclagemeletronica/Bando-de-Dados já configurado no sistema e na localização do repositório já definido).
- :check: Verificando recibos de vendas... (deve verificar se tem novos recibos de vendas vindo do repositório de recibos já configurado no sistema)
- :check: Caso não teve nenhuma alteração, deve mostrar um icon checkmark em verde com a mensagem em verde dizendo:
    - :check: "Recibos locais já atualizado, sem novos recibos" para a atualização dos recibos.
    - :check: "Pesagens locais já atualizado, sem novas pesagens" para a atualização de pesagens.
    - :check: "Banco de dados já atualizado, sem novos registros" para a atualização de banco de dados.
    - :check: "Sem novos recibos de vendas" para a verificação dos recibos de vendas.
- Caso teve novos registros que chegaram, mostre o seguinte:
    - :check: No caso de recibos novos, deve mostrar os nomes dos novos recibos e as datas.
    - :check: No caso de pesagens novas, deve mostrar os nomes das pesagens e a data.
    - :check: No caso de novos registros no banco de dados, diga em qual arquivo .json foi modificado mostrando qual cliente e data foi adicionado no arquivo.
    - :check: Caso tenha recibos novos que o nome e data não existir no banco de dados deve mostrar uma mensagem de aviso dizendo o nome e data do recibo não está no arquivo .json.
- :check: Deve mostrar um simbolo de loading ao lado de cada atualização, seguindo as melhores tecnicas no avaloania.
- :check: Eles devem ser atualizados de forma assincrona, onde eles deve ser feitos pull juntos.

## Reajuste de geração de recibos de compra e atualização no banco de dados

- Mudar botão "Exportar" de criação de recibos de compra para "Salvar recibo".
- Os valores do nome do cliente, do valor de venda e os itens com seus pesos esperados pelo banco de dados devem ser pegos dos campos do sistema.
- o sistema envia esses dados esperados pelo banco para o banco de dados, adicionando no banco como é definido hoje (adicionando ao arquivo do mes e ano da compra)
- Quando é criado um recibo de compra novo, não é necessário extrair do pdf por ser muito complexo, só é retirado do PDF quando estiver reconstruindo o banco de dados do zero.
- Deve ser possivel criar quantos itens personalizados eu quiser, onde pode ser adicionados mais campos pelo usuario.
- Isso deve refletir no PDF gerado, qualquer campo criado na tela de criação de recibos de venda deve ir ao recibo em PDF e no banco de dados.
- Sempre começa com 4 campos de itens personalizados, mas o cliente pode adicionar mais.

## Reajuste de geração de recibos de venda e atualização do banco de dados

- Deve possuir os campos de itens personalizados do sistema de recibos de compras (4 campos de itens personalizados podendo ser aumentado pelo cliente)
- Deve ser possivel colocar a data no recibo de venda na tela de criação do recibo de venda,porque a venda pode ser de outro dia.
- Quando é criado um recibo de venda novo, não é necessário extrair do pdf por ser muito complexo, só é retirado do PDF quando estiver reconstruindo o banco de dados do zero.
- O .json dessa venda deve pegar os dados do nome, da data e de todos os itens com seus pesos do que foi colocado na tela de criação do recibo, e no final o total da venda.
- Esses dados devem ser lidos do .json das vendas e adicionado ao excel existente (hoje ele só pega as somas dos pesos de cada item, deve pegar o total da venda também.

## Reajuste de push dos recibos e banco de dados

- Remova os botões de Sincronizar recibos e sincronizar pesagems e atualizar estoque.
- Quando é criado um novo recibo de compra ou de venda, ele deve fazer um pull no repositório, salvar o recibo no repositório e depois fazer um push no repositório para atualizar externamente.
- Não precisa mais fazer sincronização automática em nenhuma página.
- O banco de dados vai ser atualizado quando entrar novos registros, os registros são adicionados quando é criado um recibo de compra ou venda.

## Reajuste de exclusão de recibos de vendas e atualização do banco de dados

- Quando clicar em Excluir um recibo de vendas, deve pegar o nome e data do .meta.json dele.
- Deve buscar o .json das vendas da data que foi pega do .meta.json (transforme 10/06/2026 em 10-06-2026 para facilitar a busca)
- Deve remover o registro de venda do nome da pessoa naquela data do .json de vendas do dia, caso seja a unica ele deve apagar o .json da venda.
- Deve então na tela de estoque recalcular dos .jsons devido que agora foi excluido um registro. 
