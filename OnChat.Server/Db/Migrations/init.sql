create table users
(
    id       uuid
        primary key unique not null default uuidv7(),
    mail     varchar(255)
        unique not null,
    username varchar(50) not null unique,
    password_hash varchar(512) not null,
    age      smallint not null,
    public_key bytea null
);

create table messages(
    id uuid primary key unique default uuidv7(),
    ephemeral_public_key bytea not null,
    nonce bytea not null,
    ciphertext bytea not null,
    tag bytea not null,
    sender_id uuid not null references users(id),
    receiver_id uuid not null references users(id),
    timestamp timestamptz not null default now()
);

CREATE FUNCTION uuidv7(timestamptz DEFAULT clock_timestamp()) RETURNS uuid
AS $$
    -- Replace the first 48 bits of a uuidv4 with the current
    -- number of milliseconds since 1970-01-01 UTC
    -- and set the "ver" field to 7 by setting additional bits
select encode(
               set_bit(
                       set_bit(
                               overlay(uuid_send(gen_random_uuid()) placing
                                       substring(int8send((extract(epoch from $1)*1000)::bigint) from 3)
                                       from 1 for 6),
                               52, 1),
                       53, 1), 'hex')::uuid;
$$ LANGUAGE sql volatile parallel safe;

COMMENT ON FUNCTION uuidv7(timestamptz) IS
    'Generate a uuid-v7 value with a 48-bit timestamp (millisecond precision) and 74 bits of randomness';

CREATE OR REPLACE FUNCTION uuid_generate_v7()
    RETURNS uuid
AS $$
DECLARE
    unix_ts_ms bigint;
    uuid_hex text;
BEGIN
    -- Get current timestamp in milliseconds since Unix epoch
    unix_ts_ms = (EXTRACT(EPOCH FROM clock_timestamp()) * 1000)::bigint;

    -- Build the hex representation:
    -- Timestamp part (48 bits = 12 hex chars) – big-endian
    uuid_hex = lpad(to_hex(unix_ts_ms), 12, '0');

    -- Add the version nibble (4 bits = 1 hex char) at position 7
    -- Version 7 = hex '7'
    uuid_hex = uuid_hex || '7';

    -- Add a random 12-bit value (3 hex chars) – can be replaced with a monotonic counter if needed
    uuid_hex = uuid_hex || lpad(to_hex(floor(random() * 4096)::int), 3, '0');

    -- Variant bits: first two bits of next byte = '10' (RFC 4122)
    -- Next byte will have '8', '9', 'A', or 'B' as high nibble
    -- We set low nibble to random (1 hex char) and then combine
    uuid_hex = uuid_hex || lpad(to_hex(128 + floor(random() * 64)::int), 2, '0');

    -- Remaining 58 bits (14.5 hex chars – actually 60 bits but padded) – we use 15 hex chars of randomness
    FOR i IN 1..15 LOOP
            uuid_hex = uuid_hex || lpad(to_hex(floor(random() * 16)::int), 1, '0');
        END LOOP;

    -- Insert hyphens per UUID format (8-4-4-4-12)
    RETURN (substring(uuid_hex, 1, 8) || '-' ||
            substring(uuid_hex, 9, 4) || '-' ||
            substring(uuid_hex, 13, 4) || '-' ||
            substring(uuid_hex, 17, 4) || '-' ||
            substring(uuid_hex, 21, 12))::uuid;
END;
$$ LANGUAGE plpgsql VOLATILE;

