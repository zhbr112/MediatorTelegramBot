{
  description = "Mediator Telegram Bot application and NixOS deployment module";

  # Входы: зависимости нашего флейка
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  # Выходы: что предоставляет наш флейк
  outputs = { self, nixpkgs, flake-utils }:
    # Используем flake-utils для поддержки разных архитектур (x86_64, aarch64)
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };

        # Имя вашего проекта. Используйте имя из .csproj файла.
        projectName = "MediatorTelegramBot";

      in
      {
        # --- ЧТО МОЖНО СОБРАТЬ (аналог Dockerfile) ---
        # Мы определяем пакет, который содержит скомпилированное приложение.
        packages = {
          default = pkgs.buildDotnetModule {
            pname = projectName;
            version = "0.1.0"; # Можете использовать свою версию

            # Источник кода — это текущий репозиторий.
            src = ./.;

            # Информация для сборки, как и раньше
            projectFile = "${projectName}.csproj";
            nugetTargetId = "net9.0"; # Убедитесь, что версия .NET верна

            # Nix автоматически выполнит dotnet restore, build и publish.
          };
        };


        # --- КАК ЭТО РАЗВЕРНУТЬ (аналог docker-compose) ---
        # Мы определяем модуль для NixOS, который можно импортировать в конфигурацию системы.
        nixosModules.default = { config, lib, ... }: with lib; {
          # Определяем "API" нашего сервиса: какие опции пользователь сможет настраивать.
          options.services.${projectName} = {
            enable = mkEnableOption "Enable the Mediator Telegram Bot service";

            package = mkOption {
              type = types.package;
              # По умолчанию используется пакет, который мы определили выше в этом же флейке.
              default = self.packages.${system}.default;
              description = "The package to use for the service.";
            };

            environment = mkOption {
              type = types.attrsOf types.str;
              default = {};
              description = "Environment variables for the service, used for configuration.";
            };

            passwordFile = lib.mkOption {
              type = lib.types.path;
              description = "Path to the file containing the password for the 'test' PostgreSQL user.";
            };
          };

          # Реализация сервиса, если он включен (`enable = true;`).
          config = lib.mkIf config.services.${projectName}.enable {
            # 1. Настраиваем PostgreSQL
            services.postgresql = {
              enable = true;
              # Мы заменяем 'ensureUsers' и 'ensureDatabases' этой одной опцией.
              initialScriptFile =
                  let
                  # Читаем пароль из файла, как и раньше.
                  password = builtins.readFile config.services.${projectName}.passwordFile;
                  # ВАЖНО: Экранируем одинарные кавычки в пароле, чтобы избежать SQL-инъекций.
                  escapedPassword = lib.replaceStrings [ "'" ] [ "''" ] password;
                  in
                  # Создаем SQL-скрипт "на лету"
                  pkgs.writeText "mediator-db-init.sql" ''
                  -- Создаем пользователя (роль) с правом входа и паролем
                  CREATE ROLE test WITH LOGIN PASSWORD '${escapedPassword}';
                  -- Создаем базу данных и сразу назначаем ее владельцем нашего нового пользователя
                  CREATE DATABASE mediator WITH OWNER test;
                  '';
              };
            
            # 2. Создаем системного пользователя для запуска сервиса
            users.users.mediatorbot = {
              isSystemUser = true;
              group = "mediatorbot";
            };
            users.groups.mediatorbot = {};

            # 3. Настраиваем systemd-сервис
            systemd.services.${projectName} = {
              description = "Mediator Telegram Bot Service";
              wantedBy = [ "multi-user.target" ];
              
              # Запускаться после сети и базы данных
              after = [ "network.target" "postgresql.service" ];
              requires = [ "postgresql.service" ];

              serviceConfig = {
                User = "mediatorbot";
                Group = "mediatorbot";
                ExecStart = "${config.services.${projectName}.package}/bin/${projectName}";
                WorkingDirectory = "/var/lib/${projectName}";
                Restart = "on-failure";

                # Мы объединяем переменные окружения, определенные пользователем,
                # с нашей сгенерированной connection string.
                Environment =
                let
                    # Читаем пароль из файла ОДИН РАЗ
                    password = builtins.readFile config.services.${projectName}.passwordFile;
                    # Собираем connection string
                    connectionString = "Host=/run/postgresql;Database=mediator;Username=test;Password=${password}";
                in
                [
                    "ASPNETCORE_ENVIRONMENT=Production"
                    "DOTNET_ENVIRONMENT=Production"
                    # Внедряем нашу connection string
                    "ConnectionStrings__DefaultConnection=${connectionString}"
                ]
                # Добавляем любые другие переменные от пользователя
                ++ (lib.mapAttrsToList (name: value: "${name}=${value}") config.services.${projectName}.environment);
            };
            };
          };
        };
      }
    );
}