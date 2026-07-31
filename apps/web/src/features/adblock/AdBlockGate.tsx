import { useEffect } from "react";
import { RefreshCw, ShieldAlert } from "lucide-react";
import { useAdBlockGate } from "../../lib/adBlockDetector";

// Overlay que bloqueia o uso do sistema enquanto um bloqueador de anuncios estiver ativo.
// A liberacao e automatica assim que o bloqueador e desativado (o hook reavalia sozinho).
export function AdBlockGate() {
  const { blocked, checking, recheck } = useAdBlockGate();

  // Trava a rolagem do fundo enquanto o aviso estiver visivel.
  useEffect(() => {
    if (!blocked) {
      return;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [blocked]);

  if (!blocked) {
    return null;
  }

  return (
    <div
      className="adblock-gate"
      role="alertdialog"
      aria-modal="true"
      aria-labelledby="adblock-title"
      aria-describedby="adblock-desc"
    >
      <div className="adblock-card">
        <ShieldAlert className="adblock-icon" size={48} aria-hidden="true" />
        <h2 id="adblock-title">Desative o bloqueador de anúncios</h2>
        <p id="adblock-desc">
          Detectamos um bloqueador de anúncios (AdBlock, uBlock, AdGuard, Brave, etc.) ativo neste site.
          O LotoAnalytics é gratuito e se mantém com anúncios — por isso, o uso do sistema fica
          bloqueado enquanto o bloqueador estiver ligado.
        </p>
        <ol className="adblock-steps">
          <li>Abra a extensão de bloqueio de anúncios do seu navegador.</li>
          <li>
            Desative-a para <strong>lotoanalytic.com.br</strong> (ou adicione o site à lista de
            permitidos/allowlist).
          </li>
          <li>Clique em “Já desativei” abaixo ou recarregue a página.</li>
        </ol>
        <div className="adblock-actions">
          <button type="button" onClick={recheck} disabled={checking}>
            <RefreshCw size={16} aria-hidden="true" /> {checking ? "Verificando…" : "Já desativei"}
          </button>
        </div>
        <p className="adblock-hint">A liberação é automática assim que o bloqueador for desativado.</p>
      </div>
    </div>
  );
}
