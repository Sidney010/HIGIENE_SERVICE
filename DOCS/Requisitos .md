# Requisitos do Sistema de Agendamento para Barbearia, Salão e Manicure

Legenda de prioridade: **MVP** = primeira entrega ao estabelecimento. **F2** = segunda fase. **F3** = evolução futura.

Prefixos: **RF** = requisito funcional. **RNF** = requisito não funcional. **RN** = regra de negócio.

## 1. Módulo Cadastro

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-CAD-01 | Cadastrar, editar e inativar clientes (nome, telefone, e-mail, nascimento, observações). | MVP |
| RF-CAD-02 | Registrar o consentimento LGPD do cliente no cadastro. | MVP |
| RF-CAD-03 | Cadastrar profissionais com especialidades, comissão padrão e status. | MVP |
| RF-CAD-04 | Cadastrar serviços com categoria, duração e preço. | MVP |
| RF-CAD-05 | Definir quais serviços cada profissional executa, com preço, duração e comissão próprios opcionais. | MVP |
| RF-CAD-06 | Cadastrar unidades com endereço, telefone e horário de funcionamento. | MVP |
| RF-CAD-07 | Cadastrar recursos físicos (cadeira, maca, sala) e vinculá-los a serviços. | F2 |
| RF-CAD-08 | Manter ficha do cliente com alergias e observações de pele ou unha, com acesso restrito. | F2 |
| RF-CAD-09 | Importar clientes de planilha CSV. | F2 |
| RN-CAD-01 | Nenhum cadastro com histórico é excluído fisicamente; usar inativação (soft delete). | MVP |
| RN-CAD-02 | O telefone do cliente deve ser único dentro do estabelecimento. | MVP |

## 2. Módulo Agenda e Agendamento

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-AGE-01 | Configurar a disponibilidade semanal de cada profissional por unidade. | MVP |
| RF-AGE-02 | Registrar bloqueios (folga, férias, almoço, atestado). | MVP |
| RF-AGE-03 | Consultar horários livres por serviço, profissional e data. | MVP |
| RF-AGE-04 | Criar agendamento com um ou mais serviços (ex.: corte e barba). | MVP |
| RF-AGE-05 | Visualizar a agenda por dia, semana e profissional (visão da recepção). | MVP |
| RF-AGE-06 | Cancelar e remarcar agendamentos. | MVP |
| RF-AGE-07 | Registrar check-in, início, conclusão e no-show. | MVP |
| RF-AGE-08 | Permitir "encaixe" manual pela recepção, com permissão especial. | F2 |
| RF-AGE-09 | Manter lista de espera e avisar o cliente quando um horário vagar. | F2 |
| RF-AGE-10 | Agendamentos recorrentes (ex.: toda quinta às 18h). | F3 |
| RN-AGE-01 | Um profissional não pode ter dois agendamentos sobrepostos. | MVP |
| RN-AGE-02 | O agendamento deve estar dentro da disponibilidade do profissional e do horário da unidade. | MVP |
| RN-AGE-03 | A duração total é a soma dos serviços mais o intervalo opcional de higienização. | MVP |
| RN-AGE-04 | O profissional deve estar habilitado para todos os serviços do agendamento. | MVP |
| RN-AGE-05 | Respeitar antecedência mínima e máxima configuráveis. | MVP |
| RN-AGE-06 | O cliente não pode ter dois agendamentos simultâneos. | MVP |
| RN-AGE-07 | Transições de status inválidas são bloqueadas. Fluxo: Pendente, Confirmado, Em atendimento, Concluído, com desvios para Cancelado, No-show e Remarcado. | MVP |
| RN-AGE-08 | O preço do serviço é congelado no momento do agendamento. | MVP |
| RN-AGE-09 | Dois clientes não podem reservar o mesmo horário ao mesmo tempo (controle de concorrência). | MVP |

## 3. Módulo Atendimento ao Cliente (Self-service)

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-CLI-01 | O cliente agenda por site ou página responsiva, sem precisar instalar app. | MVP |
| RF-CLI-02 | O cliente consulta, cancela e remarca seus agendamentos. | MVP |
| RF-CLI-03 | Identificação simples por telefone com código de verificação (OTP). | MVP |
| RF-CLI-04 | Agendamento via bot de WhatsApp. | F3 |
| RF-CLI-05 | O cliente vê seu histórico de atendimentos e favorita um profissional. | F2 |
| RF-CLI-06 | O cliente avalia o atendimento (nota e comentário). | F2 |

## 4. Módulo Financeiro

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-FIN-01 | Abrir comanda a partir do agendamento ou do atendimento sem hora marcada. | MVP |
| RF-FIN-02 | Registrar pagamentos (Pix, cartão, dinheiro), inclusive parciais e divididos. | MVP |
| RF-FIN-03 | Aplicar desconto com permissão e motivo obrigatório. | MVP |
| RF-FIN-04 | Calcular comissão por profissional (percentual ou fixo, por serviço). | MVP |
| RF-FIN-05 | Relatório de comissões por período e profissional. | MVP |
| RF-FIN-06 | Abertura e fechamento de caixa, com sangria e suprimento. | F2 |
| RF-FIN-07 | Cobrar sinal ou pagamento antecipado (Pix) para reduzir faltas. | F2 |
| RF-FIN-08 | Pacotes e assinaturas (clube de cortes mensal). | F3 |
| RF-FIN-09 | Cupons, promoções e programa de fidelidade. | F3 |
| RN-FIN-01 | Comissão só é gerada para atendimentos concluídos e pagos. | MVP |
| RN-FIN-02 | Estorno exige permissão e registro de motivo. | MVP |
| RN-FIN-03 | Caixa fechado não aceita lançamentos retroativos. | F2 |
| RN-FIN-04 | Cancelamento fora do prazo pode gerar multa ou retenção de sinal (configurável). | F2 |
| RN-FIN-05 | Após reincidência de no-show (limite configurável), exigir pagamento antecipado. | F2 |

