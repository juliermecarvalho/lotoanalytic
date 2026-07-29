using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConvertUserHistoryIdsToUuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                ALTER TABLE IF EXISTS jogos_gerados DROP CONSTRAINT IF EXISTS "FK_jogos_gerados_geracoes_jogos_geracao_jogo_id";
                ALTER TABLE IF EXISTS jogos_conferidos DROP CONSTRAINT IF EXISTS "FK_jogos_conferidos_conferencias_conferencia_id";
                ALTER TABLE IF EXISTS geracoes_jogos DROP CONSTRAINT IF EXISTS "FK_geracoes_jogos_usuarios_usuario_id";
                ALTER TABLE IF EXISTS conferencias DROP CONSTRAINT IF EXISTS "FK_conferencias_usuarios_usuario_id";

                DROP INDEX IF EXISTS "IX_geracoes_jogos_usuario_id_criado_em";
                DROP INDEX IF EXISTS "IX_jogos_gerados_geracao_jogo_id_numero_jogo";
                DROP INDEX IF EXISTS "IX_conferencias_usuario_id_criado_em";
                DROP INDEX IF EXISTS "IX_jogos_conferidos_conferencia_id_numero_jogo";

                ALTER TABLE usuarios DROP CONSTRAINT IF EXISTS "PK_usuarios";
                ALTER TABLE geracoes_jogos DROP CONSTRAINT IF EXISTS "PK_geracoes_jogos";
                ALTER TABLE jogos_gerados DROP CONSTRAINT IF EXISTS "PK_jogos_gerados";
                ALTER TABLE conferencias DROP CONSTRAINT IF EXISTS "PK_conferencias";
                ALTER TABLE jogos_conferidos DROP CONSTRAINT IF EXISTS "PK_jogos_conferidos";

                ALTER TABLE usuarios ADD COLUMN id_uuid uuid;
                ALTER TABLE geracoes_jogos ADD COLUMN id_uuid uuid;
                ALTER TABLE geracoes_jogos ADD COLUMN usuario_id_uuid uuid;
                ALTER TABLE jogos_gerados ADD COLUMN id_uuid uuid;
                ALTER TABLE jogos_gerados ADD COLUMN geracao_jogo_id_uuid uuid;
                ALTER TABLE conferencias ADD COLUMN id_uuid uuid;
                ALTER TABLE conferencias ADD COLUMN usuario_id_uuid uuid;
                ALTER TABLE jogos_conferidos ADD COLUMN id_uuid uuid;
                ALTER TABLE jogos_conferidos ADD COLUMN conferencia_id_uuid uuid;

                UPDATE usuarios SET id_uuid = gen_random_uuid();
                UPDATE geracoes_jogos SET id_uuid = gen_random_uuid();
                UPDATE jogos_gerados SET id_uuid = gen_random_uuid();
                UPDATE conferencias SET id_uuid = gen_random_uuid();
                UPDATE jogos_conferidos SET id_uuid = gen_random_uuid();

                UPDATE geracoes_jogos
                SET usuario_id_uuid = usuarios.id_uuid
                FROM usuarios
                WHERE geracoes_jogos.usuario_id = usuarios.id;

                UPDATE conferencias
                SET usuario_id_uuid = usuarios.id_uuid
                FROM usuarios
                WHERE conferencias.usuario_id = usuarios.id;

                UPDATE jogos_gerados
                SET geracao_jogo_id_uuid = geracoes_jogos.id_uuid
                FROM geracoes_jogos
                WHERE jogos_gerados.geracao_jogo_id = geracoes_jogos.id;

                UPDATE jogos_conferidos
                SET conferencia_id_uuid = conferencias.id_uuid
                FROM conferencias
                WHERE jogos_conferidos.conferencia_id = conferencias.id;

                ALTER TABLE usuarios ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE geracoes_jogos ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE geracoes_jogos ALTER COLUMN usuario_id_uuid SET NOT NULL;
                ALTER TABLE jogos_gerados ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE jogos_gerados ALTER COLUMN geracao_jogo_id_uuid SET NOT NULL;
                ALTER TABLE conferencias ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE conferencias ALTER COLUMN usuario_id_uuid SET NOT NULL;
                ALTER TABLE jogos_conferidos ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE jogos_conferidos ALTER COLUMN conferencia_id_uuid SET NOT NULL;

                ALTER TABLE usuarios DROP COLUMN id;
                ALTER TABLE usuarios RENAME COLUMN id_uuid TO id;

                ALTER TABLE geracoes_jogos DROP COLUMN id;
                ALTER TABLE geracoes_jogos DROP COLUMN usuario_id;
                ALTER TABLE geracoes_jogos RENAME COLUMN id_uuid TO id;
                ALTER TABLE geracoes_jogos RENAME COLUMN usuario_id_uuid TO usuario_id;

                ALTER TABLE jogos_gerados DROP COLUMN id;
                ALTER TABLE jogos_gerados DROP COLUMN geracao_jogo_id;
                ALTER TABLE jogos_gerados RENAME COLUMN id_uuid TO id;
                ALTER TABLE jogos_gerados RENAME COLUMN geracao_jogo_id_uuid TO geracao_jogo_id;

                ALTER TABLE conferencias DROP COLUMN id;
                ALTER TABLE conferencias DROP COLUMN usuario_id;
                ALTER TABLE conferencias RENAME COLUMN id_uuid TO id;
                ALTER TABLE conferencias RENAME COLUMN usuario_id_uuid TO usuario_id;

                ALTER TABLE jogos_conferidos DROP COLUMN id;
                ALTER TABLE jogos_conferidos DROP COLUMN conferencia_id;
                ALTER TABLE jogos_conferidos RENAME COLUMN id_uuid TO id;
                ALTER TABLE jogos_conferidos RENAME COLUMN conferencia_id_uuid TO conferencia_id;

                ALTER TABLE usuarios ADD CONSTRAINT "PK_usuarios" PRIMARY KEY (id);
                ALTER TABLE geracoes_jogos ADD CONSTRAINT "PK_geracoes_jogos" PRIMARY KEY (id);
                ALTER TABLE jogos_gerados ADD CONSTRAINT "PK_jogos_gerados" PRIMARY KEY (id);
                ALTER TABLE conferencias ADD CONSTRAINT "PK_conferencias" PRIMARY KEY (id);
                ALTER TABLE jogos_conferidos ADD CONSTRAINT "PK_jogos_conferidos" PRIMARY KEY (id);

                CREATE INDEX "IX_geracoes_jogos_usuario_id_criado_em" ON geracoes_jogos (usuario_id, criado_em);
                CREATE UNIQUE INDEX "IX_jogos_gerados_geracao_jogo_id_numero_jogo" ON jogos_gerados (geracao_jogo_id, numero_jogo);
                CREATE INDEX "IX_conferencias_usuario_id_criado_em" ON conferencias (usuario_id, criado_em);
                CREATE UNIQUE INDEX "IX_jogos_conferidos_conferencia_id_numero_jogo" ON jogos_conferidos (conferencia_id, numero_jogo);

                ALTER TABLE geracoes_jogos ADD CONSTRAINT "FK_geracoes_jogos_usuarios_usuario_id" FOREIGN KEY (usuario_id) REFERENCES usuarios (id) ON DELETE CASCADE;
                ALTER TABLE conferencias ADD CONSTRAINT "FK_conferencias_usuarios_usuario_id" FOREIGN KEY (usuario_id) REFERENCES usuarios (id) ON DELETE CASCADE;
                ALTER TABLE jogos_gerados ADD CONSTRAINT "FK_jogos_gerados_geracoes_jogos_geracao_jogo_id" FOREIGN KEY (geracao_jogo_id) REFERENCES geracoes_jogos (id) ON DELETE CASCADE;
                ALTER TABLE jogos_conferidos ADD CONSTRAINT "FK_jogos_conferidos_conferencias_conferencia_id" FOREIGN KEY (conferencia_id) REFERENCES conferencias (id) ON DELETE CASCADE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("A reversao de UUID para bigint nao e suportada sem perder referencias.");
        }
    }
}
