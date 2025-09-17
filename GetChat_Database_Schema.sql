-- ============================================
-- GetChat Webhook Database Schema
-- SQL Server DDL Script
-- ============================================
-- This script creates the database schema required for the GetChat webhook service
-- which processes chat transcripts from Olark chat service and stores them as activities

-- ============================================
-- Database Creation (if needed)
-- ============================================
-- Uncomment the following lines if you need to create the database
-- CREATE DATABASE [yourdb];

-- ============================================
-- Main Database Tables
-- ============================================

-- S_CONTACT table - Contact information
-- This table stores contact details including registration numbers for customer lookup
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='S_CONTACT' AND xtype='U')
BEGIN
    CREATE TABLE [yourdb].[dbo].[S_CONTACT] (
        [ROW_ID] NVARCHAR(15) NOT NULL PRIMARY KEY,
        [PR_DEPT_OU_ID] NVARCHAR(15) NULL,
        [X_REGISTRATION_NUM] NVARCHAR(50) NULL,
        [EMAIL_ADDR] NVARCHAR(100) NULL,
        [FST_NAME] NVARCHAR(50) NULL,
        [LST_NAME] NVARCHAR(50) NULL,
        [PHONE_NUM] NVARCHAR(20) NULL,
        [CREATED] DATETIME NULL,
        [CREATED_BY] NVARCHAR(15) NULL,
        [LAST_UPD] DATETIME NULL,
        [LAST_UPD_BY] NVARCHAR(15) NULL,
        [ROW_STATUS] NVARCHAR(1) NULL DEFAULT 'Y'
    );
    
    -- Create indexes for performance
    CREATE INDEX [IX_S_CONTACT_REG_NUM] ON [yourdb].[dbo].[S_CONTACT] ([X_REGISTRATION_NUM]);
    CREATE INDEX [IX_S_CONTACT_EMAIL] ON [yourdb].[dbo].[S_CONTACT] ([EMAIL_ADDR]);
    CREATE INDEX [IX_S_CONTACT_OU_ID] ON [yourdb].[dbo].[S_CONTACT] ([PR_DEPT_OU_ID]);
END;

-- S_EMPLOYEE table - Employee information
-- This table stores employee details for chat operator lookup
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='S_EMPLOYEE' AND xtype='U')
BEGIN
    CREATE TABLE [yourdb].[dbo].[S_EMPLOYEE] (
        [ROW_ID] NVARCHAR(15) NOT NULL PRIMARY KEY,
        [LOGIN] NVARCHAR(50) NULL,
        [EMAIL_ADDR] NVARCHAR(100) NULL,
        [FST_NAME] NVARCHAR(50) NULL,
        [LST_NAME] NVARCHAR(50) NULL,
        [DEPT_OU_ID] NVARCHAR(15) NULL,
        [ACTIVE_FLG] NVARCHAR(1) NULL DEFAULT 'Y',
        [CREATED] DATETIME NULL,
        [CREATED_BY] NVARCHAR(15) NULL,
        [LAST_UPD] DATETIME NULL,
        [LAST_UPD_BY] NVARCHAR(15) NULL,
        [ROW_STATUS] NVARCHAR(1) NULL DEFAULT 'Y'
    );
    
    -- Create indexes for performance
    CREATE INDEX [IX_S_EMPLOYEE_EMAIL] ON [yourdb].[dbo].[S_EMPLOYEE] ([EMAIL_ADDR]);
    CREATE INDEX [IX_S_EMPLOYEE_LOGIN] ON [yourdb].[dbo].[S_EMPLOYEE] ([LOGIN]);
    CREATE INDEX [IX_S_EMPLOYEE_ACTIVE] ON [yourdb].[dbo].[S_EMPLOYEE] ([ACTIVE_FLG]);
END;

