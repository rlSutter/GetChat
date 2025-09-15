using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Web.Configuration;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using System.Text;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using log4net;

namespace GetChat
{
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

    public class GetChat : IHttpHandler
    {
         // Globals
        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

        public void ProcessRequest(HttpContext context)
        {
            // This web service stores a JSON artifact provided by Olark into a Siebel activity
            // The parameters are as follows:
            //      HttpContext         - the JSON string

            // ============================================
            // Declarations
            //  Generic   
            string mypath = "";
            string errmsg = "";
            string temp = "";

            //  Database 
            string ConnS = "";
            string SqlS = "";
            SqlConnection con = null;
            SqlCommand cmd = null;
            SqlDataReader dr = null;

            //  Logging
            FileStream fs = null;
            string logfile = "";
            string Logging = "";
            DateTime dt = DateTime.Now;
            string LogStartTime = dt.ToString();
            string Debug = "N";
            bool results = false;
            string VersionNum = "100";

            // Log4Net configuration
            string ltemp = "";
            log4net.Config.XmlConfigurator.Configure();
            log4net.ILog eventlog = log4net.LogManager.GetLogger("EventLog");
            log4net.ILog debuglog = log4net.LogManager.GetLogger("DebugLog");   

            // Web Service 
            com.certegrity.cloudsvc.Service wsvcs = new com.certegrity.cloudsvc.Service();

            // Value array
            string[] Visitor = new string[20];
            string[,] Employee = new string[5,5];
            string[,] ChatItem = new string[500,5];
            int ctr = 0;

            // Activity variables
            string ActivityId = "";
            string EmpId = "";
            string EmpLogin = "";
            string DeptName = "";
            string OuId = "";
            string ContactId = "";
            DateTime timestamp = DateTime.Now;
            string StatusCd = "";

            // JSON
            var myChat = new Data();
            string textChat = "";

            // String variables
            string ts = "";
            int ii = 10;
            char crtn = (char)ii;
            ii = 13;
            char lfeed = (char)ii;
            string crlf = lfeed.ToString() + crtn.ToString();

            // ============================================
            // Debug Setup
            mypath = HttpRuntime.AppDomainAppPath;
            Logging = "Y";
            try
            {
                temp = WebConfigurationManager.AppSettings["GetChat_debug"];
                Debug = temp;
            }
            catch { }

            // ============================================
            // Get system defaults
            ConnectionStringSettings connSettings = ConfigurationManager.ConnectionStrings["hcidb"];
            if (connSettings != null)
            {
                ConnS = connSettings.ConnectionString;
            }
            if (ConnS == "")
            {
                ConnS = "server=HCIDBSQL\\HCIDB;uid=SIEBEL;pwd=SIEBEL;database=siebeldb";
            }

            // ============================================
            // Get the JSON object
            JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
            string jsonString = String.Empty;
            HttpContext.Current.Request.InputStream.Position = 0;
            using (StreamReader inputStream = new StreamReader(HttpContext.Current.Request.InputStream))
            {
                jsonString = inputStream.ReadToEnd();
            }

            // Check to see if object is encoded
            if (jsonString.IndexOf("%7B%") > 0)
            {
                jsonString = System.Web.HttpContext.Current.Server.UrlDecode(jsonString);
                jsonString = jsonString.Replace("data=", "");
            }
            if (jsonString.Length == 0)
            {
                goto CloseLog;
            }


            // ============================================
            // Store JSON in a separate file
            try
            {
                string Jsonfile = "C:\\Logs\\GetChat-JSON.log";
                if (File.Exists(Jsonfile))
                {
                    fs = new FileStream(Jsonfile, FileMode.Append, FileAccess.Write, FileShare.Write);
                }
                else
                {
                    fs = new FileStream(Jsonfile, FileMode.CreateNew, FileAccess.Write, FileShare.Write);
                }
                writeoutputfs(ref fs, jsonString);
                fs.Flush();
                fs.Close();
                fs.Dispose();
            }
            catch (Exception e)
            {
                errmsg = errmsg + "Error opening JSON log: " + e.ToString();
            }

            // ============================================
            // Deserialize the string into objects
            try
            {
                myChat = JsonConvert.DeserializeObject<Data>(jsonString);
            }
            catch (Exception e)
            {
                errmsg = errmsg + ", " + e.ToString();
                goto CloseLog;
            }

            // ============================================
            // Open log file if applicable
            if ((Logging == "Y" & Debug != "T") | Debug == "Y")
            {
                logfile = "C:\\Logs\\GetChat.log";
                try
                {
                    log4net.GlobalContext.Properties["LogFileName"] = logfile;
                    log4net.Config.XmlConfigurator.Configure();
                }
                catch (Exception e)
                {
                    errmsg = errmsg + "Error opening debug Log: " + e.ToString();
                }

                if (Debug == "Y" & errmsg == "")
                {
                    debuglog.Debug("----------------------------------");
                    debuglog.Debug("Trace Log Started " + LogStartTime);
                    debuglog.Debug("Parameters-");
                    debuglog.Debug("  jsonString: " + jsonString);
                    if (myChat.id != null)
                    {
                        debuglog.Debug("  chat id: " + myChat.id.ToString());
                    }
                    debuglog.Debug(" ");
                }
            }

            // ============================================
            // Extract visitor information
            if (Debug == "Y")
            {
                debuglog.Debug("\r\n VISITOR: \r\n");
            }
            try
            {
                if (myChat.visitor.browser != null)
                {
                    Visitor[0] = myChat.visitor.browser;
                }
                if (myChat.visitor.city != null)
                {
                    Visitor[1] = myChat.visitor.city;
                }
                if (myChat.visitor.country != null)
                {
                    Visitor[2] = myChat.visitor.country;
                }
                if (myChat.visitor.countryCode != null)
                {
                    Visitor[3] = myChat.visitor.countryCode;
                }
                if (myChat.visitor.emailAddress != null)
                {
                    Visitor[4] = myChat.visitor.emailAddress;
                }
                if (myChat.visitor.fullName != null)
                {
                    Visitor[5] = myChat.visitor.fullName;
                }
                if (myChat.visitor.id != null)
                {
                    Visitor[6] = myChat.visitor.id;
                }
                if (myChat.visitor.ip != null)
                {
                    Visitor[7] = myChat.visitor.ip;
                }
                if (myChat.visitor.kind != null)
                {
                    Visitor[8] = myChat.visitor.kind;
                }
                if (myChat.visitor.operatingSystem != null)
                {
                    Visitor[9] = myChat.visitor.operatingSystem;
                }
                if (myChat.visitor.organization != null)
                {
                    Visitor[10] = myChat.visitor.organization;
                }
                if (myChat.visitor.referrer != null)
                {
                    Visitor[11] = myChat.visitor.referrer;
                }
                if (myChat.visitor.region != null)
                {
                    Visitor[12] = myChat.visitor.region;
                }
                if (myChat.visitor.customFields != null)
                {
                    Visitor[13] = myChat.visitor.customFields.internalCustomerId;
                }
                if (myChat.visitor.phoneNumber != null)
                {
                    Visitor[14] = myChat.visitor.phoneNumber;
                }

                if (Debug == "Y")
                {
                    debuglog.Debug(">fullName: " + Visitor[5] + " \r\n internalCustomerId: " + Visitor[13]);
                }
            }
            catch (Exception e)
            {
                // If there are no chat-items, don't bother saving
                errmsg = errmsg + ", " + e.ToString();
            }

            // ============================================
            // Extract employee (operators) information
            //  Assume there is one operator
            if (Debug == "Y")
            {
                debuglog.Debug("\r\n EMPLOYEES: \r\n");
            }
            try
            {
                Employee[0, 0] = "";
                Employee[0, 1] = "";
                Employee[0, 2] = "";
                Employee[0, 3] = "";
                Employee[0, 4] = "";
                ctr = 0;

                foreach (var kvp in myChat.operators)
                {
                    if (kvp.Value.emailAddress != null)
                    {
                        Employee[ctr, 0] = kvp.Value.emailAddress.ToString();
                    }
                    if (kvp.Value.id != null)
                    {
                        Employee[ctr, 1] = kvp.Value.id.ToString();
                    }
                    if (kvp.Value.kind != null)
                    {
                        Employee[ctr, 2] = kvp.Value.kind.ToString();
                    }
                    if (kvp.Value.nickname != null)
                    {
                        Employee[ctr, 3] = kvp.Value.nickname.ToString();
                    }
                    if (kvp.Value.username != null)
                    {
                        Employee[ctr, 4] = kvp.Value.username.ToString();
                    }
                    if (Debug == "Y")
                    {
                        debuglog.Debug(ctr.ToString() + ">emailAddress: " + Employee[ctr, 0] + " \r\n id: " + Employee[ctr, 1] + " \r\n kind: " + Employee[ctr, 2] + " \r\n nickname: " + Employee[ctr, 3] + " \r\n username: " + Employee[ctr, 4]);
                    }
                    ctr = ctr + 1;
                }
            }
            catch (Exception e)
            {
                errmsg = errmsg + ", " + e.ToString();                
            }

            // ============================================
            // Extract department name
            try {
                foreach (Groups group in myChat.groups)
                {
                    if (group.name != null)
                    {
                        DeptName = group.name;
                    }
                    if (Debug == "Y") { debuglog.Debug("\r\n Group: " + DeptName); }
                }
            }
            catch (Exception e)
            {
                errmsg = errmsg + ", " + e.ToString();
            }

            // ============================================
            // Extract chat item information
            //  Each item is a line of conversation
            ctr = 0;
            if (Debug == "Y")
            {
                debuglog.Debug("\r\n CHAT: \r\n");
            }
            try
            {
                foreach (Item chatline in myChat.items)
                {
                    if (chatline.body != null) { ChatItem[ctr, 0] = chatline.body.ToString(); }
                    else { ChatItem[ctr, 0] = ""; }

                    if (chatline.kind != null) { ChatItem[ctr, 1] = chatline.kind.ToString(); }
                    else { ChatItem[ctr, 1] = ""; }

                    if (chatline.nickname != null) { ChatItem[ctr, 2] = chatline.nickname.ToString(); }
                    else { ChatItem[ctr, 2] = ""; }

                    if (chatline.operatorId != null) { ChatItem[ctr, 3] = chatline.operatorId.ToString(); }
                    else { ChatItem[ctr, 3] = ""; }

                    if (chatline.timestamp != null) { ChatItem[ctr, 4] = chatline.timestamp.ToString(); }
                    else { ChatItem[ctr, 4] = ""; }
                    ctr = ctr + 1;
                }
            }
            catch (Exception e)
            {
                // If there are no chat-items, don't bother saving
                errmsg = errmsg + ", " + e.ToString();
                goto CloseLog;
            }

            // ============================================
            // Create conversation string
            //  Append all of the individual text lines together into a single chat string
            if (ctr > 0) {
                string EmpName = Employee[0,3];
                if (EmpName == "") { EmpName = "Employee"; }
                for (int i=0; i<ctr; i++) {
                    ts = "";
                    try {
                        timestamp = ConvertFromUnixTimestamp(System.Convert.ToDouble(ChatItem[i,4]));
                        ts = " (" + timestamp.ToString() + ")";
                    }
                    catch (Exception e2)
                    {
                    }
                    if (ChatItem[i, 1] == "MessageToOperator" || ChatItem[i, 1] == "OfflineMessage")
                    {
                        textChat = textChat + Visitor[5] + ts + " > \"" + ChatItem[i,0] + "\"" + crlf + crlf;
                    }
                    else 
                    {
                        textChat = textChat + EmpName + ts + " > \"" + ChatItem[i, 0] + "\"" + crlf + crlf;
                    }                
                }
            }

            if (Debug == "Y")
            {
                debuglog.Debug(textChat);
            }
            if (textChat.Length < 1) { goto CloseLog; }

            // ============================================
            // Open database connections
            try
            {
                errmsg = OpenDBConnection(ref ConnS, ref con, ref cmd);
            }
            catch (Exception e)
            {
                errmsg = errmsg + ", " + e.ToString();
                goto CloseLog;
            }

            // ============================================
            // Locate the customer id if possible from the supplied registration id
            //  If no id supplied, then do not try
            if (Debug == "Y")
            {
                debuglog.Debug("\r\n LOCATING VISITOR: \r\n");
            }
            if (Visitor[13] != null && Visitor[13] != "")
            {
                SqlS = "SELECT PR_DEPT_OU_ID, ROW_ID " +
                    "FROM siebeldb.dbo.S_CONTACT " +
                    "WHERE X_REGISTRATION_NUM='" + Visitor[13] + "'";
                if (Debug == "Y") { debuglog.Debug("Visitor Query: \r\n " + SqlS); }
                try
                {
                    cmd = new SqlCommand(SqlS, con);
                    cmd.CommandType = System.Data.CommandType.Text;
                    dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            if (dr[0] == DBNull.Value) { OuId = ""; } else { OuId = dr[0].ToString(); }
                            if (dr[1] == DBNull.Value) { ContactId = ""; } else { ContactId = dr[1].ToString(); }
                        }
                    }
                    else
                    {
                        errmsg = "No contact found";
                    }
                    dr.Close();
                }
                catch (Exception e)
                {
                    if (Debug == "Y") { debuglog.Debug("Error: " + e.ToString()); }
                    errmsg = errmsg + "\r\nError: " + e.ToString();
                    goto CloseDB;
                }
                if (EmpId == "") { EmpId = ""; }
                if (Debug == "Y")
                {
                    debuglog.Debug("  .. ContactId: " + ContactId);
                    debuglog.Debug("  .. OuId: " + OuId + "\r\n");
                }
            }

