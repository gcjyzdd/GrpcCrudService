-- Add task management columns to existing Jobs table
ALTER TABLE Jobs ADD COLUMN TaskStatus INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Jobs ADD COLUMN TaskStartedAt TEXT;
ALTER TABLE Jobs ADD COLUMN TaskEndedAt TEXT;
ALTER TABLE Jobs ADD COLUMN TaskErrorMessage TEXT;