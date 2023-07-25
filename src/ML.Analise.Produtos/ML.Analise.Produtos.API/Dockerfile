FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ML.Analise.Produtos.API.csproj", "ML.Analise.Produtos.API/"]
RUN dotnet restore "ML.Analise.Produtos.API/ML.Analise.Produtos.API.csproj"
WORKDIR "/src/ML.Analise.Produtos.API"
COPY . .
RUN dotnet build "ML.Analise.Produtos.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ML.Analise.Produtos.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ML.Analise.Produtos.API.dll"]