import { expect, test } from "@playwright/test";

test("user can navigate implemented screens", async ({ page }) => {
  await page.goto("/");

  await expect(page.getByRole("button", { name: /Entrar com Keycloak/ })).toBeVisible();

  // O menu lateral tem apenas o gerador; as demais telas sairam do menu.
  await expect(page.getByRole("link", { name: /Gerador/ })).toBeVisible();
  await expect(page.getByRole("link", { name: /Dashboard/ })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Estatisticas/ })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Conferidor/ })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Historicos/ })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Perfil/ })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Modalidades/ })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Importar/ })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Admin/ })).toHaveCount(0);

  await page.getByRole("link", { name: /Gerador/ }).click();
  await expect(page.getByRole("heading", { name: "Geração de jogos" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Gerar jogos" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Filtros matemáticos" })).toBeVisible();
});
