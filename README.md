#GriesserPresuSyncv2
Descripción del proyecto
Es un Worker netcore que se puede ejecutar como servicio. Recupera de la api de migriesser.com los presupuestos y los inserta dentro de una tabla intermedia de SAGE.

Como crear un ejecutable del servicio
Para poder generar un fichero EXE del proyecto ejecutar:

dotnet publish --self-contained --configuration Release --runtime win-x64 /p:PublishSingleFile=true
