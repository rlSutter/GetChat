# GetChat Webhook Service

## Overview

The GetChat webhook service is a C# ASP.NET web service that processes chat transcript notifications from Olark chat service. When a chat session is completed, Olark sends a webhook notification containing the full conversation transcript, visitor information, and operator details. This service then stores the chat data as an activity record in the database for tracking and follow-up purposes.

## Architecture

### Components

- **GetChat.ashx** - ASP.NET HTTP handler declaration
- **GetChat.ashx.cs** - Main webhook processing logic
- **Database Schema** - SQL Server tables for contact management, employee tracking, and activity records
- **Logging** - log4net integration for debugging and monitoring

### Data Flow

1. Olark sends JSON webhook notification with chat transcript data
2. Service deserializes the JSON payload
3. Extracts visitor information, operator details, and chat messages
4. Looks up contact record using registration number (if provided)
5. Identifies the operator/employee from email address
6. Creates formatted conversation transcript
7. Inserts activity record with full chat details
8. Logs processing results and performance data

## Database Schema

### Primary Tables

#### S_CONTACT (yourdb.dbo.S_CONTACT)
Stores contact information including registration numbers for customer lookup.

**Key Fields:**
- `ROW_ID` - Primary key
- `PR_DEPT_OU_ID` - Organizational unit ID
- `X_REGISTRATION_NUM` - Registration number for customer identification
- `EMAIL_ADDR` - Contact's email address
- `FST_NAME`, `LST_NAME` - Contact names
- `PHONE_NUM` - Contact phone number

#### S_EMPLOYEE (yourdb.dbo.S_EMPLOYEE)
Stores employee information for chat operator identification.

**Key Fields:**
- `ROW_ID` - Primary key
- `LOGIN` - Employee login name
- `EMAIL_ADDR` - Employee email address
- `FST_NAME`, `LST_NAME` - Employee names
- `DEPT_OU_ID` - Department organizational unit
- `ACTIVE_FLG` - Active status flag

#### S_EVT_ACT (yourdb.dbo.S_EVT_ACT)
Tracks activities and events, including chat sessions.

**Key Fields:**
- `ROW_ID` - Primary key
- `ACTIVITY_UID` - Unique activity identifier
- `COMMENTS_LONG` - Formatted chat transcript
- `EMAIL_RECIP_ADDR` - Visitor's email address
- `TARGET_PER_ID` - Contact ID (if found)
- `OWNER_PER_ID` - Employee ID of chat operator
- `TODO_CD` - Activity type (set to "Online Help" for chats)
- `EVT_STAT_CD` - Status (Done, Not Started)
- `X_DESC_TEXT` - Additional chat details

### Enhanced Tables (Optional)

#### CHAT_SESSIONS (yourdb.dbo.CHAT_SESSIONS)
Additional tracking for chat sessions with visitor details.

#### CHAT_MESSAGES (yourdb.dbo.CHAT_MESSAGES)
Individual chat messages for detailed analysis.

## Configuration

### Web.config Settings

