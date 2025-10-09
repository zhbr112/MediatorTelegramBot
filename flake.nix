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

            packages = {
              default = pkgs.buildDotnetModule {
                pname = projectName;
                version = "0.1.0";
                src = ./.;
                projectFile = "${projectName}.csproj";
                nugetDeps = ./deps.json;
                dotnet-sdk = pkgs.dotnet-sdk_9;
                dotnet-runtime = pkgs.dotnet-runtime_9;
                preConfigure = ''
                  cat ${cfg.secretsFile} > secrets.json
                '';
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
              # --- КОНЕЦ ИСПРАВЛЕННОГО БЛОКА ---
              
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

                  # 3. УПРОЩЕНО: Скрипт подготовки теперь проще, так как root все может.
                  ExecStartPre = pkgs.writeShellScript "prepare-bot-secrets" ''
                    set -e
                    cp -r ${cfg.package}/lib/MediatorTelegramBot/ /var/lib/mediator-bot/
                    cp /var/lib/mediator-bot/secrets.json /var/lib/mediator-bot/MediatorTelegramBot/secrets.json
                  '';
                  
                  ExecStart = "${pkgs.dotnet-runtime_9}/bin/dotnet /var/lib/mediator-bot/MediatorTelegramBot/MediatorTelegramBot.dll";

                 
                  
                  Restart = "on-failure";
                };
              };
            };
          };
      }
    );
}