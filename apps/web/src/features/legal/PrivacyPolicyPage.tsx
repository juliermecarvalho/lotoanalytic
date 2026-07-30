// Pagina de Politica de Privacidade. Exigida pelo Google AdSense e pela LGPD:
// divulga o uso de cookies, o Google como fornecedor terceiro de anuncios e os
// direitos do titular dos dados. Rota publica (sem login) para ser rastreavel.
const CONTACT_EMAIL = "julierme@trustimage.com.br";
const LAST_UPDATED = "30 de julho de 2026";

export function PrivacyPolicyPage() {
  return (
    <section className="page legal-page">
      <header className="page-header">
        <h1>Política de Privacidade</h1>
        <p>Última atualização: {LAST_UPDATED}</p>
      </header>

      <div className="panel legal-content">
        <p>
          Esta Política de Privacidade descreve como o LotoAnalytics ("nós") coleta, usa e
          protege as informações dos usuários ("você") ao utilizar o site
          {" "}
          <strong>lotoanalytic.com.br</strong> e seus subdomínios. Ao acessar o site, você
          concorda com as práticas descritas neste documento.
        </p>

        <h2>1. Informações que coletamos</h2>
        <p>
          O LotoAnalytics é uma ferramenta de análise estatística de loterias. Podemos coletar:
        </p>
        <ul>
          <li>
            <strong>Dados de conta:</strong> quando você cria uma conta ou faz login, tratamos
            dados de autenticação (como nome de usuário e e-mail) por meio de um provedor de
            identidade (Keycloak).
          </li>
          <li>
            <strong>Dados de uso:</strong> os jogos, dezenas e conferências que você informa para
            gerar estatísticas ou conferir resultados.
          </li>
          <li>
            <strong>Dados técnicos:</strong> informações coletadas automaticamente pelo navegador,
            como endereço IP, tipo de dispositivo e páginas visitadas.
          </li>
        </ul>

        <h2>2. Cookies</h2>
        <p>
          Utilizamos cookies e tecnologias semelhantes para manter sua sessão, lembrar
          preferências e exibir anúncios. Você pode desativar os cookies nas configurações do seu
          navegador, mas isso pode afetar o funcionamento de algumas funcionalidades.
        </p>

        <h2>3. Publicidade — Google AdSense</h2>
        <p>
          Este site utiliza o <strong>Google AdSense</strong>, um serviço de publicidade fornecido
          pela Google. O Google, como fornecedor terceiro, utiliza cookies para exibir anúncios com
          base em visitas anteriores a este e a outros sites.
        </p>
        <ul>
          <li>
            O uso de cookies de publicidade permite que o Google e seus parceiros exibam anúncios
            com base na sua navegação.
          </li>
          <li>
            Você pode desativar a publicidade personalizada acessando as{" "}
            <a href="https://www.google.com/settings/ads" target="_blank" rel="noopener noreferrer">
              Configurações de anúncios do Google
            </a>
            .
          </li>
          <li>
            Para saber como o Google usa os dados quando você utiliza sites de parceiros, consulte a{" "}
            <a
              href="https://policies.google.com/technologies/partner-sites"
              target="_blank"
              rel="noopener noreferrer"
            >
              política de privacidade e termos do Google
            </a>
            .
          </li>
        </ul>

        <h2>4. Como usamos as informações</h2>
        <p>Utilizamos as informações coletadas para:</p>
        <ul>
          <li>fornecer e manter as funcionalidades de análise e conferência de jogos;</li>
          <li>autenticar e gerenciar contas de usuário;</li>
          <li>exibir anúncios e sustentar a operação gratuita do serviço;</li>
          <li>melhorar a experiência e a segurança do site.</li>
        </ul>

        <h2>5. Compartilhamento de dados</h2>
        <p>
          Não vendemos seus dados pessoais. Compartilhamos informações apenas com provedores
          necessários à operação do serviço (como o provedor de anúncios Google e o provedor de
          identidade) ou quando exigido por lei.
        </p>

        <h2>6. Seus direitos (LGPD)</h2>
        <p>
          De acordo com a Lei Geral de Proteção de Dados (Lei nº 13.709/2018), você tem direito a
          confirmar a existência de tratamento, acessar, corrigir, anonimizar ou solicitar a
          exclusão dos seus dados, bem como revogar o consentimento. Para exercer esses direitos,
          entre em contato conosco.
        </p>

        <h2>7. Contato</h2>
        <p>
          Em caso de dúvidas sobre esta Política de Privacidade, entre em contato pelo e-mail{" "}
          <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
        </p>

        <h2>8. Alterações nesta política</h2>
        <p>
          Esta Política de Privacidade pode ser atualizada periodicamente. Recomendamos revisá-la
          com frequência. A data da última atualização é indicada no topo desta página.
        </p>
      </div>
    </section>
  );
}