```xml
<appSettings>
    <!-- Debug mode: Y=Yes, N=No, T=Trace -->
    <add key="GetChat_debug" value="N" />
    
    <!-- Sales department employee settings -->
    <add key="GetChat_SalesEmpId" value="YOUR_SALES_EMP_ID" />
    <add key="GetChat_SalesEmpLogin" value="YOUR_SALES_LOGIN" />
    
    <!-- Technical support employee settings -->
    <add key="GetChat_TechSupportEmpId" value="YOUR_TECH_EMP_ID" />
    <add key="GetChat_TechSupportEmpLogin" value="YOUR_TECH_LOGIN" />
</appSettings>

<connectionStrings>
    <add name="YourConnectionStringName" 
         connectionString="server=YOUR_SERVER\YOUR_INSTANCE;uid=YOUR_USER;pwd=YOUR_PASSWORD;database=YOUR_DATABASE" 
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

### log4net Configuration

The service uses log4net for logging. Configure in web.config:

```xml
<log4net>
    <appender name="EventLogAppender" type="log4net.Appender.EventLogAppender">
        <applicationName value="GetChat" />
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
        </layout>
    </appender>
    
    <appender name="DebugLogAppender" type="log4net.Appender.RollingFileAppender">
        <file value="C:\Logs\GetChat.log" />
        <appendToFile value="true" />
        <rollingStyle value="Size" />
        <maxSizeRollBackups value="10" />
        <maximumFileSize value="10MB" />
        <staticLogFileName value="false" />
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
        </layout>
    </appender>
    
    <logger name="EventLog">
        <level value="INFO" />
        <appender-ref ref="EventLogAppender" />
    </logger>
    
    <logger name="DebugLog">
        <level value="DEBUG" />
        <appender-ref ref="DebugLogAppender" />
    </logger>
