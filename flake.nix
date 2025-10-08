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
          };

          # Реализация сервиса, если он включен (`enable = true;`).
          config = mkIf config.services.${projectName}.enable {
            # 1. Настраиваем PostgreSQL
            services.postgresql = {
              enable = true;
              ensureUsers = [{
                name = "test";
                # Пароль будет взят из файла, определенного в системной конфигурации.
                passwordFile = "/run/keys/postgres-test-password";
              }];
              initialDatabases = [{
                name = "mediator";
                owner = "test";
              }];
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

                # Команда для запуска приложения из нашего пакета
                ExecStart = "${config.services.${projectName}.package}/bin/${projectName}";
                
                # Рабочая директория
                WorkingDirectory = "/var/lib/${projectName}";
                
                Restart = "on-failure";
                
                # Передаем переменные окружения, определенные пользователем
                Environment = [
                  "ASPNETCORE_ENVIRONMENT=Production"
                  "DOTNET_ENVIRONMENT=Production"
                ] ++ (mapAttrsToList (name: value: "${name}=${value}") config.services.${projectName}.environment);

                # Безопасно передаем пароль от БД в сервис
                LoadCredential = [
                  "postgres-test-password:${config.security.secrets.postgres-test-password.path}"
                ];
              };
            };
          };
        };
      }
    );
}