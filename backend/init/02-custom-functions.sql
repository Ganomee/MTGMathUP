-- Custom PostgreSQL functions for MTG Mullagain
-- These functions support hand analysis and queries

-- Function to count intersection of two integer arrays
CREATE OR REPLACE FUNCTION count_intersection(arr1 integer[], arr2 integer[])
RETURNS integer AS $$
BEGIN
    RETURN (
        SELECT COUNT(*)
        FROM unnest(arr1) AS elem
        WHERE elem = ANY(arr2)
    );
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Function to calculate Jaccard similarity between two integer arrays
CREATE OR REPLACE FUNCTION jaccard_similarity(arr1 integer[], arr2 integer[])
RETURNS float AS $$
DECLARE
    intersection_count integer;
    union_count integer;
BEGIN
    intersection_count := count_intersection(arr1, arr2);
    union_count := array_length(arr1, 1) + array_length(arr2, 1) - intersection_count;
    
    IF union_count = 0 THEN
        RETURN 0.0;
    END IF;
    
    RETURN intersection_count::float / union_count::float;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Function to get array overlap percentage
CREATE OR REPLACE FUNCTION overlap_percentage(arr1 integer[], arr2 integer[])
RETURNS float AS $$
DECLARE
    intersection_count integer;
    min_length integer;
BEGIN
    intersection_count := count_intersection(arr1, arr2);
    min_length := LEAST(array_length(arr1, 1), array_length(arr2, 1));
    
    IF min_length = 0 THEN
        RETURN 0.0;
    END IF;
    
    RETURN intersection_count::float / min_length::float;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Verify functions are created
SELECT proname, proargtypes FROM pg_proc WHERE proname IN ('count_intersection', 'jaccard_similarity', 'overlap_percentage');


