# GetChat Webhook Service

## Table of Contents

1. [Overview](#overview)
2. [Design](#design)
3. [Architecture](#architecture)
4. [Data Model](#data-model)
5. [Database Schema](#database-schema)
6. [Data Structures](#data-structures)
7. [Webhook Payload Format](#webhook-payload-format)
8. [Business Logic](#business-logic)
9. [Assumptions](#assumptions)
10. [Error Handling](#error-handling)
11. [Security Considerations](#security-considerations)
12. [Configuration](#configuration)
13. [Deployment](#deployment)
14. [Monitoring and Maintenance](#monitoring-and-maintenance)
15. [API Reference](#api-reference)
16. [Reporting and Analytics](#reporting-and-analytics)
17. [References](#references)
18. [Update History](#update-history)
19. [Notifications](#notifications)
20. [Related Web Services](#related-web-services)
21. [Executing](#executing)
22. [Testing](#testing)
23. [Scheduling](#scheduling)
24. [Monitoring](#monitoring)
25. [Logging](#logging)
26. [Results](#results)
27. [Programming](#programming)
28. [Operational](#operational)
29. [Support](#support)
30. [License](#license)

## Overview

This service implements an integration point for the Olark chat service. When a chat is "completed", then that service initiates a call to this one, for the purpose of putting the information received into Siebel activity records.

The GetChat webhook service is a C# ASP.NET web service that processes chat transcript notifications from Olark chat service. When a chat session is completed, Olark sends a webhook notification containing the full conversation transcript, visitor information, and operator details. This service then stores the chat data as an activity record in the database for tracking and follow-up purposes.

### Purpose
This service serves as a bridge between the Olark chat platform and the internal customer relationship management system, ensuring that all chat interactions are properly recorded and tracked for customer service follow-up and analytics.

### Scope
The service handles:
- Real-time chat transcript processing
- Visitor identification and contact matching
- Operator identification and assignment
- Activity record creation in the CRM system
- Offline message handling
- Department-specific routing

## Design

### System Architecture

The GetChat service follows a layered architecture pattern with clear separation of concerns:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Olark Chat    │──▶│  GetChat.ashx   │───▶│   SQL Server    │
│   Platform      │    │   Web Service   │    │   Database      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌─────────────────┐
                       │   Log4net       │
                       │   Logging       │
                       └─────────────────┘
```

### Design Principles

1. **Single Responsibility**: Each component has a specific, well-defined purpose
2. **Fail-Safe**: Comprehensive error handling and logging
3. **Performance**: Efficient database operations with connection pooling
4. **Maintainability**: Clear code structure and comprehensive documentation
5. **Security**: Input validation and SQL injection prevention

## Architecture

### Components

- **GetChat.ashx** - ASP.NET HTTP handler declaration
- **GetChat.ashx.cs** - Main webhook processing logic
- **Database Schema** - SQL Server tables for contact management, employee tracking, and activity records
- **Logging** - log4net integration for debugging and monitoring

### Technology Stack

- **Framework**: ASP.NET 4.0+
- **Database**: Microsoft SQL Server
- **Logging**: log4net
- **JSON Processing**: Newtonsoft.Json
- **Web Server**: IIS

### Data Flow

1. Olark sends JSON webhook notification with chat transcript data
2. Service deserializes the JSON payload
3. Extracts visitor information, operator details, and chat messages
4. Looks up contact record using registration number (if provided)
5. Identifies the operator/employee from email address
6. Creates formatted conversation transcript
7. Inserts activity record with full chat details
8. Logs processing results and performance data

## Data Model

In order to support the information generated from this service:

### Database Schema Changes
- The column `S_EVT_ACT.X_DESC_TEXT` was created to store the chat items provided by Olark.
- Created the employee "Technical Support" (with ID "1-XXXXX") to act as the "default" employee in case the operator record is not found.

### Activity Record Field Mappings

The following fields have special meaning in an activity created from this service:

| FIELD | DESCRIPTION |
|-------|-------------|
| `RPLY_PH_NUM` | Contains the chat id from Olark |
| `SRA_TYPE_CD` | Mapped to code "Tech Support - Web Site" |
| `COMMENTS_LONG` | Will contain the first 1480 characters of the chat |
| `TODO_CD` | Mapped to code "Online Help" |
| `TODO_ACTL_END_DT` | Will be the date/time stamp of the last chat item |

### Entity Relationship Overview

The GetChat service interacts with several key entities in the database:

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   VISITOR   │    │   CONTACT   │    │  EMPLOYEE   │
│             │    │             │    │             │
│ - ID        │    │ - ROW_ID    │    │ - ROW_ID    │
│ - Email     │    │ - Email     │    │ - Email     │
│ - Name      │    │ - Name      │    │ - Name      │
│ - Phone     │    │ - RegNum    │    │ - Login     │
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       │                   │                   │
       └───────────────────┼───────────────────┘
                           │
                           ▼
                  ┌─────────────┐
                  │  ACTIVITY   │
                  │             │
                  │ - ROW_ID    │
                  │ - Comments  │
                  │ - Contact   │
                  │ - Owner     │
                  │ - Type      │
                  └─────────────┘
```

### Data Flow Relationships

1. **Visitor → Contact**: Matched by registration number or email
2. **Visitor → Employee**: Matched by operator email from chat
3. **Contact + Employee → Activity**: Creates activity record with both references

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
    <add key="GetChat_debug" value="Y" />
    
    <!-- Sales department employee settings -->
    <add key="GetChat_SalesEmpId" value="1-XXXXX" />
    <add key="GetChat_SalesEmpLogin" value="SALES_USER" />
    
    <!-- Technical support employee settings -->
    <add key="GetChat_TechSupportEmpId" value="1-XXXXX" />
    <add key="GetChat_TechSupportEmpLogin" value="TECHNICAL SUPPORT" />
</appSettings>

<connectionStrings>
    <!-- Main database connection -->
    <add name="hcidb" 
         connectionString="server=YOUR_SERVER\YOUR_INSTANCE;uid=YOUR_USER;pwd=YOUR_PASSWORD;Min Pool Size=3;Max Pool Size=5;Connect Timeout=10;database=" 
         providerName="System.Data.SqlClient" />
    
    <!-- ASP.NET membership database -->
    <add name="ApplicationServices"
         connectionString="data source=.\SQLEXPRESS;Integrated Security=SSPI;AttachDBFilename=|DataDirectory|\aspnetdb.mdf;User Instance=true"
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

### log4net Configuration

The service uses log4net for logging with both remote syslog and file-based logging:

```xml
<log4net>
    <!-- Remote syslog appender for centralized logging -->
    <appender name="RemoteSyslogAppender" type="log4net.Appender.RemoteSyslogAppender">
        <identity value="" />
        <layout type="log4net.Layout.PatternLayout" value="%message"/>
        <remoteAddress value="YOUR_SYSLOG_SERVER_IP" />
        <filter type="log4net.Filter.LevelRangeFilter">
            <levelMin value="DEBUG" />
        </filter>
    </appender>
    
    <!-- File-based rolling log appender -->
    <appender name="LogFileAppender" type="log4net.Appender.RollingFileAppender">
        <file type="log4net.Util.PatternString" value="%property{LogFileName}"/>
        <appendToFile value="true"/>
        <rollingStyle value="Size"/>
        <maxSizeRollBackups value="3"/>
        <maximumFileSize value="10000KB"/>
        <staticLogFileName value="true"/>
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%message%newline"/>
        </layout>
    </appender>
    
    <!-- Event log logger for system events -->
    <logger name="EventLog">
        <level value="ALL"/>
        <appender-ref ref="RemoteSyslogAppender"/>
    </logger>
    
    <!-- Debug log logger for detailed troubleshooting -->
    <logger name="DebugLog">
        <level value="ALL"/>
        <appender-ref ref="LogFileAppender"/>
    </logger>
</log4net>
```

## Data Structures

### Core Data Classes

The Olark service provides data in JSON object format. The JSON.Net library is used to convert this into C# classes. The class defined to receive data from JSON is as follows:

```csharp
public class Data
{
    public string id;
    public List<Item> items = new List<Item>();
    public string kind;
    public Dictionary<string, Employee> operators = new Dictionary<string,Employee>();
    public List<Groups> groups = new List<Groups>();
    public Visitor visitor; 
}

public class Item
{
    public string body { get; set; }
    public string kind { get; set; }
    public string nickname { get; set; }
    public string operatorId { get; set; }
    public string timestamp { get; set; }
}

public class Operator
{
    public string id { get; set; }
}

public class Employee
{
    public string emailAddress { get; set; }
    public string id { get; set; }
    public string kind { get; set; }
    public string username { get; set; }
    public string nickname { get; set; }
}

public class Groups
{
    public string kind { get; set; }
    public string id { get; set; }
    public string name { get; set; }
}

public class Visitor
{
    public string browser { get; set; }
    public string city { get; set; }
    public string country { get; set; }
    public string countryCode { get; set; }
    public CustomFields customFields = new CustomFields();
    public string emailAddress { get; set; }
    public string fullName { get; set; }
    public string id { get; set; }
    public string ip { get; set; }
    public string kind { get; set; }
    public string operatingSystem { get; set; }
    public string organization { get; set; }
    public string phoneNumber { get; set; }
    public string referrer { get; set; }
    public string region { get; set; }
}

public class CustomFields
{
    public string internalCustomerId { get; set; }
}

public class KeyValue
{
    public string key { get; set; }
    public string value { get; set; }
}
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
        "ip": "192.168.1.100",
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

## Assumptions

### Technical Assumptions

1. **JSON Format**: The supplied JSON file is properly formed
2. **Database Connectivity**: The service assumes reliable database connectivity and will retry failed connections
3. **Email Matching**: Employee identification relies on exact email address matching between Olark operators and database records
4. **Registration Numbers**: Customer identification assumes registration numbers are provided in custom fields when available
5. **Department Routing**: Offline messages are routed based on group names (Sales vs. Technical Support)

### Business Assumptions

1. **Employee Setup**: The employee record has been setup with a related contact record for each operator, and the email address of the operator account matches that of the employee's contact record
2. **Contact Records**: Existing customers have registration numbers that can be used for identification
3. **Activity Tracking**: All chat sessions should be recorded as activities for follow-up purposes
4. **Department Structure**: The organization has distinct Sales and Technical Support departments
5. **Message Priority**: Chat activities are assigned high priority (2-High) by default

### Operational Assumptions

1. **Logging**: Comprehensive logging is required for troubleshooting and monitoring
2. **Error Handling**: Failed webhook processing should not break the chat system
3. **Performance**: The service should handle multiple concurrent webhook requests
4. **Security**: Input validation and sanitization are critical for system security
5. **Maintenance**: Regular cleanup of old chat records is necessary for database performance

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

## References

This service was implemented as per [http://www.olark.com/customer/portal/articles/311822-webhooks-integration-tutorial](http://www.olark.com/customer/portal/articles/311822-webhooks-integration-tutorial). The following was referenced during implementation:

### External Documentation

1. **Olark Webhook Integration Tutorial**: [http://www.olark.com/customer/portal/articles/311822-webhooks-integration-tutorial](http://www.olark.com/customer/portal/articles/311822-webhooks-integration-tutorial)
2. **Olark Webhook Admin Page**: [https://www.olark.com/crm/webhook](https://www.olark.com/crm/webhook) - The admin page for setting the "web hook"
3. **Olark Customer Information**: [http://www.olark.com/blog/2011/who-am-i-speaking-with/](http://www.olark.com/blog/2011/who-am-i-speaking-with/) - Putting customer information into Olark
4. **ASP.NET JSON Handling**: [http://stackoverflow.com/questions/12401239/pass-jquery-json-into-asp-net-httphandler](http://stackoverflow.com/questions/12401239/pass-jquery-json-into-asp-net-httphandler)
5. **MVC JSON Posting**: [http://blog.stevehayter.me/post/17663987708/posting-json-to-an-mvc-action-method-and-how-to](http://blog.stevehayter.me/post/17663987708/posting-json-to-an-mvc-action-method-and-how-to)
6. **JSON Formatting Tool**: [http://jsonformat.com](http://jsonformat.com) - A useful tool for formatting a JSON string for readability
7. **JSON.NET Library**: [http://james.newtonking.com/projects/json-net.aspx](http://james.newtonking.com/projects/json-net.aspx) - The JSON.net library for use in converting between .NET objects and JSON

### Internal Documentation

1. **Database Schema Documentation**: Internal CRM database schema reference
2. **Employee Management System**: Internal employee record structure documentation
3. **Activity Management System**: Internal activity tracking system documentation
4. **Security Guidelines**: Internal security and data handling policies

### Related Systems

1. **CRM System**: Customer relationship management system for contact and activity tracking
2. **Employee Directory**: Internal employee management system
3. **Olark Chat Platform**: External chat service provider
4. **Logging Infrastructure**: Centralized logging and monitoring system
5. **External Cloud Service**: External service integration for additional functionality
6. **Remote Syslog Server**: Centralized logging at YOUR_SYSLOG_SERVER_IP

### External Service Integration

The service integrates with an external cloud service:

```xml
<applicationSettings>
    <GetChat.Properties.Settings>
        <setting name="GetChat_com_external_service_Service" serializeAs="String">
            <value>http://your-cloud-service.com/basic/service.asmx</value>
        </setting>
    </GetChat.Properties.Settings>
</applicationSettings>
```

## Update History

### 2/17/20
- Updated to support WebServicesSecurity#Version_Management

### 5/17/19
- Updated to fix logging to LogPerformanceData - the method name was being cast as "PROCESSREQUEST"

### 1/14/16
- Updated to improve logging

### 2/25/14
- Updated to incorporate log4net for use in generating rolling debug log files, as well as to log transactions and errors to SysLog

### 12/18/13
- Modified to make the transaction logging information more complete, as well as to add support for SysLog#Web_Service_Logging

### 5/2/13
- Updated to set the activity creator to be the same as the assigned employee so it displays correctly in activity reports, and to better handle group settings in error conditions

### 4/22/13
- Updated to include the contact information in offline message activities

### 4/19/13
- Changed the default for Sales group offline messages to be Trevor. Updated logging

### 4/15/13
- Updated to support groups for determining who to assign offline messages to. Added application settings for the Sales and Technical Support departments for this purpose, as well as added items to the custom class definition

### 2/15/13
- Updated to put the default employee for messages in the web.config file and to more scrupulously remove used objects

## Notifications

None at this time.

## Related Web Services

None at this time.

## Support

For technical support or questions about this service, please refer to the application logs and database records. The service includes comprehensive logging to assist with troubleshooting.

### Support Contacts

- **Technical Issues**: Contact the development team
- **Database Issues**: Contact the database administration team
- **Olark Integration**: Contact the vendor support team
- **System Monitoring**: Contact the system administration team

## Executing

This service is executed by Olark using the following URL:
```
http://your-domain.com/GetChat.ashx
```

The service is provided a JSON object similar to the following (and formatted using http://jsonformat.com/):

```json
{
    "kind": "Conversation",
    "items": [
        {
            "body": "Does every restaurant require this training?",
            "timestamp": "1365539948.56",
            "kind": "MessageToOperator",
            "nickname": "USA (Coventry, RI) #2979"
        },
        {
            "body": "Good afternoon, let me check RI regulations.",
            "timestamp": "1365540069.95",
            "kind": "MessageToVisitor",
            "nickname": "Carlos Palacios",
            "operatorId": "637529"
        },
        {
            "body": "RI is mandatory for every server. Our online and classroom program is approved.",
            "timestamp": "1365540224.54",
            "kind": "MessageToVisitor",
            "nickname": "Carlos Palacios",
            "operatorId": "637529"
        },
        {
            "body": "So I can take either the online course or take a class?",
            "timestamp": "1365540256.17",
            "kind": "MessageToOperator",
            "nickname": "USA (Coventry, RI) #2979"
        },
        {
            "body": "Yes",
            "timestamp": "1365540268.46",
            "kind": "MessageToVisitor",
            "nickname": "Carlos Palacios",
            "operatorId": "637529"
        },
        {
            "body": "Ok thank you!",
            "timestamp": "1365540276.25",
            "kind": "MessageToOperator",
            "nickname": "USA (Coventry, RI) #2979"
        }
    ],
    "operators": {
        "637529": {
            "username": "operator1",
            "emailAddress": "operator1@example.com",
            "kind": "Operator",
            "nickname": "Support Agent",
            "id": "637529"
        }
    },
    "groups": [
        {
            "kind": "Group",
            "id": "3e7a37bf205abdb7a37b7c7e65028742",
            "name": "Sales"
        }
    ],
    "visitor": {
        "city": "Coventry",
        "kind": "Visitor",
        "countryCode": "US",
        "referrer": "http://www.example.com/index.html",
        "ip": "74.103.202.54",
        "region": "RI",
        "operatingSystem": "Macintosh",
        "emailAddress": "",
        "country": "United States",
        "organization": "Example ISP",
        "fullName": "USA (Coventry, RI) #2979",
        "id": "iUiN8QHwDtI3lBcPDaT5yHW426533109",
        "browser": "Safari 6.0"
    },
    "id": "634WrkRJKEip4aBey7SC89F426533109"
}
```

This service attempts to parse this information to determine whom to create the activities for.

## Testing

This web service can be tested using Fiddler (available at your-tools-directory\FiddlerSetup.exe) by doing the following using the Composer tab:

1. Create a POST transaction to `http://your-production-server/GetChat.ashx` if using a production server, or `http://localhost:8080/GetChat.ashx` if executing on the development machine.
2. In the Request Headers box add `Content-type: application/json; charset=utf-8`
3. In the Request Body of the transaction, enter a test JSON object (formatted or non-formatted).
4. Click the "Execute" button to send the transaction
5. Check the transaction in Siebel in the Activities > All Activities view.

The results will be reported in the log file `C:\Logs\GetChat.log` on the server tested or the local development workstation.

## Configuration

The configuration for this web service is stored in the web.config file located in the `C:\Inetpub\wwwroot` folder in the following tags:

```xml
<configuration>
  <appSettings>
    <add key="GetChat_debug" value="Y"/>
    <add key="GetChat_TechSupportEmpId" value="1-XXXXX"/>
    <add key="GetChat_TechSupportEmpLogin" value="TECH_USER"/>
    <add key="GetChat_SalesEmpId" value="1-XXXXX"/>
    <add key="GetChat_SalesEmpLogin" value="SALES_USER"/>
  </appSettings>
  <connectionStrings>
    <add name="ApplicationServices"
         connectionString="data source=.\SQLEXPRESS;Integrated Security=SSPI;AttachDBFilename=|DataDirectory|\aspnetdb.mdf;User Instance=true"
         providerName="System.Data.SqlClient" />
    <add name="hcidb" connectionString="server=<enter database info>;Min Pool Size=3;Max Pool Size=5;Connect Timeout=10;database=" providerName="System.Data.SqlClient"/>
  </connectionStrings>
</configuration>
```

### Configuration Item Descriptions

- **GetChat_debug**: The only way to enable debug mode. The value stored in here turns that mode on or off.
- **GetChat_SalesEmpId**: Used to specify the employee id for Sales department chats when the chat is an offline message
- **GetChat_SalesEmpLogin**: Used to specify the employee login for Sales department chats when the chat is an offline message
- **GetChat_TechSupportEmpId**: Used to specify the employee id for Technical Support department chats when the chat is an offline message
- **GetChat_TechSupportEmpLogin**: Used to specify the employee login for Technical Support department chats when the chat is an offline message
- **hcidb connectionString**: Used to specify the database connection string

## Scheduling

This service is not scheduled. It is invoked ad-hoc by other applications and web services.

## Monitoring

This service may not be monitored at this time.

## Logging

This service provides a "Debug" log, `GetChat.log` which is produced in the log folder (`C:\Logs` on the application servers), which is initiated when the Debug parameter is set to "Y".

### Debug Log Example

```
----------------------------------
Trace Log Started 4/18/2013 9:57:42 AM
Parameters-
  jsonString: {"kind": "Conversation", "tags": [], "items": [{"body": "I was in a train the trainer session 2 weeks ago and haven't heard anything yet. When will I know if I passed?", "timestamp": "1366289423.94", "kind": "MessageToOperator", "nickname": "Customer (City, State) #1234"}, {"body": "Please provide me with your name and I will look into it.", "timestamp": "1366289453.98", "kind": "MessageToVisitor", "nickname": "Support Agent", "operatorId": "637528"}, {"body": "Customer Name", "timestamp": "1366289462.17", "kind": "MessageToOperator", "nickname": "Customer (City, State) #1234"}, {"body": "Hi Sarah, You attended last week, so it should be processed any day.  You will receive email results as soon as your exam is processed.  Let me know if you have any questions.", "timestamp": "1366289713.49", "kind": "MessageToVisitor", "nickname": "Support Agent", "operatorId": "637528"}, {"body": "Thank you! Just anxious is all I guess.", "timestamp": "1366289762.73", "kind": "MessageToOperator", "nickname": "Customer (City, State) #1234"}, {"body": "Understandable, you're welcome! :)", "timestamp": "1366289798.62", "kind": "MessageToVisitor", "nickname": "Support Agent", "operatorId": "637528"}], "operators": {"637528": {"username": "kapturek", "emailAddress": "kapturek@gettips.com", "kind": "Operator", "nickname": "Support Agent", "id": "637528"}}, "groups": [{"kind": "Group", "id": "3e7a37bf205abdb7a37b7c7e65028742", "name": "Sales"}], "visitor": {"city": "Marathon", "kind": "Visitor", "conversationBeginPage": "<url>", "countryCode": "US", "referrer": "<url>", "ip": "72.43.34.174", "region": "NY", "operatingSystem": "Windows", "emailAddress": "", "country": "United States", "organization": "Road Runner", "fullName": "Customer (City, State) #1234", "id": "GyTX20O64YBMceDHZhKukzCW42653385", "browser": "Microsoft Internet Explorer 9.0"}, "id": "GBF6dT59iBeu8fdCpNQPzA6z42653385"}
  chat id: GBF6dT59iBeu8fdCpNQPzA6z42653385

 VISITOR: 

>fullName: Customer (City, State) #1234 
 internalCustomerId: 

 EMPLOYEES: 

0>emailAddress: operator@example.com 
 id: 637528 
 kind: Operator 
 nickname: Support Agent 
 username: operator1

 Group: Sales

 CHAT: 

Customer (City, State) #1234 (4/18/2013 7:50:23 AM) > "I was in a train the trainer session 2 weeks ago and haven't heard anything yet. When will I know if I passed?"

Support Agent (4/18/2013 7:50:53 AM) > "Please provide me with your name and I will look into it."

Customer (City, State) #1234 (4/18/2013 7:51:02 AM) > "Customer Name"

Support Agent (4/18/2013 7:55:13 AM) > "Hi Sarah, You attended last week, so it should be processed any day.  You will receive email results as soon as your exam is processed.  Let me know if you have any questions."

Customer (City, State) #1234 (4/18/2013 7:56:02 AM) > "Thank you! Just anxious is all I guess."

Support Agent (4/18/2013 7:56:38 AM) > "Understandable, you're welcome! :)"

 LOCATING VISITOR: 

 LOCATING EMPLOYEE: 

Employee Query: 
 SELECT C.ROW_ID, C.LOGIN FROM yourdb.dbo.S_EMPLOYEE C WHERE C.EMAIL_ADDR='operator@example.com'
  .. EmpId: 1-XXXXX
  .. EmpLogin: OPERATOR1

 GENERATING ACTIVITY: 

  .. ActivityId: 2JOBR7J9B61

 Activity Query: 
 INSERT INTO yourdb.dbo.S_EVT_ACT (ACTIVITY_UID,ALARM_FLAG,APPT_REPT_FLG,APPT_START_DT,ASGN_MANL_FLG,ASGN_USR_EXCLD_FLG,BEST_ACTION_FLG,BILLABLE_FLG,CAL_DISP_FLG,COMMENTS_LONG,CONFLICT_ID,COST_CURCY_CD,COST_EXCH_DT,CREATED,CREATED_BY,CREATOR_LOGIN,DCKING_NUM,DURATION_HRS,EMAIL_ATT_FLG,EMAIL_FORWARD_FLG,EMAIL_RECIP_ADDR,EVT_PRIORITY_CD,EVT_STAT_CD,LAST_UPD,LAST_UPD_BY,MODIFICATION_NUM,NAME,OWNER_LOGIN,OWNER_PER_ID, PCT_COMPLETE,PRIV_FLG,ROW_ID,ROW_STATUS,TARGET_OU_ID,TARGET_PER_ID,TEMPLATE_FLG,TMSHT_RLTD_FLG,TODO_CD,TODO_PLAN_START_DT, TODO_ACTL_END_DT,SRA_TYPE_CD,COMMENTS,RPLY_PH_NUM,X_DESC_TEXT) VALUES('2JOBR7J9B61','N','N','4/18/2013 7:56:38 AM','Y','Y','N','N','N','Customer (City, State) #1234 (4/18/2013 7:50:23 AM) > "I was in a train the trainer session 2 weeks ago and haven''t heard anything yet. When will I know if I passed?"

Support Agent (4/18/2013 7:50:53 AM) > "Please provide me with your name and I will look into it."

Customer (City, State) #1234 (4/18/2013 7:51:02 AM) > "Customer Name"

Support Agent (4/18/2013 7:55:13 AM) > "Hi Sarah, You attended last week, so it should be processed any day.  You will receive email results as soon as your exam is processed.  Let me know if you have any questions."

Customer (City, State) #1234 (4/18/2013 7:56:02 AM) > "Thank you! Just anxious is all I guess."

Support Agent (4/18/2013 7:56:38 AM) > "Understandable, you''re welcome! :)"

',0,'USD','4/18/2013 7:56:38 AM','4/18/2013 7:56:38 AM','0-1','ADMIN',0,0.00,'N','N','','2-High','Done', '4/18/2013 7:56:38 AM','0-1',0, 'Online chat with customer','OPERATOR1', '1-XXXXX',100,'N','2JOBR7J9B61','Y','','','N','N', 'Online Help', '4/18/2013 7:56:38 AM', '4/18/2013 7:56:38 AM','Tech Support - Web Site','Online chat with customer','GBF6dT59iBeu8fdCpNQPzA6z42653385','Customer (City, State) #1234 (4/18/2013 7:50:23 AM) > "I was in a train the trainer session 2 weeks ago and haven''t heard anything yet. When will I know if I passed?"

Support Agent (4/18/2013 7:50:53 AM) > "Please provide me with your name and I will look into it."

Customer (City, State) #1234 (4/18/2013 7:51:02 AM) > "Customer Name"

Support Agent (4/18/2013 7:55:13 AM) > "Hi Sarah, You attended last week, so it should be processed any day.  You will receive email results as soon as your exam is processed.  Let me know if you have any questions."

Customer (City, State) #1234 (4/18/2013 7:56:02 AM) > "Thank you! Just anxious is all I guess."

Support Agent (4/18/2013 7:56:38 AM) > "Understandable, you''re welcome! :)"

')
4/18/2013 9:57:42 AM, Results: at 4/18/2013 9:57:42 AM
Trace Log Ended 4/18/2013 9:57:42 AM
----------------------------------
```

If debug logging is disabled, transactions are logged to the SysLog server as in the following:

```
Results: True for chat id uZblVHLngNl20xUc9I3LX224DX2bKBWB, stored to activity id 9Q24EH05F9 at 12/18/2013 11:44:44 AM
```

Finally, the JSON string provided to this service itself is logged to the file `GetChat-JSON.log` in the same directory.

## Results

When this service is executed, it creates activity records in the Siebel database. There is no other results provided other than the log file.

## Deployment

The application is part of the "GetChat" web site, which is "published" using the Build>Publish Web Site option in Visual Studio to the `\\your-server\share` share (which is actually `C:\Inetpub\wwwroot` on those servers).

## Programming

The source code is available in the `GetChat.ashx.cs` file and includes comprehensive error handling, logging, and database integration.

## Operational

The following are operational notes:

### 5/2/13
Updated all activity records created so that `S_EVT_ACT.CREATED_BY` and `S_EVT_ACT.CREATOR_LOGIN` match `S_EVT_ACT.OWNER_PER_ID` and `S_EVT_ACT.OWNER_LOGIN`. This is so that activity records properly attribute activities. The following SQL was used for this purpose:

```sql
UPDATE yourdb.dbo.S_EVT_ACT
SET CREATED_BY=U.OWNER_PER_ID,CREATOR_LOGIN=U.OWNER_LOGIN 
FROM (
SELECT ROW_ID, OWNER_LOGIN, OWNER_PER_ID
FROM yourdb.dbo.S_EVT_ACT
WHERE COMMENTS = 'Online chat with customer'
) U
WHERE yourdb.dbo.S_EVT_ACT.ROW_ID=U.ROW_ID 

SELECT ROW_ID, OWNER_LOGIN, OWNER_PER_ID, CREATOR_LOGIN, CREATED_BY
FROM yourdb.dbo.S_EVT_ACT
WHERE COMMENTS = 'Online chat with customer'
```

### 4/18/17
Removed Olark from CMOpenSClass. Explore the idea "You would have to enable "Invisible Olark" at https://www.olark.com/customize/advanced, and then add a click to chat image as described at https://www.olark.com/help/addimage"

### 7/3/17
Discovered and corrected an issue when the right most character of a string contains an apostrophe. Wrote code to detect this circumstance and correct the SQL generated.

## License

This software is proprietary and confidential. Unauthorized copying, distribution, or modification is prohibited.
