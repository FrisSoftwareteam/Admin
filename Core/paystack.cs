using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace FirstReg.PayStack
{
    public class CustomField
    {
        public string display_name { get; set; }
        public string variable_name { get; set; }
        public string value { get; set; }
    }

    public class Metadata
    {
        public List<CustomField> custom_fields { get; set; }
        public string referrer { get; set; }
    }

    public class History
    {
        public string type { get; set; }
        public string message { get; set; }
        public int time { get; set; }
    }

    public class Log
    {
        public int start_time { get; set; }
        public int time_spent { get; set; }
        public int attempts { get; set; }
        public object authentication { get; set; }
        public int errors { get; set; }
        public bool success { get; set; }
        public bool mobile { get; set; }
        public List<object> input { get; set; }
        public List<History> history { get; set; }
    }

    public class Authorization
    {
        public string authorization_code { get; set; }
        public string bin { get; set; }
        public string last4 { get; set; }
        public string exp_month { get; set; }
        public string exp_year { get; set; }
        public string channel { get; set; }
        public string card_type { get; set; }
        public string bank { get; set; }
        public string country_code { get; set; }
        public string brand { get; set; }
        public bool reusable { get; set; }
        public string signature { get; set; }
    }

    public class Photo
    {
        public string type { get; set; }
        public string typeId { get; set; }
        public string typeName { get; set; }
        public string url { get; set; }
        public bool isPrimary { get; set; }
    }

    public class Metadata2
    {
        public List<Photo> photos { get; set; }
    }

    public class Customer
    {
        public int id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string customer_code { get; set; }
        public string phone { get; set; }
        public Metadata2 metadata { get; set; }
        public string risk_action { get; set; }
    }

    public class PlanObject
    {
    }

    public class Subaccount
    {
    }

    public class Data
    {
        public int id { get; set; }
        public string domain { get; set; }
        public string status { get; set; }
        public string reference { get; set; }
        public int amount { get; set; }
        public object message { get; set; }
        public string gateway_response { get; set; }
        public DateTime paid_at { get; set; }
        public DateTime created_at { get; set; }
        public string channel { get; set; }
        public string currency { get; set; }
        public string ip_address { get; set; }
        public Metadata metadata { get; set; }
        public Log log { get; set; }
        public int fees { get; set; }
        public object fees_split { get; set; }
        public Authorization authorization { get; set; }
        public Customer customer { get; set; }
        public object plan { get; set; }
        public DateTime paidAt { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime transaction_date { get; set; }
        public PlanObject plan_object { get; set; }
        public Subaccount subaccount { get; set; }

        public PaymentStatus TranxStatus =>
            status.Equals("success", StringComparison.CurrentCultureIgnoreCase) ? PaymentStatus.successful : PaymentStatus.failed;
        public bool Successful => TranxStatus == PaymentStatus.successful;
    }

    public class Response
    {
        public bool status { get; set; }
        public string message { get; set; }
        public Data data { get; set; }

        public string GetCustomData(FirstReg.CustomField field) =>
            data.metadata.custom_fields.First(x => x.variable_name == field.ToString()).value;
    }
}