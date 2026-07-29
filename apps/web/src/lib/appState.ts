import { createContext, Dispatch, SetStateAction, useContext } from "react";
import { CurrentUserResponse } from "./apiClient";
import { AuthService, AuthSession } from "./auth";

export type AppState = {
  apiBaseUrl: string;
  auth: AuthSession | null;
  currentUser: CurrentUserResponse | null;
};

export const AppStateContext = createContext<{
  state: AppState;
  setState: Dispatch<SetStateAction<AppState>>;
  authService: AuthService;
} | null>(null);

// Expoe o estado global da aplicacao (API, sessao e usuario atual) para as paginas.
export function useAppState() {
  const context = useContext(AppStateContext);

  if (!context) {
    throw new Error("AppStateContext nao configurado.");
  }

  return context;
}
