CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE raffles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    organisation VARCHAR(255),
    prizes TEXT,
    number_of_tickets INT NOT NULL CHECK (number_of_tickets > 0),
    ticket_price DECIMAL(10,2) NOT NULL,
    currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE tickets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    raffle_id UUID NOT NULL REFERENCES raffles(id) ON DELETE CASCADE,
    ticket_number INT NOT NULL,
    is_claimed BOOLEAN DEFAULT FALSE,
    claimed_name VARCHAR(255),
    claimed_surname VARCHAR(255),
    claimed_mobile VARCHAR(50),
    claimed_email VARCHAR(255),
    purchased_from VARCHAR(255),
    claimed_at TIMESTAMP WITH TIME ZONE,
    UNIQUE(raffle_id, ticket_number)
);

CREATE INDEX idx_tickets_raffle_id ON tickets(raffle_id);
CREATE INDEX idx_raffles_user_id ON raffles(user_id);