## 5. Módulo Estoque e Produtos

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-EST-01 | Cadastrar produtos com custo, preço de venda e estoque mínimo. | F2 |
| RF-EST-02 | Vender produtos na comanda com baixa automática no estoque. | F2 |
| RF-EST-03 | Registrar entrada, perda e ajuste de estoque. | F2 |
| RF-EST-04 | Alertar quando o estoque ficar abaixo do mínimo. | F2 |
| RF-EST-05 | Vincular consumo de insumos a serviços (ficha técnica). | F3 |

## 6. Módulo Notificações

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-NOT-01 | Enviar confirmação ao criar agendamento. | MVP |
| RF-NOT-02 | Enviar lembrete 24h e 2h antes, com opção de confirmar ou cancelar. | MVP |
| RF-NOT-03 | Avisar o cliente em espera quando surgir vaga. | F2 |
| RF-NOT-04 | Mensagem de aniversário e de reativação de clientes inativos. | F3 |
| RF-NOT-05 | Canais configuráveis: WhatsApp, SMS e e-mail. | MVP |
| RN-NOT-01 | O cliente só recebe mensagens de marketing se tiver dado consentimento. | MVP |

## 7. Módulo Usuários e Segurança

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-SEG-01 | Login com e-mail e senha para a equipe. | MVP |
| RF-SEG-02 | Perfis de acesso: administrador, recepção, profissional e cliente. | MVP |
| RF-SEG-03 | O profissional vê apenas a própria agenda e as próprias comissões. | MVP |
| RF-SEG-04 | Registrar auditoria de alterações (quem, o quê, quando). | F2 |
| RF-SEG-05 | Autenticação em dois fatores para administradores. | F3 |
| RN-SEG-01 | Senhas armazenadas com hash forte (bcrypt ou argon2). | MVP |
| RN-SEG-02 | Dados sensíveis do cliente só podem ser acessados por perfis autorizados. | MVP |

## 8. Módulo Relatórios e Dashboard

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-REL-01 | Faturamento por dia, semana e mês. | MVP |
| RF-REL-02 | Atendimentos por profissional e por serviço. | MVP |
| RF-REL-03 | Taxa de ocupação da agenda e taxa de no-show. | F2 |
| RF-REL-04 | Ticket médio e clientes novos versus recorrentes. | F2 |
| RF-REL-05 | Exportação para Excel ou CSV e PDF. | F2 |

## 9. Módulo Biossegurança e Conformidade

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-BIO-01 | Registrar a higienização de instrumentos e equipamentos (data, responsável, método). | F2 |
| RF-BIO-02 | Checklist de abertura e fechamento da unidade. | F3 |
| RF-LGPD-01 | Exportar e anonimizar os dados de um cliente mediante solicitação. | F2 |
| RF-LGPD-02 | Política de privacidade e termo de consentimento exibidos no primeiro agendamento. | MVP |

## 10. Módulo Administração do Sistema (SaaS / Multi-tenant)

| ID | Requisito | Prioridade |
| --- | --- | --- |
| RF-SAAS-01 | Isolar os dados de cada estabelecimento (tenant). | MVP |
| RF-SAAS-02 | Configurar regras por estabelecimento: prazos, multas, comissões e intervalos. | MVP |
| RF-SAAS-03 | Gerenciar planos e cobrança dos estabelecimentos clientes. | F3 |
| RF-SAAS-04 | Personalizar a página de agendamento (logo, cores, link próprio). | F2 |

## 11. Requisitos Não Funcionais

| ID | Requisito |
| --- | --- |
| RNF-01 | **Desempenho:** consulta de horários livres em até 2 segundos. |
| RNF-02 | **Disponibilidade:** meta de 99,5% ou mais, com backup diário automático. |
| RNF-03 | **Usabilidade:** a recepção deve conseguir criar um agendamento em até 4 cliques. Interface responsiva para celular. |
| RNF-04 | **Segurança:** tráfego HTTPS, proteção contra SQL Injection, XSS e CSRF, e limite de tentativas de login. |
| RNF-05 | **Escalabilidade:** arquitetura multi-tenant, API stateless e filas para notificações. |
| RNF-06 | **Fuso horário:** datas armazenadas em UTC e exibidas no fuso da unidade. |
| RNF-07 | **Auditabilidade:** registros financeiros nunca são apagados, apenas estornados. |
| RNF-08 | **Manutenibilidade:** API documentada (OpenAPI) e testes automatizados nas regras de agendamento. |
| RNF-09 | **Conformidade:** atendimento à LGPD (consentimento, minimização e direito de exclusão). |
| RNF-10 | **Portabilidade:** funcionar nos navegadores atuais (Chrome, Safari, Edge, Firefox). |

## 12. Sugestão de Entregas

**Fase 1 (MVP):** Cadastro, Agenda e Agendamento, Atendimento ao Cliente (web), Financeiro básico (comanda, pagamento, comissão), Notificações (confirmação e lembrete), Usuários e Segurança, Relatórios básicos e Multi-tenant básico.

**Fase 2:** Caixa, sinal antecipado, lista de espera, estoque, avaliações, auditoria, biossegurança, LGPD completa e relatórios avançados.

**Fase 3:** WhatsApp bot, pacotes e assinaturas, fidelidade, agendamento recorrente e cobrança SaaS.
