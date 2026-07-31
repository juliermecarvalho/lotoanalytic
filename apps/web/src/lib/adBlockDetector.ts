import { useCallback, useEffect, useRef, useState } from "react";

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

// Tempo para o Funding Choices (recuperacao oficial do AdSense) renderizar a mensagem
// antes de o overlay proprio assumir o bloqueio.
const OFFICIAL_RECOVERY_GRACE_MS = 2500;

// Indica se a mensagem OFICIAL de recuperacao de bloqueio (Funding Choices) esta VISIVEL
// na tela. Quando esta, o overlay proprio se cala para nao criar um muro duplo.
// Ignora o iframe-sinal invisivel do bootstrap (name="googlefcPresent").
export function isOfficialAdBlockMessageVisible(): boolean {
  if (typeof document === "undefined") {
    return false;
  }

  const candidates = document.querySelectorAll(
    'iframe[src*="fundingchoices"], iframe[name*="googlefc"], .fc-dialog-container, .fc-ab-root, [class^="fc-"], [class*=" fc-"]'
  );

  for (const node of Array.from(candidates)) {
    const element = node as HTMLElement;
    if (element.getAttribute("name") === "googlefcPresent") {
      continue; // sinal invisivel do bootstrap; nao conta como mensagem
    }
    const rect = element.getBoundingClientRect();
    if (rect.width > 20 && rect.height > 20) {
      return true;
    }
  }

  return false;
}

// Monitora continuamente o bloqueador e reavalia periodicamente, liberando a tela
// automaticamente assim que o usuario desativa a extensao.
//
// Modo hibrido: se um bloqueador for detectado MAS a mensagem oficial (Funding Choices)
// estiver na tela, o overlay proprio nao aparece (a oficial tem prioridade). Caso a
// oficial nao apareca dentro da janela de espera (ex.: site ainda nao aprovado, ou um
// bloqueador que tambem barra o Funding Choices), o overlay proprio assume o bloqueio.
export function useAdBlockGate(pollIntervalMs = 4000): AdBlockGateState {
  const [blocked, setBlocked] = useState(false);
  const [checking, setChecking] = useState(true);
  const graceTimerRef = useRef<number | null>(null);

  const clearGraceTimer = () => {
    if (graceTimerRef.current !== null) {
      window.clearTimeout(graceTimerRef.current);
      graceTimerRef.current = null;
    }
  };

  const recheck = useCallback(() => {
    setChecking(true);
    clearGraceTimer();

    void detectAdBlock().then((adBlocked) => {
      // Sem bloqueador: libera.
      if (!adBlocked) {
        setBlocked(false);
        setChecking(false);
        return;
      }

      // Ha bloqueador, mas a mensagem oficial ja esta na tela: defere para ela.
      if (isOfficialAdBlockMessageVisible()) {
        setBlocked(false);
        setChecking(false);
        return;
      }

      // Da uma janela para o Funding Choices renderizar a mensagem oficial. So assume
      // o bloqueio se, apos a espera, nenhuma mensagem oficial estiver visivel.
      graceTimerRef.current = window.setTimeout(() => {
        setBlocked(!isOfficialAdBlockMessageVisible());
        setChecking(false);
        graceTimerRef.current = null;
      }, OFFICIAL_RECOVERY_GRACE_MS);
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
      clearGraceTimer();
    };
  }, [recheck, pollIntervalMs]);

  return { blocked, checking, recheck };
}
