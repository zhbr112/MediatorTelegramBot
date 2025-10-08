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
        nixosModules.default = { config, lib, pkgs, ... }:
        let
            cfg = config.services.${projectName};
        in
        {
            options.services.${projectName} = {
            enable = lib.mkEnableOption "Enable the Mediator Telegram Bot service";
            package = lib.mkOption {
                type = lib.types.package;
                default = self.packages.${system}.default;
                description = "The package to use for the service.";
            };
            passwordFile = lib.mkOption {
                type = lib.types.path;
                description = "Path to the file containing the password for the PostgreSQL user.";
            };
            environment = lib.mkOption {
                type = lib.types.attrsOf lib.types.str;
                default = {};
                description = "Additional environment variables for the service.";
            };
            };

            config = lib.mkIf cfg.enable {
            # Создаем пользователя для сервиса. Это единственное, что модуль делает для системы, кроме самого сервиса.
            users.users.mediatorbot = { isSystemUser = true; group = "mediatorbot"; };
            users.groups.mediatorbot = {};

            # Определяем ТОЛЬКО сервис для бота. НИКАКОГО PostgreSQL.
            systemd.services.${projectName} = {
                description = "Mediator Telegram Bot Service";
                wantedBy = [ "multi-user.target" ];
                # Мы просто говорим, что нужно запускаться ПОСЛЕ PostgreSQL. Мы его не настраиваем.
                after = [ "network.target" "postgresql.service" ];
                requires = [ "postgresql.service" ];
                serviceConfig = {
                User = "mediatorbot";
                Group = "mediatorbot";
                ExecStart = "${cfg.package}/bin/${projectName}";
                WorkingDirectory = "/var/lib/${projectName}";
                Restart = "on-failure";
                Environment =
                    let
                    password = builtins.readFile cfg.passwordFile;
                    connectionString = "Host=/run/postgresql;Database=mediator;Username=test;Password=${password}";
                    in
                    [
                    "ASPNETCORE_ENVIRONMENT=Production"
                    "DOTNET_ENVIRONMENT=Production"
                    "ConnectionStrings__DefaultConnection=${connectionString}"
                    ]
                    ++ (lib.mapAttrsToList (name: value: "${name}=${value}") cfg.environment);
                };
            };
            };
        };
      }
    );
}