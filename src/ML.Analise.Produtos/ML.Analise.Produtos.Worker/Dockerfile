FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ML.Analise.Produtos.Worker.csproj", "ML.Analise.Produtos.Worker/"]
RUN dotnet restore "ML.Analise.Produtos.Worker/ML.Analise.Produtos.Worker.csproj"
WORKDIR "/src/ML.Analise.Produtos.Worker"
COPY . .
RUN dotnet build "ML.Analise.Produtos.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ML.Analise.Produtos.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ML.Analise.Produtos.Worker.dll"]