            // ============================================
            // Locate the employee id from the operator information

            // If this is an offline message - set the status of this as an open activity and send to Nick
            if (ChatItem[0, 1] == "OfflineMessage") {
                StatusCd = "Not Started";
                if (DeptName == "Sales")
                {
                    EmpId = WebConfigurationManager.AppSettings["GetChat_SalesEmpId"];
                    if (EmpId == "") { EmpId = "1-44LQB"; }
                    EmpLogin = WebConfigurationManager.AppSettings["GetChat_SalesEmpLogin"];
                    if (EmpLogin == "") { EmpLogin = "ESTELLET"; }
                }
                else
                {
                    EmpId = WebConfigurationManager.AppSettings["GetChat_TechSupportEmpId"];
                    if (EmpId == "") { EmpId = "1-EMN4X"; }
                    EmpLogin = WebConfigurationManager.AppSettings["GetChat_TechSupportEmpLogin"];
                    if (EmpLogin == "") { EmpLogin = "TECHNICAL SUPPORT"; }
                }
                if (Debug == "Y") { debuglog.Debug("Offline Message \r\n EmpId: " + EmpId + "\r\n EmpLogin: "+ EmpLogin + "\r\n"); }

                // Add contact information to the body of the message
                textChat = textChat + "CONTACT INFORMATION:" + crlf;
                textChat = textChat + " Fullname: " + Visitor[5] + crlf;
                textChat = textChat + " Phone: " + Visitor[14] + crlf;
                textChat = textChat + " Email: " + Visitor[4] + crlf;
                textChat = textChat + " Country " + Visitor[2] + crlf;
                goto GenActivity;
            }

