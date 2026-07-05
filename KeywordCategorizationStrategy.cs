using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Application.Categorization;

public class KeywordCategorizationStrategy : ICategorizationStrategy
{
    public int Priority => 10;

    private static readonly Dictionary<TransactionCategory, string[]> Keywords = new()
    {
        [TransactionCategory.Food] = ["grocery", "supermarket", "restaurant", "cafe", "coffee", "burger", "pizza",
            "food", "spar", "checkers", "woolworths food", "pick n pay", "hungry", "mcdonalds", "kfc", "nandos",
            "steers", "debonairs", "fishaways", "ocean basket", "eat", "dining"],
        [TransactionCategory.Transport] = ["uber", "bolt", "taxi", "fuel", "petrol", "garage", "parking", "toll",
            "bus", "train", "metrorail", "transport", "cab", "lyft", "engen", "caltex", "bp pump", "shell", "sasol"],
        [TransactionCategory.Entertainment] = ["netflix", "dstv", "showmax", "spotify", "cinema", "movie", "theatre",
            "gaming", "steam", "playstation", "xbox", "apple music", "youtube", "twitch", "concert", "event"],
        [TransactionCategory.Healthcare] = ["pharmacy", "doctor", "hospital", "clinic", "dentist", "optometrist",
            "medical", "health", "clicks pharmacy", "dis-chem", "mediclinic", "netcare", "medihelp", "discovery health"],
        [TransactionCategory.Shopping] = ["woolworths", "mr price", "h&m", "zara", "edgars", "foschini", "ackermans",
            "pep", "truworths", "jet", "clothing", "store", "mall", "amazon", "takealot", "game stores", "makro"],
        [TransactionCategory.Utilities] = ["eskom", "electricity", "water", "municipality", "vodacom", "mtn", "cell c",
            "telkom", "afrihost", "vumatel", "internet", "prepaid", "airtime", "data"],
        [TransactionCategory.Travel] = ["airbnb", "hotel", "kulula", "comair", "flysafair", "british airways",
            "airlines", "flight", "airport", "travel", "booking.com", "trivago", "airlink"],
        [TransactionCategory.Education] = ["university", "college", "school", "tuition", "course", "udemy",
            "coursera", "nsfas", "study", "education", "bookshop", "stationery"],
        [TransactionCategory.Salary] = ["salary", "payroll", "wage", "remuneration", "payslip"],
        [TransactionCategory.Transfer] = ["transfer", "send money", "payment to", "eft", "instant pay"],
        [TransactionCategory.Fees] = ["service fee", "bank charge", "fee", "commission", "penalty", "fine", "charge"],
        [TransactionCategory.Insurance] = ["insurance", "assurance", "cover", "premium", "old mutual",
            "sanlam", "momentum", "liberty", "discovery life"],
        [TransactionCategory.Housing] = ["rent", "mortgage", "bond", "levy", "rates", "property", "hoa",
            "estate", "maintenance", "plumber", "electrician", "painter"],
    };


    public bool TryCategorize(string description, string merchantName, decimal amount, out TransactionCategory category)
    {
        var combined = $"{description} {merchantName}".ToLowerInvariant();
        foreach (var (cat, keywords) in Keywords)
        {
            if (keywords.Any(k => combined.Contains(k)))
            {
                category = cat;
                return true;
            }
        }
        category = TransactionCategory.Uncategorized;
        return false;
    }
}
