{
  description = "A flake for the Mediator Telegram Bot project";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };

        # 1. Пакет для сборки вашего .NET приложения
        # Он воспроизводит шаги из Dockerfile
        mediator-telegram-bot-pkg = pkgs.buildDotnetModule {
          pname = "mediator-telegram-bot";
          version = "0.1.0"; # Укажите вашу версию

          src = ./.;

          # !!! ВАЖНО: Укажите правильный путь к вашему .csproj файлу
          projectFile = "MediatorTelegramBot/MediatorTelegramBot.csproj";
          # NuGet-зависимости будут автоматически извлечены и кешированы Nix'ом
          # NuGetLockFilePath = "packages.lock.json"; # Раскомментируйте, если используете lock-файл

          # Версии .NET SDK и рантайма, соответствующие вашему Dockerfile
          dotnet-sdk = pkgs.dotnet-sdk_9;
          dotnet-runtime = pkgs.dotnet-runtime_9;

          # Это аналог `dotnet publish`
          # buildDotnetModule выполняет это по умолчанию
        };

      in
      {
        # Пакет можно собрать вручную командой `nix build .`
        packages.default = mediator-telegram-bot-pkg;

        # 2. Модуль NixOS для развертывания всего стека
        nixosModules.default = { config, lib, pkgs, ... }:
          with lib;
          let
            cfg = config.services.mediatorTelegramBot;
          in
          {
            options.services.mediatorTelegramBot = {
              enable = mkEnableOption "Enable the Mediator Telegram Bot service";

              package = mkOption {
                type = types.package;
                default = self.packages.${system}.default;
                description = "The package to use for the bot.";
              };

              secretsFile = mkOption {
                type = types.path;
                description = ''
                  Path to the secrets.json file.
                  This file will be copied to the working directory before the service starts.
                '';
                example = "/path/to/your/secrets.json";
              };

              database = {
                user = mkOption {
                  type = types.str;
                  default = "test";
                  description = "PostgreSQL user for the application.";
                };
                passwordFile = mkOption {
                  type = types.path;
                  description = "Path to a file containing the PostgreSQL user password.";
                };
                name = mkOption {
                  type = types.str;
                  default = "mediator";
                  description = "PostgreSQL database name for the application.";
                };
              };
            };

            config = mkIf cfg.enable {
              # 3. Настройка PostgreSQL, аналог 'db' сервиса в docker-compose
              services.postgresql = {
                enable = true;
                enableTCPIP = true; # Разрешить подключения по TCP/IP (localhost)
                initialScript = pkgs.writeText "mediator-db-init" ''
                  CREATE ROLE "${cfg.database.user}" WITH LOGIN PASSWORD '${builtins.readFile cfg.database.passwordFile}';
                  CREATE DATABASE "${cfg.database.name}" WITH OWNER = "${cfg.database.user}";
                '';
              };
              
              # Пользователь от имени которого будет работать сервис для безопасности
              users.users.mediator-bot = {
                isSystemUser = true;
                group = "mediator-bot";
              };
              users.groups.mediator-bot = {};

              # 4. Настройка systemd-сервиса, аналог 'server' в docker-compose
              systemd.services.mediator-telegram-bot = {
                description = "Mediator Telegram Bot Service";
                wantedBy = [ "multi-user.target" ];
                # Запускать после и требовать наличия PostgreSQL
                after = [ "postgresql.service" ];
                requires = [ "postgresql.service" ];

                serviceConfig = {
                  Type = "simple";
                  User = "mediator-bot";
                  Group = "mediator-bot";
                  
                  # Публикация .NET происходит в подпапку lib/<pname>
                  WorkingDirectory = "${cfg.package}/lib/mediator-telegram-bot";
                  
                  # Копируем файл с секретами в рабочую директорию перед стартом
                  ExecStartPre = "${pkgs.coreutils}/bin/cp ${cfg.secretsFile} ./secrets.json";
                  
                  # Команда запуска, аналог ENTRYPOINT
                  ExecStart = "${pkgs.dotnet-runtime_9}/bin/dotnet MediatorTelegramBot.dll";
                  
                  # Аналог restart: unless-stopped
                  Restart = "on-failure";
                };
              };
            };
          };
      }
    );
}