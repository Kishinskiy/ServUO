# ==========================================
# ЭТАП 1: СБОРКА ЯДРА ЧЕРЕЗ .NET 10.0 SDK (BUILDER)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Копируем исходный код только самого ядра сервера
COPY . .

# Мягко восстанавливаем NuGet зависимости внутри изолированной Linux среды
RUN dotnet restore .

# Компилируем и публикуем готовый бинарник ядра (Server.dll) в папку /app/publish
RUN dotnet build -c release

# ==========================================
# ЭТАП 2: ЛЕГКОВЕСНЫЙ ЗАПУСК (.NET 10.0 RUNTIME)
# ==========================================
FROM  mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

# Забираем из первого этапа только скомпилированные файлы ядра
COPY --from=builder /app/publish .

# Копируем системные данные, рантаймы и базовые конфигурации
COPY Config/ ./Config/
COPY Data/ ./Data/
COPY runtimes/ ./runtimes/

# Открываем порт для подключения ClassicUO клиентов
EXPOSE 2593

# Запуск кроссплатформенного .NET ядра
ENTRYPOINT ["dotnet", "ServUO", "-release"]
