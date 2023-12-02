# Diret�rio para monitorar
$monitoringPath = "C:\Users\kobus\AppData\Roaming\slobs-client\Media\sounds_to_play"

# Carregue a classe System.Media.SoundPlayer
Add-Type -TypeDefinition @"
    using System;
    using System.IO;
    using System.Media;
"@

# Crie um objeto SoundPlayer
$soundPlayer = New-Object System.Media.SoundPlayer

# Loop infinito para monitorar continuamente
while ($true) {
    # Verifique se h� arquivos .wav na pasta
    $wavFiles = Get-ChildItem -Path $monitoringPath -Filter *.wav

    if ($wavFiles.Count -gt 0) {
        # Exclua todos os arquivos .wav encontrados
        

        # Defina o caminho do arquivo de �udio a ser reproduzido
        $soundPlayer.SoundLocation = "$monitoringPath\seu_som.wav"

        # Reproduza o �udio
        $soundPlayer.Play()
    }

    foreach ($file in $wavFiles) {
        Remove-Item -Path $file.FullName -Force
    }

    # Aguarde um tempo antes de verificar novamente
    Start-Sleep -Seconds 2
}
