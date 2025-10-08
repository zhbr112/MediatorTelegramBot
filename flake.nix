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

        mediator-telegram-bot-pkg = pkgs.buildDotnetModule {
          pname = "mediator-telegram-bot";
          version = "0.1.0";

          src = ./.;

          projectFile = "MediatorTelegramBot.csproj"; # <-- Не забудьте проверить этот путь

          nugetDepsHash = "sha256-AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

          dotnet-sdk = pkgs.dotnet-sdk_9;
          dotnet-runtime = pkgs.dotnet-runtime_9;
        };

      in
      {
        packages.default = mediator-telegram-bot-pkg;

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
                  
                  WorkingDirectory = "${cfg.package}/lib/mediator-telegram-bot";
                  
                  ExecStartPre = "${pkgs.coreutils}/bin/cp ${cfg.secretsFile} ./secrets.json";
                  
                  ExecStart = "${pkgs.dotnet-runtime_9}/bin/dotnet MediatorTelegramBot.dll";
                  
                  Restart = "on-failure";
                };
              };
            };
          };
      }
    );
}