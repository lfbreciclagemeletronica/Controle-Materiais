#!/bin/bash
# publish-mac.sh — Gera build de release para macOS (arm64 e x64)
# Execute em um Mac com .NET SDK instalado:
#   chmod +x publish-mac.sh && ./publish-mac.sh

PROJ="ControleMateriais.Desktop/ControleMateriais.Desktop.csproj"
OUT="release"

TARGETS=("osx-arm64" "osx-x64")
LABELS=("macOS Apple Silicon (arm64)" "macOS Intel (x64)")

for i in "${!TARGETS[@]}"; do
    RID="${TARGETS[$i]}"
    LABEL="${LABELS[$i]}"
    DEST="$OUT/$RID"

    echo "==> Publicando $LABEL em $DEST ..."

    dotnet publish "$PROJ" \
        -c Release \
        -r "$RID" \
        --self-contained true \
        -o "$DEST" \
        /p:PublishSingleFile=true \
        /p:PublishAot=false \
        /p:PublishReadyToRun=true \
        /p:StripSymbols=true

    if [ $? -ne 0 ]; then
        echo "ERRO ao publicar $LABEL"
        exit 1
    fi

    ZIP="$OUT/ControleMateriais-$RID.zip"
    cd "$DEST" && zip -r "../../$ZIP" . && cd - > /dev/null
    echo "    -> $ZIP"
done

echo ""
echo "Builds concluidos! Arquivos em ./$OUT/"
echo "Faca o upload dos .zip em: https://github.com/lfbreciclagemeletronica/Controle-Materiais/releases/new"