-- S_EVT_ACT table - Activity/Event tracking
-- This table stores chat activity records with full conversation transcripts
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='S_EVT_ACT' AND xtype='U')
BEGIN
    CREATE TABLE [yourdb].[dbo].[S_EVT_ACT] (
        [ROW_ID] NVARCHAR(15) NOT NULL PRIMARY KEY,
        [ACTIVITY_UID] NVARCHAR(15) NULL,
        [ALARM_FLAG] NVARCHAR(1) NULL DEFAULT 'N',
        [APPT_REPT_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [APPT_START_DT] DATETIME NULL,
        [ASGN_MANL_FLG] NVARCHAR(1) NULL DEFAULT 'Y',
        [ASGN_USR_EXCLD_FLG] NVARCHAR(1) NULL DEFAULT 'Y',
        [BEST_ACTION_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [BILLABLE_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [CAL_DISP_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [COMMENTS_LONG] NVARCHAR(1500) NULL,
        [CONFLICT_ID] INT NULL DEFAULT 0,
        [COST_CURCY_CD] NVARCHAR(3) NULL DEFAULT 'USD',
        [COST_EXCH_DT] DATETIME NULL,
        [CREATED] DATETIME NULL,
        [CREATED_BY] NVARCHAR(15) NULL,
        [CREATOR_LOGIN] NVARCHAR(50) NULL,
        [DCKING_NUM] INT NULL DEFAULT 0,
        [DURATION_HRS] DECIMAL(5,2) NULL DEFAULT 0.00,
        [EMAIL_ATT_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [EMAIL_FORWARD_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [EMAIL_RECIP_ADDR] NVARCHAR(100) NULL,
        [EVT_PRIORITY_CD] NVARCHAR(10) NULL,
        [EVT_STAT_CD] NVARCHAR(10) NULL,
        [LAST_UPD] DATETIME NULL,
        [LAST_UPD_BY] NVARCHAR(15) NULL,
        [MODIFICATION_NUM] INT NULL DEFAULT 0,
        [NAME] NVARCHAR(100) NULL,
        [OWNER_LOGIN] NVARCHAR(50) NULL,
        [OWNER_PER_ID] NVARCHAR(15) NULL,
        [PCT_COMPLETE] INT NULL DEFAULT 100,
        [PRIV_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [ROW_STATUS] NVARCHAR(1) NULL DEFAULT 'Y',
        [TARGET_OU_ID] NVARCHAR(15) NULL,
        [TARGET_PER_ID] NVARCHAR(15) NULL,
        [TEMPLATE_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [TMSHT_RLTD_FLG] NVARCHAR(1) NULL DEFAULT 'N',
        [TODO_CD] NVARCHAR(50) NULL,
        [TODO_PLAN_START_DT] DATETIME NULL,
        [TODO_ACTL_END_DT] DATETIME NULL,
        [SRA_TYPE_CD] NVARCHAR(50) NULL,
        [COMMENTS] NVARCHAR(500) NULL,
        [RPLY_PH_NUM] NVARCHAR(20) NULL,
        [X_DESC_TEXT] NVARCHAR(MAX) NULL
    );
    
    -- Create indexes for performance
    CREATE INDEX [IX_S_EVT_ACT_EMAIL] ON [yourdb].[dbo].[S_EVT_ACT] ([EMAIL_RECIP_ADDR]);
    CREATE INDEX [IX_S_EVT_ACT_TARGET] ON [yourdb].[dbo].[S_EVT_ACT] ([TARGET_PER_ID]);
    CREATE INDEX [IX_S_EVT_ACT_CREATED] ON [yourdb].[dbo].[S_EVT_ACT] ([CREATED]);
    CREATE INDEX [IX_S_EVT_ACT_TODO_CD] ON [yourdb].[dbo].[S_EVT_ACT] ([TODO_CD]);
    CREATE INDEX [IX_S_EVT_ACT_STATUS] ON [yourdb].[dbo].[S_EVT_ACT] ([EVT_STAT_CD]);
    CREATE INDEX [IX_S_EVT_ACT_OWNER] ON [yourdb].[dbo].[S_EVT_ACT] ([OWNER_PER_ID]);
END;

-- ============================================
-- Chat-Specific Tables (Optional Enhancement)
-- ============================================

-- CHAT_SESSIONS table - Chat session tracking
-- This table provides additional tracking for chat sessions
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CHAT_SESSIONS' AND xtype='U')
BEGIN
    CREATE TABLE [yourdb].[dbo].[CHAT_SESSIONS] (
        [CHAT_ID] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [VISITOR_ID] NVARCHAR(50) NULL,
        [VISITOR_EMAIL] NVARCHAR(100) NULL,
        [VISITOR_NAME] NVARCHAR(100) NULL,
        [VISITOR_PHONE] NVARCHAR(20) NULL,
        [VISITOR_COUNTRY] NVARCHAR(50) NULL,
        [VISITOR_CITY] NVARCHAR(50) NULL,
        [VISITOR_IP] NVARCHAR(45) NULL,
        [VISITOR_BROWSER] NVARCHAR(100) NULL,
        [VISITOR_OS] NVARCHAR(50) NULL,
        [DEPARTMENT] NVARCHAR(50) NULL,
        [OPERATOR_EMAIL] NVARCHAR(100) NULL,
        [OPERATOR_ID] NVARCHAR(50) NULL,
        [OPERATOR_NICKNAME] NVARCHAR(50) NULL,
        [CHAT_START_TIME] DATETIME NULL,
        [CHAT_END_TIME] DATETIME NULL,
        [MESSAGE_COUNT] INT NULL DEFAULT 0,
        [ACTIVITY_ID] NVARCHAR(15) NULL,
        [CONTACT_ID] NVARCHAR(15) NULL,
        [STATUS] NVARCHAR(20) NULL DEFAULT 'COMPLETED',
        [CREATED] DATETIME NULL DEFAULT GETDATE(),
        [CREATED_BY] NVARCHAR(15) NULL DEFAULT 'SYSTEM'
    );
    
    -- Create indexes for performance
    CREATE INDEX [IX_CHAT_SESSIONS_VISITOR] ON [yourdb].[dbo].[CHAT_SESSIONS] ([VISITOR_ID]);
    CREATE INDEX [IX_CHAT_SESSIONS_EMAIL] ON [yourdb].[dbo].[CHAT_SESSIONS] ([VISITOR_EMAIL]);
    CREATE INDEX [IX_CHAT_SESSIONS_OPERATOR] ON [yourdb].[dbo].[CHAT_SESSIONS] ([OPERATOR_EMAIL]);
    CREATE INDEX [IX_CHAT_SESSIONS_ACTIVITY] ON [yourdb].[dbo].[CHAT_SESSIONS] ([ACTIVITY_ID]);
    CREATE INDEX [IX_CHAT_SESSIONS_CREATED] ON [yourdb].[dbo].[CHAT_SESSIONS] ([CREATED]);
END;

-- CHAT_MESSAGES table - Individual chat messages
-- This table stores individual chat messages for detailed analysis
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CHAT_MESSAGES' AND xtype='U')
BEGIN
    CREATE TABLE [yourdb].[dbo].[CHAT_MESSAGES] (
        [MESSAGE_ID] NVARCHAR(50) NOT NULL PRIMARY KEY,
        [CHAT_ID] NVARCHAR(50) NULL,
        [MESSAGE_BODY] NVARCHAR(MAX) NULL,
        [MESSAGE_KIND] NVARCHAR(50) NULL,
        [NICKNAME] NVARCHAR(50) NULL,
        [OPERATOR_ID] NVARCHAR(50) NULL,
        [TIMESTAMP] DATETIME NULL,
        [UNIX_TIMESTAMP] BIGINT NULL,
        [IS_VISITOR_MESSAGE] BIT NULL DEFAULT 0,
        [CREATED] DATETIME NULL DEFAULT GETDATE()
    );
    
    -- Create indexes for performance
    CREATE INDEX [IX_CHAT_MESSAGES_CHAT_ID] ON [yourdb].[dbo].[CHAT_MESSAGES] ([CHAT_ID]);
    CREATE INDEX [IX_CHAT_MESSAGES_KIND] ON [yourdb].[dbo].[CHAT_MESSAGES] ([MESSAGE_KIND]);
    CREATE INDEX [IX_CHAT_MESSAGES_TIMESTAMP] ON [yourdb].[dbo].[CHAT_MESSAGES] ([TIMESTAMP]);
    CREATE INDEX [IX_CHAT_MESSAGES_OPERATOR] ON [yourdb].[dbo].[CHAT_MESSAGES] ([OPERATOR_ID]);
END;

-- ============================================
-- Views for Reporting
-- ============================================

-- View to show chat activity statistics
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='V_CHAT_ACTIVITY_STATS' AND xtype='V')
BEGIN
    EXEC('CREATE VIEW [yourdb].[dbo].[V_CHAT_ACTIVITY_STATS] AS
    SELECT 
        e.LOGIN as OperatorLogin,
        e.EMAIL_ADDR as OperatorEmail,
        COUNT(a.ROW_ID) as ChatCount,
        AVG(a.DURATION_HRS) as AvgDuration,
        MAX(a.CREATED) as LastChatDate,
        COUNT(CASE WHEN a.EVT_STAT_CD = ''Not Started'' THEN 1 END) as PendingChats,
        COUNT(CASE WHEN a.EVT_STAT_CD = ''Done'' THEN 1 END) as CompletedChats
    FROM [yourdb].[dbo].[S_EVT_ACT] a
    LEFT JOIN [yourdb].[dbo].[S_EMPLOYEE] e ON a.OWNER_PER_ID = e.ROW_ID
    WHERE a.TODO_CD = ''Online Help''
    GROUP BY e.LOGIN, e.EMAIL_ADDR');
END;

-- View to show visitor chat history
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='V_VISITOR_CHAT_HISTORY' AND xtype='V')
BEGIN
    EXEC('CREATE VIEW [yourdb].[dbo].[V_VISITOR_CHAT_HISTORY] AS
    SELECT 
        a.EMAIL_RECIP_ADDR as VisitorEmail,
        a.NAME as ChatSubject,
        a.CREATED as ChatDate,
        a.EVT_STAT_CD as Status,
        e.LOGIN as OperatorLogin,
        a.COMMENTS_LONG as ChatTranscript,
        a.X_DESC_TEXT as ChatId
    FROM [yourdb].[dbo].[S_EVT_ACT] a
    LEFT JOIN [yourdb].[dbo].[S_EMPLOYEE] e ON a.OWNER_PER_ID = e.ROW_ID
    WHERE a.TODO_CD = ''Online Help''
    ORDER BY a.CREATED DESC');
END;

-- ============================================
-- Stored Procedures
-- ============================================

-- Procedure to clean up old chat records
IF EXISTS (SELECT * FROM sysobjects WHERE name='SP_CLEANUP_OLD_CHATS' AND xtype='P')
    DROP PROCEDURE [yourdb].[dbo].[SP_CLEANUP_OLD_CHATS];

EXEC('CREATE PROCEDURE [yourdb].[dbo].[SP_CLEANUP_OLD_CHATS]
    @DaysOld INT = 365
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Delete old chat activity records
    DELETE FROM [yourdb].[dbo].[S_EVT_ACT] 
    WHERE TODO_CD = ''Online Help'' 
    AND CREATED < DATEADD(DAY, -@DaysOld, GETDATE());
    
    -- Delete old chat sessions
    DELETE FROM [yourdb].[dbo].[CHAT_SESSIONS] 
    WHERE CREATED < DATEADD(DAY, -@DaysOld, GETDATE());
    
    -- Delete old chat messages
    DELETE FROM [yourdb].[dbo].[CHAT_MESSAGES] 
    WHERE CREATED < DATEADD(DAY, -@DaysOld, GETDATE());
    
    SELECT @@ROWCOUNT as RecordsDeleted;
END');

-- Procedure to get chat statistics by date range
IF EXISTS (SELECT * FROM sysobjects WHERE name='SP_GET_CHAT_STATISTICS' AND xtype='P')
    DROP PROCEDURE [yourdb].[dbo].[SP_GET_CHAT_STATISTICS];

EXEC('CREATE PROCEDURE [yourdb].[dbo].[SP_GET_CHAT_STATISTICS]
    @StartDate DATETIME,
    @EndDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) as TotalChats,
        COUNT(CASE WHEN EVT_STAT_CD = ''Done'' THEN 1 END) as CompletedChats,
        COUNT(CASE WHEN EVT_STAT_CD = ''Not Started'' THEN 1 END) as PendingChats,
        AVG(DURATION_HRS) as AvgDuration,
        COUNT(DISTINCT EMAIL_RECIP_ADDR) as UniqueVisitors,
        COUNT(DISTINCT OWNER_PER_ID) as ActiveOperators
    FROM [yourdb].[dbo].[S_EVT_ACT]
    WHERE TODO_CD = ''Online Help''
    AND CREATED BETWEEN @StartDate AND @EndDate;
END');

-- ============================================
-- Sample Data (Optional)
-- ============================================
-- Uncomment the following section to insert sample data for testing

/*
-- Sample employee records
INSERT INTO [yourdb].[dbo].[S_EMPLOYEE] 
([ROW_ID], [LOGIN], [EMAIL_ADDR], [FST_NAME], [LST_NAME], [ACTIVE_FLG], [CREATED], [CREATED_BY], [ROW_STATUS])
VALUES 
('1-EMP001', 'TECHSUPPORT', 'techsupport@yourdomain.com', 'Technical', 'Support', 'Y', GETDATE(), 'SYSTEM', 'Y'),
('1-EMP002', 'SALES', 'sales@yourdomain.com', 'Sales', 'Representative', 'Y', GETDATE(), 'SYSTEM', 'Y');

-- Sample contact record
INSERT INTO [yourdb].[dbo].[S_CONTACT] 
([ROW_ID], [PR_DEPT_OU_ID], [X_REGISTRATION_NUM], [EMAIL_ADDR], [FST_NAME], [LST_NAME], [CREATED], [CREATED_BY], [ROW_STATUS])
VALUES 
('1-CONT001', '1-OU001', 'REG123456', 'customer@example.com', 'John', 'Doe', GETDATE(), 'SYSTEM', 'Y');
*/

-- ============================================
-- Permissions
-- ============================================
-- Grant necessary permissions to the application user
-- Replace 'GetChatUser' with your actual application user

/*
-- Create application user (uncomment if needed)
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = ''GetChatUser'')
BEGIN
    CREATE LOGIN [GetChatUser] WITH PASSWORD = ''YourSecurePassword123!'';
END;

-- Create database user
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = ''GetChatUser'')
BEGIN
    USE [yourdb];
    CREATE USER [GetChatUser] FOR LOGIN [GetChatUser];
END;

-- Grant permissions
USE [yourdb];
GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[S_CONTACT] TO [GetChatUser];
GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[S_EMPLOYEE] TO [GetChatUser];
GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[S_EVT_ACT] TO [GetChatUser];
GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[CHAT_SESSIONS] TO [GetChatUser];
GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[CHAT_MESSAGES] TO [GetChatUser];
GRANT EXECUTE ON [dbo].[SP_CLEANUP_OLD_CHATS] TO [GetChatUser];
GRANT EXECUTE ON [dbo].[SP_GET_CHAT_STATISTICS] TO [GetChatUser];
*/

-- ============================================
-- Script Completion
-- ============================================
PRINT 'GetChat Database Schema created successfully!';
PRINT 'Remember to:';
PRINT '1. Update connection strings in web.config';
PRINT '2. Create and configure application user with appropriate permissions';
PRINT '3. Test the webhook endpoint with sample chat data';
PRINT '4. Set up log4net configuration for logging';
PRINT '5. Configure Olark webhook URL to point to the service endpoint';
