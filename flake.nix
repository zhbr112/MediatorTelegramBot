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
      in
      {
        packages = {
          default = pkgs.buildDotnetModule {
            pname = projectName;
            version = "0.1.0";
            src = ./.;
            projectFile = "${projectName}.csproj";
            nugetDeps = ./deps.json;
            dotnet-sdk = pkgs.dotnet-sdk_9;
            dotnet-runtime = pkgs.dotnet-runtime_9;
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
                authentication = pkgs.lib.mkOverride 10 ''
                    #type database DBuser auth-method
                    local all all trust
                    host all all all trust
                '';
                enable = true;
                enableTCPIP = true;
                
                # Этот скрипт будет запущен ОДИН РАЗ при первой инициализации базы данных.
                # Nix вставит сюда ПУТЬ к файлу с паролем, а команда `cat`
                # прочитает его СОДЕРЖИМОЕ уже при запуске на вашей машине.
                ensureUsers = [{
                  name = cfg.database.user;
                  ensureClauses.login = true;
                  ensureClauses.superuser = true;
                }];

                ensureDatabases = [cfg.database.name];                
              };
              systemd.services.mediator-telegram-bot = {
                description = "Mediator Telegram Bot Service";
                wantedBy = [ "multi-user.target" ];
                after = [ "postgresql.service" "systemd-tmpfiles-setup.service"];
                requires = [ "postgresql.service" ];

                serviceConfig = {
                  Type = "simple";

                  # 2. ИЗМЕНЕНО: Запускаем сервис от имени root.
                  User = "root";
                  Group = "root";
                  
                  WorkingDirectory = "/var/lib/mediator-bot";
                  
                  ExecStart = "${pkgs.dotnet-runtime_9}/bin/dotnet ${cfg.package}/lib/MediatorTelegramBot/MediatorTelegramBot.dll";
                  EnvironmentFile = cfg.secretsFile;                 
                  
                  Restart = "on-failure";
                };
              };             
            };
          };
      }
    );
}