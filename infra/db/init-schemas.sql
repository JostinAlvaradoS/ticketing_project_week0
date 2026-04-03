-- Initial schemas for Speckit ticketing MVP
-- Creates one schema per bounded context as defined in the plan

CREATE SCHEMA IF NOT EXISTS bc_identity;
CREATE SCHEMA IF NOT EXISTS bc_catalog;
CREATE SCHEMA IF NOT EXISTS bc_inventory;
CREATE SCHEMA IF NOT EXISTS bc_ordering;
CREATE SCHEMA IF NOT EXISTS bc_payment;
CREATE SCHEMA IF NOT EXISTS bc_fulfillment;
CREATE SCHEMA IF NOT EXISTS bc_notification;
CREATE SCHEMA IF NOT EXISTS bc_waitlist;

-- Optional: set search_path example for services (they should set schema explicitly in app config)
-- ALTER ROLE postgres SET search_path = bc_identity,public;

-- End of init-schemas.sql
