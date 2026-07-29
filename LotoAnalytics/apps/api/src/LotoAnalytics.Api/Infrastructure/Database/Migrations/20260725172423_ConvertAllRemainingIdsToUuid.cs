using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConvertAllRemainingIdsToUuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                ALTER TABLE IF EXISTS concurso_dezenas DROP CONSTRAINT IF EXISTS "FK_concurso_dezenas_concursos_concurso_id";
                ALTER TABLE IF EXISTS concurso_rateios DROP CONSTRAINT IF EXISTS "FK_concurso_rateios_concursos_concurso_id";
                ALTER TABLE IF EXISTS concurso_ganhadores_municipios DROP CONSTRAINT IF EXISTS "FK_concurso_ganhadores_municipios_concursos_concurso_id";
                ALTER TABLE IF EXISTS concursos DROP CONSTRAINT IF EXISTS "FK_concursos_modalidades_modalidade_id";

                DROP INDEX IF EXISTS "IX_concursos_modalidade_id_numero";
                DROP INDEX IF EXISTS "IX_concurso_dezenas_concurso_id_tipo_posicao";
                DROP INDEX IF EXISTS "IX_concurso_rateios_concurso_id_faixa";
                DROP INDEX IF EXISTS "IX_concurso_ganhadores_municipios_concurso_id";

                ALTER TABLE concurso_dezenas DROP CONSTRAINT IF EXISTS "PK_concurso_dezenas";
                ALTER TABLE concurso_rateios DROP CONSTRAINT IF EXISTS "PK_concurso_rateios";
                ALTER TABLE concurso_ganhadores_municipios DROP CONSTRAINT IF EXISTS "PK_concurso_ganhadores_municipios";
                ALTER TABLE concursos DROP CONSTRAINT IF EXISTS "PK_concursos";
                ALTER TABLE modalidades DROP CONSTRAINT IF EXISTS "PK_modalidades";
                ALTER TABLE planos DROP CONSTRAINT IF EXISTS "PK_planos";

                ALTER TABLE modalidades ADD COLUMN id_uuid uuid;
                UPDATE modalidades
                SET id_uuid = CASE codigo
                    WHEN 'lotofacil' THEN '00000000-0000-0000-0000-000000000001'::uuid
                    WHEN 'mega_sena' THEN '00000000-0000-0000-0000-000000000002'::uuid
                    WHEN 'quina' THEN '00000000-0000-0000-0000-000000000003'::uuid
                    WHEN 'maismilionaria' THEN '00000000-0000-0000-0000-000000000004'::uuid
                    WHEN 'lotomania' THEN '00000000-0000-0000-0000-000000000005'::uuid
                    WHEN 'timemania' THEN '00000000-0000-0000-0000-000000000006'::uuid
                    WHEN 'dupla_sena' THEN '00000000-0000-0000-0000-000000000007'::uuid
                    WHEN 'dia_de_sorte' THEN '00000000-0000-0000-0000-000000000008'::uuid
                    WHEN 'super_sete' THEN '00000000-0000-0000-0000-000000000009'::uuid
                    ELSE gen_random_uuid()
                END;

                ALTER TABLE planos ADD COLUMN id_uuid uuid;
                UPDATE planos
                SET id_uuid = CASE codigo
                    WHEN 'gratis' THEN '10000000-0000-0000-0000-000000000001'::uuid
                    WHEN 'premium' THEN '10000000-0000-0000-0000-000000000002'::uuid
                    ELSE gen_random_uuid()
                END;

                ALTER TABLE concursos ADD COLUMN id_uuid uuid;
                ALTER TABLE concursos ADD COLUMN modalidade_id_uuid uuid;
                UPDATE concursos SET id_uuid = gen_random_uuid();
                UPDATE concursos
                SET modalidade_id_uuid = modalidades.id_uuid
                FROM modalidades
                WHERE concursos.modalidade_id = modalidades.id;

                ALTER TABLE concurso_dezenas ADD COLUMN id_uuid uuid;
                ALTER TABLE concurso_dezenas ADD COLUMN concurso_id_uuid uuid;
                UPDATE concurso_dezenas SET id_uuid = gen_random_uuid();
                UPDATE concurso_dezenas
                SET concurso_id_uuid = concursos.id_uuid
                FROM concursos
                WHERE concurso_dezenas.concurso_id = concursos.id;

                ALTER TABLE concurso_rateios ADD COLUMN id_uuid uuid;
                ALTER TABLE concurso_rateios ADD COLUMN concurso_id_uuid uuid;
                UPDATE concurso_rateios SET id_uuid = gen_random_uuid();
                UPDATE concurso_rateios
                SET concurso_id_uuid = concursos.id_uuid
                FROM concursos
                WHERE concurso_rateios.concurso_id = concursos.id;

                ALTER TABLE concurso_ganhadores_municipios ADD COLUMN id_uuid uuid;
                ALTER TABLE concurso_ganhadores_municipios ADD COLUMN concurso_id_uuid uuid;
                UPDATE concurso_ganhadores_municipios SET id_uuid = gen_random_uuid();
                UPDATE concurso_ganhadores_municipios
                SET concurso_id_uuid = concursos.id_uuid
                FROM concursos
                WHERE concurso_ganhadores_municipios.concurso_id = concursos.id;

                ALTER TABLE modalidades ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE planos ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE concursos ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE concursos ALTER COLUMN modalidade_id_uuid SET NOT NULL;
                ALTER TABLE concurso_dezenas ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE concurso_dezenas ALTER COLUMN concurso_id_uuid SET NOT NULL;
                ALTER TABLE concurso_rateios ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE concurso_rateios ALTER COLUMN concurso_id_uuid SET NOT NULL;
                ALTER TABLE concurso_ganhadores_municipios ALTER COLUMN id_uuid SET NOT NULL;
                ALTER TABLE concurso_ganhadores_municipios ALTER COLUMN concurso_id_uuid SET NOT NULL;

                ALTER TABLE modalidades DROP COLUMN id;
                ALTER TABLE modalidades RENAME COLUMN id_uuid TO id;

                ALTER TABLE planos DROP COLUMN id;
                ALTER TABLE planos RENAME COLUMN id_uuid TO id;

                ALTER TABLE concursos DROP COLUMN id;
                ALTER TABLE concursos DROP COLUMN modalidade_id;
                ALTER TABLE concursos RENAME COLUMN id_uuid TO id;
                ALTER TABLE concursos RENAME COLUMN modalidade_id_uuid TO modalidade_id;

                ALTER TABLE concurso_dezenas DROP COLUMN id;
                ALTER TABLE concurso_dezenas DROP COLUMN concurso_id;
                ALTER TABLE concurso_dezenas RENAME COLUMN id_uuid TO id;
                ALTER TABLE concurso_dezenas RENAME COLUMN concurso_id_uuid TO concurso_id;

                ALTER TABLE concurso_rateios DROP COLUMN id;
                ALTER TABLE concurso_rateios DROP COLUMN concurso_id;
                ALTER TABLE concurso_rateios RENAME COLUMN id_uuid TO id;
                ALTER TABLE concurso_rateios RENAME COLUMN concurso_id_uuid TO concurso_id;

                ALTER TABLE concurso_ganhadores_municipios DROP COLUMN id;
                ALTER TABLE concurso_ganhadores_municipios DROP COLUMN concurso_id;
                ALTER TABLE concurso_ganhadores_municipios RENAME COLUMN id_uuid TO id;
                ALTER TABLE concurso_ganhadores_municipios RENAME COLUMN concurso_id_uuid TO concurso_id;

                ALTER TABLE modalidades ADD CONSTRAINT "PK_modalidades" PRIMARY KEY (id);
                ALTER TABLE planos ADD CONSTRAINT "PK_planos" PRIMARY KEY (id);
                ALTER TABLE concursos ADD CONSTRAINT "PK_concursos" PRIMARY KEY (id);
                ALTER TABLE concurso_dezenas ADD CONSTRAINT "PK_concurso_dezenas" PRIMARY KEY (id);
                ALTER TABLE concurso_rateios ADD CONSTRAINT "PK_concurso_rateios" PRIMARY KEY (id);
                ALTER TABLE concurso_ganhadores_municipios ADD CONSTRAINT "PK_concurso_ganhadores_municipios" PRIMARY KEY (id);

                CREATE UNIQUE INDEX "IX_concursos_modalidade_id_numero" ON concursos (modalidade_id, numero);
                CREATE UNIQUE INDEX "IX_concurso_dezenas_concurso_id_tipo_posicao" ON concurso_dezenas (concurso_id, tipo, posicao);
                CREATE UNIQUE INDEX "IX_concurso_rateios_concurso_id_faixa" ON concurso_rateios (concurso_id, faixa);
                CREATE INDEX "IX_concurso_ganhadores_municipios_concurso_id" ON concurso_ganhadores_municipios (concurso_id);

                ALTER TABLE concursos ADD CONSTRAINT "FK_concursos_modalidades_modalidade_id" FOREIGN KEY (modalidade_id) REFERENCES modalidades (id) ON DELETE CASCADE;
                ALTER TABLE concurso_dezenas ADD CONSTRAINT "FK_concurso_dezenas_concursos_concurso_id" FOREIGN KEY (concurso_id) REFERENCES concursos (id) ON DELETE CASCADE;
                ALTER TABLE concurso_rateios ADD CONSTRAINT "FK_concurso_rateios_concursos_concurso_id" FOREIGN KEY (concurso_id) REFERENCES concursos (id) ON DELETE CASCADE;
                ALTER TABLE concurso_ganhadores_municipios ADD CONSTRAINT "FK_concurso_ganhadores_municipios_concursos_concurso_id" FOREIGN KEY (concurso_id) REFERENCES concursos (id) ON DELETE CASCADE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("A reversao de UUID para bigint nao e suportada sem perder referencias.");
        }
    }
}
