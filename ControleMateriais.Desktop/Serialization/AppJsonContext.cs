using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ControleMateriais.Desktop.ViewModels;


namespace ControleMateriais.Desktop.Serialization
{


    [JsonSourceGenerationOptions(
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(PriceTableViewModel.ValoresMensais))]
    [JsonSerializable(typeof(System.Collections.Generic.List<PriceTableViewModel.Linha>))]

    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
