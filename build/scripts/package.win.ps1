Remove-Item -Path build\CommitDock\*.pdb -Force
Compress-Archive -Path build\CommitDock -DestinationPath "build\commitdock_${env:VERSION}.${env:RUNTIME}.zip" -Force
