using System;
using System.Collections.Generic;

namespace ControleMateriais.Desktop
{
    internal static class ItemCatalog
    {
        public record Section(string Titulo, IReadOnlyList<string> Itens);

        public static readonly string[] OrderedItems =
        {
            "Placa Drive",
            "Placa Notebook A",
            "Placa Notebook B",
            "Placa Notebook C",
            "Placa Mãe A",
            "Placa Mãe B",
            "Placa Mãe C",
            "Placa Mãe D",
            "Placa de Servidor",
            "Placa Leve Especial",
            "Placa Leve Especial com Ponta",
            "Placa Leve Especial Completa",
            "Placa Dourada A",
            "Placa Dourada B",
            "Placa Tapete A",
            "Placa Tapete B",
            "Placa Conectora",
            "Placa Leve",
            "Placa Leve com Ponta",
            "Placa Intermediária A",
            "Placa Intermediária B",
            "Placa Intermediária C",
            "Placa Pesada",
            "Placa Pesada com Ponta",
            "Placa Tablet",
            "Placa Marrom",
            "HD Completo",
            "HD sem placa/Sucateado",
            "Placa de HD",
            "Placa de Celular Completa",
            "Celular Sem tampa e bateria (Botão e Flip)",
            "Celular Smart sem tampa e Sem bateria",
            "Celular Smart Com Bateria Blindada",
            "Celular Replicas blindado e sem Baterias",
            "Memórias Douradas",
            "Memória Prata",
            "Processadores Plástico Chapa A",
            "Processadores Plástico Chapa B",
            "Processadores Plástico",
            "Processadores Slot",
            "Processadores Plástico Preto",
            "Processadores Cerâmico A",
            "Processadores Cerâmico B",
            "Processadores Cerâmico C",
            "Baterias de Notebook",
            "Baterias de Tablet",
            "Baterias de Celular",
            "Fonte completa",
            "Raio X",
            "Desmanche Eletrônicos Consultar",
        };

        /// <summary>
        /// Mapeia nomenclaturas antigas para os nomes atuais do catálogo.
        /// Usado para garantir retrocompatibilidade com recibos e tabelas de preços criados
        /// antes da renomeação dos itens.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> LegacyNameMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Celular Smart sem Bateria Botão e Flip"]  = "Celular Sem tampa e bateria (Botão e Flip)",
                ["Celular Smart Sem Bateria"]               = "Celular Smart sem tampa e Sem bateria",
                ["Celular Smart Com Bateria"]               = "Celular Smart Com Bateria Blindada",
                ["Celular Replicas com e sem Baterias"]     = "Celular Replicas blindado e sem Baterias",
            };
    }
}
