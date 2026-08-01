# CLAUDE.md

> Leia `AGENTS.md` antes de alterar codigo, testes, documentacao ou scripts deste repositorio.

## Repositorio canonico (onde editar)

- O repositorio oficial fica **na WSL**: `\\wsl.localhost\Ubuntu\home\julierme\repo\lotoanalytic`.
- **Sempre** faca as alteracoes de codigo, testes, docs e scripts nessa pasta.
- **Nunca** edite copias fora da WSL (ex.: caminhos em `D:\...` no Windows). Elas podem estar desatualizadas e as mudancas se perdem.

## Conta git (quem commita)

- A conta correta e **`juliermecarvalho@gmail.com`**, ja configurada no WSL.
- Qualquer outra conta (ex.: e-mail corporativo) esta **errada** para este repositorio.
- Antes de commitar, confirme com `git config user.email`; se estiver diferente, ajuste:

```bash
git config user.email "juliermecarvalho@gmail.com"
```
