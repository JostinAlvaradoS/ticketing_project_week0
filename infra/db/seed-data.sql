-- Seed data for Ticketing Platform MVP
-- Events and Seats in both bc_catalog and bc_inventory
-- Based on actual DbContext definitions from each service

-- ============================================================================
-- bc_catalog: Events
-- ============================================================================
INSERT INTO bc_catalog."Events" ("Id", "Name", "Description", "EventDate", "BasePrice")
VALUES
  ('11111111-1111-1111-1111-111111111111', 'Concierto Jazz Noche', 'Una noche de jazz contemporáneo con artistas internacionales', NOW() + INTERVAL '7 days', 200000.00),
  ('22222222-2222-2222-2222-222222222222', 'Teatro: La Vida es Bella', 'Adaptación teatral de la novela clásica', NOW() + INTERVAL '14 days', 150000.00),
  ('33333333-3333-3333-3333-333333333333', 'Match de Fútbol: Local vs Internacional', 'Partido amistoso de clasificación', NOW() + INTERVAL '21 days', 180000.00),
  ('44444444-4444-4444-4444-444444444444', 'Conferencia Tecnología 2026', 'Charlas sobre tendencias en software', NOW() + INTERVAL '30 days', 120000.00),
  ('55555555-5555-5555-5555-555555555555', 'Concierto Rock Nacional', 'Festival con bandas locales e internacionales', NOW() + INTERVAL '45 days', 250000.00);

-- ============================================================================
-- bc_catalog: Seats for all events
-- ============================================================================

-- Event 1: Concierto Jazz (8 rows x 100 seats)
INSERT INTO bc_catalog."Seats" ("Id", "EventId", "SectionCode", "RowNumber", "SeatNumber", "Price", "Status", "CurrentReservationId")
SELECT 
  md5('seat-11111111-' || row_num::text || '-' || seat_num::text)::uuid,
  '11111111-1111-1111-1111-111111111111'::uuid,
  CASE WHEN row_num <= 2 THEN 'VIP' WHEN row_num <= 5 THEN 'FRONT' ELSE 'BACK' END,
  row_num,
  seat_num,
  CASE WHEN row_num <= 2 THEN 250000.00 WHEN row_num <= 5 THEN 180000.00 ELSE 120000.00 END,
  'available',
  null
FROM (
  SELECT generate_series(1, 8) as row_num, generate_series(1, 100) as seat_num
) t
WHERE row_num <= 8 AND seat_num <= 100;

-- Event 2: Teatro (12 rows x 100 seats)
INSERT INTO bc_catalog."Seats" ("Id", "EventId", "SectionCode", "RowNumber", "SeatNumber", "Price", "Status", "CurrentReservationId")
SELECT 
  md5('seat-22222222-' || row_num::text || '-' || seat_num::text)::uuid,
  '22222222-2222-2222-2222-222222222222'::uuid,
  CASE WHEN row_num <= 3 THEN 'VIPFRONT' WHEN row_num <= 6 THEN 'PREMIUM' ELSE 'GENERAL' END,
  row_num,
  seat_num,
  CASE WHEN row_num <= 3 THEN 200000.00 WHEN row_num <= 6 THEN 150000.00 ELSE 100000.00 END,
  'available',
  null
FROM (
  SELECT generate_series(1, 12) as row_num, generate_series(1, 100) as seat_num
) t
WHERE row_num <= 12 AND seat_num <= 100;

-- Event 3: Fútbol (25 rows x 100 seats)
INSERT INTO bc_catalog."Seats" ("Id", "EventId", "SectionCode", "RowNumber", "SeatNumber", "Price", "Status", "CurrentReservationId")
SELECT 
  md5('seat-33333333-' || row_num::text || '-' || seat_num::text)::uuid,
  '33333333-3333-3333-3333-333333333333'::uuid,
  CASE WHEN row_num <= 5 THEN 'PLATEA_VIP' WHEN row_num <= 15 THEN 'PLATEA' ELSE 'BALCON' END,
  row_num,
  seat_num,
  CASE WHEN row_num <= 5 THEN 250000.00 WHEN row_num <= 15 THEN 180000.00 ELSE 120000.00 END,
  'available',
  null
FROM (
  SELECT generate_series(1, 25) as row_num, generate_series(1, 100) as seat_num
) t
WHERE row_num <= 25 AND seat_num <= 100;

-- Event 4: Conferencia (5 rows x 100 seats)
INSERT INTO bc_catalog."Seats" ("Id", "EventId", "SectionCode", "RowNumber", "SeatNumber", "Price", "Status", "CurrentReservationId")
SELECT 
  md5('seat-44444444-' || row_num::text || '-' || seat_num::text)::uuid,
  '44444444-4444-4444-4444-444444444444'::uuid,
  'MAIN',
  row_num,
  seat_num,
  150000.00,
  'available',
  null
FROM (
  SELECT generate_series(1, 5) as row_num, generate_series(1, 100) as seat_num
) t
WHERE row_num <= 5 AND seat_num <= 100;

-- Event 5: Rock Festival (50 rows x 100 seats)
INSERT INTO bc_catalog."Seats" ("Id", "EventId", "SectionCode", "RowNumber", "SeatNumber", "Price", "Status", "CurrentReservationId")
SELECT 
  md5('seat-55555555-' || row_num::text || '-' || seat_num::text)::uuid,
  '55555555-5555-5555-5555-555555555555'::uuid,
  CASE WHEN row_num <= 10 THEN 'TRIBUNA_A' WHEN row_num <= 25 THEN 'TRIBUNA_B' ELSE 'GENERAL' END,
  row_num,
  seat_num,
  CASE WHEN row_num <= 10 THEN 300000.00 WHEN row_num <= 25 THEN 250000.00 ELSE 200000.00 END,
  'available',
  null
FROM (
  SELECT generate_series(1, 50) as row_num, generate_series(1, 100) as seat_num
) t
WHERE row_num <= 50 AND seat_num <= 100;

-- ============================================================================
-- bc_inventory: Copy seats from catalog with correct mapping
-- ============================================================================
INSERT INTO bc_inventory."Seats" ("Id", "Section", "Row", "Number", "Reserved")
SELECT 
  c."Id",
  c."SectionCode",
  c."RowNumber"::text,
  c."SeatNumber",
  CASE WHEN c."Status" = 'available' THEN false ELSE true END
FROM bc_catalog."Seats" c;