            // If this is otherwise, then go into the message
            //  -Default employee id is "Technical Support" or "Sales" in case none is found
            //  -All messages left will be tagged to this employee
            StatusCd = "Done";
            if (Debug == "Y")
            {
                debuglog.Debug("\r\n LOCATING EMPLOYEE: \r\n");
            }
            string emailAddress = "techsupport@gettips.com";
            if (Employee[0, 0].IndexOf("@")>0) { emailAddress = Employee[0, 0]; }
            if (Employee[0, 1].IndexOf("@") > 0) { emailAddress = Employee[0, 1]; }
            if (Employee[0, 2].IndexOf("@") > 0) { emailAddress = Employee[0, 2]; }
            if (Employee[0, 3].IndexOf("@") > 0) { emailAddress = Employee[0, 3]; }
            if (Employee[0, 4].IndexOf("@") > 0) { emailAddress = Employee[0, 4]; }
            if (emailAddress != "")
            {
                SqlS = "SELECT C.ROW_ID, C.LOGIN " +
                       "FROM siebeldb.dbo.S_EMPLOYEE C " +
                       "WHERE C.EMAIL_ADDR='" + emailAddress + "'";
                if (Debug == "Y") { debuglog.Debug("Employee Query: \r\n " + SqlS); }
                try
                {
                    cmd = new SqlCommand(SqlS, con);
                    cmd.CommandType = System.Data.CommandType.Text;
                    dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            if (dr[0] == DBNull.Value) {
                                if (DeptName == "Sales") { EmpId = "1-EVRKX"; } else { EmpId = "1-EMN4X"; }
                            } else { EmpId = dr[0].ToString(); }
                            if (dr[1] == DBNull.Value) {
                                if (DeptName == "Sales") { EmpLogin = "SALES"; } else { EmpLogin = "TECHNICAL SUPPORT"; } 
                            } else { EmpLogin = dr[1].ToString(); }
                        }
                    }
                    else
                    {
                        errmsg = "No employee found";
                    }
                    dr.Close();
                }
                catch (Exception e)
                {
                    if (Debug == "Y") { debuglog.Debug("Error: " + e.ToString()); }
                    errmsg = errmsg + "\r\nError: " + e.ToString();
                    goto CloseDB;
                }
                if (EmpId == "") {
                    if (DeptName == "Sales") { 
                        EmpId = "1-EVRKX";
                        EmpLogin = "SALES";
                    } else { 
                        EmpId = "1-EMN4X";
                        EmpLogin = "TECHNICAL SUPPORT";
                    }
                }
                if (Debug == "Y")
                {
                    debuglog.Debug("  .. EmpId: " + EmpId);
                    debuglog.Debug("  .. EmpLogin: " + EmpLogin + "\r\n");
                }
            }

