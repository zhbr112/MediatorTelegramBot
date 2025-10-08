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

        projectName = "MediatorTelegramBot";

        mediator-telegram-bot-pkg = pkgs.buildDotnetModule {
          pname = "mediator-telegram-bot";
          version = "0.1.0";
          src = ./.;

          # --- МЕТОД ПРИНУЖДЕНИЯ ---
          # Указываем только проектный файл
          projectFile = "MediatorTelegramBot.csproj";

          # И добавляем ЗАВЕДОМО НЕПРАВИЛЬНЫЙ ХЕШ.
          # Это заставит Nix запустить онлайн-этап скачивания зависимостей,
          # который завершится ошибкой несоответствия хеша.
          # Эта ошибка - НАША ЦЕЛЬ.
          #nugetDepsHash = "sha256-AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

          nuGetUnsafeLockfileVersion = "2";
          nuGetLockfile = ./packages.lock.json; # Adjust path if necessary
          # --- КОНЕЦ ИСПРАВЛЕНИЯ ---

          dotnet-sdk = pkgs.dotnet-sdk_9;
          dotnet-runtime = pkgs.dotnet-runtime_9;
        };

      in
      {
        packages = {
          default = pkgs.buildDotnetModule {
            pname = projectName;
            version = "0.1.0"; # Можете указать свою версию
            src = ./.;

            # Укажите путь к вашему главному файлу проекта.
            # Если у вас .sln файл, используйте solutionFile = "./MySolution.sln";
            projectFile = "${projectName}.csproj";

            # Укажите TargetFramework из вашего .csproj файла
            # Например, <TargetFramework>net9.0</TargetFramework>
            # nuGetUnsafeLockfileVersion = "2";
            # nuGetLockfile = ./packages.lock.json;
            # nugetTargetId = "net9.0";
            nugetDeps = ./deps.json;
            dotnet-sdk = pkgs.dotnet-sdk_9;
            dotnet-runtime = pkgs.dotnet-runtime_9;
            preConfigure = ''
              echo "{}" > secrets.json
            '';

            # `buildDotnetModule` автоматически выполняет restore, build и publish.
            # Результатом будет готовое к запуску приложение.
          };
        };

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
                description = "Path to the secrets.json file.";
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
              # --- ИСПРАВЛЕННЫЙ БЛОК ---
              services.postgresql = {
                enable = true;
                enableTCPIP = true;
                
                # Этот скрипт будет запущен ОДИН РАЗ при первой инициализации базы данных.
                # Nix вставит сюда ПУТЬ к файлу с паролем, а команда `cat`
                # прочитает его СОДЕРЖИМОЕ уже при запуске на вашей машине.
                initialScript = pkgs.writeText "mediator-db-init" ''
                  CREATE ROLE "${cfg.database.user}" WITH LOGIN PASSWORD '$(cat ${cfg.database.passwordFile})';
                  CREATE DATABASE "${cfg.database.name}" WITH OWNER = "${cfg.database.user}";
                '';
              };
              # --- КОНЕЦ ИСПРАВЛЕННОГО БЛОКА ---
              
              users.users.mediator-bot = {
                isSystemUser = true;
                group = "mediator-bot";
              };
              users.groups.mediator-bot = {};

              systemd.services.mediator-telegram-bot = {
                description = "Mediator Telegram Bot Service";
                wantedBy = [ "multi-user.target" ];
                after = [ "postgresql.service" ];
                requires = [ "postgresql.service" ];

                serviceConfig = {
                  Type = "simple";
                  User = "mediator-bot";
                  Group = "mediator-bot";
                  
                  WorkingDirectory = "/var/lib/mediator-bot";

                  ExecStartPre = pkgs.writeShellScript "prepare-bot-env" ''
                    set -e # Прерывать выполнение при любой ошибке

                    # --- ФИНАЛЬНОЕ ИСПРАВЛЕНИЕ: Полная очистка перед подготовкой ---
                    # Определяем рабочую директорию
                    TARGET_DIR="/var/lib/mediator-bot"

                    # 1. Полностью удаляем старую директорию, чтобы избежать проблем с правами
                    rm -rf "$TARGET_DIR"

                    # 2. Создаем ее заново (теперь она будет принадлежать root)
                    mkdir -p "$TARGET_DIR"

                    # 3. Копируем приложение (root без проблем пишет в свою директорию)
                    cp -r ${cfg.package}/* "$TARGET_DIR/"

                    # 4. Копируем секреты
                    cp ${cfg.secretsFile} "$TARGET_DIR/secrets.json"

                    # 5. Передаем права на все готовое нашему пользователю
                    chown -R mediator-bot:mediator-bot "$TARGET_DIR"
                    # --- КОНЕЦ ИСПРАВЛЕНИЯ ---
                  '';
                  
                  ExecStart = "${pkgs.aspnetcore-runtime_9}/bin/dotnet MediatorTelegramBot.dll";
                  
                  Restart = "on-failure";
                };
              };
            };
          };
      }
    );
}