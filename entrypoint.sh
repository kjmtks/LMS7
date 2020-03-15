#!/bin/sh

sleep 1
cd /app/ALMS.App 

dotnet restore
PATH="$PATH:/root/.dotnet/tools" dotnet ef database update

echo $APP_ENVIRONMENT

cd /app/ALMS.App/out
dotnet ALMS.App.dll

