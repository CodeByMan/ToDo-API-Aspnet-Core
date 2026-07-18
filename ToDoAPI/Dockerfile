FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ToDoAPI-ASPNET.csproj ./
RUN dotnet restore ToDoAPI-ASPNET.csproj

COPY . ./
RUN dotnet publish ToDoAPI-ASPNET.csproj -c Release --no-restore -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "ToDoAPI-ASPNET.dll"]
