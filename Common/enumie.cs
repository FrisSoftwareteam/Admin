namespace FirstReg
{
    #region enumie
    public enum Currency
    {
        NGN = 566, USD = 840
    }
    public enum PaymentGateway
    {
        Bank,
        PayStack
    }
    public enum UserType
    {
        Shareholder,
        StockBroker,
        CompanySec,
        FRAdmin,
        SystemAdmin
    }
    public enum ShareholderStatus
    {
        Pending,
        Approved
    }
    public enum ShareHoldingStatus
    {
        Pending,
        Verified
    }
    public enum ECertStatus
    {
        Pending,
        Downloaded,
        Completed
    }
    public enum PostType
    {
        blog, events
    }
    public enum SubscriptionType
    {
        IndividualShareholder,
        CorporateShareholder,
        StockBroker,
        MAccess,
        ENotifier,
        FirstCard
    }
    public enum PaymentType
    {
        Bank, Online, Credit
    }
    public enum PaymentStatus
    {
        successful, failed, pending, cancelled
    }
    public enum PaymentItem
    {
        Subscription,
        ShareSubscription
    }
    public enum CustomField
    {
        pay_item, years, accounts
    }
    public enum blobfolder
    {
        posts, avatars, reports, certs
    }
    public enum Roles
    {
        Shareholders,
        StockBrokers,
        Registers,
        FRAdmin,
        SysAdmin,
        UserManager,
        Blog,
        Events,
        AnnualReports,
        FAQs,
        FileManager,
        SupportTickets,
        Payments,
        ShareOffers,
        FormResponses,
        AuditLogs,
    }
    public enum LastIdField
    {
        RegisterHolding,
        ShareHolders
    }
    public enum ShareholderActivationIssue
    {
        Signature,
        ClearingNo
    }
    #endregion
}