
# Definição do banco de dados relacional

## Tecnologias

- SQLite database
- Dapper

## Contrução no .NET

- Projeto Class Library dentro do solution ControleMateriais
- Construção usando o Repository Pattern
- Criptografia de dados
- Rotina inteligente de backup com limpeza automatizada no encerramento da aplicação

## Definição Arquitetural

A solution do projeto deve ser dividida em dois projetos:

1. `ControleMateriais.Desktop` é o projeto principal, ele vai consumir as interfaces dos repositórios do banco de dados.
2. `ControleMateriais.Data` é o Class Library com a configuração do SQLite e do Dapper, onde fica toda a estruturação do banco de dados.

## Dependências 

- `Microsoft.Data.Sqlite.Core` : Drive oficial da Microsoft para o SQLite.
- `SQLitePCLRaw.bundle.e_sqlcipher` : Suport a criptografia usando SQLCipher.
- `Dapper` : Micro-ORM para consultas rápidas e seguras usando parâmetros.

## Estrutura 

```
ControleMateriais.Data
|
|- Common/DbSettings.cs # Definição  de caminhos e chavs
|
|- Interfaces/IMaterialRepository.cs # Contrato para o projeto Desktop
|
|- Repositories/SqliteMaterialRepository.cs # Implementação real das consultas SQL
|
|- Services/
    |
    |- DatabaseInitializer.cs # Cria o Arquivo .db e as tabelas
    |- BackupService.cs       # Gerencia backups e rotina de limpeza
```


