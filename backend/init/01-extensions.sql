-- PostgreSQL extensions for MTG Mullagain
-- This script enables required extensions for the application

-- Enable pgvector extension for vector similarity search
CREATE EXTENSION IF NOT EXISTS vector;

-- Enable intarray extension for efficient integer array operations
CREATE EXTENSION IF NOT EXISTS intarray;

-- Verify extensions are installed
SELECT extname, extversion FROM pg_extension WHERE extname IN ('vector', 'intarray');


