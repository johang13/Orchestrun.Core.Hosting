CREATE TABLE IF NOT EXISTS test_items
(
    id         SERIAL PRIMARY KEY,
    message    TEXT        NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
