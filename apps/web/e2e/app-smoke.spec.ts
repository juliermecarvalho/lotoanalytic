import { expect, test } from "@playwright/test";

test("user can navigate implemented screens", async ({ page }) => {
  await page.goto("/");

  // A raiz entrega o painel estatistico em tela cheia, com sua propria barra lateral.
  await expect(page.getByRole("heading", { name: "Painel estatístico Lotofácil" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Painel" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Geração de jogos" })).toBeVisible();

  await page.getByRole("link", { name: "Geração de jogos" }).click();
  await expect(page.getByRole("heading", { name: "Filtros matemáticos" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Gerar jogos" })).toBeVisible();
});
