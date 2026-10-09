cybersecurity_bottasksid
CREATE DATABASE IF NOT EXISTS cybersecurity_bot;
USE cybersecurity_bot;

CREATE TABLE IF NOT EXISTS tasks (
    id            INT           NOT NULL AUTO_INCREMENT,   -- unique number for each task, given by MySQL
    title         VARCHAR(100)  NOT NULL,                  -- e.g. 'Enable two-factor authentication'
    description   VARCHAR(500)  NOT NULL DEFAULT '',       -- more detail about the task
    reminder_date DATE          NULL,                      -- optional: NULL means no reminder
    is_completed  BOOLEAN       NOT NULL DEFAULT FALSE,    -- set to TRUE when marked as completed
    created_at    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
);

CREATE USER IF NOT EXISTS 'cyberbot_app'@'localhost' IDENTIFIED BY 'CyberAware#2026';
GRANT SELECT, INSERT, UPDATE, DELETE ON cybersecurity_bot.* TO 'cyberbot_app'@'localhost';

DESCRIBE tasks;