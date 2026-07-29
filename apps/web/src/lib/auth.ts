import { User, UserManager, WebStorageStateStore } from "oidc-client-ts";

export type AuthSession = {
  accessToken: string;
  username?: string;
  email?: string;
};

export type AuthService = {
  getSession: () => Promise<AuthSession | null>;
  login: () => Promise<void>;
  logout: () => Promise<void>;
  completeLogin: () => Promise<AuthSession | null>;
};

const keycloakAuthority = import.meta.env.VITE_KEYCLOAK_AUTHORITY ?? "http://localhost:8080/realms/lotoanalytics";
const keycloakClientId = import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? "lotoanalytics-web";
const appOrigin = window.location.origin;

const userManager = new UserManager({
  authority: keycloakAuthority,
  client_id: keycloakClientId,
  redirect_uri: `${appOrigin}/auth/callback`,
  post_logout_redirect_uri: appOrigin,
  response_type: "code",
  scope: "openid profile email",
  userStore: new WebStorageStateStore({ store: window.localStorage })
});

export const keycloakAuthService: AuthService = {
  // Recupera a sessao OIDC salva no navegador.
  async getSession() {
    const user = await userManager.getUser();
    return mapUser(user);
  },

  // Redireciona o usuario para o login do Keycloak.
  async login() {
    await userManager.signinRedirect();
  },

  // Encerra a sessao OIDC no Keycloak.
  async logout() {
    await userManager.signoutRedirect();
  },

  // Finaliza o callback OIDC apos o Keycloak redirecionar para o frontend.
  async completeLogin() {
    const user = await userManager.signinRedirectCallback();
    return mapUser(user);
  }
};

// Converte o usuario OIDC para o estado minimo usado pela aplicacao.
function mapUser(user: User | null): AuthSession | null {
  if (!user || user.expired) {
    return null;
  }

  return {
    accessToken: user.access_token,
    username: typeof user.profile.preferred_username === "string" ? user.profile.preferred_username : user.profile.name,
    email: typeof user.profile.email === "string" ? user.profile.email : undefined
  };
}