GenActivity:
            // ============================================
            // Create activity record in Siebel for this chat
            if (Debug == "Y")
            {
                debuglog.Debug("\r\n GENERATING ACTIVITY: \r\n");
            }
            ActivityId = wsvcs.GenerateRecordId("S_EVT_ACT", "N", Debug);
            if (Debug == "Y")
            {
                debuglog.Debug("  .. ActivityId: " + ActivityId);
            }
            string logtime = "GETDATE()";
            if (timestamp.ToString() != null && timestamp.ToString() != "") { logtime = "'" + timestamp.ToString() + "'"; }
            string COMMENTS_LONG = textChat.Replace("'", "''");
            COMMENTS_LONG = COMMENTS_LONG.Replace("  ", " ");
            if (COMMENTS_LONG.Length > 1480) {
                    COMMENTS_LONG = COMMENTS_LONG.Substring(0, 1480);
                    if (COMMENTS_LONG.Substring(COMMENTS_LONG.Length - 1, 1) == "'" && COMMENTS_LONG.Substring(COMMENTS_LONG.Length - 2, 1) != "'") { COMMENTS_LONG = COMMENTS_LONG + "'"; }
                }
            SqlS = "INSERT INTO siebeldb.dbo.S_EVT_ACT " +
                "(ACTIVITY_UID,ALARM_FLAG,APPT_REPT_FLG,APPT_START_DT,ASGN_MANL_FLG,ASGN_USR_EXCLD_FLG,BEST_ACTION_FLG,BILLABLE_FLG,CAL_DISP_FLG," +
                "COMMENTS_LONG,CONFLICT_ID,COST_CURCY_CD,COST_EXCH_DT,CREATED,CREATED_BY,CREATOR_LOGIN,DCKING_NUM,DURATION_HRS,EMAIL_ATT_FLG," +
                "EMAIL_FORWARD_FLG,EMAIL_RECIP_ADDR,EVT_PRIORITY_CD,EVT_STAT_CD,LAST_UPD,LAST_UPD_BY,MODIFICATION_NUM,NAME,OWNER_LOGIN,OWNER_PER_ID," +
                " PCT_COMPLETE,PRIV_FLG,ROW_ID,ROW_STATUS,TARGET_OU_ID,TARGET_PER_ID,TEMPLATE_FLG,TMSHT_RLTD_FLG,TODO_CD,TODO_PLAN_START_DT, TODO_ACTL_END_DT," +
                "SRA_TYPE_CD,COMMENTS,RPLY_PH_NUM,X_DESC_TEXT) " +
                "VALUES('" + ActivityId + "','N','N'," + logtime + ",'Y','Y','N','N','N'," +
                "'" + COMMENTS_LONG + "',0,'USD'," + logtime + "," + logtime + ",'" + EmpId + "','" + EmpLogin + "',0,0.00,'N'," +
                "'N','" + Visitor[4] + "','2-High','" + StatusCd + "', " + logtime + ",'" + EmpId + "',0, 'Online chat with customer','" + EmpLogin + "', '" + EmpId + "'," +
                "100,'N','" + ActivityId + "','Y','" + OuId + "','" + ContactId + "','N','N', 'Online Help', " + logtime + ", " + logtime + "," +
                "'Tech Support - Web Site','Online chat with customer','" + myChat.id.ToString() + "','" + textChat.Replace("'", "''") + "')";
            if (Debug == "Y") { debuglog.Debug("\r\n Activity Query: \r\n " + SqlS); }
            try
            {
                cmd = new SqlCommand(SqlS, con);
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
                results = true;
            }
            catch (Exception e)
            {
                if (Debug == "Y") { debuglog.Debug("Error: " + e.ToString()); }
                errmsg = errmsg + "\r\nError: " + e.ToString();
            }

