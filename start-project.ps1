#
#
#

# Instalar Avalonia UI
dotnet new install Avalonia.Templates

# Configurar projeto MVVM
dotnet new sln -n ControleMateriais
dotnet new avalonia.mvvm -o ControleMateriais.Desktop
dotnet sln add ControleMateriais.Desktop/ControleMateriais.Desktop.csproj

