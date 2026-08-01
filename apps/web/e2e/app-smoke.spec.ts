import { expect, test } from "@playwright/test";

test("user can navigate implemented screens", async ({ page }) => {
  await page.goto("/dashboard/lotofacil");

  // O painel estatistico da Lotofacil abre dentro do chrome padrao, com sua barra lateral.
  await expect(page.getByRole("heading", { name: "Painel estatístico Lotofácil" })).toBeVisible();

  // Atalho do painel para a geracao de jogos.
  await page.getByRole("link", { name: "Gerar jogos" }).click();
  await expect(page.getByRole("heading", { name: "Filtros matemáticos" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Gerar jogos" })).toBeVisible();

  // Botao de retorno do gerador para o painel.
  await page.getByRole("link", { name: "Voltar para o painel" }).click();
  await expect(page.getByRole("heading", { name: "Painel estatístico Lotofácil" })).toBeVisible();
});