</log4net>
```

## Webhook Payload Format

The service expects JSON payloads in the following format (based on Olark webhook structure):

```json
{
    "id": "chat_session_id",
    "kind": "Chat",
    "visitor": {
        "id": "visitor_id",
        "fullName": "John Doe",
        "emailAddress": "john.doe@example.com",
        "phoneNumber": "+1234567890",
        "country": "United States",
        "countryCode": "US",
        "city": "New York",
        "region": "NY",
        "ip": "192.168.1.1",
        "browser": "Chrome",
        "operatingSystem": "Windows",
        "referrer": "https://example.com",
        "organization": "Example Corp",
        "customFields": {
            "internalCustomerId": "REG123456"
        }
    },
    "operators": {
        "operator_id": {
            "id": "operator_id",
            "emailAddress": "operator@yourdomain.com",
            "username": "operator_login",
            "nickname": "Support Agent",
            "kind": "operator"
        }
    },
    "groups": [
        {
            "id": "group_id",
            "name": "Sales",
            "kind": "group"
        }
    ],
    "items": [
        {
            "body": "Hello, how can I help you?",
            "kind": "MessageToVisitor",
            "nickname": "Support Agent",
            "operatorId": "operator_id",
            "timestamp": "1445956507"
        },
        {
            "body": "I need help with my order",
            "kind": "MessageToOperator",
            "nickname": "John Doe",
            "operatorId": null,
            "timestamp": "1445956510"
        }
    ]
}
```

## Business Logic

### Chat Processing Rules

1. **Visitor Identification**: If a registration number is provided in custom fields, the service attempts to locate the contact record.

2. **Operator Identification**: The service identifies the chat operator by matching email addresses in the operators section with employee records.

3. **Department Assignment**: Based on the group name (Sales vs. Technical Support), different default employees are assigned for offline messages.

4. **Message Formatting**: Chat messages are formatted with timestamps and speaker identification:
   - Visitor messages: "Visitor Name (timestamp) > 'message'"
   - Operator messages: "Operator Name (timestamp) > 'message'"

5. **Activity Creation**: All chats create activity records with:
   - Type: "Online Help"
   - Priority: "2-High"
   - Status: "Done" (for completed chats) or "Not Started" (for offline messages)

6. **Offline Message Handling**: Offline messages are assigned to department-specific employees and include additional contact information in the transcript.

### Message Types

- **MessageToOperator**: Messages from visitor to operator
- **MessageToVisitor**: Messages from operator to visitor
- **OfflineMessage**: Messages left when no operator is available

## Error Handling

### Logging Levels

- **Event Log**: Records successful processing and errors
- **Debug Log**: Detailed trace information (when debug mode is enabled)
- **JSON Log**: Raw webhook payloads stored in `C:\Logs\GetChat-JSON.log`

### Error Scenarios

1. **Database Connection Issues**: Automatic retry with connection pooling disabled
2. **JSON Parsing Errors**: Detailed error logging with original payload
3. **Missing Contact Records**: Logged as informational messages
4. **Missing Employee Records**: Falls back to default department employees
5. **SQL Execution Errors**: Comprehensive error logging with context

## Security Considerations

### Input Validation

- JSON payload validation and sanitization
- SQL injection prevention through parameterized queries (where applicable)
- Input size limits and truncation
- Error message sanitization

### Access Control

- Database user should have minimal required permissions
- Log files should be secured with appropriate file system permissions
- Webhook endpoint should be protected with authentication if possible

## Deployment

### Prerequisites

1. SQL Server with appropriate database
2. .NET Framework 4.0 or higher
3. log4net library
4. Newtonsoft.Json library
5. Appropriate database permissions

### Installation Steps

1. Deploy the web service files to IIS
2. Run the database schema script (`GetChat_Database_Schema.sql`)
3. Configure web.config with appropriate connection strings and settings
4. Set up log4net configuration
5. Create application user with necessary database permissions
6. Configure Olark webhook URL to point to the service endpoint

### Testing

1. Enable debug mode in web.config
2. Send test webhook payload to the service
3. Verify database updates and log entries
4. Test error scenarios (invalid JSON, database unavailable, etc.)

## Monitoring and Maintenance

### Performance Monitoring

- Monitor log file sizes and rotation
- Track database performance for contact and employee lookups
- Monitor webhook response times

### Maintenance Tasks

- Regular cleanup of old chat records using `SP_CLEANUP_OLD_CHATS`
- Log file rotation and archival
- Database index maintenance
- Review chat activity reports

### Troubleshooting

#### Common Issues

1. **Webhook Not Processing**: Check IIS logs, verify endpoint URL
2. **Database Connection Errors**: Verify connection string and permissions
3. **JSON Parsing Errors**: Check webhook payload format
4. **Missing Activity Records**: Verify web service configuration and permissions
5. **Employee Not Found**: Check employee email addresses in database

#### Debug Mode

Enable debug mode by setting `GetChat_debug` to "Y" in web.config. This will:
- Log detailed trace information
- Record all SQL queries
- Show step-by-step processing information
- Display visitor and operator details

## API Reference

### Endpoint

```
POST /GetChat.ashx
Content-Type: application/json
```

### Request

Raw JSON payload from Olark webhook.

### Response

- **Success**: HTTP 200 with no content
- **Error**: HTTP 500 with error details in logs

### Headers

The service accepts standard HTTP headers. No special authentication headers are required (consider adding authentication for production use).

## Reporting and Analytics

### Available Views

1. **V_CHAT_ACTIVITY_STATS**: Operator performance statistics
2. **V_VISITOR_CHAT_HISTORY**: Complete chat history by visitor

### Stored Procedures

1. **SP_CLEANUP_OLD_CHATS**: Clean up old chat records
2. **SP_GET_CHAT_STATISTICS**: Get chat statistics for date ranges

### Sample Queries

```sql
-- Get chat statistics for the last 30 days
EXEC SP_GET_CHAT_STATISTICS 
    @StartDate = DATEADD(DAY, -30, GETDATE()),
    @EndDate = GETDATE();

-- Get operator performance
SELECT * FROM V_CHAT_ACTIVITY_STATS 
ORDER BY ChatCount DESC;

-- Get visitor chat history
SELECT * FROM V_VISITOR_CHAT_HISTORY 
WHERE VisitorEmail = 'customer@example.com';
```

## Version History

- **v1.0.0** - Initial implementation with basic chat processing
- **v1.0.1** - Added comprehensive logging and error handling
- **v1.0.2** - Enhanced database schema with indexes and views
- **v1.0.3** - Added offline message handling and department routing

## Support

For technical support or questions about this service, please refer to the application logs and database records. The service includes comprehensive logging to assist with troubleshooting.

## License

This software is proprietary and confidential. Unauthorized copying, distribution, or modification is prohibited.
