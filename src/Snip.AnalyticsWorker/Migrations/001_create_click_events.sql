CREATE TABLE IF NOT EXISTS click_events (
    slug String,
    destination_url String,
    timestamp DateTime,
    ip_address Nullable(String),
    user_agent Nullable(String),
    referer Nullable(String)
) ENGINE = MergeTree()
ORDER BY (slug, timestamp)