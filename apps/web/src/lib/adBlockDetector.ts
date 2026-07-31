import { useCallback, useEffect, useState } from "react";

// URL do script do AdSense usada como sonda de rede (bloqueada por extensoes de bloqueio).
const AD_SCRIPT_URL = "https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js";

// Classes tipicas de anuncio, cobertas pelas regras cosmeticas dos bloqueadores (EasyList etc.).
const BAIT_CLASSES = "adsbox ad-banner ads ad-placement pub_300x250 adsbygoogle sponsored-ad";

// Detecta bloqueadores de anuncio combinando dois sinais independentes:
// 1) um elemento-isca com classes tipicas de anuncio (escondido por regras cosmeticas);
// 2) a requisicao ao script do AdSense (recusada no nivel de rede).
// Basta um dos sinais para considerar que ha bloqueio ativo.
export async function detectAdBlock(): Promise<boolean> {
  const [baitBlocked, requestBlocked] = await Promise.all([detectByBait(), detectByRequest()]);
  return baitBlocked || requestBlocked;
}

// Insere um elemento-isca e verifica se o bloqueador o escondeu/removeu.
function detectByBait(): Promise<boolean> {
  return new Promise((resolve) => {
    if (typeof document === "undefined") {
      resolve(false);
      return;
    }

    const bait = document.createElement("div");
    bait.className = BAIT_CLASSES;
    bait.setAttribute("aria-hidden", "true");
    bait.style.cssText =
      "position:absolute;left:-9999px;top:-9999px;width:6px;height:6px;pointer-events:none;";
    bait.innerHTML = "&nbsp;";
    document.body.appendChild(bait);

    // Espera curta para o bloqueador aplicar as regras cosmeticas antes da leitura.
    window.setTimeout(() => {
      const style = window.getComputedStyle(bait);
      const blocked =
        !document.body.contains(bait) ||
        bait.offsetHeight === 0 ||
        bait.clientHeight === 0 ||
        style.display === "none" ||
        style.visibility === "hidden" ||
        style.opacity === "0";
      bait.remove();
      resolve(blocked);
    }, 150);
  });
}

// Tenta requisitar o script do AdSense; extensoes de bloqueio recusam a conexao.
async function detectByRequest(): Promise<boolean> {
  if (typeof fetch === "undefined" || (typeof navigator !== "undefined" && !navigator.onLine)) {
    return false;
  }

  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), 4000);
  try {
    await fetch(`${AD_SCRIPT_URL}?adblock-probe=${Date.now()}`, {
      method: "GET",
      mode: "no-cors",
      cache: "no-store",
      signal: controller.signal
    });
    return false;
  } catch {
    // Timeout (rede lenta) nao deve ser tratado como bloqueio, evitando falso positivo.
    return !controller.signal.aborted;
  } finally {
    window.clearTimeout(timeout);
  }
}

export interface AdBlockGateState {
  blocked: boolean;
  checking: boolean;
  recheck: () => void;
}

// Monitora continuamente o bloqueador e reavalia periodicamente, liberando a tela
// automaticamente assim que o usuario desativa a extensao.
export function useAdBlockGate(pollIntervalMs = 4000): AdBlockGateState {
  const [blocked, setBlocked] = useState(false);
  const [checking, setChecking] = useState(true);

  const recheck = useCallback(() => {
    setChecking(true);
    void detectAdBlock().then((result) => {
      setBlocked(result);
      setChecking(false);
    });
  }, []);

  useEffect(() => {
    recheck();
    const interval = window.setInterval(recheck, pollIntervalMs);

    // Reavalia quando a aba volta ao foco: o usuario pode ter desativado a extensao.
    const onVisibilityChange = () => {
      if (document.visibilityState === "visible") {
        recheck();
      }
    };
    document.addEventListener("visibilitychange", onVisibilityChange);

    return () => {
      window.clearInterval(interval);
      document.removeEventListener("visibilitychange", onVisibilityChange);
    };
  }, [recheck, pollIntervalMs]);

  return { blocked, checking, recheck };
}
