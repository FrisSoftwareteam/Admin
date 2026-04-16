namespace FirstReg.OnlineAccess.Controllers
{
    public static class Routes
    {
        // root
        public const string Login = "login";

        //forms
        public const string Forms = "forms";
        public const string DataUpdate = "data-update";
        public const string ChangeOfAddress = "change-of-address";
        public const string DividendCard = "dividend-card";
        public const string EDividend = "e-dividend";
        public const string ShareholderUpdate = "shareholder-update";
        public const string ShareholderUpdateSearch = "shareholder-update/search";
        public const string ShareholderUpdateForm = "shareholder-update/form/{account}";
        public const string Completed = "completed";

        public const string ShareOffer = "share-offer";
        public const string ShareOfferHome = "{code?}";
        public const string PublicOffer = "{code}/po";
        public const string RightIssue = "{code}/rights/{acc?}";
        public const string ShareOfferSubscription = "subscription/{code?}";
        public const string FindShareOfferSubscription = "subscription/find";
        public const string PayShareOffer = "pay/{code}";
    }
}