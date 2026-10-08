-- Garantia FINAL contra overbooking (RN-AGE-01 / RN-AGE-09 / CT-CON-01).
-- A validação em código dá a mensagem amigável; esta constraint impede o erro mesmo com
-- duas requisições simultâneas. Aplique APÓS a migration inicial (EnsureCreated também serve nos testes).
--
-- Intervalo semiaberto [Inicio, Fim): um atendimento que termina às 10:00 não conflita com o que começa às 10:00.
-- Só participam status que ocupam agenda. CANCELADO, NO_SHOW e REMARCADO liberam o horário.

CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE agendamento
    ADD CONSTRAINT ex_agendamento_profissional_sem_sobreposicao
    EXCLUDE USING gist (
        "ProfissionalId" WITH =,
        tstzrange("Inicio", "Fim", '[)') WITH &&
    )
    WHERE ("Status" IN ('PENDENTE', 'CONFIRMADO', 'EM_ATENDIMENTO', 'CONCLUIDO'));