CloseDB:
            // ============================================
            // Close database connections and objects
            try
                {
                    CloseDBConnection(ref con, ref cmd, ref dr);
                }
            catch (Exception e)
                {
                    errmsg = errmsg + ", " + e.ToString();
                }

CloseLog:
            // ============================================
            // Close the log file, if any
            try
            {
                ltemp = String.Format("{0:d/M/yyyy HH:mm:ss}", dt) + ": Results: " + results.ToString() + " for chat id " + myChat.id.ToString() + ", stored to activity id " + ActivityId + " and contact id " + ContactId;
                eventlog.Info("GetChat : " + ltemp);
            }
            catch (Exception e)
            {
                errmsg = errmsg + ", " + e.ToString();
            }

            if (errmsg != "" && errmsg != "No error") { eventlog.Error("GetChat : Error" + errmsg); }
            try
            {
                if ((Logging == "Y" & Debug != "T") | Debug == "Y")
                {
                    DateTime et = DateTime.Now;
                    if (errmsg != "") { debuglog.Debug("\r\n Error: " + errmsg); }
                    if (Logging == "Y")
                    {
                        debuglog.Debug(ltemp);
                    }
                    if (Debug == "Y")
                    {
                        debuglog.Debug("Trace Log Ended " + et.ToString());
                        debuglog.Debug("----------------------------------");
                    }
                }
            }
            catch { }

            // ============================================
            // Release Objects
            try
            {
                fs.Flush();
                fs.Close();
                fs.Dispose();
                fs = null;
                myChat = null;
                jsonSerializer = null;
                Visitor = null;
                Employee = null;
                ChatItem = null;
                GC.Collect();
            }
            catch { }

            // ============================================
            // Log Performance Data
            try
            {
                String MyMachine = System.Environment.MachineName.ToString();
                wsvcs.LogPerformanceData2Async(MyMachine, "GETCHAT", LogStartTime, VersionNum, Debug);
            }
            catch
            {
            }

        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        // ============================================
        // DATABASE FUNCTIONS
        // Open Database Connection
        private string OpenDBConnection(ref string ConnS, ref SqlConnection con, ref SqlCommand cmd)
        {
            string SqlS = "";
            string result = "";
            try
            {
                con = new SqlConnection(ConnS);
                con.Open();
                if (con != null)
                {
                    try
                    {
                        cmd = new SqlCommand(SqlS, con);
                        cmd.CommandTimeout = 300;
                    }
                    catch (Exception ex2) { result = "Open error: " + ex2.ToString(); }
                }
            }
            catch
            {
                if (con.State != System.Data.ConnectionState.Closed) { con.Dispose(); }
                ConnS = ConnS + ";Pooling=false";
                try
                {
                    con = new SqlConnection(ConnS);
                    con.Open();
                    if (con != null)
                    {
                        try
                        {
                            cmd = new SqlCommand(SqlS, con);
                            cmd.CommandTimeout = 300;
                        }
                        catch (Exception ex2)
                        {
                            result = "Open error: " + ex2.ToString();
                        }
                    }
                }
                catch (Exception ex2)
                {
                    result = "Open error: " + ex2.ToString();
                }
            }
            return result;
        }

        // Close Database Connection
        private void CloseDBConnection(ref SqlConnection con, ref SqlCommand cmd, ref SqlDataReader dr)
        {
            // This function closes a database connection safely

            // Handle datareader
            try
            {
                dr.Close();
            }
            catch { }

            try
            {
                dr = null;
            }
            catch { }


            // Handle command
            try
            {
                cmd.Dispose();
            }
            catch { }

            try
            {
                cmd = null;
            }
            catch { }


            // Handle connection
            try
            {
                con.Close();
            }
            catch { }

            try
            {
                SqlConnection.ClearPool(con);
            }
            catch { }

            try
            {
                con.Dispose();
            }
            catch { }

            try
            {
                con = null;
            }
            catch { }
        }

         // ============================================
        // OTHER FUNCTIONS
        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            timestamp = timestamp - 18000;
            return origin.AddSeconds(timestamp); 
        }

        // ============================================
        // DEBUG FUNCTIONS
        private bool writeoutputfs(ref FileStream fs, String instring)
        {
            // This function writes a line to a previously opened filestream, and then flushes it
            // promptly.  This assists in debugging services
            Boolean result;
            try
            {
                instring = instring + "\r\n";
                //byte[] bytesStream = new byte[instring.Length];
                Byte[] bytes = encoding.GetBytes(instring);
                fs.Write(bytes, 0, bytes.Length);
                result = true;
            }
            catch
            {
                result = false;
            }
            fs.Flush();
            return result;
        }


    }
}