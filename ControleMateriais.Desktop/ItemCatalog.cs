using System.Collections.Generic;

namespace ControleMateriais.Desktop
{
    internal static class ItemCatalog
    {
        public record Section(string Titulo, IReadOnlyList<string> Itens);

        public static readonly string[] OrderedItems =
        {
            // NOTEBOOKS
            "Placa Notebook A",
            "Placa Notebook B",
            "Placa Notebook C",
            // MÃES
            "Placa Mãe A",
            "Placa Mãe B",
            "Placa Mãe C",
            "Placa Mãe D",
            "Placa de Servidor",
            // LEVES
            "Placa Leve",
            "Placa Leve com Ponta",
            "Placa Leve Especial",
            "Placa Leve Especial com Ponta",
            "Placa Leve Especial Completa",
            "Placa Drive",
            // DOURADAS E TAPETES
            "Placa Dourada A",
            "Placa Dourada B",
            "Placa Tapete A",
            "Placa Tapete B",
            "Placa Conectora",
            // INTERMEDIÁRIAS
            "Placa Intermediária A",
            "Placa Intermediária B",
            "Placa Intermediária C",
            // PESADA
            "Placa Pesada",
            "Placa Pesada com Ponta",
            "Placa Lisa",
            // MARROM
            "Placa Marrom",
            // HDS
            "HD Completo",
            "HD sem Placa/Sucateado",
            "Placa de HD",
            // CELULARES
            "Placa de Celular Completa",
            "Placa Tablet",
            "Celular Smart sem Bateria Botão e Flip",
            "Celular Smart Sem Bateria",
            "Celular Smart Com Bateria",
            "Celular Replicas com e sem Baterias",
            // PROCESSADORES
            "Processadores Cerâmico A",
            "Processadores Cerâmico B",
            "Processadores Cerâmico C",
            "Processadores Plástico Preto",
            "Processadores Plástico Chapa A",
            "Processadores Plástico Chapa B",
            "Processadores Plástico",
            "Processadores Slot",
            // MEMÓRIAS
            "Memórias Douradas",
            "Memória Prata",
            // ITENS INTEIROS
            "Desmanche Eletrônicos Consultar Itens",
            // BATERIAS
            "Baterias de Notebook",
            "Baterias de Celular",
            "Baterias de Tablet",
            // ITENS DIVERSOS
            "Raio X",
            "Fonte completa",
            "Fonte sem Fio",
        };

        public static readonly Section[] Sections =
        {
            new("NOTEBOOKS", new[]
            {
                "Placa Notebook A",
                "Placa Notebook B",
                "Placa Notebook C",
            }),
            new("MÃES", new[]
            {
                "Placa Mãe A",
                "Placa Mãe B",
                "Placa Mãe C",
                "Placa Mãe D",
                "Placa de Servidor",
            }),
            new("LEVES", new[]
            {
                "Placa Leve",
                "Placa Leve com Ponta",
                "Placa Leve Especial",
                "Placa Leve Especial com Ponta",
                "Placa Leve Especial Completa",
                "Placa Drive",
            }),
            new("DOURADAS E TAPETES", new[]
            {
                "Placa Dourada A",
                "Placa Dourada B",
                "Placa Tapete A",
                "Placa Tapete B",
                "Placa Conectora",
            }),
            new("INTERMEDIÁRIAS", new[]
            {
                "Placa Intermediária A",
                "Placa Intermediária B",
                "Placa Intermediária C",
            }),
            new("PESADA", new[]
            {
                "Placa Pesada",
                "Placa Pesada com Ponta",
                "Placa Lisa",
            }),
            new("MARROM", new[]
            {
                "Placa Marrom",
            }),
            new("HDS", new[]
            {
                "HD Completo",
                "HD sem Placa/Sucateado",
                "Placa de HD",
            }),
            new("CELULARES", new[]
            {
                "Placa de Celular Completa",
                "Placa Tablet",
                "Celular Smart sem Bateria Botão e Flip",
                "Celular Smart Sem Bateria",
                "Celular Smart Com Bateria",
                "Celular Replicas com e sem Baterias",
            }),
            new("PROCESSADORES", new[]
            {
                "Processadores Cerâmico A",
                "Processadores Cerâmico B",
                "Processadores Cerâmico C",
                "Processadores Plástico Preto",
                "Processadores Plástico Chapa A",
                "Processadores Plástico Chapa B",
                "Processadores Plástico",
                "Processadores Slot",
            }),
            new("MEMÓRIAS", new[]
            {
                "Memórias Douradas",
                "Memória Prata",
            }),
            new("ITENS INTEIROS", new[]
            {
                "Desmanche Eletrônicos Consultar Itens",
            }),
            new("BATERIAS", new[]
            {
                "Baterias de Notebook",
                "Baterias de Celular",
                "Baterias de Tablet",
            }),
            new("ITENS DIVERSOS", new[]
            {
                "Raio X",
                "Fonte completa",
                "Fonte sem Fio",
            }),
        };
    }
}
